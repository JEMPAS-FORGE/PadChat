using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Web.Script.Serialization;

[assembly:System.Reflection.AssemblyInformationalVersion("0.2.2-preview.15")]

// No audio, input hooks, clipboard, UI focus changes or game control here.
interface ILauncherHost {
 bool Valid {get;}
 bool GameRunning {get;}
 bool HelperRunning {get;}
 bool OtherVoiceOwner {get;}
 void Start(string argument);
 void Stop();
 void Report(string state);
}

sealed class LauncherLoop {
 readonly ILauncherHost host;
 bool manual,hadGame,wasRunning;
 string pendingArgument="";
 long nextStart,startedAt,noGameSince=-1,nextStop;
 int failures;
 public LauncherLoop(ILauncherHost h){host=h;}
 public void Request(string argument){manual=true;pendingArgument=argument??"";nextStart=0;}
 public static int RetryDelay(int failures){return Math.Min(60000,2000*(1<<Math.Min(5,Math.Max(0,failures-1))));}
 public void Tick(long now){
  if(!host.Valid){host.Report("repair-needed");return;}
  bool game=host.GameRunning,running=host.HelperRunning;
  if(game){hadGame=true;noGameSince=-1;}
  else if(noGameSince<0)noGameSince=now;
  bool wanted=game||(manual&&!hadGame);
  if(running){
   if(!wasRunning)startedAt=now;
   if(now-startedAt>=60000)failures=0;
   wasRunning=true;
   if(pendingArgument!="") {host.Start(pendingArgument);pendingArgument="";}
   if(!wanted&&now-noGameSince>=5000&&now>=nextStop){host.Stop();nextStop=now+5000;host.Report("closing-after-game");}
   else host.Report(wanted?"running":"waiting-for-game-exit");
   return;
  }
  if(wasRunning){
   wasRunning=false;
   if(wanted){failures++;nextStart=now+RetryDelay(failures);}
  }
  if(!wanted){manual=false;hadGame=false;failures=0;nextStart=0;host.Report("waiting-for-wow");return;}
  if(host.OtherVoiceOwner){host.Report("another-companion-running");return;}
  if(now<nextStart){host.Report("restart-delay");return;}
  try{
   host.Start(pendingArgument);pendingArgument="";
   wasRunning=true;startedAt=now;nextStart=now+2000;host.Report("starting");
  }catch(Exception){failures++;nextStart=now+RetryDelay(failures);host.Report("start-failed-retrying");}
 }
}

sealed class InstalledLauncherHost:ILauncherHost {
 readonly string app,root,helper,game;
 readonly bool valid;
 string lastState;DateTime nextWrite;
 public const string Version="0.2.2-preview.15";
 public InstalledLauncherHost(string directory){
  app=Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar);
  root=Path.GetDirectoryName(app);helper=Path.Combine(app,"PadChatVoice.exe");
  try{
   var state=new JavaScriptSerializer().Deserialize<LauncherInstallState>(File.ReadAllText(Path.Combine(root,"install.json")));
   game=Path.GetFullPath(state.Game).TrimEnd(Path.DirectorySeparatorChar);
   // The game executable may be temporarily absent during a Battle.net update.
   // Validate the actual running client later, instead of abandoning startup.
   valid=state.Voice&&state.Version==Version&&Path.IsPathRooted(state.Game)&&
    File.Exists(helper)&&FileVersionInfo.GetVersionInfo(helper).ProductVersion==Version;
  }catch{valid=false;}
 }
 public bool Valid{get{return valid;}}
 public bool GameRunning{get{
  foreach(string name in new[]{"Wow","WowB"})foreach(var p in Process.GetProcessesByName(name))using(p){
   try{if(WoWClients.IsGame(p)&&string.Equals(Path.GetDirectoryName(p.MainModule.FileName),game,StringComparison.OrdinalIgnoreCase))return true;}catch{}
  }
  return false;
 }}
 Process FindHelper(){
  foreach(var p in Process.GetProcessesByName("PadChatVoice")){
   try{if(p.SessionId==Process.GetCurrentProcess().SessionId&&!p.HasExited&&string.Equals(p.MainModule.FileName,helper,StringComparison.OrdinalIgnoreCase))return p;}catch{}
   p.Dispose();
  }
  return null;
 }
 public bool HelperRunning{get{using(var p=FindHelper())return p!=null;}}
 public bool OtherVoiceOwner{get{
  try{using(var m=Mutex.OpenExisting("PadChat.Voice.v1"))return true;}catch(WaitHandleCannotBeOpenedException){return false;}
 }}
 public void Start(string argument){
  if(argument!=""&&argument!="--microphone"&&argument!="--bindings")throw new ArgumentException("Unsupported startup request");
  using(var p=Process.Start(new ProcessStartInfo(helper,argument){WorkingDirectory=app,UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden})){}
 }
 public void Stop(){
  // A PID-specific cooperative signal cannot stop a different old companion.
  using(var p=FindHelper())if(p!=null)try{using(var e=EventWaitHandle.OpenExisting("PadChat.Stop.Process."+p.Id))e.Set();}catch(WaitHandleCannotBeOpenedException){}
 }
 public void Report(string state){
  if(lastState==state&&DateTime.UtcNow<nextWrite)return;
  lastState=state;nextWrite=DateTime.UtcNow.AddSeconds(30);
  try{File.WriteAllText(Path.Combine(root,"launcher-status.json"),new JavaScriptSerializer().Serialize(new{state=state,version=Version,pid=Process.GetCurrentProcess().Id,updated=DateTime.UtcNow.ToString("o")}));}catch{}
 }
 sealed class LauncherInstallState {public string Game,Version;public bool Voice,Startup;}
}

