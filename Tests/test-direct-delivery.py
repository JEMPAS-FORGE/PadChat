from pathlib import Path
import os,json,subprocess
root=Path(__file__).resolve().parent.parent
source=(root/'Helper/VoicePTT.cs').read_text(encoding='utf-8')
def method(name):
 start=source.index(' static '+name); brace=source.index('{',start);depth=1;end=brace+1
 while depth:
  depth+=(source[end]=='{')-(source[end]=='}');end+=1
 return source[start:end]
methods='\n'.join(method(n) for n in ['bool ValidPrefix','bool IsCharacterName','string SpokenInvite','bool TryDestination','string Clean','void BeginDelivery','void AdvanceDelivery','void Cancel'])
harness=r'''
using System;using System.Collections.Generic;using System.Text;using System.Text.RegularExpressions;
partial class VoicePTT {
 static int stage,sid=7,modifiers,sends,opens,enters,toggles;static long Now,deadline;
 static string text,command,label,line,oldClip,marker,inviteName,phase,edit,ready,body,result;
 static bool released,characterInvite,sameGame,focused,bad,apiError,dropAck,ui;
 static string Clip(){return Clipboard.value;}static bool SameGame(){return sameGame;}
 static void InvalidateSpeechSession(){sid++;phase="idle";stage=0;released=false;}
 static void RestoreProbeClipboard(){}static void WriteWorker(string value){}
 static void Status(string value,int duration){result=value;}
 static int CurrentShortcutModifiers(){return modifiers;}static short GetAsyncKeyState(int key){return 0;}
 static void Bridge(ushort key){
  if(key==0x79||key==0x72||key==0x73){edit=ready;focused=true;}
  else if(key==0x7A||key==0x74)focused=false;
  else if(key==0x78){opens++;edit="";focused=true;}
  else if(key==0x75){toggles++;focused=false;}
  else if(key==0x70){
   if(focused||body==null)throw new Exception("Send attempted without staged body/focus cleanup");
   if(!apiError)sends++;
   edit=dropAck?"unavailable":DirectVoiceAck()+(apiError?"ERROR;":"ISSUED;")+(ui?"1":"0");focused=true;
  }
 }
 static void Keys(params ushort[] keys){
  if(keys.Length==2&&keys[1]==0x56){
   var p=Clipboard.value.Split(';');body=Uri.UnescapeDataString(p[4]);
   if(p.Length!=6||DirectVoiceChecksum(p[3]+"\n"+body).ToString()!=p[5])throw new Exception("Bad wire checksum");
   edit=DirectVoiceAck()+"READY";
  }else if(keys.Length==2&&keys[1]==0x43)Clipboard.SetText(bad&&stage==20?"bad":edit);
  else if(keys.Length==1&&keys[0]==0x0D)enters++;
 }
 static void Run(string destination,string phrase,int problem=0,bool controllerUI=false,string original="original clipboard"){
  stage=sends=opens=enters=toggles=0;sid=7;Now=deadline=0;phase="result";text=phrase;
  ready=destination;released=sameGame=true;focused=false;body=null;bad=problem==1;apiError=problem==3;dropAck=problem==4;modifiers=problem==5?1:0;ui=controllerUI;
  Clipboard.value=original;BeginDelivery();
  for(Now=20;Now<5000&&phase!="idle";Now+=20){
   // Production Lua freezes READY/ACK markers against held WASD input.
   if(focused&&!edit.StartsWith("PCVD_ACK"))edit=ready;
   if(problem==2&&stage==21)sameGame=false;
   if(stage!=0&&Now>=deadline)AdvanceDelivery();
  }
  if(phase!="idle"||stage!=0||released)throw new Exception("Delivery failed to finish/cancel");
  int expected=(problem==1||problem==2||problem==3||problem==5)?0:1;
  if(sends!=expected||opens!=0||enters!=0)throw new Exception("Wrong send count or opened native chat/Enter");
  if(problem==0&&(body!=phrase||Clipboard.value!=original||focused))throw new Exception("Body/clipboard/focus cleanup failed");
  if(controllerUI&&problem==0&&toggles!=1)throw new Exception("Controller focus not restored once");
 }
 static void Main(){
  TestDirectVoiceWire();
  foreach(string command in new[]{"/s","/y","/p","/ra","/g","/o","/i","/rw","/e","/4","/w Friend-Realm"})
   foreach(bool ui in new[]{false,true})Run("CKPTT_READY3:1.2;"+command+";Destination","Hello caf\u00e9; turn right",0,ui);
  for(int p=1;p<=5;p++)Run("CKPTT_READY3:1.2;/p;Party","Movement test",p);
  Run("CKPTT_READY3:1.2;/s;Say","Empty clipboard",0,false,"");
  oldClip="before";directVoiceToken="1.2";directVoiceId="7";directVoicePacket="owned";Clipboard.value="new user clipboard";
  RestoreDirectVoiceClipboard();if(Clipboard.value!="new user clipboard")throw new Exception("Foreign clipboard overwritten");
  oldClip="";directVoiceToken="1.2";directVoiceId="7";directVoicePacket="owned";Clipboard.value="owned";
  RestoreDirectVoiceClipboard();if(Clipboard.value!=""||directVoiceToken!=null)throw new Exception("Cancelled packet retained");
  string dest,lab;
  if(!TryDestination("CKPTT_READY2:1;/invite;Invite",out dest,out lab)||dest!="/invite")throw new Exception("Legacy invite rejected");
  Console.WriteLine("PASS: actual public helper direct delivery; all channels/whisper UTF-8, held movement, focus/clipboard cleanup, modifier/focus/bad ACK/API failure and uncertain no retry.");
 }
}
class Clipboard{public static string value;public static void SetText(string v){value=v;}public static void Clear(){value="";}}
'''
test=root/'DirectTransport.cs';test.write_text(harness.replace(' static void Main(){',methods+'\n static void Main(){'),encoding='utf-8')
exe=root/'DirectTransport.exe'
subprocess.run(['C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe','/nologo','/codepage:65001','/target:exe','/platform:x64','/out:'+str(exe),str(test),str(root/'Helper/VoiceDirectDelivery.cs')],check=True)
p=subprocess.run([str(exe)],capture_output=True,text=True,timeout=20)
assert p.returncode==0,p.stdout+p.stderr
print(p.stdout)
(root/'DIRECT-TEST-REPORT.json').write_text(json.dumps({'passed':True,'productionMethodsExtracted':True,'compiledTransport':True,'addon':json.loads((root/'Tests/DIRECT-ADDON-TEST.json').read_text()),'livePublicGameplayVerified':False,'personalMouse5MovementConfirmed':True,'noGameInputOrAudio':True},indent=2))
