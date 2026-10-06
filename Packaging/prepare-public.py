"""Create public sources from the tested personal implementation, never live files."""
from pathlib import Path

root = Path(__file__).resolve().parent
personal = root.parent.parent / 'controller-keyboard'
out = root / 'src'
out.mkdir(exist_ok=True)
for name in ('VoicePTT.cs', 'VoiceKeyboard.cs', 'VoiceMicrophone.cs', 'VoiceController.cs',
             'voice_worker.py', 'microphone_inputs.py'):
    text = (personal / name).read_text(encoding='utf-8-sig')
    if name.endswith('.cs'):
        text = text.replace('WoWControllerVoiceDraft', 'PadChat.Voice.v1')
        text = text.replace('WoWControllerVoiceMicrophone', 'PadChat.Microphone.v1')
        text = text.replace('Path.Combine(voiceRoot,"voice-microphone.json")', 'Path.Combine(dataRoot,"voice-microphone.json")')
        text = text.replace('Path.Combine(voiceRoot,"voice-health.json")', 'Path.Combine(dataRoot,"voice-health.json")')
    if name == 'VoiceMicrophone.cs':
        start = text.index(' static ProcessStartInfo PythonInfo(')
        end = text.index(' static void StopWorker()', start)
        text = text[:start] + ''' static readonly string dataRoot=Environment.GetEnvironmentVariable("PADCHAT_DATA_DIR") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"PadChat");
 static ProcessStartInfo PythonInfo(string arguments){
  var info=new ProcessStartInfo(Path.Combine(voiceRoot,@"Backend\\voice-backend.exe"),arguments){
   WorkingDirectory=voiceRoot,UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,
   RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true};
  info.EnvironmentVariables["PADCHAT_DATA_DIR"]=dataRoot;
  info.EnvironmentVariables["HF_HUB_OFFLINE"]="1";
  return info;
 }
 static void WorkerError(string value){
  if(string.IsNullOrWhiteSpace(value))return;
  // Dependency failures only. Do not persist speech or microphone data.
  try{File.WriteAllText(Path.Combine(dataRoot,"backend-error.txt"),value,new UTF8Encoding(false));}catch{}
 }
''' + text[end:]
        text = text.replace('worker.ErrorDataReceived+=(s,e)=>{};', 'worker.ErrorDataReceived+=(s,e)=>WorkerError(e.Data);')
        text = text.replace('Only inputs available on the laptop are listed. An iPad or a PS5 controller connected to the iPad needs a separate microphone link to the laptop.', 'Choose a microphone connected to the Windows PC, such as a built-in microphone or USB headset. PadChat does not configure game audio playback.')
        text = text.replace('WoW Voice Microphone', 'PadChat Microphone')
        text = text.replace('WoW voice — hold F8 / Share', 'PadChat — hold F8 / Share')
        text = text.replace('  microphoneTray=new NotifyIcon', '''  menu.Items.Add("Help / supported connections",null,(s,e)=>Process.Start(Path.Combine(voiceRoot,"START-HERE.txt")));
  menu.Items.Add("Exit PadChat voice",null,(s,e)=>Application.Exit());
  microphoneTray=new NotifyIcon''')
        text = text.replace('  microphoneEnumeration.Start();microphoneEnumeration.BeginOutputReadLine();microphoneEnumeration.BeginErrorReadLine();', '''  try{microphoneEnumeration.Start();microphoneEnumeration.BeginOutputReadLine();microphoneEnumeration.BeginErrorReadLine();}
  catch(Exception ex){microphoneInfo.Text="Could not start microphone settings: "+ex.Message;}''')
    elif name == 'VoicePTT.cs':
        text = text.replace('Process.GetProcessById((int)pid).ProcessName=="WowB"', 'WoWClients.IsProcess(Process.GetProcessById((int)pid).ProcessName)')
        # A random WinMM joystick must not have button 9 treated as Share.
        text = text.replace(' static uint NormalizeXInput(ushort buttons)', ''' [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] struct JoyCaps {
  public ushort manufacturer,product;
  [MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)] public string name;
  public uint xmin,xmax,ymin,ymax,zmin,zmax,numButtons,periodMin,periodMax,rmin,rmax,umin,umax,vmin,vmax,caps,maxAxes,numAxes,maxButtons;
  [MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)] public string regKey;
  [MarshalAs(UnmanagedType.ByValTStr,SizeConst=260)] public string oem;
 }
 [DllImport("winmm.dll",CharSet=CharSet.Unicode)] static extern uint joyGetDevCapsW(UIntPtr id,out JoyCaps caps,uint size);
 static bool SupportedNativeController(uint id){JoyCaps caps;return joyGetDevCapsW(new UIntPtr(id),out caps,(uint)Marshal.SizeOf(typeof(JoyCaps)))==0&&caps.manufacturer==0x054c;}
 static uint NormalizeXInput(ushort buttons)''')
        text = text.replace('if(joystick!=uint.MaxValue&&joyGetPosEx', 'if(joystick!=uint.MaxValue&&SupportedNativeController(joystick)&&joyGetPosEx')
        text = text.replace('if(joyGetPosEx(i,ref state)==0){candidate=i;count++;}', 'if(SupportedNativeController(i)&&joyGetPosEx(i,ref state)==0){candidate=i;count++;}')
        text = text.replace('  bool created;using(var mutex=', '''  Directory.CreateDirectory(dataRoot);
  if(args.Length>0&&args[0]=="--stop"){
   try{using(var ev=System.Threading.EventWaitHandle.OpenExisting("PadChat.Stop.v1"))ev.Set();}catch(System.Threading.WaitHandleCannotBeOpenedException){}
   return;
  }
  bool created;using(var mutex=''')
        text = text.replace('   StartWorker();SetupMicrophoneMenu();InstallKeyboardVoice();', '''   stopEvent=new System.Threading.EventWaitHandle(false,System.Threading.EventResetMode.AutoReset,"PadChat.Stop.v1");
   SetupMicrophoneMenu();InstallKeyboardVoice();
   try{StartWorker();}catch(Exception ex){WorkerError(ex.Message);Status("PadChat voice could not start. Run Setup again to repair.\\n"+ex.Message,8000);}''')
        text = text.replace('   if(microphoneEvent!=null&&microphoneEvent.WaitOne(0))', '   if(stopEvent!=null&&stopEvent.WaitOne(0)){Application.Exit();return;}\n   if(microphoneEvent!=null&&microphoneEvent.WaitOne(0))')
        text = text.replace(' static Badge badge;', ' static System.Threading.EventWaitHandle stopEvent;\n static Badge badge;')
        text = text.replace('microphoneEvent.Dispose();}', 'microphoneEvent.Dispose();stopEvent.Dispose();}')
    elif name == 'voice_worker.py':
        text = text.replace('from faster_whisper import WhisperModel', "import pcm_audio\nsys.modules['faster_whisper.audio'] = pcm_audio\nfrom faster_whisper import WhisperModel")
        text = text.replace('        import av\n', '')
        text = text.replace('ROOT = Path(__file__).resolve().parent', 'ROOT = Path(sys.executable).resolve().parent if getattr(sys, "frozen", False) else Path(__file__).resolve().parent')
        text = text.replace("    main()\n", '''    import multiprocessing
    multiprocessing.freeze_support()
    try:
        main()
    except Exception as exc:
        print('PadChat backend startup failed: ' + str(exc), file=sys.stderr, flush=True)
        sys.exit(1)
''')
    elif name == 'microphone_inputs.py':
        text = text.replace('import json', 'import json\nimport os')
        text = text.replace("SETTINGS = Path(__file__).with_name('voice-microphone.json')", "SETTINGS = Path(os.environ.get('PADCHAT_DATA_DIR', str(Path(os.environ.get('LOCALAPPDATA', str(Path.home()))) / 'PadChat'))) / 'voice-microphone.json'")
        text = text.replace('WoW Voice Microphone settings', 'PadChat Microphone settings')
    (out / name).write_text(text, encoding='utf-8')
print('Public sources generated without changing the running personal helper.')