static class PadChatLauncher {
 const string Prefix="PadChat.Launcher.";
 static void Signal(string name){try{using(var e=EventWaitHandle.OpenExisting(Prefix+name+".v1"))e.Set();}catch(WaitHandleCannotBeOpenedException){}}
 static int Main(string[] args){
  if(args.Length==1&&args[0]=="--stop"){Signal("Stop");return 0;}
  if(args.Length==1&&args[0]=="--self-test"){LauncherTests.Run();return 0;}
  string request=args.Length==0?"":args.Length==1?args[0]:"invalid";
  if(request!=""&&request!="--voice"&&request!="--microphone"&&request!="--bindings")return 2;
  bool created;
  using(var mutex=new Mutex(true,Prefix+"Owner.v1",out created)){
   if(!created){if(request!="")Signal(request=="--microphone"?"Microphone":request=="--bindings"?"Bindings":"Open");return 0;}
   using(var stop=new EventWaitHandle(false,EventResetMode.AutoReset,Prefix+"Stop.v1"))
   using(var open=new EventWaitHandle(false,EventResetMode.AutoReset,Prefix+"Open.v1"))
   using(var mic=new EventWaitHandle(false,EventResetMode.AutoReset,Prefix+"Microphone.v1"))
   using(var bindings=new EventWaitHandle(false,EventResetMode.AutoReset,Prefix+"Bindings.v1")){
    var host=new InstalledLauncherHost(AppDomain.CurrentDomain.BaseDirectory);
    if(!host.Valid){host.Report("repair-needed");return 3;}
    var loop=new LauncherLoop(host);var clock=Stopwatch.StartNew();
    if(request!="")loop.Request(request=="--voice"?"":request);
    while(!stop.WaitOne(1000)){
     if(open.WaitOne(0))loop.Request("");
     if(mic.WaitOne(0))loop.Request("--microphone");
     if(bindings.WaitOne(0))loop.Request("--bindings");
     try{loop.Tick(clock.ElapsedMilliseconds);}catch{host.Report("check-failed-retrying");}
    }
    host.Report("stopped");
   }
  }
  return 0;
 }
}

// Exercises the actual policy used by the shipping launcher, with fake OS state.
static class LauncherTests {
 sealed class Host:ILauncherHost {
  public bool valid=true,game,running,other,failStart; public int starts,stops;public string state,argument;
  public bool Valid{get{return valid;}}public bool GameRunning{get{return game;}}
  public bool HelperRunning{get{return running;}}public bool OtherVoiceOwner{get{return other;}}
  public void Start(string arg){starts++;argument=arg;if(failStart)throw new IOException("fixture");running=true;}
  public void Stop(){stops++;running=false;}public void Report(string s){state=s;}
 }
 static void Check(bool b,string message){if(!b)throw new Exception("Launcher: "+message);}
 public static void Run(){
  var h=new Host();var l=new LauncherLoop(h);l.Tick(0);Check(h.starts==0,"idle login starts no speech engine");
  h.game=true;l.Tick(1000);l.Tick(2000);Check(h.starts==1,"launch once; duplicate tick");
  h.game=false;l.Tick(3000);h.game=true;l.Tick(4000);Check(h.stops==0,"transient detection gap");
  h.running=false;l.Tick(5000);l.Tick(6999);Check(h.starts==1,"crash backoff");l.Tick(7000);Check(h.starts==2,"crash restarts");
  h.game=false;l.Tick(8000);l.Tick(13000);Check(h.stops==1,"game exit stops only matching helper");l.Tick(14000);
  h.game=true;l.Tick(15000);Check(h.starts==3,"game relaunch restarts");
  h=new Host{game=true,other=true};l=new LauncherLoop(h);for(int i=0;i<100;i++)l.Tick(i*1000);Check(h.starts==0&&h.state=="another-companion-running","legacy owner blocks spawn storm");
  h.other=false;l.Tick(101000);Check(h.starts==1,"owner released starts correct companion");
  h=new Host{game=true,failStart=true};l=new LauncherLoop(h);l.Tick(0);l.Tick(1999);Check(h.starts==1,"failed spawn bounded");l.Tick(2000);Check(h.starts==2,"failed spawn retries");
  Check(LauncherLoop.RetryDelay(20)==60000,"retry cap");h.failStart=false;l.Tick(6000);Check(h.running,"transient start failure recovers");
  h=new Host();l=new LauncherLoop(h);l.Request("--microphone");l.Tick(0);Check(h.running&&h.argument=="--microphone","manual setup works before WoW");l.Tick(7000);Check(h.stops==0,"configuration remains open before first game");
  h.game=true;l.Tick(8000);h.game=false;l.Tick(9000);l.Tick(14000);Check(h.stops==1,"manual session ends after game");
  h=new Host{valid=false,game=true};l=new LauncherLoop(h);l.Tick(0);Check(h.starts==0&&h.state=="repair-needed","invalid install never launches");
  h=new Host{game=true,running=true};l=new LauncherLoop(h);l.Tick(0);Check(h.starts==0,"adopts correct manual helper");l.Request("--bindings");l.Tick(1000);Check(h.starts==1&&h.argument=="--bindings","duplicate menu request forwarded");
  Console.WriteLine("PASS: login wait, launch/relaunch, detection grace, adoption, duplicate prevention, crash backoff/cap, legacy-owner guard, manual setup, settings requests, invalid-install refusal.");
 }
}
