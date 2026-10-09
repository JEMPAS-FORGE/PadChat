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

 public static string FindGame() {

  var paths=new List<string>();

  foreach(string root in new[]{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}) {

   string clean=Normalize(root);if(clean!=null)paths.Add(Path.Combine(clean,"World of Warcraft","_classic_beta_"));

  }

  foreach(string value in RegistryPaths()) {

   string clean=Normalize(value);if(clean==null)continue;

   paths.Add(clean);paths.Add(Path.Combine(clean,"_classic_beta_"));

  }

  return Choose(paths,p=>LongFile.Exists(Path.Combine(p,"WowB.exe")));

 }

 public static void SelfTest() {

  string good=Path.Combine(Path.GetTempPath(),"Game with spaces — test");int tried=0;

  string found=Choose(new[]{"\"E:\\Unrelated\"","E:\\bad\nentry","relative",good},p=>{tried++;return p==good;});

  if(found!=good||tried!=1)throw new Exception("Discovery accepted malformed registry metadata");

  found=Choose(new[]{good+"-offline",good},p=>{if(p.EndsWith("-offline"))throw new IOException("Unavailable drive");return true;});

  if(found!=good||Choose(new[]{"\"invalid\""},p=>true)!="")throw new Exception("Discovery did not recover to Browse");

  Console.WriteLine("PASS: malformed/quoted/control-character/relative registry paths, unavailable candidates and Browse fallback.");

 }

}

