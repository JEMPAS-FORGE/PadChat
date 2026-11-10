using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
partial class VoicePTT {
 static string fixtureClipboard="";
 static void FixtureSetClipboard(string value){fixtureClipboard=value;}
 static void FixtureClearClipboard(){fixtureClipboard="";}
 static void CheckExperience(bool condition,string why){if(!condition)throw new Exception(why);}
 public static void ExperienceChecks(string output){
  string token,cmd,dest;bool test,recover;
  CheckExperience(ParseProbeDetails("PCVQ2;1.5;NORMAL;%2F7;Trade",out token,out test,out recover,out cmd,out dest)&&cmd=="/7"&&dest=="Trade"&&!test&&!recover,"Trade probe");
  CheckExperience(ParseProbeDetails("PCVQ2;1;NORMAL;%2Fw Friend-Realm;Reply to Friend-Realm",out token,out test,out recover,out cmd,out dest)&&cmd=="/w Friend-Realm","Whisper probe");
  foreach(string wire in new[]{"PCVQ2;1;NORMAL;%2Frun;bad","PCVQ2;1;NORMAL;%QQ;bad","PCVQ2;1;NORMAL;%2Fs;bad%0Atext","PCVQ2;1;NORMAL;%2Fs;bad%7Ctext","PCVQ2;no;NORMAL;%2Fs;Say","PCVQ2;1;SEND;%2Fs;Say"})
   CheckExperience(!ParseProbeDetails(wire,out token,out test,out recover,out cmd,out dest),"Unsafe probe accepted");
  foreach(string wire in new[]{"PCA1;99;TOP;1;0","PCA1;16;OTHER;1;0","PCA1;16;TOP;yes;0","PCA1;16;TOP;1;0;extra"}){
   bool rejected=false;try{ParseAppearance(wire);}catch(ArgumentException){rejected=true;}CheckExperience(rejected,"Unsafe appearance record accepted");
  }
  var settings=ParseGameSettings("PCV3;mouse;6;0;;1;KEEP;SHARE;NONE;0;");
  gameSettingsManaged=true;voiceShortcutConfigured=true;voiceShortcut=settings.shortcut;voiceEnabled=true;voiceReview=false;voicePad="SHARE";voicePadMod="NONE";voiceVocabulary="";
  AtomicVoiceFile(GameSettingsPath(),json.Serialize(settings));CheckExperience(VoiceSettingsAlreadyApplied(settings),"Unchanged voice preferences must not restart capture engine");
  voiceShortcut=new VoiceShortcut{kind="mouse",key=5,modifiers=0};CheckExperience(!VoiceSettingsAlreadyApplied(settings),"Runtime shortcut change must be reapplied");voiceShortcut=settings.shortcut;
  string preferenceFixture=Path.Combine(dataRoot,"AppearanceFixture.lua");
  File.WriteAllText(preferenceFixture,"PadChatDB = { [\"appearanceWire\"] = \"PCA1;20;BOTTOM;0;1\" }");
  ReadGameAppearance(preferenceFixture);CheckExperience(appearance.size==20&&appearance.position=="BOTTOM"&&!appearance.meter&&appearance.sound&&VoiceSettingsAlreadyApplied(settings),"Appearance-only update isolation");
  File.WriteAllText(preferenceFixture,"PadChatDB = { [\"appearanceWire\"] = \"PCA1;99;TOP;1;0\" }");
  try{ReadGameAppearance(preferenceFixture);}catch(ArgumentException){}CheckExperience(appearance.size==20&&appearance.position=="BOTTOM","Invalid appearance must retain old settings");
  appearance=ParseAppearance("PCA1;16;TOP;1;1");int sounds=0;readySoundAction=()=>{sounds++;};sid=101;readyCueSession=-1;
  badge=new Badge();var handle=badge.Handle;phase="recording";overlayMicReady=false;microphoneTest=false;recordingCommand="/7";recordingLabel="Trade";
  ApplyCaptureProgress("listening",new Dictionary<string,object>{{"id",101}});CheckExperience(sounds==0,"Stream-open cue too early");
  ApplyCaptureProgress("level",new Dictionary<string,object>{{"id",101},{"level",0}});ApplyCaptureProgress("level",new Dictionary<string,object>{{"id",101},{"level",80}});
  CheckExperience(sounds==1&&badge.label.Text.Contains("To: Trade"),"Ready sound must be once at actual samples with destination");
  appearance.sound=false;sid++;CaptureReadySound();CheckExperience(sounds==1,"Sound off ignored");
  foreach(int size in new[]{14,16,20,24}){
   foreach(string pos in new[]{"TOP","CENTER","BOTTOM"}){
    appearance=ParseAppearance("PCA1;"+size+";"+pos+";1;0");ApplyOverlayFont();
    var bounds=VoiceOverlayBounds(new Rectangle(-1920,0,1920,1080),new Rectangle(-1920,0,1920,1040));
    CheckExperience(bounds.Left>=-1920&&bounds.Right<=0&&bounds.Top>=0&&bounds.Bottom<=1040,"Overlay outside active monitor");
    badge.Bounds=bounds;badge.SetMicrophoneState(true,true,70,Now,Now);RecordingStatus();
    if(pos=="TOP")badge.ComposeFrame().Save(Path.Combine(output,"overlay-"+size+".png"));
   }
  }
  appearance=new AppearanceSettings();microphoneTest=false;recognizedForRecovery="Recovered café words";RetainUnsent(false);
  CheckExperience(RecoveryRecord("1").Contains("Recovered%20caf%C3%A9%20words")&&!recoveryUncertain,"UTF8 recovery");
  microphoneTest=true;recognizedForRecovery="private mic test";RetainUnsent(true);CheckExperience(recoveryText=="Recovered café words","Mic tests must not enter recovery");microphoneTest=false;
  InvalidateSpeechSession();CheckExperience(recognizedForRecovery==""&&recoveryText=="Recovered café words","Recovery retention across session invalidation");
  foreach(string result in new[]{"REFUSED","ERROR","MISSING","ISSUED"}){
   recognizedForRecovery="Fixture "+result;recoveryText="older failure";recoveryUncertain=false;sendAttempted=true;
   directVoiceToken="1";directVoiceId="7";stage=24;label="Trade";text=recognizedForRecovery;
   fixtureClipboard=result=="MISSING"?"unrelated clipboard":"PCVD_ACK1:1;7;"+result+";0";AdvanceDirectVoice();
   CheckExperience(result=="ISSUED"?recoveryText=="older failure":recoveryText=="Fixture "+result,"Send recovery classification");
   if(result!="ISSUED")CheckExperience(recoveryUncertain==(result!="REFUSED"),"Uncertain send must be marked");
  }
  fixtureSameGame=true;fixtureCommand="";probeResult=false;probeStage=2;probeDeadline=0;phase="probe";
  recoveryText="Only copy this";recoveryUncertain=true;fixtureClipboard="PCVQ2;22;RECOVER;%2Fs;SAY";AdvanceVoiceProbe(true);
  CheckExperience(fixtureClipboard=="PCVRD1;22;1;Only%20copy%20this"&&probeStage==3,"Recovery response");
  probeDeadline=0;AdvanceVoiceProbe(true);
  CheckExperience(phase=="idle"&&fixtureClipboard=="Only copy this"&&fixtureCommand=="","Recovery must copy, never record or send");
  badge.Dispose();badge=null;
  Console.WriteLine("PASS: strict appearance and v2/legacy protocol, ready sound once on audio callback, 12 position/size combinations, destination label, UTF-8 session recovery, no mic-test retention, known/uncertain send classification, requested copy without recording or resend.");
 }
}
class ExperienceFixture {
 [STAThread]static int Main(string[] args){try{VoicePTT.ExperienceChecks(args[0]);return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
