using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

partial class VoicePTT {
 [DllImport("user32.dll")]static extern uint GetGuiResources(IntPtr process,uint kind);
 [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")]static extern IntPtr SurfaceStyle(IntPtr h,int index);
 static void AssertMeter(bool ok,string why){if(!ok)throw new Exception(why);}
 public static void TestMeter(string folder){
  Application.EnableVisualStyles();
  using(var form=new Badge()){
   form.Location=new Point(-20000,-20000);
   form.label.Text="Listening - release Share to send\nLooking for a group";
   IntPtr foreground=GetForegroundWindow();form.Show();
   form.SetMicrophoneState(true,true,0,1000,1000);form.RenderOverlay(1000,true);
   AssertMeter(form.Region==null&&form.TransparencyKey.IsEmpty&&form.Opacity==1,"Renderer still uses a changing region/colour key/global alpha");
   AssertMeter((SurfaceStyle(form.Handle,-20).ToInt64()&0x080800A0)==0x080800A0,"Click-through/no-activation/layered/toolwindow flags missing");
   AssertMeter(form.MeterLevel==0,"Silence generated fake activity");
   form.ComposeFrame().Save(System.IO.Path.Combine(folder,"meter-silent.png"),ImageFormat.Png);
   long now=1000;
   for(int i=0;i<15;i++){now+=40;form.SetMicrophoneState(true,true,72,now,now);form.RenderOverlay(now,false);}
   AssertMeter(form.MeterLevel>65&&form.MeterLevel<=72,"Real signal does not drive meter");
   form.ComposeFrame().Save(System.IO.Path.Combine(folder,"meter-speaking.png"),ImageFormat.Png);
   using(var image=(Bitmap)form.ComposeFrame().Clone()){
    AssertMeter(image.GetPixel(0,0).A==0&&image.GetPixel(450,155).A==0,"Rectangular background returned");
    int ink=0;for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++)if(image.GetPixel(x,y).A>0)ink++;
    AssertMeter(ink>300&&ink<image.Width*image.Height/4,"Text/meter absent or overlay filled screen");
   }
   // No new microphone samples: the last level must decay, not freeze as speech.
   now+=650;form.RenderOverlay(now,false);
   for(int i=0;i<30;i++){now+=40;form.RenderOverlay(now,false);}
   AssertMeter(form.MeterLevel<1,"Stale microphone signal stayed lit");
   int textBuilds=form.TextBuildCount;uint handles=GetGuiResources(Process.GetCurrentProcess().Handle,0);
   var clock=Stopwatch.StartNew();int before=form.FrameCount;
   for(int i=0;i<500;i++){
    now+=20;form.SetMicrophoneState(true,true,(i/20)%2==0?70:8,now,now);form.RenderOverlay(now,false);
   }
   clock.Stop();uint after=GetGuiResources(Process.GetCurrentProcess().Handle,0);
   AssertMeter(after<=handles+3,"Native drawing resources leaked");
   AssertMeter(form.TextBuildCount==textBuilds,"Meter changes rebuild text/reshape window");
   AssertMeter(form.FrameCount-before<=251,"Animation rate exceeded cap");
   AssertMeter(GetForegroundWindow()==foreground,"Overlay stole foreground");
   form.SetMicrophoneState(false,false,0,0,now);form.label.Text="Finishing your message...";form.RenderOverlay(now,true);
   AssertMeter(!form.MicrophoneActive&&form.MeterLevel==0,"PTT release left mic active");
   form.ComposeFrame().Save(System.IO.Path.Combine(folder,"meter-processing.png"),ImageFormat.Png);
   before=form.FrameCount;for(int i=0;i<50;i++){now+=40;form.RenderOverlay(now,false);}
   AssertMeter(form.FrameCount==before,"Static status continues to redraw");
   foreach(string text in new[]{"",new string('W',400),"Message cancelled; nothing sent","Listening\nOne more message"}){
    form.Hide();form.label.Text=text;form.Show();form.RenderOverlay(now+=100,true);
    AssertMeter(form.Region==null&&GetForegroundWindow()==foreground,"Hide/show reshaped or activated overlay");
   }
   form.Size=new Size(320,120);form.RenderOverlay(now+=100,true);
   form.Size=new Size(10,10);form.RenderOverlay(now+=100,true);
   Console.WriteLine("PASS: atomic native frame updates, alpha-only background, real level rise/silence/stale decay/PTT release, cached text, idle stops rendering, resize/hide/show, no focus change and no GDI leak.");
   Console.WriteLine("500 meter updates: "+clock.ElapsedMilliseconds+" ms; GDI handles before="+handles+", after="+after+"; text layouts="+textBuilds);
   form.Close();
  }
 }
}
class OverlayMeterFixture{[STAThread]static void Main(string[] a){VoicePTT.TestMeter(a[0]);}}
