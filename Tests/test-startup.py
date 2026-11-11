from pathlib import Path
import os, json, subprocess, tempfile, time, uuid

r=Path(__file__).resolve().parent.parent
csc=Path(os.environ['WINDIR'])/'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
result=[]
with tempfile.TemporaryDirectory(prefix='Startup-',dir=r/'Tests') as temp:
 d=Path(temp); app=d/'App'; app.mkdir(); game=d/'Game';game.mkdir()
 def build(exe,sources,main=None):
  args=[str(csc),'/nologo','/codepage:65001','/target:exe','/platform:x64','/r:System.Web.Extensions.dll','/out:'+str(exe)]
  if main:args+=['/main:'+main]
  p=subprocess.run(args+list(map(str,sources)),capture_output=True,text=True)
  assert p.returncode==0,p.stdout+p.stderr
 def run(exe,*args):
  p=subprocess.run([str(exe),*args],capture_output=True,text=True,timeout=20)
  assert p.returncode==0,p.stdout+p.stderr
  return p.stdout
 src=r/'Packaging/Launcher.cs'; framework=r/'Packaging/Framework.cs'
 prod=d/'LauncherPolicy.exe';build(prod,[src,framework])
 result.append({'name':'shipping launcher policy','passed':True,'output':run(prod,'--self-test')})
 # The OS adapter remains production code. Change named synchronization objects
 # only, so these real process tests cannot contact a user's companion/launcher.
 ns='PadChatFixture.'+uuid.uuid4().hex+'.'
 isolated=d/'Launcher.cs';isolated.write_text(src.read_text().replace('PadChat.',ns),encoding='utf-8')
 launcher=app/'PadChatLauncher.exe';build(launcher,[isolated,framework])
 # Fake speech helper records launches, owns only fixture events, and never
 # loads models, hooks input, captures audio, uses clipboard or sends to WoW.
 helper=d/'Helper.cs';helper.write_text(r'''
using System;using System.IO;using System.Diagnostics;using System.Threading;
[assembly:System.Reflection.AssemblyInformationalVersion("0.2.2-preview.15")]
class FakeVoice {
 static void Main(string[] args){
  string root=Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar));
  bool created;using(var owner=new Mutex(true,"NAMESPACEVoice.v1",out created)){
   if(!created){File.AppendAllText(Path.Combine(root,"requests.txt"),string.Join(" ",args)+"\n");return;}
   File.AppendAllText(Path.Combine(root,"starts.txt"),Process.GetCurrentProcess().Id+"\n");
   using(var stop=new EventWaitHandle(false,EventResetMode.AutoReset,"NAMESPACEStop.Process."+Process.GetCurrentProcess().Id))while(!stop.WaitOne(100)){}
  }
 }
}
'''.replace('NAMESPACE',ns),encoding='utf-8')
 build(app/'PadChatVoice.exe',[helper])
 game_source=d/'Game.cs';game_source.write_text('using System;[assembly:System.Reflection.AssemblyFileVersion("1.60.1.70888")] class FakeGame {static void Main(){Console.ReadLine();}}')
 build(game/'Wow.exe',[game_source])
 (d/'install.json').write_text(json.dumps({'Game':str(game),'Version':'0.2.2-preview.15','Voice':True,'Startup':True}))
 def count():return len((d/'starts.txt').read_text().splitlines()) if (d/'starts.txt').exists() else 0
 def wait(check,description,seconds=12):
  end=time.monotonic()+seconds
  while time.monotonic()<end:
   if check():return
   time.sleep(.1)
  raise AssertionError(description)
 def status():
  try:return json.loads((d/'launcher-status.json').read_text())['state']
  except (OSError,ValueError):return ''
 processes=[]
 def start(exe,*args):
  p=subprocess.Popen([str(exe),*args],stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
  processes.append(p);return p
 def stop_game(p):p.communicate(input=b'\n',timeout=5)
 try:
  watcher=start(launcher)
  wait(lambda:status()=='waiting-for-wow','startup did not wait')
  assert count()==0
  duplicate=start(launcher);assert duplicate.wait(timeout=5)==0
  g=start(game/'Wow.exe');wait(lambda:count()==1,'game launch did not start helper')
  manual=start(launcher,'--microphone');assert manual.wait(timeout=5)==0
  wait(lambda:(d/'requests.txt').exists(),'menu request not forwarded')
  assert '--microphone' in (d/'requests.txt').read_text() and count()==1
  stop_game(g);wait(lambda:status()=='waiting-for-wow','game exit did not close helper')
  g=start(game/'Wow.exe');wait(lambda:count()==2,'game relaunch did not restart helper')
  # Kill only the PID written by our isolated fake helper, verifying its image.
  pid=int((d/'starts.txt').read_text().splitlines()[-1])
  killer=d/'KillFixture.cs';killer.write_text('using System;using System.Diagnostics;class KillFixture {static void Main(string[] a){using(var p=Process.GetProcessById(int.Parse(a[0]))){if(!string.Equals(p.MainModule.FileName,a[1],StringComparison.OrdinalIgnoreCase))throw new Exception("Not fixture");p.Kill();p.WaitForExit(5000);}}}')
  build(d/'KillFixture.exe',[killer]);run(d/'KillFixture.exe',str(pid),str(app/'PadChatVoice.exe'))
  wait(lambda:count()==3,'crashed helper did not restart')
  run(launcher,'--stop');assert watcher.wait(timeout=5)==0
  stop_game(g)
  # Stopping supervisor cannot silently kill a live/manual helper; installer
  # performs a separate cooperative stop. Simulate that exact scoped signal.
  signal=d/'Signal.cs';signal.write_text('using System;using System.Threading;class Signal {static void Main(string[] a){using(var e=EventWaitHandle.OpenExisting(a[0]))e.Set();}}')
  build(d/'Signal.exe',[signal]);pid=int((d/'starts.txt').read_text().splitlines()[-1]);run(d/'Signal.exe',ns+'Stop.Process.'+str(pid))
  time.sleep(.3)
  result.append({'name':'isolated OS lifecycle','passed':True,'checks':['login waits','duplicate launcher exits','exact launch client starts helper','menu forwards without duplicate','game exit closes helper','game relaunch','unexpected helper exit recovers','cooperative launcher stop']})
 finally:
  # Fixture namespace and recorded process path only. Never touch real PadChat.
  for p in processes:
   if p.poll() is None:p.terminate();p.wait(timeout=5)
  if (d/'starts.txt').exists() and (d/'KillFixture.exe').exists():
   for pid in (d/'starts.txt').read_text().splitlines():
    subprocess.run([str(d/'KillFixture.exe'),pid,str(app/'PadChatVoice.exe')],capture_output=True)
(r/'STARTUP-TEST-REPORT.json').write_text(json.dumps(result,indent=2)+'\n')
print('PASS: production startup policy and isolated real-process launch/relaunch/crash/menu/exit lifecycle. No user input, audio or real game access.')
