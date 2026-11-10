using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
partial class VoicePTT {
 static bool fixtureSameGame=true;
 static string fixtureCommand="",fixtureStatus="";
 static void CheckCue(bool ok,string why){if(!ok)throw new Exception(why);}
 static Dictionary<string,object> Progress(int id,int value){return new Dictionary<string,object>{{"id",id},{"level",value}};}
 static void ResetCue(){sid=7;phase="recording";overlayMicReady=false;level=0;overlayLevelAt=0;preview="";microphoneTest=false;probeOwned=null;directVoiceToken=null;fixtureSameGame=true;fixtureCommand="";released=false;fixtureStatus="";}
 public static void TestReadyCue(string folder){
  Application.EnableVisualStyles();
  using(badge=new Badge()){
   badge.Location=new Point(-20000,-20000);ResetCue();RecordingStatus();
   CheckCue(!badge.label.Text.Contains("SPEAK NOW")&&badge.label.ForeColor==Color.FromArgb(255,207,105),"Startup invited speech too early");
   badge.SetMicrophoneState(true,false,0,0,1000);badge.ComposeFrame().Save(System.IO.Path.Combine(folder,"getting-ready.png"),ImageFormat.Png);
   CheckCue(ApplyCaptureProgress("listening",Progress(7,0))&&!overlayMicReady,"Stream open alone gave green cue");
   CheckCue(ApplyCaptureProgress("level",Progress(6,80))&&!overlayMicReady,"Old session gave green cue");
   CheckCue(ApplyCaptureProgress("level",Progress(7,0))&&overlayMicReady,"First silent audio callback did not confirm capture");
   CheckCue(badge.label.Text.StartsWith("READY - SPEAK NOW")&&badge.label.ForeColor==Color.FromArgb(103,235,188),"Ready text/colour missing");
   badge.SetMicrophoneState(true,true,0,1000,1000);badge.ComposeFrame().Save(System.IO.Path.Combine(folder,"ready-speak.png"),ImageFormat.Png);
   ApplyCaptureProgress("listening",Progress(7,0));CheckCue(overlayMicReady,"Out-of-order stream-start cleared real readiness");
   string heading=badge.label.Text;ApplyCaptureProgress("level",Progress(7,90));CheckCue(badge.label.Text==heading,"Volume makes ready heading flicker");
   phase="processing";overlayMicReady=false;ApplyCaptureProgress("level",Progress(7,80));CheckCue(!overlayMicReady,"Late level revived readiness after release");
   ResetCue();Stop();CheckCue(phase=="idle"&&fixtureCommand=="cancel"&&fixtureStatus.Contains("before the microphone was ready"),"Early release tried recognition/sending");
   ResetCue();overlayMicReady=true;Stop();CheckCue(phase=="processing"&&fixtureCommand=="stop"&&released,"Ready release stopped normal send flow");
   ResetCue();phase="probe";probeStage=3;probeDeadline=0;probeResult=false;voiceEnabled=true;ready=true;
   AdvanceVoiceProbe(false);CheckCue(phase=="idle"&&fixtureCommand==""&&fixtureStatus.Contains("green cue"),"Released probe started capture or remained waiting forever");
   ResetCue();phase="probe";probeStage=1;probeDeadline=0;fixtureSameGame=false;hideAt=123;
   AdvanceVoiceProbe(true);CheckCue(phase=="idle"&&hideAt==0&&!badge.Visible,"Focus-loss left pending cue visible");
   ResetCue();microphoneTest=true;ApplyCaptureProgress("level",Progress(7,0));CheckCue(badge.label.Text.Contains("microphone test")&&badge.label.Text.Contains("Nothing will be sent"),"Test-mode warning lost");
   Console.WriteLine("PASS: amber before actual samples; green on same-session callback including silence; old/late events ignored; stable heading; early release/focus-loss clear preparation; normal release and mic-test isolation preserved. No audio/input used.");
  }
 }
}
class ReadyCueFixture{[STAThread]static void Main(string[] args){VoicePTT.TestReadyCue(args[0]);}}
