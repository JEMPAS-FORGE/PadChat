// Set the framework target explicitly when using the bundled legacy csc.
// This opts into modern .NET 4.8 path handling before any static IO initialises.
[assembly:System.Runtime.Versioning.TargetFramework(".NETFramework,Version=v4.8",FrameworkDisplayName=".NET Framework 4.8")]

static class WoWClients {
 public static bool IsProcess(string name){return string.Equals(name,"Wow",System.StringComparison.OrdinalIgnoreCase)||string.Equals(name,"WowB",System.StringComparison.OrdinalIgnoreCase);}
 public static bool SupportedVersion(string value){return (value??"").StartsWith("1.60.",System.StringComparison.Ordinal);}
 public static bool SupportedExecutable(string path){try{return SupportedVersion(System.Diagnostics.FileVersionInfo.GetVersionInfo(path).FileVersion);}catch{return false;}}
 // Wow.exe is also used by Retail: the executable name alone is not a client check.
 static readonly object Gate=new object();
 static readonly System.Collections.Generic.Dictionary<string,bool> Verified=new System.Collections.Generic.Dictionary<string,bool>();
 public static bool IsGame(System.Diagnostics.Process process){
  try{
   if(!IsProcess(process.ProcessName))return false;
   string key=process.Id+":"+process.StartTime.ToUniversalTime().Ticks;
   lock(Gate){bool result;if(Verified.TryGetValue(key,out result))return result;
    result=SupportedExecutable(process.MainModule.FileName);
    if(Verified.Count>=16)Verified.Clear();Verified[key]=result;return result;
   }
  }catch{return false;}
 }
 public static string ExecutablePath(string folder){
  string first=null;
  foreach(string name in new[]{"Wow.exe","WowB.exe"}){string path=System.IO.Path.Combine(folder,name);
   if(!System.IO.File.Exists(path))continue;if(first==null)first=path;if(SupportedExecutable(path))return path;
  }
  return first;
 }
 public static bool IsForeverFolder(string folder){string exe=ExecutablePath(folder);return exe!=null&&SupportedExecutable(exe);}
 public static bool AnyClientRunning(){
  foreach(string name in new[]{"Wow","WowB"})foreach(var process in System.Diagnostics.Process.GetProcessesByName(name))using(process){return true;}
  return false;
 }
}
