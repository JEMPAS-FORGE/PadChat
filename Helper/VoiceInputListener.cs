using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;

partial class VoicePTT {
 // Windows invokes low-level hooks on their installing thread. Never run UI,
 // process waits, file IO, speech work or synchronous UI dispatch on this thread.
 // Only the selected shortcut or an explicitly armed capture is queued. No
 // arbitrary typing, audio, words or recipients are recorded in diagnostics.
 class InputPolicy {
  public string kind;public int key,modifiers,generation;
  public bool enabled;public long published,captureAt;
  public IntPtr captureWindow,reviewWindow;
 }
 class VoiceInput {
  public string kind;public int key,modifiers,generation;
  public bool down,capture,cancelCapture,cancelReview;
  public long at,captureAt;public IntPtr window;
 }
 [StructLayout(LayoutKind.Sequential)] struct InputMessage {
  public IntPtr window;public uint message;public UIntPtr wparam;public IntPtr lparam;
  public uint time;public int x,y;public uint reserved;
 }
 [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
 [DllImport("user32.dll",SetLastError=true)] static extern int GetMessage(out InputMessage message,IntPtr window,uint min,uint max);
 [DllImport("user32.dll")] static extern bool PeekMessage(out InputMessage message,IntPtr window,uint min,uint max,uint remove);
 [DllImport("user32.dll")] static extern bool TranslateMessage(ref InputMessage message);
 [DllImport("user32.dll")] static extern IntPtr DispatchMessage(ref InputMessage message);
 [DllImport("user32.dll",SetLastError=true)] static extern bool PostThreadMessage(uint thread,uint message,UIntPtr wparam,IntPtr lparam);
 static Thread inputThread;static uint inputThreadId;
 static volatile InputPolicy inputPolicy;
 static readonly ConcurrentQueue<VoiceInput> inputQueue=new ConcurrentQueue<VoiceInput>();
 static int inputQueued,inputOverflow,inputInterruptions,inputProbeCount;
 static long inputEvents;
 static long inputRetryAt;
 const uint InputProbe=0x8001;
 const uint InputFaultProbe=0x8002;
 static bool inputFaultTest;
 const int InputCapacity=128,InputMaxAge=250;

 static void PublishInputPolicy(){
  IntPtr window=GetForegroundWindow();var old=inputPolicy;
  bool changed=old==null||old.kind!=voiceShortcut.kind||old.key!=voiceShortcut.key||old.modifiers!=voiceShortcut.modifiers||old.enabled!=voiceEnabled;
  inputPolicy=new InputPolicy{kind=voiceShortcut.kind,key=voiceShortcut.key,modifiers=voiceShortcut.modifiers,enabled=voiceEnabled,
   generation=old==null?1:old.generation+(changed?1:0),published=Now,
   captureWindow=ShortcutCaptureOwnsFocus()?window:IntPtr.Zero,captureAt=shortcutArmAt,
   reviewWindow=phase=="review"?target:IntPtr.Zero};
 }
 static bool QueueVoiceInput(string kind,int key,bool down,bool up,bool injected){
  if(injected||!down&&!up)return false;
  var policy=inputPolicy;if(policy==null)return false;
  // Filter before asking for focus/modifiers. Ordinary typing and mouse motion
  // never enter the queue; injected protocol shortcuts never affect PTT.
  if(policy.captureWindow==IntPtr.Zero&&!(kind=="keyboard"&&key==0x1B&&policy.reviewWindow!=IntPtr.Zero)
   &&(!policy.enabled||kind!=policy.kind||key!=policy.key))return false;
  return QueueVoiceInput(policy,kind,key,down,up,false,GetForegroundWindow(),CurrentShortcutModifiers(),Now);
 }
 static bool QueueVoiceInput(InputPolicy policy,string kind,int key,bool down,bool up,bool injected,IntPtr window,int modifiers,long at){
  if(policy==null||injected||!down&&!up)return false;
  bool capture=down&&window!=IntPtr.Zero&&window==policy.captureWindow&&at>=policy.captureAt&&!(kind=="keyboard"&&IsModifierKey(key));
  bool cancelReview=kind=="keyboard"&&key==0x1B&&down&&window!=IntPtr.Zero&&window==policy.reviewWindow;
  bool shortcut=policy.enabled&&policy.kind==kind&&policy.key==key;
  if(!capture&&!cancelReview&&!shortcut)return false;
  if(Interlocked.Increment(ref inputQueued)>InputCapacity){Interlocked.Decrement(ref inputQueued);Interlocked.Exchange(ref inputOverflow,1);return false;}
  inputQueue.Enqueue(new VoiceInput{kind=kind,key=key,down=down,modifiers=modifiers,window=window,at=at,generation=policy.generation,
   capture=capture,cancelCapture=capture&&kind=="keyboard"&&key==0x1B,cancelReview=cancelReview,captureAt=policy.captureAt});
  Interlocked.Increment(ref inputEvents);
  return capture||cancelReview;
 }
 static bool CurrentShortcutEvent(VoiceInput value,int generation,long now){
  return value.generation==generation&&now>=value.at&&now-value.at<=InputMaxAge&&value.kind==voiceShortcut.kind&&value.key==voiceShortcut.key;
 }
 static void ApplyShortcutInput(VoiceInput value,bool sameGame){
  if(value.down){
   if(voiceEnabled&&CanCaptureShortcut(voiceShortcut,value.kind,(uint)value.key,sameGame,value.modifiers,keyboardPhysicalDown,false))keyboardHeld=true;
   keyboardPhysicalDown=true;
  }else{keyboardHeld=false;keyboardPhysicalDown=false;}
 }
 static void ResetInterruptedInput(){
  inputInterruptions++;keyboardHeld=false;keyboardPhysicalDown=GetAsyncKeyState(voiceShortcut.key)<0;
  heldAt=0;latched=true;pressSource="";
  // A delayed release must not become permission to send an earlier recording.
  if(recordSource=="keyboard"&&phase!="idle")Cancel("Voice input interrupted; nothing sent. Release PTT and press again.");
 }
 static void DrainVoiceInputs(){
  var policy=inputPolicy;if(policy==null)return;
  bool interrupted=Interlocked.Exchange(ref inputOverflow,0)!=0;
  VoiceInput value;
  while(inputQueue.TryDequeue(out value)){
   Interlocked.Decrement(ref inputQueued);
   if(Now-value.at>InputMaxAge){interrupted=true;continue;}
   if(interrupted||value.generation!=policy.generation)continue;
   if(value.capture){
    if(!shortcutArmed||value.captureAt!=shortcutArmAt||!ShortcutCaptureOwnsFocus()||value.window!=GetForegroundWindow())continue;
    if(value.cancelCapture)CancelShortcutCapture();
    else SetCapturedShortcut(new VoiceShortcut{kind=value.kind,key=value.key,modifiers=value.modifiers});
    continue;
   }
   if(value.cancelReview){if(phase=="review"&&SameGame())Cancel("Review cancelled; nothing sent");continue;}
   if(CurrentShortcutEvent(value,policy.generation,Now))ApplyShortcutInput(value,value.window!=IntPtr.Zero&&value.window==GetForegroundWindow()&&IsGame(value.window));
  }
  if(interrupted)ResetInterruptedInput();
 }
 static void StartInputListener(){
  if(inputThread!=null&&inputThread.IsAlive)return;
  keyboardPhysicalDown=GetAsyncKeyState(voiceShortcut.key)<0;PublishInputPolicy();
  Exception error=null;
  using(var readyEvent=new ManualResetEvent(false)){
   inputThread=new Thread(()=>{
    bool announced=false;
    try{
     InputMessage message;PeekMessage(out message,IntPtr.Zero,0,0,0);inputThreadId=GetCurrentThreadId();
     keyboardHook=SetWindowsHookEx(13,keyboardCallback,GetModuleHandle(null),0);
     if(keyboardHook==IntPtr.Zero)throw new InvalidOperationException("Could not enable voice shortcut: "+Marshal.GetLastWin32Error());
     mouseHook=SetWindowsHookEx(14,mouseCallback,GetModuleHandle(null),0);
     if(mouseHook==IntPtr.Zero)throw new InvalidOperationException("Could not enable mouse voice shortcut: "+Marshal.GetLastWin32Error());
     announced=true;readyEvent.Set();
     int result;
     while((result=GetMessage(out message,IntPtr.Zero,0,0))>0){
      if(message.message==InputProbe){Interlocked.Increment(ref inputProbeCount);continue;}
      if(inputFaultTest&&message.message==InputFaultProbe)throw new InvalidOperationException("Isolated input listener recovery test");
      TranslateMessage(ref message);DispatchMessage(ref message);
     }
     if(result<0)Interlocked.Exchange(ref inputOverflow,1);
    }catch(Exception ex){
     // The startup event is disposed once the caller receives readiness. A
     // later pump failure must terminate cleanly for the normal retry path.
     if(!announced){error=ex;readyEvent.Set();}
     Interlocked.Exchange(ref inputOverflow,1);
    }
    finally{
     if(keyboardHook!=IntPtr.Zero)UnhookWindowsHookEx(keyboardHook);keyboardHook=IntPtr.Zero;
     if(mouseHook!=IntPtr.Zero)UnhookWindowsHookEx(mouseHook);mouseHook=IntPtr.Zero;
    }
   });
   inputThread.IsBackground=true;inputThread.Name="PadChat physical input";inputThread.SetApartmentState(ApartmentState.STA);inputThread.Start();
   // Hook installation contains no speech/process/file work. Wait for the exact
   // startup outcome so partial installation cannot masquerade as readiness.
   readyEvent.WaitOne();
  }
  if(error!=null){inputThread.Join();inputThread=null;throw new InvalidOperationException("Voice input listener could not start",error);}
 }
 static void MaintainInputListener(){
  if(inputThread!=null&&inputThread.IsAlive)return;
  if(Now<inputRetryAt)return;
  ResetInterruptedInput();
  try{StopInputListener();StartInputListener();inputRetryAt=0;WriteHealth(ready?"ready":"loading");}
  catch(Exception ex){inputRetryAt=Now+5000;WorkerError("Input listener: "+ex.Message);WriteHealth("input-error");}
 }
 static void StopInputListener(){
  var thread=inputThread;if(thread==null)return;
  if(thread.IsAlive){PostThreadMessage(inputThreadId,0x12,UIntPtr.Zero,IntPtr.Zero);if(!thread.Join(3000))throw new InvalidOperationException("Voice input listener did not stop");}
  inputThread=null;inputThreadId=0;
  VoiceInput value;while(inputQueue.TryDequeue(out value))Interlocked.Decrement(ref inputQueued);
 }
 static void TestInputThread(){
  StartInputListener();
  try{
   if(inputThreadId==GetCurrentThreadId()||keyboardHook==IntPtr.Zero||mouseHook==IntPtr.Zero)throw new Exception("Hooks did not install on the independent thread");
   var before=Interlocked.CompareExchange(ref inputProbeCount,0,0);
   // Exercise the actual Win32 message pump while this caller is blocked for
   // longer than Windows' maximum hook timeout. No OS input is injected, no
   // microphone opened, no focus changed and no game controlled by this test.
   if(!PostThreadMessage(inputThreadId,InputProbe,UIntPtr.Zero,IntPtr.Zero))throw new Exception("Listener probe post failed");
   Thread.Sleep(2200);
   if(Interlocked.CompareExchange(ref inputProbeCount,0,0)!=before+1||!inputThread.IsAlive)throw new Exception("Blocked caller also blocked input listener");
   if(!PostThreadMessage(inputThreadId,InputProbe,UIntPtr.Zero,IntPtr.Zero))throw new Exception("Second listener probe failed");
   var deadline=Now+1000;while(Interlocked.CompareExchange(ref inputProbeCount,0,0)!=before+2&&Now<deadline)Thread.Sleep(5);
   if(Interlocked.CompareExchange(ref inputProbeCount,0,0)!=before+2)throw new Exception("Listener did not remain responsive");
  }finally{StopInputListener();}
  if(keyboardHook!=IntPtr.Zero||mouseHook!=IntPtr.Zero||inputThread!=null)throw new Exception("Listener cleanup failed");
  inputFaultTest=true;
  try{
   StartInputListener();
   if(!PostThreadMessage(inputThreadId,InputFaultProbe,UIntPtr.Zero,IntPtr.Zero)||!inputThread.Join(3000))throw new Exception("Listener fault did not terminate cleanly");
   if(keyboardHook!=IntPtr.Zero||mouseHook!=IntPtr.Zero||inputOverflow!=1)throw new Exception("Listener fault cleanup/cancellation failed");
  }finally{inputFaultTest=false;StopInputListener();inputOverflow=0;}
  StartInputListener();StopInputListener();
  if(keyboardHook!=IntPtr.Zero||mouseHook!=IntPtr.Zero||inputThread!=null)throw new Exception("Listener restart cleanup failed");
  Console.WriteLine("PASS: actual keyboard/mouse hooks on independent thread; message pump survives 2200 ms caller stall; post-start fault cleanup and restart. No physical-input/live-game claim.");
 }
 static void TestInputRouting(){
  var saved=voiceShortcut;bool savedEnabled=voiceEnabled;
  try{
   voiceEnabled=true;voiceShortcut=new VoiceShortcut{kind="mouse",key=6,modifiers=1};
   var policy=new InputPolicy{kind="mouse",key=6,modifiers=1,enabled=true,generation=7};
   var window=new IntPtr(99);
   if(QueueVoiceInput(policy,"mouse",6,true,false,true,window,1,10)||inputQueued!=0)throw new Exception("Injected input queued");
   QueueVoiceInput(policy,"mouse",6,true,false,false,window,1,10);
   VoiceInput value;if(!inputQueue.TryDequeue(out value))throw new Exception("Mouse 5 down missing");Interlocked.Decrement(ref inputQueued);
   keyboardPhysicalDown=false;keyboardHeld=false;ApplyShortcutInput(value,true);
   if(!keyboardHeld||!keyboardPhysicalDown)throw new Exception("Modified Mouse 5 failed");
   ApplyShortcutInput(new VoiceInput{kind="mouse",key=6,down=false},true);
   if(keyboardHeld||keyboardPhysicalDown)throw new Exception("Mouse release stuck");
   ApplyShortcutInput(new VoiceInput{kind="mouse",key=6,down=true,modifiers=0},true);
   if(keyboardHeld)throw new Exception("Wrong modifiers accepted");
   ApplyShortcutInput(new VoiceInput{kind="mouse",key=6,down=false},true);
   ApplyShortcutInput(value,false);if(keyboardHeld)throw new Exception("Other application triggered voice");
   keyboardPhysicalDown=true;ApplyShortcutInput(value,true);if(keyboardHeld)throw new Exception("Repeat armed voice");
   if(CurrentShortcutEvent(value,8,20)||CurrentShortcutEvent(value,7,261)||!CurrentShortcutEvent(value,7,260))throw new Exception("Stale/configuration input accepted");
   policy.captureWindow=window;policy.captureAt=9;
   if(!QueueVoiceInput(policy,"keyboard",0x79,true,false,false,window,4,10))throw new Exception("Binding capture did not own input");
   inputQueue.TryDequeue(out value);Interlocked.Decrement(ref inputQueued);
   if(!value.capture||value.key!=0x79||value.modifiers!=4||value.kind!="keyboard")throw new Exception("Binding capture changed");
   if(QueueVoiceInput(policy,"keyboard",0x79,true,false,false,new IntPtr(100),4,10)||inputQueued!=0)throw new Exception("Capture stole another app's input");
   policy.captureWindow=IntPtr.Zero;policy.reviewWindow=window;
   if(!QueueVoiceInput(policy,"keyboard",0x1B,true,false,false,window,0,10))throw new Exception("Review Escape not queued");
   inputQueue.TryDequeue(out value);Interlocked.Decrement(ref inputQueued);if(!value.cancelReview)throw new Exception("Review Escape route changed");
   for(int i=0;i<InputCapacity+10;i++)QueueVoiceInput(policy,"mouse",6,true,false,false,window,1,10);
   if(inputQueued!=InputCapacity||inputOverflow!=1)throw new Exception("Input backlog not bounded");
   // A stalled UI cannot replay presses or interpret a delayed release as send.
   inputPolicy=policy;keyboardHeld=true;recordSource="keyboard";phase="idle";DrainVoiceInputs();
   if(keyboardHeld||inputQueued!=0||!latched||heldAt!=0)throw new Exception("Stall did not require a fresh press");
   voiceShortcut=new VoiceShortcut();keyboardPhysicalDown=false;keyboardHeld=false;
   ApplyShortcutInput(new VoiceInput{kind="keyboard",key=0x77,down=true,modifiers=0},true);
   if(!keyboardHeld)throw new Exception("F8 input changed");
   ApplyShortcutInput(new VoiceInput{kind="keyboard",key=0x77,down=false},true);if(keyboardHeld)throw new Exception("F8 release changed");
  }finally{
   VoiceInput value;while(inputQueue.TryDequeue(out value))Interlocked.Decrement(ref inputQueued);
   voiceShortcut=saved;voiceEnabled=savedEnabled;inputPolicy=null;keyboardHeld=false;keyboardPhysicalDown=false;inputOverflow=0;phase="idle";recordSource="";
  }
  Console.WriteLine("PASS: Mouse 5/F8 press/release, modifiers, repeat/focus/injection isolation, binding capture, review Escape, bounded backlog and stale-press rejection.");
 }
}
