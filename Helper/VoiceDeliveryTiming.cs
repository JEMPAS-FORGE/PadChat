using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

partial class VoicePTT {
 [DllImport("user32.dll")] static extern uint GetClipboardSequenceNumber();
 static uint deliveryCopySequence;
 static bool FreshDeliveryAcknowledgement(int step,uint copied,uint current,string value,string previous,string full,string body){
  // Only advance a readback wait after a new clipboard write. Never shorten
  // focus/paste settling waits or skip the production destination/echo checks.
  if(copied==0||current==0||copied==current)return false;
  if(step==2){
   string destination,destinationLabel;
   return value!=previous&&(value.StartsWith("CKPTT_DENIED:")||TryDestination(value,out destination,out destinationLabel));
  }
  if(step==6)return !string.IsNullOrEmpty(body)&&(value==full||value==body);
  if(step==9)return Regex.IsMatch(value,@"\ACKPTT_EXIT:[0-9.]+\|[01]\z");
  return false;
 }
 static bool DeliveryAcknowledged(){
  if(stage!=2&&stage!=21&&stage!=24)return false;
  try{
   uint current=GetClipboardSequenceNumber();
   if(deliveryCopySequence==0||current==0||current==deliveryCopySequence)return false;
   string value=Clip();
   if(stage==21||stage==24)return FreshDirectAcknowledgement(stage,deliveryCopySequence,current,value,directVoiceToken,directVoiceId);
   return FreshDeliveryAcknowledgement(stage,deliveryCopySequence,current,value,oldClip,line,text);
  }catch(ExternalException){return false;} // Clipboard contention retains the original deadline.
 }
 static bool FreshDirectAcknowledgement(int step,uint copied,uint current,string value,string token,string session){
  if(copied==0||current==0||copied==current||string.IsNullOrEmpty(token)||string.IsNullOrEmpty(session))return false;
  string prefix="PCVD_ACK1:"+token+";"+session+";";
  if(step==21)return value==prefix+"READY";
  if(step==24)return value!=null&&Regex.IsMatch(value,@"\A"+Regex.Escape(prefix)+@"(ISSUED|ERROR|REFUSED);[01]\z");
  return false;
 }
 static void TestDeliveryTiming(){
  foreach(string value in new[]{"PCVD_ACK1:1.2;3;ISSUED;0","PCVD_ACK1:1.2;3;ERROR;1","PCVD_ACK1:1.2;3;REFUSED;0"})
   if(!FreshDirectAcknowledgement(24,10,11,value,"1.2","3"))throw new Exception("Fresh terminal direct acknowledgement ignored");
  if(!FreshDirectAcknowledgement(21,10,11,"PCVD_ACK1:1.2;3;READY","1.2","3"))throw new Exception("Fresh direct READY ignored");
  foreach(string bad in new[]{null,"","PCVD_ACK1:1.2;4;READY","PCVD_ACK1:2.3;3;READY","PCVD_ACK1:1.2;3;ISSUED;0","PCVD_ACK1:1.2;3;READY\n"})
   if(FreshDirectAcknowledgement(21,10,11,bad,"1.2","3"))throw new Exception("Stale/wrong direct READY accepted");
  foreach(uint sequence in new uint[]{0,10})
   if(FreshDirectAcknowledgement(21,10,sequence,"PCVD_ACK1:1.2;3;READY","1.2","3"))throw new Exception("Unchanged sequence accepted");
  if(FreshDirectAcknowledgement(24,10,11,"PCVD_ACK1:1.2;3;ISSUED;0\n","1.2","3")||FreshDirectAcknowledgement(20,10,11,"PCVD_ACK1:1.2;3;READY","1.2","3"))throw new Exception("Malformed acknowledgement or paste wait bypassed");
  string body="fixture hello",full="/p "+body,ready="CKPTT_READY2:1;/p;PARTY";
  if(!FreshDeliveryAcknowledgement(2,1,2,ready,"old",full,body)
   ||!FreshDeliveryAcknowledgement(2,1,2,"CKPTT_DENIED:1|unavailable","old",full,body)
   ||!FreshDeliveryAcknowledgement(6,1,2,full,"old",full,body)
   ||!FreshDeliveryAcknowledgement(6,1,2,body,"old",full,body)
   ||!FreshDeliveryAcknowledgement(9,1,2,"CKPTT_EXIT:1.2|0","old",full,body)
   ||!FreshDeliveryAcknowledgement(9,1,2,"CKPTT_EXIT:1.2|1","old",full,body))throw new Exception("Fresh readback not recognised");
  foreach(uint copied in new uint[]{0,2})
   if(FreshDeliveryAcknowledgement(6,copied,2,full,"old",full,body))throw new Exception("Absent/unchanged clipboard sequence accepted");
  if(FreshDeliveryAcknowledgement(6,1,0,full,"old",full,body)
   ||FreshDeliveryAcknowledgement(2,1,2,ready,ready,full,body)
   ||FreshDeliveryAcknowledgement(2,1,2,"CKPTT_READY2:1;/run;bad","old",full,body)
   ||FreshDeliveryAcknowledgement(6,1,2,"CKPTT_VERIFY:9","old",full,body)
   ||FreshDeliveryAcknowledgement(6,1,2,"another message","old",full,body)
   ||FreshDeliveryAcknowledgement(9,1,2,"CKPTT_EXIT_WAIT:9","old",full,body)
   ||FreshDeliveryAcknowledgement(9,1,2,"CKPTT_EXIT:1|1\n","old",full,body))throw new Exception("Non-acknowledgement shortened readback wait");
  for(int step=0;step<=11;step++)if(step!=2&&step!=6&&step!=9
   &&FreshDeliveryAcknowledgement(step,1,2,full,"old",full,body))throw new Exception("Non-readback settling wait shortened");
  if(!FreshDeliveryAcknowledgement(6,uint.MaxValue,1,full,"old",full,body))throw new Exception("Clipboard sequence wrap mishandled");
  Console.WriteLine("PASS: adaptive readback only accepts fresh exact acknowledgements; stale/malformed/unavailable sequences retain original deadlines.");
 }
}
