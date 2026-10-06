// PadChat per-user installation. No elevated task, driver, network or game control.
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;
using File=LongFile;
using Directory=LongDirectory;

class InstallState { public string Game,Version; public string[] Games; public bool Voice,Startup; }
class Engine {
  public const string Version="0.3.0-preview.1";
 public readonly string Root; public readonly bool Test;
 static readonly JavaScriptSerializer Json=new JavaScriptSerializer();
 public Engine(string root,bool test){Root=Path.GetFullPath(root);Test=test;}
 public string StatePath{get{return Path.Combine(Root,"install.json");}}
 public InstallState ReadState(){return File.Exists(StatePath)?Json.Deserialize<InstallState>(File.ReadAllText(StatePath)):null;}
 public static string SafeChild(string root,string relative){
  if(string.IsNullOrEmpty(relative)||Path.IsPathRooted(relative))throw new IOException("Invalid package path");
  string basePath=Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
  string full=Path.GetFullPath(Path.Combine(basePath,relative));
  if(!full.StartsWith(basePath,StringComparison.OrdinalIgnoreCase))throw new IOException("Package path escapes its folder");
  return full;
 }
 public static void AssertNoLinks(string path){
  var d=new DirectoryInfo(LongPath.IO(path));
  while(d!=null){if(d.Exists&&(d.Attributes&FileAttributes.ReparsePoint)!=0)throw new IOException("Please choose a folder without junctions or symbolic links.");d=d.Parent;}
 }
 public void ValidateGame(string game){
  game=Path.GetFullPath(game);AssertNoLinks(game);
  string exe=WoWClients.FindExecutable(game);
  if(exe==null||!Directory.Exists(Path.Combine(game,"Data")))throw new IOException("Select a WoW client folder containing its Wow executable and Data, such as _retail_ or _classic_era_.");
  if(!Test){
   var v=FileVersionInfo.GetVersionInfo(exe);
   if(!SupportedVersion(v.FileVersion))throw new IOException("This preview targets current Retail (12.x), Era (1.15.x), Anniversary (2.5.x), progression Classic (5.5.x) and Forever (1.60.x). Older or unknown clients need a separate compatibility check.");
   if(WoWClients.AnyRunning())throw new IOException("Close all WoW clients before installing or updating PadChat.");
  }
  AssertNoLinks(Path.Combine(game,"Interface","AddOns","PadChat"));
 }
 public static bool SupportedVersion(string value){return WoWClients.SupportsVersion(value);}
 static string[] GameFolders(InstallState s){return s==null?new string[0]:s.Games??new[]{s.Game};}
 static void CopyTree(string from,string to){
  Directory.CreateDirectory(to);
  foreach(string file in Directory.GetFiles(from)){File.Copy(file,Path.Combine(to,Path.GetFileName(file)),true);}
  foreach(string dir in Directory.GetDirectories(from)){if((File.GetAttributes(dir)&FileAttributes.ReparsePoint)!=0)throw new IOException("Unexpected linked folder");CopyTree(dir,Path.Combine(to,Path.GetFileName(dir)));}
 }
 static void DeleteOwnedTree(string path){
  AssertNoLinks(path);
  if(!Directory.Exists(path))return;
  // Refuse linked descendants before any recursive removal.
  foreach(string dir in Directory.GetDirectories(path,"*",SearchOption.AllDirectories))AssertNoLinks(dir);
  Directory.Delete(path,true);
 }
 static string Hash(string file){using(var h=SHA256.Create())using(var s=File.OpenRead(file)){return BitConverter.ToString(h.ComputeHash(s)).Replace("-","").ToLowerInvariant();}}
 public static void Extract(Stream payload,string target){
  using(var zip=new ZipArchive(payload,ZipArchiveMode.Read)){
   foreach(var entry in zip.Entries){string dest=SafeChild(target,entry.FullName);if(entry.FullName.EndsWith("/")){Directory.CreateDirectory(dest);continue;}
    Directory.CreateDirectory(Path.GetDirectoryName(dest));using(var input=entry.Open())using(var output=File.Create(dest))input.CopyTo(output);
   }
  }
  string mf=SafeChild(target,"manifest.json");
  var expected=Json.Deserialize<Dictionary<string,string>>(File.ReadAllText(mf));
  var files=Directory.GetFiles(target,"*",SearchOption.AllDirectories);
  if(files.Length!=expected.Count+1)throw new IOException("Package contains unexpected files");
  foreach(var item in expected){string file=SafeChild(target,item.Key);if(!File.Exists(file)||Hash(file)!=item.Value)throw new IOException("Package integrity failed: "+item.Key);}
 }
 public void StopHelper(){
  if(Test)return;
  string helper=Path.Combine(Root,"App","PadChatVoice.exe");
  if(File.Exists(helper)){var p=Process.Start(new ProcessStartInfo(helper,"--stop"){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden});if(!p.WaitForExit(5000))throw new IOException("PadChat voice did not respond. Exit it from the tray before continuing.");p.Dispose();}
  // A matching installed process must finish closing before its DLLs move.
  for(int n=0;n<60;n++){
   bool running=false;
   foreach(var p in Process.GetProcessesByName("PadChatVoice")){try{running|=string.Equals(p.MainModule.FileName,helper,StringComparison.OrdinalIgnoreCase);}catch{}finally{p.Dispose();}}
   if(!running)return;System.Threading.Thread.Sleep(100);
  }
  throw new IOException("PadChat is still closing. Try again in a few seconds.");
 }
 public void Install(string game,bool voice,bool startup,string payloadPath,string uninstallerPath,bool failAfterSwap){
  game=Path.GetFullPath(game);ValidateGame(game);AssertNoLinks(Root);Directory.CreateDirectory(Root);
  var previous=ReadState();
  string work=Path.Combine(Root,"stage-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(work);
  string addon=Path.Combine(game,"Interface","AddOns","PadChat"), app=Path.Combine(Root,"App");
  string addonNew=addon+".padchat-new", addonOld=addon+".padchat-backup", appNew=app+".new", appOld=app+".backup";
  bool movedAddon=false,movedApp=false,hadAddon=Directory.Exists(addon),hadApp=Directory.Exists(app);
  string oldState=File.Exists(StatePath)?File.ReadAllText(StatePath):null;
  try{
   using(var p=File.OpenRead(payloadPath))Extract(p,work);
   foreach(var p in new[]{addonNew,addonOld,appNew,appOld}){AssertNoLinks(p);if(Directory.Exists(p))throw new IOException("A previous installation left a staging folder: "+p+". Review it before retrying.");}
   Directory.CreateDirectory(Path.GetDirectoryName(addon));CopyTree(Path.Combine(work,"PadChat"),addonNew);
   if(voice){CopyTree(Path.Combine(work,"App"),appNew);VerifyBackend(appNew);}
   StopHelper();
   if(hadAddon)Directory.Move(addon,addonOld);movedAddon=true;Directory.Move(addonNew,addon);
   if(hadApp)Directory.Move(app,appOld);movedApp=true;if(voice)Directory.Move(appNew,app);
   if(failAfterSwap)throw new IOException("Injected update failure");
   var games=new List<string>(GameFolders(previous));if(!games.Contains(game,StringComparer.OrdinalIgnoreCase))games.Add(game);
   var state=new InstallState{Game=game,Games=games.ToArray(),Voice=voice,Startup=voice&&startup,Version=Version};
   File.WriteAllText(StatePath,Json.Serialize(state));
   File.Copy(uninstallerPath,Path.Combine(Root,"Uninstall.exe"),true);
   CopyTree(Path.Combine(work,"Docs"),Path.Combine(Root,"Docs"));
   Integrate(state);
   // Cleanup after commit cannot turn a successful update into a partial rollback.
   foreach(var backup in new[]{addonOld,appOld})try{DeleteOwnedTree(backup);}catch(IOException){}catch(UnauthorizedAccessException){}
  }catch{
   if(movedAddon){DeleteOwnedTree(addon);if(hadAddon&&Directory.Exists(addonOld))Directory.Move(addonOld,addon);}
   if(movedApp){DeleteOwnedTree(app);if(hadApp&&Directory.Exists(appOld))Directory.Move(appOld,app);}
   if(oldState==null){if(File.Exists(StatePath))File.Delete(StatePath);}else File.WriteAllText(StatePath,oldState);
   if(previous!=null)Integrate(previous);else RemoveIntegration();
   throw;
  }finally{
   foreach(var p in new[]{work,addonNew,appNew})DeleteOwnedTree(p);
  }
 }
 public static void VerifyBackend(string app){
  string worker=Path.Combine(app,"Backend","voice-backend.exe");
  if(!File.Exists(worker))throw new IOException("The bundled speech engine is missing");
  var info=new ProcessStartInfo(worker,"--list-inputs"){WorkingDirectory=app,UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true};
  using(var p=Process.Start(info)){
   p.StandardInput.Close();
   var stdout=p.StandardOutput.ReadToEndAsync();var stderr=p.StandardError.ReadToEndAsync();
   if(!p.WaitForExit(30000)){p.Kill();throw new IOException("The speech engine did not start within 30 seconds.");}
   if(p.ExitCode!=0||!stdout.Result.Contains("\"microphones\""))throw new IOException("The speech engine could not start. "+stderr.Result);
  }
 }
 void Shortcut(string directory,string label,string target,string arguments){
  Directory.CreateDirectory(directory);
  Type t=Type.GetTypeFromProgID("WScript.Shell");dynamic shell=Activator.CreateInstance(t);
  dynamic link=shell.CreateShortcut(Path.Combine(directory,label+".lnk"));link.TargetPath=target;link.Arguments=arguments;link.WorkingDirectory=Path.GetDirectoryName(target);link.Save();
  System.Runtime.InteropServices.Marshal.FinalReleaseComObject(link);System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);
 }
 string StartupPath{get{return Test?Path.Combine(Root,"test-os","Startup"):Environment.GetFolderPath(Environment.SpecialFolder.Startup);}}
 string MenuPath{get{return Test?Path.Combine(Root,"test-os","StartMenu"):Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs),"PadChat");}}
 void Integrate(InstallState s){
  if(Test){Directory.CreateDirectory(StartupPath);Directory.CreateDirectory(MenuPath);File.WriteAllText(Path.Combine(MenuPath,"registration.json"),Json.Serialize(s));if(s.Startup)File.WriteAllText(Path.Combine(StartupPath,"PadChat.lnk"),"mock");else if(File.Exists(Path.Combine(StartupPath,"PadChat.lnk")))File.Delete(Path.Combine(StartupPath,"PadChat.lnk"));return;}
  string startup=Path.Combine(StartupPath,"PadChat.lnk");if(File.Exists(startup))File.Delete(startup);
  Directory.CreateDirectory(MenuPath);
  foreach(string label in new[]{"PadChat Voice","Choose microphone"}){string p=Path.Combine(MenuPath,label+".lnk");if(File.Exists(p))File.Delete(p);}
  if(s.Voice){Shortcut(MenuPath,"PadChat Voice",Path.Combine(Root,"App","PadChatVoice.exe"),"");Shortcut(MenuPath,"Choose microphone",Path.Combine(Root,"App","PadChatVoice.exe"),"--microphone");if(s.Startup)Shortcut(StartupPath,"PadChat",Path.Combine(Root,"App","PadChatVoice.exe"),"");}
  Shortcut(MenuPath,"PadChat Help",Path.Combine(Root,"Docs","START-HERE.txt"),"");Shortcut(MenuPath,"Uninstall PadChat",Path.Combine(Root,"Uninstall.exe"),"");
  using(var key=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\PadChat")){
   key.SetValue("DisplayName","PadChat for World of Warcraft");key.SetValue("DisplayVersion",Version);key.SetValue("Publisher","PadChat contributors");key.SetValue("UninstallString","\""+Path.Combine(Root,"Uninstall.exe")+"\"");key.SetValue("InstallLocation",Root);key.SetValue("NoModify",1);key.SetValue("NoRepair",1);
  }
 }
 void RemoveIntegration(){
  if(File.Exists(Path.Combine(StartupPath,"PadChat.lnk")))File.Delete(Path.Combine(StartupPath,"PadChat.lnk"));
  DeleteOwnedTree(MenuPath);
  if(!Test)Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\PadChat",false);
 }
 public void Uninstall(){
  var s=ReadState();if(s==null)throw new IOException("No PadChat installation record was found.");
  // Validate recorded paths again before removing just our two managed folders.
  if(!Test&&WoWClients.AnyRunning())throw new IOException("Close all WoW clients before uninstalling PadChat.");
  var games=GameFolders(s);
  foreach(string game in games){if(string.IsNullOrWhiteSpace(game)||!Path.IsPathRooted(game))throw new IOException("The saved game folder is invalid.");AssertNoLinks(game);AssertNoLinks(Path.Combine(game,"Interface","AddOns","PadChat"));}
  AssertNoLinks(Root);StopHelper();
  foreach(string game in games)DeleteOwnedTree(Path.Combine(game,"Interface","AddOns","PadChat"));
  DeleteOwnedTree(Path.Combine(Root,"App"));
  RemoveIntegration();File.Delete(StatePath);
  // Keep microphone choice, learned words / WoW SavedVariables and documentation.
 }
}

