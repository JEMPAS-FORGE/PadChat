"""Compile production lifecycle/Tick/settings code with explicit OS boundaries.
Only UI presentation, game discovery, input-thread startup and backend launch
are replaced. Filesystem transactions, process kill/dispose, queue generations,
settings parsing/polling and Tick ordering remain the actual release methods.
"""
from pathlib import Path
import json,os,subprocess,tempfile
root=Path(__file__).resolve().parent.parent
out=root/'Audit/Fixed';out.mkdir(parents=True,exist_ok=True)
def replace_method(source,signature,replacement):
 a=source.index(' static '+signature);b=source.index('{',a);i=b+1;depth=1
 while depth:depth+=(source[i]=='{')-(source[i]=='}');i+=1
 return source[:a]+' static '+signature+replacement+source[i:]
patches={
 'VoiceMicrophone.cs':{'ProcessStartInfo PythonInfo(string arguments)':r'''{
  return new ProcessStartInfo(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName,"--stall"){
   UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true};
 }''','void ShowMicrophones()':'{auditMicrophoneOpened++;}'},
 'VoiceBindings.cs':{'void ShowVoiceBindings()':'{auditBindingOpened++;}'},
 'VoicePTT.cs':{'void Status(string message,int ms)':'{}'},
 'VoiceGameSettings.cs':{'string ForegroundGameFolder()':'{return auditGame;}',
  'string GameStartupPath()':'{return Path.Combine(dataRoot,"startup.lnk");}',
  'void ApplyGameStartup(string setting)':'{if(setting=="KEEP")return;File.WriteAllText(GameStartupPath(),"new startup");File.WriteAllText(Path.Combine(dataRoot,"install.json"),"new install");throw new System.IO.IOException("Injected OS integration failure");}'},
 'VoiceInputListener.cs':{'void StartInputListener()':'{}','void StopInputListener()':'{}'},
}
files=[]
for p in sorted((root/'Helper').glob('*.cs')):
 source=p.read_text(encoding='utf-8-sig')
 for signature,replacement in patches.get(p.name,{}).items():source=replace_method(source,signature,replacement)
 dest=out/p.name;dest.write_text(source,encoding='utf-8');files.append(dest)
