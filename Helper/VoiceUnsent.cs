using System;
using System.Windows.Forms;
partial class VoicePTT {
 static string recoveryText="",recognizedForRecovery="";
 static bool recoveryUncertain,sendAttempted;
 static ToolStripMenuItem recoveryCopy;
 static void RetainUnsent(bool uncertain){
  if(microphoneTest||string.IsNullOrWhiteSpace(recognizedForRecovery))return;
  recoveryText=Clean(recognizedForRecovery,600);recoveryUncertain=uncertain;
 }
 static string RecoveryRecord(string token){return "PCVRD1;"+token+";"+(recoveryUncertain?"1":"0")+";"+Uri.EscapeDataString(recoveryText);}
 static void CopyFailedMessage(){
  if(recoveryText.Length==0){Status("No failed message is available in this companion session.",4500);return;}
  Clipboard.SetText(recoveryText);
  Status("Failed message copied; nothing resent.\n"+(recoveryUncertain?"Delivery was uncertain. Check chat before sending again.":"The earlier attempt stopped before sending."),6000);
 }
}
