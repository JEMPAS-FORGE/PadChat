using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
partial class VoicePTT {
 static string directVoiceToken,directVoiceId,directVoiceCopiedAck,directVoiceResult;
 static string directVoicePacket;
 static void RestoreDirectVoiceClipboard(){
  if(directVoiceToken==null)return;
  try{
   string current=Clip();
   if(current==directVoicePacket||current==DirectVoiceAck()+"READY"||
    Regex.IsMatch(current,@"\A"+Regex.Escape(DirectVoiceAck())+@"(ISSUED|ERROR|REFUSED);[01]\z")){
    if(oldClip.Length>0)Clipboard.SetText(oldClip);else Clipboard.Clear();
   }
  }finally{directVoiceToken=directVoiceId=directVoicePacket=directVoiceCopiedAck=null;}
 }
 static bool directVoiceUIFocus;
 static uint DirectVoiceChecksum(string value){
  uint a=1,b=0;foreach(byte v in Encoding.UTF8.GetBytes(value)){a=(a+v)%65521;b=(b+a)%65521;}return (b<<16)|a;
 }
 static string EncodeDirectVoice(string value){
  var result=new StringBuilder();foreach(byte v in Encoding.UTF8.GetBytes(value)){
   if(v>=65&&v<=90||v>=97&&v<=122||v>=48&&v<=57||v==45||v==46||v==95||v==126)result.Append((char)v);
   else result.Append('%').Append(v.ToString("X2"));
  }return result.ToString();
 }
 static bool BeginDirectVoice(string ready){
  if(!ready.StartsWith("CKPTT_READY3:"))return false;
  string token=ready.Substring("CKPTT_READY3:".Length).Split(';')[0];
  if(!Regex.IsMatch(token,@"\A[0-9.]{1,40}\z")||inviteName!=null){Cancel("Not sent: invalid direct chat handshake");return true;}
  directVoiceToken=token;directVoiceId=sid.ToString();
  string packet="PCVD1;"+directVoiceId+";"+token+";"+command+";"+EncodeDirectVoice(text)+";"+DirectVoiceChecksum(command+"\n"+text);
  directVoicePacket=packet;
  Clipboard.SetText(packet);Keys(0x11,0x41);Keys(0x11,0x56);
  stage=20;deadline=Now+180;return true;
 }
 static string DirectVoiceAck(){return "PCVD_ACK1:"+directVoiceToken+";"+directVoiceId+";";}
 static bool DirectModifiersHeld(){return CurrentShortcutModifiers()!=0||GetAsyncKeyState(0x5B)<0||GetAsyncKeyState(0x5C)<0||GetAsyncKeyState(0x0D)<0;}
 static void AdvanceDirectVoice(){
  if(stage==20){Keys(0x11,0x41);Keys(0x11,0x43);stage=21;deadline=Now+140;return;}
  if(stage==21){
   if(Clip()!=DirectVoiceAck()+"READY"){Cancel("Not sent: direct chat handoff could not be verified");return;}
   if(DirectModifiersHeld()){Cancel("Not sent: release modifier/Enter keys before voice sending");return;}
   Bridge(0x7A);stage=22;deadline=Now+120;return;
  }
  if(stage==22){
   if(DirectModifiersHeld()){Cancel("Not sent: release modifier/Enter keys before voice sending");return;}
   Bridge(0x70);stage=23;deadline=Now+160;return;
  }
  if(stage==23){Keys(0x11,0x41);Keys(0x11,0x43);stage=24;deadline=Now+140;return;}
  if(stage==24){
   string ack=Clip();Bridge(0x7A);
   var match=Regex.Match(ack,@"\A"+Regex.Escape(DirectVoiceAck())+@"(ISSUED|ERROR|REFUSED);([01])\z");
   directVoiceUIFocus=match.Success&&match.Groups[2].Value=="1";directVoiceCopiedAck=match.Success?ack:null;
   if(match.Success&&match.Groups[1].Value=="ISSUED"){
    directVoiceResult="direct-send-issued";Status("Send requested to "+label+": "+text,4500);
   }else{
    directVoiceResult=match.Success&&match.Groups[1].Value=="ERROR"?"direct-api-rejected":match.Success&&match.Groups[1].Value=="REFUSED"?"direct-send-refused":"direct-ack-missing";
    Status("Direct send was not confirmed. It has not been retried; check chat before trying again.",6000);
   }
   stage=25;deadline=Now+100;return;
  }
  if(stage==25){if(directVoiceUIFocus)Bridge(0x75);stage=26;deadline=Now+120;return;}
  if(stage==26){
   RestoreDirectVoiceClipboard();
   phase="idle";stage=0;released=false;
  }
 }
 static void TestDirectVoiceWire(){
  if(DirectVoiceChecksum("/s\nhello")!=185074369u||EncodeDirectVoice("hi; café | /")!="hi%3B%20caf%C3%A9%20%7C%20%2F")throw new Exception("Direct voice wire checksum/UTF-8 escaping failed");
  Console.WriteLine("PASS: UTF-8 direct handoff escaping and cross-language checksum; runtime restrictions still require live verification.");
 }
}
