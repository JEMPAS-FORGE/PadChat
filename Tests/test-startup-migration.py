from pathlib import Path
import subprocess,tempfile
r=Path(__file__).resolve().parent.parent
with tempfile.TemporaryDirectory(prefix='StartupMigration-',dir=r/'Tests') as t:
 d=Path(t);source=d/'Audit.cs';exe=d/'Audit.exe'
 source.write_text(r'''
using System;using System.IO;using System.Reflection;
class Audit {
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static void Main(string[] args){
  string root=args[0],app=Path.Combine(root,"App");Directory.CreateDirectory(app);
  var engine=new Engine(root,true);var method=typeof(Engine).GetMethod("Integrate",BindingFlags.Instance|BindingFlags.NonPublic);
  var state=new InstallState{Game=Path.Combine(root,"game"),Voice=true,Startup=true,Version="0.2.2-preview.13"};
  string startup=Path.Combine(root,"test-os","Startup","PadChat.lnk"),menu=Path.Combine(root,"test-os","StartMenu");
  method.Invoke(engine,new object[]{state});Check(File.ReadAllText(startup).Contains("PadChatVoice.exe"),"Old package rollback points at missing launcher");
  File.WriteAllText(Path.Combine(app,"PadChatLauncher.exe"),"fixture");state.Version=Engine.Version;method.Invoke(engine,new object[]{state});
  Check(File.ReadAllText(startup).Contains("PadChatLauncher.exe")&&File.ReadAllText(Path.Combine(menu,"PadChat Voice.lnk")).Contains("--voice"),"New package bypasses launcher");
  state.Startup=false;method.Invoke(engine,new object[]{state});Check(!File.Exists(startup),"Opt-out ignored");
  state.Startup=true;File.Delete(Path.Combine(app,"PadChatLauncher.exe"));state.Version="0.2.2-preview.13";method.Invoke(engine,new object[]{state});
  Check(File.ReadAllText(startup).Contains("PadChatVoice.exe")&&!File.ReadAllText(startup).Contains("PadChatLauncher.exe"),"Old shortcut restore failed");
  state.Voice=false;state.Startup=false;method.Invoke(engine,new object[]{state});
  Check(!File.Exists(startup)&&!File.Exists(Path.Combine(menu,"PadChat Voice.lnk"))&&!File.Exists(Path.Combine(menu,"Choose microphone.lnk")),"Addon-only retained voice entry");
  Console.WriteLine("PASS: old/new package shortcut selection, pre-launcher rollback, startup opt-out and addon-only removal via production integration code in isolated directories.");
 }
}
''',encoding='utf-8')
 args=['C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe','/nologo','/codepage:65001','/target:exe','/main:Audit','/platform:x64','/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Web.Extensions.dll','/r:System.IO.Compression.dll','/r:System.IO.Compression.FileSystem.dll','/r:Microsoft.CSharp.dll','/out:'+str(exe),str(source),*[str(r/'Packaging'/n) for n in ['Setup.cs','Discovery.cs','Framework.cs','LongIO.cs']]]
 p=subprocess.run(args,capture_output=True,text=True);assert p.returncode==0,p.stdout+p.stderr
 p=subprocess.run([str(exe),str(d/'profile')],capture_output=True,text=True,timeout=20);assert p.returncode==0,p.stdout+p.stderr
 print(p.stdout)
