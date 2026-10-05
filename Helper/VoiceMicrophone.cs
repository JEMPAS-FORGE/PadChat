using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

partial class VoicePTT {
 static System.Threading.EventWaitHandle microphoneEvent;
 static Form microphoneWindow;
 static ComboBox microphoneChoices;
 static Label microphoneInfo;
 static Button microphoneSave;
 static NotifyIcon microphoneTray;
 static Process microphoneEnumeration;
 static readonly string voiceRoot=AppDomain.CurrentDomain.BaseDirectory;
 class MicrophoneChoice {
  public string name,hostapi;
  public override string ToString(){return name;}
 }
 static readonly string dataRoot=Environment.GetEnvironmentVariable("PADCHAT_DATA_DIR") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"PadChat");
 static ProcessStartInfo PythonInfo(string arguments){
  var info=new ProcessStartInfo(Path.Combine(voiceRoot,@"Backend\voice-backend.exe"),arguments){
   WorkingDirectory=voiceRoot,UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,
   RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true};
  info.EnvironmentVariables["PADCHAT_DATA_DIR"]=dataRoot;
  info.EnvironmentVariables["HF_HUB_OFFLINE"]="1";
  return info;
 }
 static void WorkerError(string value){
  if(string.IsNullOrWhiteSpace(value))return;
  // Dependency failures only. Do not persist speech or microphone data.
  try{File.WriteAllText(Path.Combine(dataRoot,"backend-error.txt"),value,new UTF8Encoding(false));}catch{}
 }
 static void StopWorker(){
  if(worker==null)return;
  try{if(!worker.HasExited){WriteWorker("quit");if(!worker.WaitForExit(2000))worker.Kill();}}catch(InvalidOperationException){}
  worker.Dispose();worker=null;
 }
 static void StartWorker(){
  StopWorker();ready=false;sid++;
  string old;while(messages.TryDequeue(out old)){}
  worker=new Process{StartInfo=PythonInfo("")};
  worker.OutputDataReceived+=(s,e)=>{if(e.Data!=null)messages.Enqueue(e.Data);};
  worker.ErrorDataReceived+=(s,e)=>WorkerError(e.Data);
  worker.Start();worker.BeginOutputReadLine();worker.BeginErrorReadLine();
 }
 static void SetupMicrophoneMenu(){
  var menu=new ContextMenuStrip();menu.Items.Add("Tap F8 / Share: channel; hold: speak; release: send",null,(s,e)=>Status("In WoW: tap F8 or Share to change chat channel\nHold to speak; release to send",4500));
  menu.Items.Add("Choose microphone...",null,(s,e)=>ShowMicrophones());
  menu.Items.Add("Help / supported connections",null,(s,e)=>Process.Start(Path.Combine(voiceRoot,"START-HERE.txt")));
  menu.Items.Add("Exit PadChat voice",null,(s,e)=>Application.Exit());
  microphoneTray=new NotifyIcon{Icon=SystemIcons.Application,Text="PadChat — hold F8 / Share",ContextMenuStrip=menu,Visible=true};
  microphoneTray.DoubleClick+=(s,e)=>ShowMicrophones();
 }
 static void ShowMicrophones(){
  if(microphoneWindow!=null&&!microphoneWindow.IsDisposed){microphoneWindow.Show();microphoneWindow.Activate();return;}
  if(phase!="idle")Cancel("Dictation cancelled while choosing a microphone");
  badge.Hide();hideAt=0;heldAt=0;latched=true;
  microphoneWindow=new Form{Text="PadChat Microphone",ClientSize=new Size(700,330),StartPosition=FormStartPosition.CenterScreen,
   FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,TopMost=true,Font=new Font("Segoe UI",11)};
  var heading=new Label{Text="Microphone for keyboard and controller voice typing",Location=new Point(22,18),Size=new Size(655,38),Font=new Font("Segoe UI",14,FontStyle.Bold)};
  microphoneChoices=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Location=new Point(24,73),Size=new Size(650,34)};
  microphoneChoices.SelectedIndexChanged+=(s,e)=>{microphoneSave.Enabled=microphoneChoices.SelectedItem is MicrophoneChoice;};
  var explanation=new Label{Text="Only microphones available on the Windows host PC are listed. Moonlight does not forward the iPad microphone; connect a microphone to the host or use a separate microphone link.\n\nSaving does not record. Hold F8 or Share afterwards to speak.",Location=new Point(24,123),Size=new Size(650,100)};
  microphoneInfo=new Label{Text="Looking for microphones...",Location=new Point(24,224),Size=new Size(650,36)};
  microphoneSave=new Button{Text="Use this microphone",Location=new Point(445,276),Size=new Size(228,38),Enabled=false};
  microphoneSave.Click+=(s,e)=>SaveMicrophone();
  var refresh=new Button{Text="Refresh list",Location=new Point(24,276),Size=new Size(160,38)};
  refresh.Click+=(s,e)=>EnumerateMicrophones();
  var close=new Button{Text="Cancel",Location=new Point(267,276),Size=new Size(150,38)};
  close.Click+=(s,e)=>microphoneWindow.Close();
  microphoneWindow.CancelButton=close;
  microphoneWindow.Controls.AddRange(new Control[]{heading,microphoneChoices,explanation,microphoneInfo,microphoneSave,refresh,close});
  microphoneWindow.Show();EnumerateMicrophones();
 }
 static void EnumerateMicrophones(){
  if(microphoneEnumeration!=null&&!microphoneEnumeration.HasExited)return;
  microphoneChoices.Items.Clear();microphoneSave.Enabled=false;microphoneInfo.Text="Looking for microphones...";
  microphoneEnumeration=new Process{StartInfo=PythonInfo("--list-inputs")};
  microphoneEnumeration.OutputDataReceived+=(s,e)=>{if(e.Data!=null)messages.Enqueue(e.Data);};
  microphoneEnumeration.ErrorDataReceived+=(s,e)=>{};
  try{microphoneEnumeration.Start();microphoneEnumeration.BeginOutputReadLine();microphoneEnumeration.BeginErrorReadLine();}
  catch(Exception ex){microphoneInfo.Text="Could not start microphone settings: "+ex.Message;}
 }
 static void UpdateMicrophones(Dictionary<string,object> data){
  if(microphoneWindow==null||microphoneWindow.IsDisposed)return;
  microphoneChoices.Items.Clear();
  var selected=data.ContainsKey("selected")?data["selected"] as Dictionary<string,object>:null;
  foreach(var value in (IEnumerable)data["devices"]){
   var d=(Dictionary<string,object>)value;
   var choice=new MicrophoneChoice{name=(string)d["name"],hostapi=(string)d["hostapi"]};
   int index=microphoneChoices.Items.Add(choice);
   if(selected!=null&&(string)selected["name"]==choice.name&&(string)selected["hostapi"]==choice.hostapi)microphoneChoices.SelectedIndex=index;
  }
  microphoneInfo.Text=data.ContainsKey("error")?"Could not list inputs: "+(string)data["error"]:
   microphoneChoices.Items.Count==0?"No microphones found. Connect one, then refresh.":"Select an input above. Your choice is remembered; it never switches silently.";
 }
 static void SaveMicrophone(){
  var chosen=microphoneChoices.SelectedItem as MicrophoneChoice;if(chosen==null)return;
  try{
   string path=Path.Combine(dataRoot,"voice-microphone.json"),temp=path+".tmp";
   File.WriteAllText(temp,json.Serialize(new {name=chosen.name,hostapi=chosen.hostapi}),new UTF8Encoding(false));
   if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);
   StartWorker(); // Fresh device enumeration also picks up USB hotplug changes.
   microphoneWindow.Close();Status("Microphone saved: "+chosen.name+"\nHold F8 or Share to speak once the speech model is ready.",4500);
  }catch(Exception ex){microphoneInfo.Text="Could not save microphone: "+ex.Message;}
 }
}