class SetupForm:Form {
 readonly Engine engine; TextBox folder;CheckBox voice,startup;Button install,browse;Label status;
 public SetupForm(Engine e,string[] args){
  engine=e;Text="PadChat Setup — "+Engine.Version;ClientSize=new Size(740,445);Font=new Font("Segoe UI",10);StartPosition=FormStartPosition.CenterScreen;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;
  var title=new Label{Text="Controller chat and local voice typing",Location=new Point(24,18),Size=new Size(690,35),Font=new Font("Segoe UI",17,FontStyle.Bold)};
  var expl=new Label{Text="Retail / Era / Hardcore / Anniversary / progression Classic / Forever. Windows 10/11 (64-bit). Close all WoW clients first. Preview compatibility; new clients need live verification. Repeat Setup for each game folder; one shared voice helper.",Location=new Point(24,66),Size=new Size(690,68)};
  var fLabel=new Label{Text="WoW client folder (contains its Wow executable and Data)",Location=new Point(24,142),Size=new Size(670,24)};
  folder=new TextBox{Location=new Point(24,170),Size=new Size(572,28)};browse=new Button{Text="Browse…",Location=new Point(612,167),Size=new Size(103,32)};
  var old=e.ReadState();folder.Text=old!=null?old.Game:FindGame();
  browse.Click+=(s,a)=>{using(var dialog=new FolderBrowserDialog{Description="Select the WoW client folder, such as _retail_ or _classic_era_",SelectedPath=folder.Text})if(dialog.ShowDialog()==DialogResult.OK)folder.Text=dialog.SelectedPath;};
  voice=new CheckBox{Text="Install voice typing (English, processed locally; includes speech models)",Checked=old==null||old.Voice,Location=new Point(24,218),Size=new Size(690,28)};
  startup=new CheckBox{Text="Start voice helper automatically when I sign in to Windows",Checked=old==null||old.Startup,Location=new Point(24,250),Size=new Size(690,28)};
  voice.CheckedChanged+=(s,a)=>startup.Enabled=voice.Checked;startup.Enabled=voice.Checked;
  if(args.Length==4&&args[0]=="--elevated-install"){folder.Text=args[1];voice.Checked=args[2]=="1";startup.Checked=args[3]=="1";Shown+=(s,a)=>Install(null,EventArgs.Empty);}
  var conflict=new Label{Text="Use one controller and one voice helper. Disable older keyboard add-ons if installed. Voice typing uses a microphone available on this Windows PC.",Location=new Point(24,290),Size=new Size(690,56)};
  status=new Label{Text="Choose your game folder, then install. Settings are kept on updates.",Location=new Point(24,354),Size=new Size(690,38)};
  install=new Button{Text=old==null?"Install PadChat":"Update / repair",Location=new Point(508,400),Size=new Size(206,34)};install.Click+=Install;
  Controls.AddRange(new Control[]{title,expl,fLabel,folder,browse,voice,startup,conflict,status,install});
 }
 static string FindGame(){
  var paths=new List<string>();
  foreach(string basePath in new[]{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)})foreach(string client in WoWClients.Folders)paths.Add(Path.Combine(basePath,"World of Warcraft",client));
  foreach(var hive in new[]{Registry.LocalMachine,Registry.CurrentUser})try{using(var k=hive.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall")){if(k!=null)foreach(string name in k.GetSubKeyNames())using(var app=k.OpenSubKey(name)){string p=app.GetValue("InstallLocation","") as string;if(!string.IsNullOrEmpty(p)){paths.Add(p);paths.Add(Path.Combine(p,"_classic_beta_"));}}}}catch{}
  foreach(var parent in paths.ToArray())foreach(var client in WoWClients.Folders)paths.Add(Path.Combine(parent,client));
  return paths.FirstOrDefault(p=>WoWClients.FindExecutable(p)!=null)??"";
 }
 async void Install(object sender,EventArgs args){
  string game=folder.Text;bool withVoice=voice.Checked,auto=withVoice&&startup.Checked;
  try{engine.ValidateGame(game);if(!Environment.Is64BitOperatingSystem)throw new IOException("64-bit Windows is required.");}catch(Exception ex){MessageBox.Show(this,ex.Message,"PadChat",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}
  install.Enabled=browse.Enabled=voice.Enabled=startup.Enabled=folder.Enabled=false;status.Text="Verifying and installing bundled files. This can take a minute…";
  string work=Path.Combine(Path.GetTempPath(),"PadChat-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(work);
  try{
   await Task.Run(()=>{
    using(var input=System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip"))using(var output=File.Create(Path.Combine(work,"payload.zip")))input.CopyTo(output);
    using(var input=System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Uninstall.exe"))using(var output=File.Create(Path.Combine(work,"Uninstall.exe")))input.CopyTo(output);
    engine.Install(game,withVoice,auto,Path.Combine(work,"payload.zip"),Path.Combine(work,"Uninstall.exe"),false);
   });
   status.Text="Installed. Start WoW and enable PadChat in AddOns. F9 opens the keyboard.";
   if(withVoice)Process.Start(Path.Combine(engine.Root,"App","PadChatVoice.exe"),"--microphone");
   MessageBox.Show(this,"PadChat is installed.\n\n1. Choose your microphone in the window that opens.\n2. Start WoW and enable PadChat. Disable older ControllerKeyboard add-ons.\n3. F9 opens the keyboard. Tap F8 to cycle channels; hold to speak and release to send.\n\nThe models stay on your PC; no online account or token charges.","PadChat installed");Close();
  }catch(UnauthorizedAccessException){
   status.Text="Windows permission is needed for the game folder.";
   if(MessageBox.Show(this,"The game folder is protected. Allow a Windows administrator prompt to install there? Use your SAME Windows account. The failed attempt has been rolled back.","PadChat game-folder access",MessageBoxButtons.YesNo)==DialogResult.Yes){
    try{Process.Start(new ProcessStartInfo(Application.ExecutablePath,"--elevated-install \""+game+"\" "+(withVoice?"1":"0")+" "+(auto?"1":"0")){UseShellExecute=true,Verb="runas",WindowStyle=ProcessWindowStyle.Normal});Close();}catch(System.ComponentModel.Win32Exception){status.Text="Windows permission was cancelled. Nothing was installed.";}
   }
  }
  catch(Exception ex){status.Text="Installation failed: "+ex.Message;MessageBox.Show(this,ex.Message,"PadChat setup failed");}
  finally{if(Directory.Exists(work))Directory.Delete(work,true);install.Enabled=browse.Enabled=voice.Enabled=folder.Enabled=true;startup.Enabled=voice.Checked;}
 }
}

class Program {
 [STAThread] static int Main(string[] args){
  AppContext.SetSwitch("Switch.System.IO.UseLegacyPathHandling",false);
  AppContext.SetSwitch("Switch.System.IO.BlockLongPaths",false);
  try{
   if(args.Length==4&&args[0]=="--self-test")return SelfTest(args[1],args[2],args[3]);
   Application.EnableVisualStyles();string root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"PadChat");
   var e=new Engine(root,false);
#if UNINSTALL
   if(MessageBox.Show("Remove PadChat and its Windows startup entry? WoW must be closed. Your microphone settings and WoW learned words are kept.","Uninstall PadChat",MessageBoxButtons.YesNo)!=DialogResult.Yes)return 0;
   try{e.Uninstall();MessageBox.Show("PadChat was removed. Your settings were kept.","PadChat");}
   catch(UnauthorizedAccessException){
    if(MessageBox.Show("Windows protects the game folder. Allow an administrator prompt to remove PadChat? Use the SAME Windows account.","PadChat game-folder access",MessageBoxButtons.YesNo)==DialogResult.Yes){
     try{Process.Start(new ProcessStartInfo(Application.ExecutablePath){UseShellExecute=true,Verb="runas",WindowStyle=ProcessWindowStyle.Normal});}catch(System.ComponentModel.Win32Exception){MessageBox.Show("Removal was cancelled. Run Uninstall PadChat again when ready.","PadChat");}
    }
   }
#else
   Application.Run(new SetupForm(e,args));
#endif
   return 0;
  }catch(Exception ex){if(args.Length>0){Console.Error.WriteLine(ex);return 1;}MessageBox.Show(ex.Message,"PadChat",MessageBoxButtons.OK,MessageBoxIcon.Error);return 1;}
 }
 static void Check(bool condition,string message){if(!condition)throw new Exception("TEST: "+message);}
 static int SelfTest(string testRoot,string payload,string uninstaller){
  // Only isolated new directories. Never tests against an existing install/game.
  string root=Path.GetFullPath(testRoot);if(Directory.Exists(root))throw new IOException("Self-test requires a new empty test path.");Directory.CreateDirectory(root);
  if(payload=="embedded"){
   payload=Path.Combine(root,"shipping-payload.zip");uninstaller=Path.Combine(root,"shipping-uninstall.exe");
   using(var input=System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip"))using(var output=File.Create(payload))input.CopyTo(output);
   using(var input=System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Uninstall.exe"))using(var output=File.Create(uninstaller))input.CopyTo(output);
  }
  string game=Path.Combine(root,"Games — fresh user","WoW Forever"),data=Path.Combine(root,"User Profile","PadChat");
  Directory.CreateDirectory(Path.Combine(game,"Data"));File.WriteAllText(Path.Combine(game,"WowB.exe"),"TEST-FIXTURE");
  var e=new Engine(data,true);bool rejected=false;try{e.ValidateGame(root);}catch(IOException){rejected=true;}Check(rejected,"Wrong game folder accepted");
  foreach(string version in new[]{"1.60.1.70235","12.1.0.69933","5.5.4.70032","1.15.9.70003","2.5.6.69795"})Check(Engine.SupportedVersion(version),"Client rejected: "+version);
  foreach(string version in new[]{"11.2.0","1.61.0","1.12.1","3.3.5","99.1.0",null})Check(!Engine.SupportedVersion(version),"Unknown client accepted");
  Check(WoWClients.IsProcess("Wow")&&WoWClients.IsProcess("WowClassic")&&WoWClients.IsProcess("WowB")&&WoWClients.IsProcess("WowT")&&!WoWClients.IsProcess("notepad")&&!WoWClients.IsProcess("WowHelper"),"Foreground process allowlist");
  foreach(string bad in new[]{"../escape","/escape",@"C:\escape"}){rejected=false;try{Engine.SafeChild(root,bad);}catch(IOException){rejected=true;}Check(rejected,"Unsafe path accepted");}
  // Clean install from the identical payload and engine used by the GUI.
  e.Install(game,true,true,payload,uninstaller,false);Check(e.ReadState().Voice,"Voice install state");
  string addon=Path.Combine(game,"Interface","AddOns","PadChat");Check(File.Exists(Path.Combine(addon,"PadChat.toc")),"Addon installed");Check(File.Exists(Path.Combine(data,"test-os","Startup","PadChat.lnk")),"Startup registered");
  File.WriteAllText(Path.Combine(data,"voice-microphone.json"),"{\"name\":\"Test microphone\",\"hostapi\":\"MME\"}");
  Directory.CreateDirectory(Path.Combine(game,"Interface","AddOns","OtherAddon"));File.WriteAllText(Path.Combine(game,"Interface","AddOns","OtherAddon","keep.txt"),"untouched");
  string original=File.ReadAllText(Path.Combine(addon,"PadChat.toc"));
  string corrupt=Path.Combine(root,"corrupt.zip");File.Copy(payload,corrupt);
  using(var stream=File.Open(corrupt,FileMode.Open))using(var zip=new ZipArchive(stream,ZipArchiveMode.Update)){
   var entry=zip.GetEntry("PadChat/PadChat.toc");entry.Delete();using(var output=new StreamWriter(zip.CreateEntry("PadChat/PadChat.toc").Open()))output.Write("corrupt");
  }
  rejected=false;try{e.Install(game,false,false,corrupt,uninstaller,false);}catch(IOException){rejected=true;}Check(rejected&&File.ReadAllText(Path.Combine(addon,"PadChat.toc"))==original,"Corrupted payload changed installed addon");File.Delete(corrupt);
  rejected=false;try{e.Install(game,true,true,payload,uninstaller,true);}catch(IOException){rejected=true;}Check(rejected&&File.ReadAllText(Path.Combine(addon,"PadChat.toc"))==original,"Failed update did not roll back");
  e.Install(game,true,false,payload,uninstaller,false);Check(!File.Exists(Path.Combine(data,"test-os","Startup","PadChat.lnk")),"Startup opt-out ignored");Check(File.Exists(Path.Combine(data,"voice-microphone.json")),"Mic preference lost on update");
  e.Install(game,false,false,payload,uninstaller,false);Check(!Directory.Exists(Path.Combine(data,"App")),"Keyboard-only upgrade kept helper");
  // Migrate an older one-folder record, then add another client with rollback.
  var legacy=e.ReadState();legacy.Games=null;File.WriteAllText(e.StatePath,new JavaScriptSerializer().Serialize(legacy));
  string second=Path.Combine(root,"Games — fresh user","Retail");Directory.CreateDirectory(Path.Combine(second,"Data"));File.WriteAllText(Path.Combine(second,"Wow.exe"),"TEST-FIXTURE");
  rejected=false;try{e.Install(second,false,false,payload,uninstaller,true);}catch(IOException){rejected=true;}
  Check(rejected&&File.Exists(Path.Combine(addon,"PadChat.toc"))&&!Directory.Exists(Path.Combine(second,"Interface","AddOns","PadChat"))&&e.ReadState().Games==null,"Second-client rollback damaged legacy installation");
  e.Install(second,false,false,payload,uninstaller,false);
  Check(e.ReadState().Games.Length==2&&File.Exists(Path.Combine(addon,"PadChat.toc"))&&File.Exists(Path.Combine(second,"Interface","AddOns","PadChat","PadChat.toc")),"Side-by-side client installation failed");
  e.Uninstall();Check(!Directory.Exists(addon)&&!File.Exists(e.StatePath),"Uninstall left managed addon/state");Check(File.Exists(Path.Combine(data,"voice-microphone.json")),"Uninstall lost settings");Check(File.Exists(Path.Combine(game,"Interface","AddOns","OtherAddon","keep.txt")),"Other addon was changed");
  Check(!Directory.Exists(Path.Combine(second,"Interface","AddOns","PadChat")),"Second client left installed after uninstall");
  e.Install(game,true,true,payload,uninstaller,false);Check(e.ReadState().Voice,"Reinstall failed");e.Uninstall();
  Console.WriteLine("PASS: fresh install, paths with spaces/Unicode, wrong-client rejection, path traversal, integrity, update rollback, startup opt-out, keyboard-only, uninstall, preferences, other add-ons, reinstall.");return 0;
 }
}