harness=r'''
using System;using System.IO;using System.Threading;using System.Diagnostics;using System.Collections.Generic;using System.Windows.Forms;
partial class VoicePTT {
 static int auditBindingOpened,auditMicrophoneOpened;static string auditGame;
 static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
 public static void Audit(){
  Directory.CreateDirectory(dataRoot);auditGame=Path.Combine(dataRoot,"game");
  string folder=Path.Combine(auditGame,"WTF","Account","fixture","SavedVariables");Directory.CreateDirectory(folder);
  string saved=Path.Combine(folder,"PadChat.lua");
  string on="PCV3;mouse;6;0;Fixture%20Mic;1;KEEP;SHARE;NONE;0;";
  string off="PCV3;keyboard;119;0;;0;KEEP;NONE;NONE;0;";
  File.WriteAllText(saved,"{ [\"voiceSettingsWire\"] = \""+on+"\", }");
  PollGameSettings();Check(pendingGameSettings!=null&&microphoneEnumeration!=null,"Selection was not queued");
  var first=microphoneEnumeration;int firstId=first.Id;long obsolete=microphoneGeneration;
  QueueMicrophoneOutput("{not valid JSON}",obsolete);
  File.WriteAllText(saved,"{ [\"voiceSettingsWire\"] = \""+off+"\", }");File.SetLastWriteTimeUtc(saved,DateTime.UtcNow.AddSeconds(2));gameSettingsPollAt=0;
  PollGameSettings();Check(!voiceEnabled&&voiceShortcut.key==119&&pendingGameSettings==null&&microphoneEnumeration==null,"Old mic request survived Off");
  Dictionary<string,object> value;Check(!TryReadVoiceMessage(out value),"Obsolete enum output accepted");
  bool disposed=false;try{var ignored=first.HasExited;}catch(InvalidOperationException){disposed=true;}Check(disposed,"Owned enum handle not disposed");
  // A timed-out owned child cannot block a subsequent refresh or manual restart.
  StartMicrophoneEnumeration();int secondId=microphoneEnumeration.Id;microphoneDeadline=1;
  Thread.Sleep(30);MaintainMicrophoneEnumeration();Check(microphoneEnumeration==null&&pendingGameSettings==null,"Deadline failed");
  StartMicrophoneEnumeration();Check(microphoneEnumeration.Id!=secondId,"Refresh reused hung child");
  RestartVoiceNow();Check(microphoneEnumeration==null&&!voiceEnabled,"Restart retained enumeration or enabled voice");
  // Fault after an earlier write: the second file is genuinely locked on disk.
  string a=Path.Combine(dataRoot,"transaction-a"),b=Path.Combine(dataRoot,"transaction-b");
  File.WriteAllText(a,"old a");File.WriteAllText(b,"old b");bool failed=false;
  using(var locked=new FileStream(b,FileMode.Open,FileAccess.Read,FileShare.Read)){
   try{using(var tx=new VoiceSettingsTransaction(new[]{a,b})){AtomicVoiceFile(a,"new a");AtomicVoiceFile(b,"new b");tx.Commit();}}
   catch(IOException){failed=true;}
  }
  Check(failed&&File.ReadAllText(a)=="old a"&&File.ReadAllText(b)=="old b","Partial transaction not restored");
  string mic=Path.Combine(dataRoot,"voice-microphone.json");File.WriteAllText(mic,"original mic");
  string oldShortcut=File.ReadAllText(ShortcutPath()),oldRuntime=voiceShortcut.kind;
  File.Delete(GameSettingsPath());Directory.CreateDirectory(GameSettingsPath());failed=false;
  try{ApplyGameSettings(ParseGameSettings(on),new MicrophoneChoice{name="Fixture Mic",hostapi="MME"});}catch(IOException){failed=true;}
  Check(failed&&File.ReadAllText(mic)=="original mic"&&File.ReadAllText(ShortcutPath())==oldShortcut&&voiceShortcut.kind==oldRuntime&&!voiceEnabled,"Blocked setting changed configuration");
  Directory.Delete(GameSettingsPath());AtomicVoiceFile(GameSettingsPath(),"old settings");
  AtomicVoiceFile(GameStartupPath(),"old startup");AtomicVoiceFile(Path.Combine(dataRoot,"install.json"),"old install");
  var change=ParseGameSettings(on);change.startup="ON";failed=false;
  try{ApplyGameSettings(change,new MicrophoneChoice{name="Fixture Mic",hostapi="MME"});}catch(IOException){failed=true;}
  Check(failed&&File.ReadAllText(mic)=="original mic"&&File.ReadAllText(ShortcutPath())==oldShortcut
   &&File.ReadAllText(GameSettingsPath())=="old settings"&&File.ReadAllText(GameStartupPath())=="old startup"
   &&File.ReadAllText(Path.Combine(dataRoot,"install.json"))=="old install"&&!voiceEnabled,"Startup failure was not fully rolled back");
  // Full production Tick, disabled plus recovery gate. UI boundaries count only.
  gameSettingsPollAt=long.MaxValue;inputRetryAt=long.MaxValue;toggleLatched=true;recoveryAwaitRelease=true;
  bindingEvent=new EventWaitHandle(true,EventResetMode.AutoReset);microphoneEvent=new EventWaitHandle(true,EventResetMode.AutoReset);
  Tick(null,EventArgs.Empty);
  Check(auditBindingOpened==1&&auditMicrophoneOpened==1&&!bindingEvent.WaitOne(0)&&!microphoneEvent.WaitOne(0)&&!voiceEnabled,"Disabled configuration requests ignored");
  stopEvent=new EventWaitHandle(true,EventResetMode.AutoReset);Tick(null,EventArgs.Empty);Check(!stopEvent.WaitOne(0),"Disabled stop request ignored");
  stopEvent.Dispose();bindingEvent.Dispose();microphoneEvent.Dispose();CloseNativeSony();
  Console.WriteLine("PASS: actual Tick disabled/config/stop/recovery ordering; superseded mic generations; owned hung-child timeout/disposal/restart; actual locked-file rollback and blocked-settings preservation. No audio, game input or OS startup changes.");
 }
}
class AuditMain{[STAThread]static void Main(string[] args){if(args.Length>0&&args[0]=="--stall"){Thread.Sleep(60000);return;}VoicePTT.Audit();}}
'''
(out/'AuditMain.cs').write_text(harness,encoding='utf-8')
exe=out/'AuditNative.exe'
args=['C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe','/nologo','/codepage:65001','/target:exe','/main:AuditMain','/platform:x64','/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Web.Extensions.dll','/r:Microsoft.CSharp.dll','/out:'+str(exe),str(out/'AuditMain.cs'),str(root/'Packaging/Framework.cs'),*map(str,files)]
subprocess.run(args,check=True)
profile=Path(tempfile.mkdtemp(prefix='AuditProfile-',dir=out))
env=dict(os.environ,PADCHAT_DATA_DIR=str(profile),PADCHAT_TEST_ROOT=str(profile))
r=subprocess.run([str(exe)],env=env,capture_output=True,text=True,timeout=30)
assert r.returncode==0,r.stdout+r.stderr
print(r.stdout)
(root/'AUDIT-NATIVE-TEST.json').write_text(json.dumps(dict(passed=True,output=r.stdout,substitutedBoundaries=patches,noAudioOrGameInput=True),indent=2))
