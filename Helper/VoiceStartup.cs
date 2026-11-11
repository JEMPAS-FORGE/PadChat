using System;
using System.Threading;
using System.Diagnostics;

[assembly:System.Reflection.AssemblyInformationalVersion("0.2.2-preview.14")]

partial class VoicePTT {
 static EventWaitHandle processStopEvent;
 static void RequireStartupRelease(){recoveryAwaitRelease=true;keyboardHeld=false;heldAt=0;latched=true;pressSource="";}
 static void SetupProcessStop(){
  processStopEvent=new EventWaitHandle(false,EventResetMode.AutoReset,"PadChat.Stop.Process."+Process.GetCurrentProcess().Id);
  RequireStartupRelease();
 }
 static bool ProcessStopRequested(){return processStopEvent!=null&&processStopEvent.WaitOne(0);}
 static void ExitVoiceAndLauncher(){
  // An explicit tray Exit is intentional, not a crash to undo immediately.
  try{using(var ev=EventWaitHandle.OpenExisting("PadChat.Launcher.Stop.v1"))ev.Set();}catch(WaitHandleCannotBeOpenedException){}
  System.Windows.Forms.Application.Exit();
 }
 static void TestStartupRelease(){
  RequireStartupRelease();
  if(!RecoveryReleaseGate(true,false)||!recoveryAwaitRelease||!RecoveryReleaseGate(false,true)||!recoveryAwaitRelease)
   throw new Exception("Startup accepted an already held PTT");
  if(!RecoveryReleaseGate(false,false)||recoveryAwaitRelease||RecoveryReleaseGate(false,false))
   throw new Exception("Startup fresh-press gate did not rearm");
  Console.WriteLine("PASS: initial start and crash restart require PTT release before any new message.");
 }
}
