// Developer test entry point, never part of the installed helper or Setup UI.
using System;
using System.IO;
class DiscoveryStateTest {
 static int Main(string[] args) {
  if(args.Length!=1)return 2;
  string root=Path.GetFullPath(args[0]);
  if(Directory.Exists(root))throw new IOException("Test needs a new directory");
  Directory.CreateDirectory(root);var engine=new Engine(root,true);
  foreach(string bad in new[]{"not json","[]","null","{\"Game\":\"\\\"E:\\\\bad\\\"\"}"}) {
   File.WriteAllText(engine.StatePath,bad);
   if(engine.ReadState()!=null||engine.StateWarning==null||File.ReadAllText(engine.StatePath)!=bad)throw new Exception("Corrupt state recovery failed");
  }
  Console.WriteLine("PASS: malformed/wrong-type/null/invalid-path install records recover without altering their bytes.");return 0;
 }
}
