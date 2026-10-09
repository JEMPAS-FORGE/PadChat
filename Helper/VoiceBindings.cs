// Saved keyboard/mouse push-to-talk with a physical-input capture control.
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

partial class VoicePTT {
 class VoiceShortcut {
  public string kind="keyboard";
  public int key=0x77,modifiers=0;
 }
 static VoiceShortcut voiceShortcut=new VoiceShortcut();
 static Form shortcutWindow;
 static Label shortcutCurrent,shortcutPrompt;
 static Button shortcutBind;
 static bool shortcutArmed;
 static long shortcutArmAt;
 static IntPtr mouseHook;
 static ToolStripMenuItem shortcutHelp;
 static System.Threading.EventWaitHandle bindingEvent;
 static readonly KeyboardHook mouseCallback=OnMouseShortcut;
 [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr handle,int command);
 [StructLayout(LayoutKind.Sequential)] struct HookMouse {public int x,y;public uint data,flags,time;public UIntPtr extra;}
 static string ShortcutPath(){return Path.Combine(dataRoot,"voice-shortcut.json");}
 static bool IsModifierKey(int key){return key==0x10||key==0x11||key==0x12||key>=0xA0&&key<=0xA5||key==0x5B||key==0x5C;}
 static string ShortcutProblem(VoiceShortcut s){
  if(s==null||s.modifiers<0||s.modifiers>7)return "Invalid shortcut.";
  if(s.kind=="mouse")return s.key==4||s.key==5||s.key==6?null:"Use the middle button, Mouse 4 or Mouse 5.";
  if(s.kind!="keyboard"||s.key<8||s.key>254||IsModifierKey(s.key))return "Press a key, with optional Ctrl, Alt or Shift.";
  if(s.key==0x1B)return "Escape is reserved for cancelling.";
  if(s.key>=0x70&&s.key<=0x7B&&(s.modifiers&3)==3)return "Ctrl+Shift+function keys are reserved for PadChat communication.";
  if((s.modifiers&4)!=0&&(s.key==9||s.key==0x73||s.key==0x20))return "That shortcut is reserved by Windows.";
  return null;
 }
 static string ShortcutName(VoiceShortcut s){
  string name=s.kind=="mouse"?(s.key==4?"Middle mouse":s.key==5?"Mouse 4":"Mouse 5"):((System.Windows.Forms.Keys)s.key).ToString();
  return ((s.modifiers&1)!=0?"Ctrl + ":"")+((s.modifiers&2)!=0?"Shift + ":"")+((s.modifiers&4)!=0?"Alt + ":"")+name;
 }
 static int CurrentShortcutModifiers(){return (GetAsyncKeyState(0x11)<0?1:0)|(GetAsyncKeyState(0x10)<0?2:0)|(GetAsyncKeyState(0x12)<0?4:0);}
 static bool CanCaptureShortcut(VoiceShortcut s,string kind,uint key,bool game,int modifiers,bool wasDown,bool injected){
  return !injected&&ShortcutProblem(s)==null&&s.kind==kind&&s.key==key&&game&&s.modifiers==modifiers&&!wasDown;
 }
 static VoiceShortcut ReadShortcut(string path){
  if(!File.Exists(path))return new VoiceShortcut();
  try{
   if(new FileInfo(path).Length>4096)return new VoiceShortcut();
   var s=json.Deserialize<VoiceShortcut>(File.ReadAllText(path));
   return ShortcutProblem(s)==null?s:new VoiceShortcut();
  }catch(IOException){}catch(UnauthorizedAccessException){}catch(ArgumentException){}catch(InvalidOperationException){}
  return new VoiceShortcut();
 }
 static void WriteShortcut(string path,VoiceShortcut value){
  string problem=ShortcutProblem(value);if(problem!=null)throw new ArgumentException(problem);
  Directory.CreateDirectory(Path.GetDirectoryName(path));
  string temp=path+".tmp";
  File.WriteAllText(temp,json.Serialize(value),new UTF8Encoding(false));
  if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);
 }
 static void LoadVoiceShortcut(){voiceShortcut=ReadShortcut(ShortcutPath());}
 static bool ShortcutCaptureOwnsFocus(){
  if(!shortcutArmed||shortcutWindow==null||shortcutWindow.IsDisposed||Now<shortcutArmAt)return false;
  uint pid;GetWindowThreadProcessId(GetForegroundWindow(),out pid);
  return pid==System.Diagnostics.Process.GetCurrentProcess().Id;
 }
 static void UpdateShortcutLabels(){
  string name=ShortcutName(voiceShortcut);
  if(shortcutCurrent!=null&&!shortcutCurrent.IsDisposed)shortcutCurrent.Text="Push-to-talk: "+name;
  if(shortcutHelp!=null)shortcutHelp.Text="Tap "+name+" / Share: channel; hold: speak; release: send";
  if(microphoneTray!=null)microphoneTray.Text="PadChat voice — "+(name.Length>30?name.Substring(0,30):name);
 }
 static void SetCapturedShortcut(VoiceShortcut next){
  string problem=ShortcutProblem(next);
  if(problem!=null){shortcutPrompt.Text=problem+"\nTry another key or press Escape to cancel.";return;}
  try{
   WriteShortcut(ShortcutPath(),next);voiceShortcut=next;
   shortcutArmed=false;keyboardHeld=false;keyboardPhysicalDown=true;
   heldAt=0;pressSource="";latched=true;
   shortcutBind.Text="Bind push-to-talk";
   shortcutPrompt.Text="Saved. Select WoW, tap to change channel, or hold to speak.\nRelease sends your message. No game restart is needed.";
   UpdateShortcutLabels();WriteHealth(ready?"ready":"loading");
  }catch(Exception ex){shortcutPrompt.Text="Could not save: "+ex.Message;}
 }
 static Form BuildShortcutWindow(){
  var f=new Form{Text="PadChat Voice — push-to-talk binding",ClientSize=new Size(620,315),StartPosition=FormStartPosition.CenterScreen,
   FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,TopMost=true,Font=new Font("Segoe UI",11)};
  var heading=new Label{Text="Choose your voice shortcut",Location=new Point(22,18),Size=new Size(570,34),Font=new Font("Segoe UI",15,FontStyle.Bold)};
  shortcutCurrent=new Label{Text="Push-to-talk: "+ShortcutName(voiceShortcut),Location=new Point(24,64),Size=new Size(570,30)};
  shortcutBind=new Button{Text="Bind push-to-talk",Location=new Point(24,104),Size=new Size(245,40)};
  shortcutPrompt=new Label{Text="Click Bind, then press your key or middle/side mouse button.\nYou can hold Ctrl, Alt or Shift with it. Escape cancels.",Location=new Point(24,158),Size=new Size(570,105)};
  var close=new Button{Text="Close",Location=new Point(466,269),Size=new Size(130,34)};
  close.Click+=(s,e)=>f.Close();f.CancelButton=close;
  shortcutBind.Click+=(s,e)=>{
   shortcutArmed=true;shortcutArmAt=Now+200;shortcutBind.Text="Press your shortcut...";
   shortcutPrompt.Text="Press a key or middle/side mouse button now.\nHold any modifier first. Escape cancels without changing your binding.\nThis shortcut is reserved while WoW is selected; other apps keep it.";
  };
  f.FormClosed+=(s,e)=>{shortcutArmed=false;shortcutWindow=null;};
  f.Controls.AddRange(new Control[]{heading,shortcutCurrent,shortcutBind,shortcutPrompt,close});
  return f;
 }
 static void ShowVoiceBindings(){
  if(shortcutWindow!=null&&!shortcutWindow.IsDisposed){shortcutWindow.Show();ShowWindow(shortcutWindow.Handle,5);shortcutWindow.Activate();return;}
  if(phase!="idle")Cancel("Dictation cancelled while changing the voice shortcut");
  keyboardHeld=false;heldAt=0;pressSource="";latched=true;badge.Hide();hideAt=0;
  shortcutWindow=BuildShortcutWindow();shortcutWindow.Show();ShowWindow(shortcutWindow.Handle,5);shortcutWindow.Activate();
 }
 static void CancelShortcutCapture(){
  shortcutArmed=false;shortcutBind.Text="Bind push-to-talk";
  shortcutPrompt.Text="Cancelled. Your existing shortcut is unchanged.";
 }
 static IntPtr OnMouseShortcut(int code,IntPtr message,IntPtr data){
  if(code>=0){
   var m=(HookMouse)Marshal.PtrToStructure(data,typeof(HookMouse));int msg=message.ToInt32();
   bool down=msg==0x207||msg==0x20B,up=msg==0x208||msg==0x20C;
   uint key=msg==0x207||msg==0x208?4u:(m.data>>16)==1?5u:6u;
   bool injected=(m.flags&1)!=0;
   if((down||up)&&!injected){
    if(down&&ShortcutCaptureOwnsFocus()){
     SetCapturedShortcut(new VoiceShortcut{kind="mouse",key=(int)key,modifiers=CurrentShortcutModifiers()});return new IntPtr(1);
    }
    if(voiceEnabled&&voiceShortcut.kind=="mouse"&&voiceShortcut.key==key){
     if(down){
      if(CanCaptureShortcut(voiceShortcut,"mouse",key,IsGame(GetForegroundWindow()),CurrentShortcutModifiers(),keyboardPhysicalDown,false))keyboardHeld=true;
      keyboardPhysicalDown=true;/* The addon reserves the action; capture still sees the key. */
     }else{keyboardHeld=false;keyboardPhysicalDown=false;/* Releases reach the in-game capture frame. */}
    }
   }
  }
  return CallNextHookEx(mouseHook,code,message,data);
 }
 static void TestVoiceBindings(){
  var f10=new VoiceShortcut{key=0x79,modifiers=4};
  if(!CanCaptureShortcut(f10,"keyboard",0x79,true,4,false,false)
   ||CanCaptureShortcut(f10,"keyboard",0x77,true,4,false,false)
   ||CanCaptureShortcut(f10,"keyboard",0x79,false,4,false,false)
   ||CanCaptureShortcut(f10,"keyboard",0x79,true,0,false,false)
   ||CanCaptureShortcut(f10,"keyboard",0x79,true,4,true,false)
   ||CanCaptureShortcut(f10,"keyboard",0x79,true,4,false,true))throw new Exception("Custom keyboard shortcut isolation failed");
  var mouse=new VoiceShortcut{kind="mouse",key=5,modifiers=1};
  if(!CanCaptureShortcut(mouse,"mouse",5,true,1,false,false)
   ||CanCaptureShortcut(mouse,"mouse",6,true,1,false,false)
   ||CanCaptureShortcut(mouse,"keyboard",5,true,1,false,false)
   ||CanCaptureShortcut(mouse,"mouse",5,true,1,false,true))throw new Exception("Mouse shortcut isolation failed");
  if(ShortcutProblem(new VoiceShortcut{key=0x77,modifiers=3})==null
   ||ShortcutProblem(new VoiceShortcut{key=0x1B})==null
   ||ShortcutProblem(new VoiceShortcut{kind="mouse",key=1})==null
   ||ShortcutProblem(new VoiceShortcut{key=0x73,modifiers=4})==null)throw new Exception("Reserved shortcut validation failed");
  string dir=Path.Combine(Environment.GetEnvironmentVariable("PADCHAT_TEST_ROOT")??Path.GetTempPath(),"PadChat-bindings-test-"+Guid.NewGuid().ToString("N"));
  Directory.CreateDirectory(dir);string path=Path.Combine(dir,"binding.json");
  try{
   if(ReadShortcut(path).key!=0x77)throw new Exception("Existing F8 users changed");
   WriteShortcut(path,f10);var saved=ReadShortcut(path);
   if(saved.key!=0x79||saved.modifiers!=4||saved.kind!="keyboard")throw new Exception("Saved shortcut not restored");
   WriteShortcut(path,mouse);saved=ReadShortcut(path);
   if(saved.key!=5||saved.modifiers!=1||saved.kind!="mouse")throw new Exception("Mouse update not restored");
   File.WriteAllText(path,"{bad");if(ReadShortcut(path).key!=0x77)throw new Exception("Corrupt settings recovery failed");
   File.WriteAllText(path,json.Serialize(new VoiceShortcut{key=0x77,modifiers=3}));
   if(ReadShortcut(path).modifiers!=0)throw new Exception("Reserved bridge shortcut loaded");
  }finally{Directory.Delete(dir,true);}
  Console.WriteLine("Physical keyboard/mouse binding capture, modifiers, injected-input and foreground isolation, persistence, reserved-key and corrupt-settings checks passed.");
 }
 static void RenderVoiceBindingWindow(string path){
  Application.EnableVisualStyles();
  using(var form=BuildShortcutWindow()){
   form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-2000,-2000);form.ShowInTaskbar=false;
   form.Show();Application.DoEvents();
   using(var bitmap=new Bitmap(form.Width,form.Height)){
    form.DrawToBitmap(bitmap,new Rectangle(0,0,bitmap.Width,bitmap.Height));
    bitmap.Save(path,System.Drawing.Imaging.ImageFormat.Png);
   }
   form.Hide();
  }
 }
}
