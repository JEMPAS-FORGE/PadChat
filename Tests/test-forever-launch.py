from pathlib import Path
import json, os, subprocess, tempfile
root=Path(__file__).resolve().parent.parent
csc='C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe'
with tempfile.TemporaryDirectory(prefix='ForeverLaunch-',dir=root/'Tests') as temp:
 d=Path(temp)
 def build(out,sources,main=None):
  args=[csc,'/nologo','/codepage:65001','/target:exe','/platform:x64','/out:'+str(out),'/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Web.Extensions.dll','/r:System.IO.Compression.dll','/r:System.IO.Compression.FileSystem.dll','/r:Microsoft.CSharp.dll']
  if main:args+=['/main:'+main]
  result=subprocess.run(args+list(map(str,sources)),capture_output=True,text=True)
  assert result.returncode==0,result.stdout+result.stderr
 for flavor,version in [('forever','1.60.1.123'),('retail','12.0.0.123'),('classic','1.15.8.123')]:
  area=d/flavor;area.mkdir();src=area/'Fixture.cs'
  src.write_text('using System;[assembly:System.Reflection.AssemblyFileVersion("'+version+'") ] class Fixture {static void Main(){Console.WriteLine("ready");Console.ReadLine();}}')
  build(area/'Wow.exe',[src])
 harness=d/'LaunchAudit.cs'
 harness.write_text(r"""
using System;using System.IO;using System.Linq;using System.Diagnostics;using System.Windows.Forms;
class LaunchAudit {
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 [STAThread]static int Main(string[] args){
  string root=args[0],valid=Path.Combine(root,"forever","Wow.exe"),retail=Path.Combine(root,"retail","Wow.exe"),classic=Path.Combine(root,"classic","Wow.exe");
  var engine=new Engine(Path.Combine(root,"unused-profile"),false);
  engine.ValidateGame(Path.GetDirectoryName(valid));
  foreach(string bad in new[]{retail,classic}){bool refused=false;try{engine.ValidateGame(Path.GetDirectoryName(bad));}catch(IOException){refused=true;}Check(refused,"Wrong Wow.exe version accepted");}
  string parent=Path.Combine(root,"World of Warcraft"),launch=Path.Combine(parent,"_forever_"),beta=Path.Combine(parent,"_classic_beta_");
  Directory.CreateDirectory(launch);Directory.CreateDirectory(beta);File.Copy(valid,Path.Combine(launch,"Wow.exe"));File.Copy(valid,Path.Combine(beta,"WowB.exe"));
  engine.ValidateGame(launch);engine.ValidateGame(beta);
  Check(InstallerDiscovery.Choose(InstallerDiscovery.Candidates(new string[0],new string[0],beta),WoWClients.IsForeverFolder)==launch,"Stale beta install did not migrate to sibling launch");
  Check(InstallerDiscovery.Choose(InstallerDiscovery.Candidates(new[]{root},new string[0],null),WoWClients.IsForeverFolder)==launch,"Default launch discovery failed");
  Check(InstallerDiscovery.Choose(InstallerDiscovery.Candidates(new string[0],new[]{parent},null),WoWClients.IsForeverFolder)==launch,"Custom drive registry discovery failed");
  Check(InstallerDiscovery.Choose(InstallerDiscovery.Candidates(new string[0],new[]{Path.GetDirectoryName(retail),parent},null),WoWClients.IsForeverFolder)==launch,"Discovery accepted Retail Wow.exe");
  Check(InstallerDiscovery.Choose(InstallerDiscovery.Candidates(new string[0],new[]{parent},Path.GetDirectoryName(valid)),WoWClients.IsForeverFolder)==Path.GetDirectoryName(valid),"Valid custom saved folder replaced");
  File.Delete(Path.Combine(launch,"Wow.exe"));
  Check(InstallerDiscovery.Choose(InstallerDiscovery.Candidates(new string[0],new string[0],beta),WoWClients.IsForeverFolder)==beta,"Legacy Forever fallback failed");
  bool missing=false;try{engine.ValidateGame(Path.Combine(root,"missing"));}catch(IOException e){missing=e.Message.Contains("Wow.exe")&&e.Message.Contains("_forever_");}Check(missing,"Missing launch diagnostic");
  using(var form=new SetupForm(new Engine(Path.Combine(root,"unused-form"),true),new string[0])){
   Check(form.Controls.Cast<Control>().Any(c=>c.Text=="WoW Forever client folder (contains Wow.exe)"),"Old installer label remains");
   Check(!form.Text.Contains("\u00e2\u20ac"),"Title encoding regression");
  }
  foreach(string exe in new[]{valid,retail,classic,Path.Combine(beta,"WowB.exe")}){
   var info=new ProcessStartInfo(exe){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true};
   using(var child=Process.Start(info))try{
    Check(child.StandardOutput.ReadLine()=="ready","Fixture failed to start");
    bool expected=exe==valid||exe.EndsWith("WowB.exe");
    Check(WoWClients.IsGame(child)==expected,"Companion client recognition failed: "+exe);
    Check(WoWClients.AnyClientRunning(),"Installer missed running Wow/WowB");
    bool refused=false;try{engine.ValidateGame(Path.GetDirectoryName(valid));}catch(IOException e){refused=e.Message.Contains("Close WoW");}Check(refused,"Installer allowed update with game open");
   }finally{child.StandardInput.WriteLine();child.StandardInput.Flush();if(!child.WaitForExit(3000))child.Kill();}
  }
  Console.WriteLine("PASS: production Wow.exe / legacy WowB.exe validation, Retail/Classic rejection, saved beta migration, default and registry discovery, custom path preservation, form labels, foreground client detection and running-game refusal. No installed files/settings changed.");
  return 0;
 }
}
""",encoding='utf-8')
 exe=d/'LaunchAudit.exe'
 build(exe,[harness,*[root/'Packaging'/n for n in ['Setup.cs','Discovery.cs','Framework.cs','LongIO.cs']]],'LaunchAudit')
 run=subprocess.run([str(exe),str(d)],capture_output=True,text=True,timeout=30)
 assert run.returncode==0,run.stdout+run.stderr
 print(run.stdout)
 (root/'FOREVER-LAUNCH-TEST.json').write_text(json.dumps({'passed':True,'output':run.stdout,'actualProductionSources':True,'noInstalledGameChanges':True},indent=2))
