using System;

using System.IO;

using System.Collections.Generic;

using System.Security;

using Microsoft.Win32;

static class InstallerDiscovery {

 public static string Normalize(string value) {

  if(string.IsNullOrWhiteSpace(value))return null;

  value=value.Trim();

  // Registry metadata is untrusted. Do not silently reinterpret malformed paths.

  if(value.IndexOfAny(Path.GetInvalidPathChars())>=0||!Path.IsPathRooted(value))return null;

  try{return Path.GetFullPath(value);}catch(ArgumentException){return null;}catch(NotSupportedException){return null;}catch(IOException){return null;}

 }

 public static string Choose(IEnumerable<string> candidates,Func<string,bool> usable) {

  foreach(string value in candidates) {

   string path=Normalize(value);if(path==null)continue;

   try{if(usable(path))return path;}

   catch(ArgumentException){}catch(NotSupportedException){}catch(IOException){}

   catch(UnauthorizedAccessException){}catch(SecurityException){}

  }

  return "";

 }

 static IEnumerable<string> RegistryPaths() {

  var values=new List<string>();

  foreach(var hive in new[]{Registry.LocalMachine,Registry.CurrentUser}) {

   try{using(var key=hive.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall")) {

    if(key==null)continue;

    foreach(string name in key.GetSubKeyNames()) {

     try{using(var app=key.OpenSubKey(name)) {

      if(app!=null){var value=app.GetValue("InstallLocation","") as string;if(value!=null)values.Add(value);}

     }}catch(UnauthorizedAccessException){}catch(SecurityException){}catch(IOException){}

    }

   }}catch(UnauthorizedAccessException){}catch(SecurityException){}catch(IOException){}

  }

  return values;

 }

 public static IEnumerable<string> Candidates(IEnumerable<string> roots,IEnumerable<string> registered,string previous) {
  var launch=new List<string>();var legacy=new List<string>();
  string saved=Normalize(previous);
  if(saved!=null){
   if(string.Equals(Path.GetFileName(saved.TrimEnd(Path.DirectorySeparatorChar)),"_classic_beta_",StringComparison.OrdinalIgnoreCase))
    launch.Add(Path.Combine(Path.GetDirectoryName(saved.TrimEnd(Path.DirectorySeparatorChar)),"_forever_"));
   else launch.Add(saved);
   legacy.Add(saved);
  }
  foreach(string value in roots){string clean=Normalize(value);if(clean==null)continue;
   launch.Add(Path.Combine(clean,"World of Warcraft","_forever_"));legacy.Add(Path.Combine(clean,"World of Warcraft","_classic_beta_"));
  }
  foreach(string value in registered){string clean=Normalize(value);if(clean==null)continue;
   launch.Add(Path.Combine(clean,"_forever_"));launch.Add(clean);legacy.Add(Path.Combine(clean,"_classic_beta_"));
  }
  launch.AddRange(legacy);return launch;
 }
 public static string FindGame(string previous) {
  return Choose(Candidates(new[]{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)},RegistryPaths(),previous),WoWClients.IsForeverFolder);
 }
 public static string FindGame(){return FindGame(null);}
 public static string InitialFolder(string previous){string found=FindGame(previous);return found.Length>0?found:(Normalize(previous)??"");}

 public static void SelfTest() {

  string good=Path.Combine(Path.GetTempPath(),"Game with spaces — test");int tried=0;

  string found=Choose(new[]{"\"E:\\Unrelated\"","E:\\bad\nentry","relative",good},p=>{tried++;return p==good;});

  if(found!=good||tried!=1)throw new Exception("Discovery accepted malformed registry metadata");

  found=Choose(new[]{good+"-offline",good},p=>{if(p.EndsWith("-offline"))throw new IOException("Unavailable drive");return true;});

  if(found!=good||Choose(new[]{"\"invalid\""},p=>true)!="")throw new Exception("Discovery did not recover to Browse");

  Console.WriteLine("PASS: malformed/quoted/control-character/relative registry paths, unavailable candidates and Browse fallback.");

 }

}

