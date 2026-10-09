using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

partial class VoicePTT {
 // Observe the saved physical shortcut only in foreground WoW. The addon
 // reserves its game action, so its binding capture still receives the key.
 // Injected protocol shortcuts and keys in other applications pass through.
 [StructLayout(LayoutKind.Sequential)] struct HookKey {public uint key,scan,flags,time;public UIntPtr extra;}
 delegate IntPtr KeyboardHook(int code,IntPtr message,IntPtr data);
 [DllImport("user32.dll",SetLastError=true)] static extern IntPtr SetWindowsHookEx(int type,KeyboardHook callback,IntPtr module,uint thread);
 [DllImport("user32.dll")] static extern bool UnhookWindowsHookEx(IntPtr hook);
 [DllImport("user32.dll")] static extern IntPtr CallNextHookEx(IntPtr hook,int code,IntPtr message,IntPtr data);
 [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
 [DllImport("kernel32.dll",CharSet=CharSet.Auto)] static extern IntPtr GetModuleHandle(string name);
 static readonly KeyboardHook keyboardCallback=OnKeyboard;
 static IntPtr keyboardHook;
 static bool keyboardHeld,keyboardPhysicalDown;
 static bool toggleLatched;static long toggleHeldAt;static IntPtr toggleWindow;
 static bool KeyboardToggleChord(uint buttons){return (buttons&0x208)==0x208&&(buttons&0x100)==0;}
 static void PollKeyboardToggle(bool connected,uint buttons,bool sameDevice){
  bool chord=connected&&KeyboardToggleChord(buttons);
  if(!sameDevice){toggleHeldAt=0;toggleLatched=chord;return;}
  if(!chord){toggleHeldAt=0;toggleLatched=false;return;}
  if(toggleLatched)return;
  if(toggleHeldAt==0){toggleHeldAt=Now;toggleWindow=GetForegroundWindow();}
  else if(Now-toggleHeldAt>=500){
   toggleLatched=true;
   if(phase=="idle"&&toggleWindow==GetForegroundWindow()&&IsGame(toggleWindow))Bridge(0x7B);
  }
 }
 static bool RequiresController(string source){return source=="controller";}
 static bool ShouldCycleTap(string source,bool connected,bool latched,long held,long now,string state,bool sameGame){
  return (source=="keyboard"||source=="controller"&&connected)&&!latched&&held!=0&&now-held<300&&state=="idle"&&sameGame;
 }
 static string TalkButton(){return recordSource=="keyboard"?ShortcutName(voiceShortcut):ControllerVoiceName();}
 static bool CanCaptureF8(uint key,bool game,bool modified,bool wasDown){return key==0x77&&game&&!modified&&!wasDown;}
 static IntPtr OnKeyboard(int code,IntPtr message,IntPtr data){
  if(code>=0){
   var key=(HookKey)Marshal.PtrToStructure(data,typeof(HookKey));
   int msg=message.ToInt32();
   if(QueueVoiceInput("keyboard",(int)key.key,msg==0x100||msg==0x104,msg==0x101||msg==0x105,(key.flags&0x10)!=0))return new IntPtr(1);
  }
  return CallNextHookEx(keyboardHook,code,message,data);
 }
 static void InstallKeyboardVoice(){StartInputListener();}
 static void RemoveKeyboardVoice(){StopInputListener();}
 static int RetryDelay(int failures){return (int)Math.Min(30000,2000*Math.Pow(2,Math.Min(failures-1,4)));}
 static void MaintainWorker(){
  if(worker!=null&&!worker.HasExited)return;
  ready=false;
  if(phase!="idle")Cancel("Message cancelled: speech helper restarting");
  if(workerRetryAt==0){workerFailures++;workerRetryAt=Now+RetryDelay(workerFailures);WriteHealth("restarting");}
  if(Now<workerRetryAt)return;
  workerRetryAt=0;
  try{StartWorker();}catch(Exception){workerFailures++;workerRetryAt=Now+RetryDelay(workerFailures);WriteHealth("restarting");}
 }
 static void WriteHealth(string state){
  // Health only: never audio, recognised words, clipboard or chat recipients.
  try{File.WriteAllText(Path.Combine(dataRoot,"voice-health.json"),json.Serialize(new {
   state=state,pid=Process.GetCurrentProcess().Id,session=Process.GetCurrentProcess().SessionId,
   workerPid=worker!=null&&!worker.HasExited?worker.Id:0,keyboard=ShortcutName(voiceShortcut),controller=ControllerVoiceName(),inGameConfigured=gameSettingsManaged,inGameSettings=gameSettingsStatus,voiceEnabled=voiceEnabled,inputListenerAlive=inputThread!=null&&inputThread.IsAlive,inputEvents=System.Threading.Interlocked.Read(ref inputEvents),inputInterruptions=inputInterruptions,updated=DateTime.UtcNow.ToString("o")
  }),new UTF8Encoding(false));}catch(IOException){}catch(UnauthorizedAccessException){}
 }
 static void TestKeyboardVoice(){
  if(!KeyboardToggleChord(NormalizeXInput(0x8010))||KeyboardToggleChord(NormalizeXInput(0x8000))
   ||KeyboardToggleChord(NormalizeXInput(0x0010))||KeyboardToggleChord(NormalizeXInput(0x8030))
   ||!KeyboardToggleChord(0x208)||KeyboardToggleChord(0x308))throw new Exception("Options/Triangle toggle isolation failed");
  if(!CanCaptureF8(0x77,true,false,false)||CanCaptureF8(0x77,false,false,false)
   ||CanCaptureF8(0x77,true,true,false)||CanCaptureF8(0x77,true,false,true)
   ||CanCaptureF8(0x78,true,false,false))throw new Exception("F8 focus/modifier/repeat isolation failed");
  if(RequiresController("keyboard")||!RequiresController("controller"))throw new Exception("Keyboard requires a controller");
  if(!ShouldCycleTap("keyboard",false,false,100,250,"idle",true)
   ||!ShouldCycleTap("controller",true,false,100,250,"idle",true)
   ||ShouldCycleTap("controller",false,false,100,250,"idle",true)
   ||ShouldCycleTap("keyboard",false,false,100,400,"idle",true)
   ||ShouldCycleTap("keyboard",false,true,100,250,"idle",true)
   ||ShouldCycleTap("keyboard",false,false,100,250,"recording",true)
   ||ShouldCycleTap("keyboard",false,false,100,250,"idle",false)
   ||ShouldCycleTap("keyboard",false,false,0,250,"idle",true))throw new Exception("F8 tap/hold channel routing failed");
  if(RetryDelay(1)!=2000||RetryDelay(2)!=4000||RetryDelay(10)!=30000)throw new Exception("Speech recovery backoff failed");
  Console.WriteLine("F8 tap/hold routing, keyboard PTT, focus/modifier isolation, controller independence and worker recovery tests passed.");
 }
}
