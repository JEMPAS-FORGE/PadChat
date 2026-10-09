// Isolated production-validator regression; no installed game/settings are written.
using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
class InstallerFolderTest {
 [STAThread] static int Main(string[] args) {
  if(args.Length!=4)return 2;
  string root=Path.GetFullPath(args[0]);bool legacy=args[3]=="legacy";
  if(System.IO.Directory.Exists(root))throw new IOException("Test requires a NEW directory");
  System.IO.Directory.CreateDirectory(root);
  var engine=new Engine(Path.Combine(root,"unused-user-profile"),false);
  foreach(string layout in new[]{"shared-parent-data","no-local-data","client-local-data"}) {
   string install=Path.Combine(root,layout,"World of Warcraft");
   string client=Path.Combine(install,"_classic_beta_");
   System.IO.Directory.CreateDirectory(client);
   System.IO.File.Copy(args[1],Path.Combine(client,"WowB.exe"));
   if(layout=="shared-parent-data")System.IO.Directory.CreateDirectory(Path.Combine(install,"Data"));
   if(layout=="client-local-data")System.IO.Directory.CreateDirectory(Path.Combine(client,"Data"));
   bool accepted=true;try{engine.ValidateGame(client);}catch(IOException){accepted=false;}
   if(accepted!=(legacy?layout=="client-local-data":true))throw new Exception("Unexpected layout result: "+layout);
   Console.WriteLine((accepted?"ACCEPTED: ":"REPRODUCED REJECTION: ")+layout);
   bool discovered=InstallerDiscovery.Choose(new[]{client},p=>LongFile.Exists(Path.Combine(p,"WowB.exe")))==client;
   if(!discovered)throw new Exception("Fixture discovery failed");
   if(System.IO.Directory.Exists(Path.Combine(client,"Interface")))throw new Exception("Validation changed game files");
  }
  if(!legacy) {
   string wrong=Path.Combine(root,"wrong-client");System.IO.Directory.CreateDirectory(wrong);
   System.IO.File.Copy(args[2],Path.Combine(wrong,"WowB.exe"));
   bool refused=false;try{engine.ValidateGame(wrong);}catch(IOException){refused=true;}
   if(!refused)throw new Exception("Wrong client accepted");
   refused=false;try{engine.ValidateGame(Path.Combine(root,"missing-client"));}catch(IOException ex){refused=ex.Message.Contains("WowB.exe")&&ex.Message.Contains("missing-client");}
   if(!refused)throw new Exception("Missing executable diagnostic failed");
   using(var form=new SetupForm(new Engine(Path.Combine(root,"unused-form-profile"),true),new string[0])) {
    if(form.Text!="PadChat Setup - "+Engine.Version)throw new Exception("Title encoding regression");
    bool browse=false,label=false;
    foreach(Control c in form.Controls){if(c is Button&&c.Text=="Browse...")browse=true;if(c is Label&&c.Text=="WoW Forever client folder (contains WowB.exe)")label=true;if(c.Text.Contains("\u00e2\u20ac"))throw new Exception("Mojibake UI text");}
    if(!browse||!label)throw new Exception("Installer text regression");
   }
   Console.WriteLine("PASS: production version checks, missing-file diagnostics and actual unshown SetupForm labels; no Data subfolder requirement.");
  }
  return 0;
 }
}
