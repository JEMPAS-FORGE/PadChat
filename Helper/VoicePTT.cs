// User-requested Create/Share hold-to-talk and release-to-send for WoW.
// Speech is processed locally. The helper only sends following a physical PTT
// release, in the same foreground WoW window, after a fresh channel handshake
// and exact clipboard readback from the native chat input.
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Windows.Forms;

partial class VoicePTT {
 [StructLayout(LayoutKind.Sequential)] struct JoyState {public uint size,flags,x,y,z,r,u,v,buttons,buttonNumber,pov,reserved1,reserved2;}
 [DllImport("winmm.dll")] static extern uint joyGetNumDevs();
 [DllImport("winmm.dll")] static extern uint joyGetPosEx(uint id,ref JoyState state);
 [StructLayout(LayoutKind.Sequential)] struct XGamepad {public ushort buttons;public byte leftTrigger,rightTrigger;public short lx,ly,rx,ry;}
 [StructLayout(LayoutKind.Sequential)] struct XState {public uint packet;public XGamepad gamepad;}
 [DllImport("xinput1_4.dll")] static extern uint XInputGetState(uint index,out XState state);
 [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hwnd,out uint pid);
 [StructLayout(LayoutKind.Sequential)] struct Keyboard {public ushort key,scan;public uint flags,time;public UIntPtr extra;}
 [StructLayout(LayoutKind.Explicit)] struct InputUnion {[FieldOffset(0)]public Keyboard keyboard;[FieldOffset(0)]public Mouse mouse;}
 [StructLayout(LayoutKind.Sequential)] struct Mouse {public int x,y;public uint data,flags,time;public UIntPtr extra;}
 [StructLayout(LayoutKind.Sequential)] struct Input {public uint type;public InputUnion data;}
 [DllImport("user32.dll",SetLastError=true)] static extern uint SendInput(uint count,Input[] inputs,int size);
 static void Keys(params ushort[] keys) {
  var inputs=new List<Input>();
  foreach(ushort key in keys)inputs.Add(new Input{type=1,data=new InputUnion{keyboard=new Keyboard{key=key}}});
  for(int i=keys.Length-1;i>=0;i--)inputs.Add(new Input{type=1,data=new InputUnion{keyboard=new Keyboard{key=keys[i],flags=2}}});
  if(SendInput((uint)inputs.Count,inputs.ToArray(),Marshal.SizeOf(typeof(Input)))!=inputs.Count)throw new Exception("Windows rejected keyboard input");
 }
 static void Bridge(ushort key){Keys(0x11,0x10,key);}
 static bool IsGame(IntPtr hwnd){uint pid;GetWindowThreadProcessId(hwnd,out pid);try{return WoWClients.IsProcess(Process.GetProcessById((int)pid).ProcessName);}catch{return false;}}
 class Caption:Label {
  protected override void OnPaint(PaintEventArgs e){
   var flags=TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.WordBreak|TextFormatFlags.NoPrefix;
   var bounds=ClientRectangle;
   foreach(var offset in new Point[]{new Point(-1,0),new Point(1,0),new Point(0,-1),new Point(0,1)}){
    var shadow=bounds;shadow.Offset(offset);
    TextRenderer.DrawText(e.Graphics,Text,Font,shadow,Color.Black,flags);
   }
   TextRenderer.DrawText(e.Graphics,Text,Font,bounds,ForeColor,flags);
  }
 }
 class Badge:Form {
  public Label label=new Caption();
  protected override bool ShowWithoutActivation{get{return true;}}
  protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x08000000;return p;}}
  public Badge(){FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;BackColor=Color.FromArgb(25,27,32);
   TransparencyKey=BackColor;
   Size=new Size(900,160);StartPosition=FormStartPosition.Manual;Location=new Point((Screen.PrimaryScreen.Bounds.Width-Width)/2,55);
   label.Dock=DockStyle.Fill;label.ForeColor=Color.White;label.Font=new Font("Segoe UI",16);label.TextAlign=ContentAlignment.MiddleCenter;Controls.Add(label);}
 }
 static System.Threading.EventWaitHandle stopEvent;
 static Badge badge;static Timer timer;static Process worker;
 static readonly JavaScriptSerializer json=new JavaScriptSerializer();
 static readonly ConcurrentQueue<string> messages=new ConcurrentQueue<string>();
 static readonly Stopwatch clock=Stopwatch.StartNew();
 static bool ready,latched,released;static int sid,stage,level;
 static long heldAt,deadline,hideAt,started;static string phase="idle",text,command,label,line,oldClip,marker,preview="",inviteName;static bool characterInvite;
 static IntPtr target,pressWindow;static uint joystick=uint.MaxValue;static string inputSource="none",pressSource="",recordSource="";
 static long workerRetryAt;static int workerFailures;
 static long Now{get{return clock.ElapsedMilliseconds;}}
 static void Status(string message,int ms){badge.label.Text=message;badge.Show();hideAt=ms==0?0:Now+ms;}
 static string PreviewTail(){return preview.Length>220?"..."+preview.Substring(preview.Length-220):preview;}
 static void RecordingStatus(){
  badge.label.Text=microphoneTest?"Microphone test — nothing will be sent | Level: "+level+"%\n"+PreviewTail():preview.Length>0?"Live preview — release "+TalkButton()+" to send\n"+PreviewTail():
   "Recording — "+(level<4?"waiting for speech":level<16?"quiet microphone signal":"voice detected")+"\nWords will appear here as you speak";
 }
 static void WriteWorker(string cmd){if(worker!=null&&!worker.HasExited){worker.StandardInput.WriteLine(json.Serialize(new {cmd=cmd,id=sid}));worker.StandardInput.Flush();}}
 static bool SameGame(){return target==GetForegroundWindow()&&IsGame(target);}
 static string Clip(){return Clipboard.ContainsText()?Clipboard.GetText():"";}
 static bool ValidPrefix(string value){return Regex.IsMatch(value,@"^/(?:s|y|p|ra|g|o|i|rw|e|invite|ck voiceinvite|[1-9][0-9]?|w [^\s/|;\p{Cc}]{1,100})$");}
 static bool IsCharacterName(string value){return Regex.IsMatch(value??"",@"\A\p{L}[\p{L}'-]{0,59}\z");}
 static string SpokenInvite(string value){
  var match=Regex.Match(value??"",@"\Ainvite\s+(.+?)\s*[.!?]*\z",RegexOptions.IgnoreCase);
  if(!match.Success)return null;
  string name=Regex.Replace(match.Groups[1].Value,@"\s+to (?:my |the )?(?:party|group)\s*[.!?]*\z","",RegexOptions.IgnoreCase).Trim().TrimEnd('.', '!', '?');
  if(name.Length==0||name.Length>100||Regex.IsMatch(name,@"[/;|\p{Cc}]"))return "";
  return name;
 }
 static bool TryDestination(string value,out string destination,out string destinationLabel){
  destination=null;destinationLabel=null;
  bool v2=value.StartsWith("CKPTT_READY2:");
  if(!v2&&!value.StartsWith("CKPTT_READY:"))return false;
  string[] fields=value.Split(v2?';':'|');
  if(fields.Length!=3||!ValidPrefix(fields[1])||fields[2].Length==0)return false;
  destination=fields[1];destinationLabel=fields[2];return true;
 }
 static string Clean(string value,int maxBytes){value=Regex.Replace(value??"",@"[\p{Cc}\s|]+"," ").Trim();
  while(Encoding.UTF8.GetByteCount(value)>maxBytes)value=value.Substring(0,value.Length-1);
  return value.TrimEnd();}
 static bool Chord(uint buttons){return (buttons&0x100)!=0;}
 // Normalize XInput Back (the streamed Create/Share button) into the legacy
 // DS4 Share mask used by the existing hold/release state machine.
 [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] struct JoyCaps {
  public ushort manufacturer,product;
  [MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)] public string name;
  public uint xmin,xmax,ymin,ymax,zmin,zmax,numButtons,periodMin,periodMax,rmin,rmax,umin,umax,vmin,vmax,caps,maxAxes,numAxes,maxButtons;
  [MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)] public string regKey;
  [MarshalAs(UnmanagedType.ByValTStr,SizeConst=260)] public string oem;
 }
 [DllImport("winmm.dll",CharSet=CharSet.Unicode)] static extern uint joyGetDevCapsW(UIntPtr id,out JoyCaps caps,uint size);
 static bool SupportedNativeController(uint id){JoyCaps caps;return joyGetDevCapsW(new UIntPtr(id),out caps,(uint)Marshal.SizeOf(typeof(JoyCaps)))==0&&caps.manufacturer==0x054c;}
 static uint NormalizeXInput(ushort buttons){return ((buttons&0x2000)!=0?2u:0u)|((buttons&0x0020)!=0?0x100u:0u)|((buttons&0x0010)!=0?0x200u:0u)|((buttons&0x8000)!=0?0x8u:0u)|((buttons&0x0100)!=0?0x10u:0u)|((buttons&0x0200)!=0?0x20u:0u)|((buttons&0x0040)!=0?0x400u:0u)|((buttons&0x0080)!=0?0x800u:0u);}
 static void Cancel(string reason){probeStage=0;RestoreProbeClipboard();pendingTestWords="";WriteWorker("cancel");sid++;phase="idle";stage=0;released=false;
  if(SameGame())Bridge(0x7A);Status(reason,4000);}
 static void Start(){
  target=GetForegroundWindow();if(!IsGame(target))return;recordSource=pressSource;BeginVoiceProbe(false);
 }
 static void BeginRecording(){
  target=GetForegroundWindow();if(!IsGame(target))return;
  if(!ready){Status("The offline speech model is still loading",2500);return;}
  recordSource=pressSource;
  sid++;phase="recording";released=false;level=0;preview="";started=Now;WriteWorker("start");
  Status(microphoneTest?"Microphone test: hold and speak; release to see results. Nothing is sent.":"Hold "+TalkButton()+" and speak\nRelease to "+(voiceReview?"review your message":"send to your last chat channel"),0);
 }
 static void Stop(){if(phase!="recording")return;released=true;phase="processing";WriteWorker("stop");
  Status("Finishing your message locally..."+(preview.Length>0?"\n"+PreviewTail():""),0);}
 static void BeginDelivery(){if(!SameGame()||!released){Cancel("Message cancelled: WoW lost focus");return;}
  inviteName=SpokenInvite(text);
  if(inviteName==""){Cancel("Say invite followed by a friend or character name");return;}
  characterInvite=inviteName!=null&&IsCharacterName(inviteName);
  oldClip=Clip();Bridge(inviteName==null?(ushort)0x79:characterInvite?(ushort)0x72:(ushort)0x73);stage=1;deadline=Now+160;phase="delivery";}
 static void AdvanceDelivery(){
  if(!SameGame()){Cancel("Message cancelled: WoW lost focus");return;}
  if(stage==1){Keys(0x11,0x43);stage=2;deadline=Now+130;return;}
  if(stage==2){
   marker=Clip();
   if(marker.StartsWith("CKPTT_DENIED:")){string[] p=marker.Split('|');Cancel(p.Length>1?p[1]:"Chat destination unavailable");return;}
   if((!marker.StartsWith("CKPTT_READY:")&&!marker.StartsWith("CKPTT_READY2:"))||marker==oldClip){Cancel("Not sent: close any text box or menu and try again");return;}
   if(!TryDestination(marker,out command,out label)){Cancel("Not sent: invalid chat destination");return;}
   if(inviteName==null?(command=="/invite"||command=="/ck voiceinvite"):
      characterInvite?command!="/invite":command!="/ck voiceinvite"){
    Cancel("Not sent: invite command did not match");return;
   }
   if(inviteName!=null)text=inviteName;
   text=Clean(text,245-Encoding.UTF8.GetByteCount(command)-1);
   if(text.Length==0){Cancel("No speech recognised; nothing sent");return;}
   line=command+" "+text;Bridge(0x7A);stage=3;deadline=Now+120;return;
  }
  if(stage==3){Bridge(0x78);stage=4;deadline=Now+180;return;}
  if(stage==4){Clipboard.SetText(line);Keys(0x11,0x56);stage=5;deadline=Now+180;return;}
  if(stage==5){Clipboard.SetText("CKPTT_VERIFY:"+sid);Keys(0x11,0x41);Keys(0x11,0x43);stage=6;deadline=Now+140;return;}
  if(stage==6){
   string echo=Clip();
   // Some WoW versions consume the channel prefix as soon as it is pasted.
   if(echo!=line&&echo!=text){Cancel("Not sent: chat input could not be verified");return;}
   Keys(0x0D);stage=7;deadline=Now+200;Status(inviteName!=null?"Looking up invite: "+inviteName:"Submitted to "+label+": "+text,4500);return;
  }
  if(stage==7){Clipboard.SetText("CKPTT_EXIT_WAIT:"+sid);Bridge(0x76);stage=8;deadline=Now+150;return;}
  if(stage==8){Keys(0x11,0x43);stage=9;deadline=Now+130;return;}
  if(stage==9){
   string exitState=Clip();Bridge(0x7A);
   if(Regex.IsMatch(exitState,@"^CKPTT_EXIT:[0-9.]+\|1$")){stage=10;deadline=Now+120;return;}
   stage=11;deadline=Now+100;return;
  }
  if(stage==10){Bridge(0x75);stage=11;deadline=Now+100;return;}
  if(stage==11){if(oldClip.Length>0&&Clip().StartsWith("CKPTT_EXIT"))Clipboard.SetText(oldClip);stage=0;phase="idle";released=false;}
 }
 static uint ReadButtons(out bool connected){
  XState xs;uint xbuttons=0,xindex=0;int xcount=0;
  for(uint i=0;i<4;i++)if(XInputGetState(i,out xs)==0){xcount++;xindex=i;xbuttons=NormalizeXInput(xs.gamepad.buttons)|(xs.gamepad.leftTrigger>128?0x40u:0u)|(xs.gamepad.rightTrigger>128?0x80u:0u);}
  if(xcount==1){inputSource="XInput:"+xindex;connected=true;return xbuttons;}
  // Do not accidentally dictate using another player's controller.
  if(xcount>1){inputSource="ambiguous";connected=false;return 0;}
  uint nativeButtons;if(ReadNativeSony(out connected,out nativeButtons))return nativeButtons;
  JoyState state=new JoyState{size=(uint)Marshal.SizeOf(typeof(JoyState)),flags=0xFF};
  if(joystick!=uint.MaxValue&&SupportedNativeController(joystick)&&joyGetPosEx(joystick,ref state)==0){inputSource="WinMM:"+joystick;connected=true;return state.buttons;}
  joystick=uint.MaxValue;uint candidate=0;int count=0;
  for(uint i=0;i<joyGetNumDevs();i++)if(SupportedNativeController(i)&&joyGetPosEx(i,ref state)==0){candidate=i;count++;}
  if(count==1){joystick=candidate;joyGetPosEx(joystick,ref state);inputSource="WinMM:"+joystick;connected=true;return state.buttons;}
  inputSource="none";connected=false;return 0;
 }
 static void Tick(object sender,EventArgs args){
  try{
   MaintainInputListener();
   DrainVoiceInputs();
   string message;while(messages.TryDequeue(out message)){
    var data=json.Deserialize<Dictionary<string,object>>(message);string kind=(string)data["type"];
    if(kind=="microphones"){ApplyGameMicrophoneList(data);UpdateMicrophones(data);continue;}
    if(kind=="ready"){ready=true;workerFailures=0;WriteVocabulary();if(micRecoveryPending){micRecoveryPending=false;Status("Speech helper refreshed. Reconnect the same microphone, release PTT and press again. Nothing resumed automatically.",6000);}WriteHealth("ready");continue;}
    if(!data.ContainsKey("id")||Convert.ToInt32(data["id"])!=sid)continue;
    if(kind=="level"){level=Convert.ToInt32(data["level"]);if(phase=="recording")RecordingStatus();}
    else if(kind=="partial"){
     if(phase!="recording")continue;
     string update=Clean((string)data["text"],2000);
     if(update.Length>0){preview=update;RecordingStatus();}
    }
    else if(kind=="result"){
     if(phase!="processing")continue;text=(string)data["text"];
     if(text.Trim().Length==0){Cancel("No clear speech recognised; nothing sent");continue;}
     if(microphoneTest){pendingTestWords=text;BeginVoiceProbe(true);}
     else if(RequiresReview(false,voiceReview)){phase="review";reviewWaitingRelease=true;Status("Review — press PTT again to send, Escape to cancel\n"+Clean(text,700),0);}
     else {phase="result";Status("Recognised: "+text+"\nSending to your last chat channel...",0);}
    }else if(kind=="error"){
     Cancel("Voice: "+(string)data["message"]);
     if(data.ContainsKey("microphone")&&Convert.ToBoolean(data["microphone"]))RecoverMicrophone();
    }
   }
   if(hideAt!=0&&Now>=hideAt){badge.Hide();hideAt=0;}
   PollGameSettings();
   if(voiceEnabled)MaintainWorker();
   string previousSource=inputSource;
   bool connected;uint buttons=ReadButtons(out connected);bool controllerChord=connected&&ControllerVoiceChord(buttons);
   PollKeyboardToggle(connected,buttons,previousSource==inputSource);
   if(phase=="review"&&connected&&(buttons&2)!=0){Cancel("Review cancelled; nothing sent");return;}
   if(!voiceEnabled){heldAt=0;latched=true;return;}
   bool keyboardChord=voiceEnabled&&keyboardHeld;
   bool chord=pressSource=="keyboard"?keyboardChord:pressSource=="controller"?controllerChord:keyboardChord||controllerChord;
   if(bindingEvent!=null&&bindingEvent.WaitOne(0))ShowVoiceBindings();
   if(stopEvent!=null&&stopEvent.WaitOne(0)){Application.Exit();return;}
   if(microphoneEvent!=null&&microphoneEvent.WaitOne(0))ShowMicrophones();
   if(microphoneWindow!=null&&!microphoneWindow.IsDisposed||shortcutWindow!=null&&!shortcutWindow.IsDisposed){heldAt=0;latched=chord;return;}
   if(previousSource!=inputSource){
    ControllerRecoveryNotice(previousSource,inputSource,controllerChord);
    if(ReconnectCancels(previousSource,inputSource,recordSource,phase))Cancel("Message cancelled: controller changed");
    if(pressSource!="keyboard"&&!keyboardChord){heldAt=0;latched=ReconnectMustRelease(controllerChord);pressSource=controllerChord?"controller":"";chord=controllerChord;}
   }
   if(phase!="idle"&&(!SameGame()||RequiresController(recordSource)&&!connected)){Cancel(!SameGame()?"Message cancelled: WoW lost focus":"Message cancelled: controller disconnected");}
   if(!chord){
    if(ShouldCycleTap(pressSource,connected,latched,heldAt,Now,phase,pressWindow==GetForegroundWindow()&&IsGame(pressWindow)))Bridge(0x77);
    heldAt=0;latched=false;pressSource="";if(phase=="recording")Stop();
   }
   else if(!latched){
    if(heldAt==0){heldAt=Now;pressWindow=GetForegroundWindow();pressSource=keyboardChord?"keyboard":"controller";}
    else if(Now-heldAt>=300){latched=true;if(phase=="idle"&&pressWindow==GetForegroundWindow())Start();}
   }
   if(phase=="recording"&&Now-started>29000)Cancel("Recording limit reached; try a shorter message");
   if(phase=="review"){if(!chord)reviewWaitingRelease=false;else if(!reviewWaitingRelease){reviewWaitingRelease=true;BeginDelivery();}}
   AdvanceVoiceProbe(chord);
   if(phase=="result"&&!chord)BeginDelivery();
   if(stage!=0&&Now>=deadline)AdvanceDelivery();
  }catch(Exception ex){try{Cancel("Voice error: "+ex.Message);}catch{phase="idle";stage=0;}}
  finally{PublishInputPolicy();}
 }
 [STAThread] static void Main(string[] args){
  if(args.Length==2&&args[0]=="--render-bindings"){RenderVoiceBindingWindow(args[1]);return;}
  if(args.Length>0&&args[0]=="--probe-input"){
   bool connected;uint buttons=ReadButtons(out connected);
   Console.WriteLine("Controller="+inputSource+" connected="+connected+" Share="+Chord(buttons));return;
  }
  if(args.Length>0&&args[0]=="--input-thread-test"){TestInputThread();return;}
  if(args.Length>0&&args[0]=="--self-test"){
   TestInputRouting();
   TestVoiceRecovery();TestVoiceDiagnostics();
   TestGameVoiceSettings();
   TestVoiceBindings();
   TestKeyboardVoice();
   TestNativeSony();
   if(Marshal.SizeOf(typeof(XState))!=16||!Chord(NormalizeXInput(0x0020))||!Chord(NormalizeXInput(0x8020)))throw new Exception("XInput Share mapping failed");
   for(int bit=0;bit<16;bit++)if(bit!=5&&Chord(NormalizeXInput((ushort)(1<<bit))))throw new Exception("Non-Share button triggered dictation");
   if(!Chord(0x100)||Chord(0x208)||Chord(0x8)||!ValidPrefix("/p")||ValidPrefix("/run")||ValidPrefix("/w Bad\nName")||Clean("hi\nthere |",100)!="hi there")throw new Exception("PTT safety test failed");
   string testDestination,testLabel;
   if(SpokenInvite("Invite Friendly Player.")!="Friendly Player"||SpokenInvite("invite Friendly Player to my group!")!="Friendly Player"
    ||SpokenInvite("Could you invite me?")!=null||SpokenInvite("invite Bad/quit")!=""
    ||!ValidPrefix("/ck voiceinvite")||!ValidPrefix("/invite")||ValidPrefix("/ck invitekey")
    ||!IsCharacterName("Radish")||!IsCharacterName("Name-Realm")||IsCharacterName("Friendly Player")
    ||IsCharacterName("Bad/quit"))throw new Exception("Voice invite parser test failed");
   if(!TryDestination("CKPTT_READY2:1;/w First-Last;Reply to First Last",out testDestination,out testLabel)||testDestination!="/w First-Last"||testLabel!="Reply to First Last"
    ||!TryDestination("CKPTT_READY:1|/p|PARTY",out testDestination,out testLabel)
    ||TryDestination("CKPTT_READY:1|/w First-Lasteply to First Last",out testDestination,out testLabel)
    ||TryDestination("CKPTT_READY2:1;/run;Reply",out testDestination,out testLabel)
    ||TryDestination("CKPTT_READY2:1;/w First-Last;Reply;extra",out testDestination,out testLabel))throw new Exception("PTT destination handshake test failed");
   Console.WriteLine("PTT chord, channel whitelist and single-line tests passed.");return;
  }
  Directory.CreateDirectory(dataRoot);
  if(args.Length>0&&args[0]=="--stop"){
   try{using(var ev=System.Threading.EventWaitHandle.OpenExisting("PadChat.Stop.v1"))ev.Set();}catch(System.Threading.WaitHandleCannotBeOpenedException){}
   return;
  }
  bool created;using(var mutex=new System.Threading.Mutex(true,"PadChat.Voice.v1",out created)){
   if(!created){
    if(args.Length>0&&args[0]=="--bindings"){
     using(var request=System.Threading.EventWaitHandle.OpenExisting("PadChat.Bindings.v1"))request.Set();
    }
    if(args.Length>0&&args[0]=="--microphone"){
     using(var request=System.Threading.EventWaitHandle.OpenExisting("PadChat.Microphone.v1"))request.Set();
    }
    return;
   }
   Application.EnableVisualStyles();badge=new Badge();var h=badge.Handle;
   microphoneEvent=new System.Threading.EventWaitHandle(false,System.Threading.EventResetMode.AutoReset,"PadChat.Microphone.v1");
   bindingEvent=new System.Threading.EventWaitHandle(false,System.Threading.EventResetMode.AutoReset,"PadChat.Bindings.v1");
   stopEvent=new System.Threading.EventWaitHandle(false,System.Threading.EventResetMode.AutoReset,"PadChat.Stop.v1");
   LoadVoiceShortcut();LoadGameVoiceSettings();SetupMicrophoneMenu();
   try{InstallKeyboardVoice();}catch(Exception ex){inputRetryAt=Now+5000;WorkerError("Input listener: "+ex.Message);WriteHealth("input-error");Status("Voice input could not start. Retrying shortly.\n"+ex.Message,8000);}
   try{if(voiceEnabled)StartWorker();}catch(Exception ex){WorkerError(ex.Message);Status("PadChat voice could not start. Run Setup again to repair.\n"+ex.Message,8000);}
   if(args.Length>0&&args[0]=="--bindings")ShowVoiceBindings();
   if(args.Length>0&&args[0]=="--microphone")ShowMicrophones();
   timer=new Timer{Interval=20};timer.Tick+=Tick;timer.Start();
   try{Application.Run();}finally{RemoveKeyboardVoice();CloseNativeSony();StopWorker();if(microphoneTray!=null)microphoneTray.Dispose();microphoneEvent.Dispose();bindingEvent.Dispose();stopEvent.Dispose();}
  }
 }
}
