from pathlib import Path
import json,subprocess,tempfile
root=Path(__file__).resolve().parent.parent;out=root/'Audit/Fixed';out.mkdir(parents=True,exist_ok=True)
source=r'''
using System;using System.IO;using System.Diagnostics;using System.Reflection;using System.Threading;
class InstallerAudit {
 static int Main(string[] args){
  if(args[0]=="--hold"){
   var method=typeof(Engine).GetMethod("AcquireTransaction",BindingFlags.NonPublic|BindingFlags.Static);
   using((IDisposable)method.Invoke(null,null)){Console.WriteLine("locked");Console.ReadLine();}return 0;
  }
  string root=Path.GetFullPath(args[0]);Directory.CreateDirectory(root);
  foreach(string stage in new[]{"addon-new","addon-backup","app-new","app-backup"}){
   string area=Path.Combine(root,stage),game=Path.Combine(area,"game"),data=Path.Combine(area,"profile");
   Directory.CreateDirectory(game);File.WriteAllText(Path.Combine(game,"WowB.exe"),"fixture");
   string target=stage.StartsWith("addon")?Path.Combine(game,"Interface","AddOns","PadChat"+(stage.EndsWith("new")?".padchat-new":".padchat-backup")):Path.Combine(data,stage.EndsWith("new")?"App.new":"App.backup");
   Directory.CreateDirectory(target);string marker=Path.Combine(target,"keep.txt");File.WriteAllText(marker,"preserve recovery file");
   string reason="";try{new Engine(data,true).Install(game,false,false,"missing payload","unused",false);}catch(IOException e){reason=e.Message;}
   if(!reason.Contains("staging folder")||File.ReadAllText(marker)!="preserve recovery file")throw new Exception("Refused transaction removed unowned stage: "+stage);
   if(Directory.GetDirectories(data,"stage-*").Length!=0)throw new Exception("Owned work folder leaked");
  }
  var info=new ProcessStartInfo(Process.GetCurrentProcess().MainModule.FileName,"--hold"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true};
  using(var child=Process.Start(info))try{
   if(child.StandardOutput.ReadLine()!="locked")throw new Exception("Lock child failed");
   foreach(bool uninstall in new[]{false,true}){
    bool refused=false;try{var engine=new Engine(Path.Combine(root,"concurrent"),true);if(uninstall)engine.Uninstall();else engine.Install("missing",false,false,"missing","missing",false);}
    catch(IOException e){refused=e.Message.Contains("Another PadChat");}
    if(!refused)throw new Exception("Concurrent transaction was allowed");
   }
  }finally{child.StandardInput.WriteLine();child.StandardInput.Flush();if(!child.WaitForExit(2000))child.Kill();}
  Console.WriteLine("PASS: all four pre-existing installer stages preserved; owned work cleaned; actual cross-process install/uninstall lock rejects concurrent mutation.");return 0;
 }
}
'''
p=out/'InstallerAudit.cs';p.write_text(source,encoding='utf-8');exe=out/'InstallerAudit.exe'
subprocess.run(['C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe','/nologo','/codepage:65001','/target:exe','/main:InstallerAudit','/platform:x64','/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Web.Extensions.dll','/r:System.IO.Compression.dll','/r:System.IO.Compression.FileSystem.dll','/r:Microsoft.CSharp.dll','/out:'+str(exe),str(p),*[str(root/'Packaging'/n) for n in ['Setup.cs','Discovery.cs','Framework.cs','LongIO.cs']]],check=True)
fixture=Path(tempfile.mkdtemp(prefix='InstallerAudit-',dir=out))
r=subprocess.run([str(exe),str(fixture)],capture_output=True,text=True,timeout=20)
assert r.returncode==0,r.stdout+r.stderr
print(r.stdout)
(root/'AUDIT-INSTALLER-TEST.json').write_text(json.dumps(dict(passed=True,actualInstallerEngine=True,output=r.stdout,noOSIntegration=True),indent=2))
