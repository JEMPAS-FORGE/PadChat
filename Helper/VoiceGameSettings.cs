using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

partial class VoicePTT {
 class GameVoiceSettings {
  public VoiceShortcut shortcut;
  public string microphone,startup,pad,padMod,vocabulary;
  public bool enabled,review;
 }
 static bool voiceEnabled=true,gameSettingsManaged,voiceReview;
 static string voicePad="SHARE",voicePadMod="NONE",gameSettingsStatus="Not yet configured in game";
 static string watchedSettingsFile="";
 static DateTime watchedSettingsStamp=DateTime.MinValue;
 static long gameSettingsPollAt;
 static GameVoiceSettings pendingGameSettings;
 static readonly Dictionary<string,uint> padBits=new Dictionary<string,uint>{
  {"NONE",0},{"SHARE",0x100},{"START",0x200},{"NORTH",8},{"LSTICK",0x400},{"RSTICK",0x800},
  {"LSHOULDER",0x10},{"RSHOULDER",0x20},{"LTRIGGER",0x40},{"RTRIGGER",0x80}};
 static string GameSettingsPath(){return Path.Combine(dataRoot,"voice-game-settings.json");}
 static GameVoiceSettings ParseGameSettings(string wire){
  if(wire==null||wire.Length>5000)throw new ArgumentException("Invalid voice settings size.");
  var parts=wire.Split(';');int key,mods;
  if(!(parts.Length==9&&parts[0]=="PCV1"||(parts.Length==10&&parts[0]=="PCV2"||parts.Length==11&&parts[0]=="PCV3")&&(parts[9]=="0"||parts[9]=="1"))||!int.TryParse(parts[2],out key)||!int.TryParse(parts[3],out mods)
   ||!Regex.IsMatch(parts[4],@"^(?:[A-Za-z0-9 ._-]|%[0-9A-F]{2})*$")
   ||parts[5]!="0"&&parts[5]!="1"||parts[6]!="KEEP"&&parts[6]!="ON"&&parts[6]!="OFF"
   ||!padBits.ContainsKey(parts[7])||!padBits.ContainsKey(parts[8])
   ||parts[8]!="NONE"&&parts[8]!="LTRIGGER"&&parts[8]!="RTRIGGER"&&parts[8]!="LSHOULDER"&&parts[8]!="RSHOULDER")throw new ArgumentException("Unsupported voice settings record.");
  var shortcut=new VoiceShortcut{kind=parts[1],key=key,modifiers=mods};
  if(ShortcutProblem(shortcut)!=null)throw new ArgumentException(ShortcutProblem(shortcut));
  string mic=Uri.UnescapeDataString(parts[4]);
  string vocabulary="";if(parts.Length==11){if(!Regex.IsMatch(parts[10],@"^(?:[A-Za-z0-9 ._-]|%[0-9A-F]{2})*$"))throw new ArgumentException("Invalid vocabulary encoding");vocabulary=Uri.UnescapeDataString(parts[10]);if(!ValidVocabulary(vocabulary))throw new ArgumentException("Invalid vocabulary");}
  if(mic.Length>512||Regex.IsMatch(mic,@"[\x00-\x1F\x7F]")||parts[7]==parts[8]&&parts[7]!="NONE")throw new ArgumentException("Invalid microphone or controller binding.");
  return new GameVoiceSettings{shortcut=shortcut,microphone=mic,enabled=parts[5]=="1",startup=parts[6],pad=parts[7],padMod=parts[8],review=parts.Length>=10&&parts[9]=="1",vocabulary=vocabulary};
 }
 static string ReadGameSettingsRecord(string path){
  if(new FileInfo(path).Length>4*1024*1024)throw new IOException("PadChat settings file is too large.");
  // Never execute Lua or inspect learned words, drafts, whispers or players.
  string contents;
  using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete))
  using(var reader=new StreamReader(stream,Encoding.UTF8,true))contents=reader.ReadToEnd();
  var matches=Regex.Matches(contents,@"\[""voiceSettingsWire""\]\s*=\s*""(PCV[123];[A-Za-z0-9;%. _-]{1,5000})""");
  if(matches.Count>1)throw new IOException("Multiple voice settings records found.");
  return matches.Count==1?matches[0].Groups[1].Value:null;
 }
 static string ForegroundGameFolder(){
  uint pid;GetWindowThreadProcessId(GetForegroundWindow(),out pid);
  try{using(var p=Process.GetProcessById((int)pid))if(IsGame(GetForegroundWindow()))return Path.GetDirectoryName(p.MainModule.FileName);}catch{}
  return null;
 }
 static void PollGameSettings(){
  if(Now<gameSettingsPollAt)return;gameSettingsPollAt=Now+1000;
  string game=ForegroundGameFolder();if(game==null)return;
  try{
   string accountRoot=Path.Combine(game,@"WTF\Account");if(!Directory.Exists(accountRoot))return;
   string candidate=null;DateTime newest=DateTime.MinValue;
   foreach(string account in Directory.GetDirectories(accountRoot)){
    string path=Path.Combine(account,@"SavedVariables\PadChat.lua");if(!File.Exists(path))continue;
    DateTime stamp=File.GetLastWriteTimeUtc(path);if(stamp>newest){newest=stamp;candidate=path;}
   }
   if(candidate==null||candidate==watchedSettingsFile&&newest==watchedSettingsStamp)return;
   // The most recently saved account is the active one after Save & reload.
   string wire=ReadGameSettingsRecord(candidate);
   watchedSettingsFile=candidate;watchedSettingsStamp=newest;
   if(wire==null)return;
   var settings=ParseGameSettings(wire);
   if(phase!="idle")Cancel("Message cancelled while applying in-game voice settings");
   heldAt=0;pressSource="";keyboardHeld=false;latched=true;
   if(settings.microphone.Length==0||!settings.enabled){ApplyGameSettings(settings,null);return;}
   pendingGameSettings=settings;
   if(microphoneEnumeration!=null&&!microphoneEnumeration.HasExited)return;
   microphoneEnumeration=new Process{StartInfo=PythonInfo("--list-inputs")};
   microphoneEnumeration.OutputDataReceived+=(s,e)=>{if(e.Data!=null)messages.Enqueue(e.Data);};
   microphoneEnumeration.ErrorDataReceived+=(s,e)=>{};
   microphoneEnumeration.Start();microphoneEnumeration.BeginOutputReadLine();microphoneEnumeration.BeginErrorReadLine();
  }catch(Exception ex){pendingGameSettings=null;gameSettingsStatus="Could not apply: "+ex.Message;Status(gameSettingsStatus+"\nOpen /padchat voice to correct the selection.",8000);WriteHealth("settings-error");}
 }
 static MicrophoneChoice MatchGameMicrophone(string name,IEnumerable devices){
  var exact=new List<MicrophoneChoice>();var prefixes=new List<MicrophoneChoice>();
  foreach(object value in devices){
   var d=value as Dictionary<string,object>;if(d==null||!d.ContainsKey("name")||!d.ContainsKey("hostapi")||(string)d["hostapi"]!="MME")continue;
   var mic=new MicrophoneChoice{name=(string)d["name"],hostapi="MME"};
   if(string.Equals(mic.name,name,StringComparison.OrdinalIgnoreCase))exact.Add(mic);
   else if(mic.name.Length>=20&&name.StartsWith(mic.name,StringComparison.OrdinalIgnoreCase))prefixes.Add(mic);
  }
  if(exact.Count==1)return exact[0];
  if(exact.Count==0&&prefixes.Count==1)return prefixes[0];
  throw new ArgumentException(exact.Count+prefixes.Count>1?"Microphone name is ambiguous. Choose another input.":"Selected microphone is not available to the companion. Connect it and save again.");
 }
 static void ApplyGameMicrophoneList(Dictionary<string,object> data){
  if(pendingGameSettings==null)return;var settings=pendingGameSettings;pendingGameSettings=null;
  try{
   if(data.ContainsKey("error"))throw new IOException((string)data["error"]);
   var mic=MatchGameMicrophone(settings.microphone,(IEnumerable)data["devices"]);
   ApplyGameSettings(settings,mic);
  }catch(Exception ex){gameSettingsStatus="Could not apply: "+ex.Message;Status(gameSettingsStatus+"\nOpen /padchat voice to correct the selection.",9000);WriteHealth("settings-error");}
 }
 static void AtomicVoiceFile(string path,string contents){
  Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+".tmp";
  File.WriteAllText(temp,contents,new UTF8Encoding(false));if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);
 }
 static void ApplyGameStartup(string setting){
  if(setting=="KEEP")return;
  string path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup),"PadChat.lnk");
  if(setting=="OFF"){if(File.Exists(path))File.Delete(path);}
  else {Type type=Type.GetTypeFromProgID("WScript.Shell");object shell=Activator.CreateInstance(type);
  try{
   dynamic shortcut=type.InvokeMember("CreateShortcut",System.Reflection.BindingFlags.InvokeMethod,null,shell,new object[]{path});
   shortcut.TargetPath=System.Windows.Forms.Application.ExecutablePath;shortcut.WorkingDirectory=voiceRoot;shortcut.WindowStyle=7;shortcut.Save();
   System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shortcut);
  }finally{System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);}}
  string statePath=Path.Combine(dataRoot,"install.json");
  if(File.Exists(statePath)){
   var state=json.Deserialize<System.Collections.Generic.Dictionary<string,object>>(File.ReadAllText(statePath));
   if(state!=null){state["Startup"]=setting=="ON";AtomicVoiceFile(statePath,json.Serialize(state));}
  }
 }
 static void ApplyGameSettings(GameVoiceSettings s,MicrophoneChoice mic){
  // Validate and resolve first. A missing/ambiguous microphone must never
  // silently change the input or apply only half of the user's configuration.
  ApplyGameStartup(s.startup);
  if(mic!=null)AtomicVoiceFile(Path.Combine(dataRoot,"voice-microphone.json"),json.Serialize(new{name=mic.name,hostapi=mic.hostapi}));
  WriteShortcut(ShortcutPath(),s.shortcut);
  AtomicVoiceFile(GameSettingsPath(),json.Serialize(s));
  voiceVocabulary=s.vocabulary??"";voiceShortcut=s.shortcut;voiceEnabled=s.enabled;voiceReview=s.review;voicePad=s.pad;voicePadMod=s.padMod;gameSettingsManaged=true;
  keyboardHeld=false;keyboardPhysicalDown=GetAsyncKeyState(voiceShortcut.key)<0;heldAt=0;pressSource="";latched=true;
  if(microphoneWindow!=null&&!microphoneWindow.IsDisposed)microphoneWindow.Close();
  if(shortcutWindow!=null&&!shortcutWindow.IsDisposed)shortcutWindow.Close();
  if(!voiceEnabled){StopWorker();ready=false;}else if(mic!=null||worker==null||worker.HasExited)StartWorker();
  if(voiceEnabled&&ready)WriteVocabulary();
  gameSettingsStatus="Applied from in-game Options";UpdateShortcutLabels();WriteHealth(voiceEnabled?(ready?"ready":"loading"):"disabled");
  Status("PadChat voice settings applied\n"+ShortcutName(voiceShortcut)+" / "+ControllerVoiceName()+" — "+(voiceEnabled?"tap: channel; hold: speak":"voice disabled"),5000);
 }
 static void LoadGameVoiceSettings(){
  try{
   string path=GameSettingsPath();if(!File.Exists(path)||new FileInfo(path).Length>8192)return;
   var s=json.Deserialize<GameVoiceSettings>(File.ReadAllText(path));
   if(s==null||ShortcutProblem(s.shortcut)!=null||!padBits.ContainsKey(s.pad)||!padBits.ContainsKey(s.padMod))return;
   if(!ValidVocabulary(s.vocabulary))return;voiceVocabulary=s.vocabulary??"";
   voiceEnabled=s.enabled;voiceReview=s.review;voicePad=s.pad;voicePadMod=s.padMod;gameSettingsManaged=true;gameSettingsStatus="Restored in-game settings";
  }catch{}
 }
 static bool ControllerVoiceChord(uint buttons){return voiceEnabled&&voicePad!="NONE"&&(buttons&padBits[voicePad])!=0&&(voicePadMod=="NONE"||(buttons&padBits[voicePadMod])!=0);}
 static string PadVoiceName(string key){
  switch(key){case "SHARE":return "Create / Share / Back";case "START":return "Options / Start";case "NORTH":return "Triangle / Y";
   case "LSTICK":return "L3";case "RSTICK":return "R3";case "LSHOULDER":return "L1 / LB";case "RSHOULDER":return "R1 / RB";
   case "LTRIGGER":return "L2 / LT";case "RTRIGGER":return "R2 / RT";default:return "None";}
 }
 static string ControllerVoiceName(){return voicePadMod=="NONE"?PadVoiceName(voicePad):PadVoiceName(voicePadMod)+" + "+PadVoiceName(voicePad);}
 static void TestGameVoiceSettings(){
  var s=ParseGameSettings("PCV1;mouse;6;0;Microphone%20%28USB%29;1;KEEP;SHARE;LTRIGGER");
  if(s.shortcut.key!=6||s.microphone!="Microphone (USB)"||s.padMod!="LTRIGGER")throw new Exception("In-game settings decode failed");
  foreach(string wire in new[]{"PCV2;keyboard;119;0;;1;KEEP;SHARE;NONE","PCV1;keyboard;119;3;;1;KEEP;SHARE;NONE","PCV1;mouse;1;0;;1;KEEP;SHARE;NONE","PCV1;keyboard;119;0;bad%0Aname;1;KEEP;SHARE;NONE","PCV1;keyboard;119;0;;1;KEEP;SHARE;SHARE"}){
   bool failed=false;try{ParseGameSettings(wire);}catch(ArgumentException){failed=true;}if(!failed)throw new Exception("Unsafe settings accepted");
  }
  var devices=new object[]{new Dictionary<string,object>{{"name","Microphone (USB)"},{"hostapi","MME"}},new Dictionary<string,object>{{"name","Microphone (USB)"},{"hostapi","WASAPI"}}};
  if(MatchGameMicrophone(s.microphone,devices).hostapi!="MME")throw new Exception("Mic host isolation failed");
  bool absent=false;try{MatchGameMicrophone("Missing",devices);}catch(ArgumentException){absent=true;}if(!absent)throw new Exception("Missing mic fallback allowed");
  var duplicate=new object[]{devices[0],devices[0]};bool ambiguous=false;try{MatchGameMicrophone(s.microphone,duplicate);}catch(ArgumentException){ambiguous=true;}if(!ambiguous)throw new Exception("Ambiguous mic accepted");
  var truncated=new object[]{new Dictionary<string,object>{{"name","Microphone (2- Razer Nari Essen"},{"hostapi","MME"}}};
  MatchGameMicrophone("Microphone (2- Razer Nari Essential)",truncated);
  string root=Path.Combine(Environment.GetEnvironmentVariable("PADCHAT_TEST_ROOT")??Path.GetTempPath(),"PadChat-game-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
  try{
   string path=Path.Combine(root,"PadChat.lua");File.WriteAllText(path,"PadChatDB = { [\"draft\"] = \"private\", [\"voiceSettingsWire\"] = \"PCV1;mouse;6;0;;1;KEEP;SHARE;NONE\", }");
   if(ParseGameSettings(ReadGameSettingsRecord(path)).shortcut.key!=6)throw new Exception("SavedVariables bridge failed");
   File.WriteAllText(path,"PadChatDB = { [\"draft\"] = \"PCV1;mouse;6;0;;1;KEEP;SHARE;NONE\", }");if(ReadGameSettingsRecord(path)!=null)throw new Exception("Non-settings Lua field read");
  }finally{Directory.Delete(root,true);}
  voicePad="SHARE";voicePadMod="LTRIGGER";if(ControllerVoiceChord(0x100)||ControllerVoiceChord(0x40)||!ControllerVoiceChord(0x140))throw new Exception("Controller modifier failed");voicePadMod="NONE";
  if(!ParseGameSettings("PCV2;keyboard;119;0;;1;KEEP;SHARE;NONE;1").review||ParseGameSettings("PCV1;keyboard;119;0;;1;KEEP;SHARE;NONE").review)throw new Exception("Review settings migration failed");
  Console.WriteLine("In-game settings protocol, saved-variable field isolation, microphone matching, missing/ambiguous input refusal and controller chord checks passed.");
 }
}
