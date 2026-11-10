using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

partial class VoicePTT {
 // One premultiplied-alpha frame replaces the whole surface atomically. No
 // colour-key background, child-window paints or changing glyph window region.
 class Badge:Form {
  public readonly Label label=new Label(); // Text/font storage; never a native child.
  Bitmap textFrame,frame;
  bool textDirty=true,dirty=true,micActive,micReady;
  int rawLevel;
  double shownLevel;
  long sampleAt,lastStep,nextFrame;
  public int FrameCount,TextBuildCount;
  public double MeterLevel{get{return shownLevel;}}
  public bool MicrophoneActive{get{return micActive;}}
  [StructLayout(LayoutKind.Sequential)]struct SurfacePoint{public int x,y;public SurfacePoint(int x,int y){this.x=x;this.y=y;}}
  [StructLayout(LayoutKind.Sequential)]struct SurfaceSize{public int width,height;}
  [StructLayout(LayoutKind.Sequential,Pack=1)]struct SurfaceBlend{public byte operation,flags,alpha,format;}
  [DllImport("user32.dll",SetLastError=true)]static extern bool UpdateLayeredWindow(IntPtr window,IntPtr screen,ref SurfacePoint position,ref SurfaceSize size,IntPtr source,ref SurfacePoint origin,uint key,ref SurfaceBlend blend,uint flags);
  [DllImport("gdi32.dll",SetLastError=true)]static extern IntPtr CreateCompatibleDC(IntPtr dc);
  [DllImport("gdi32.dll")]static extern bool DeleteDC(IntPtr dc);
  [DllImport("gdi32.dll")]static extern IntPtr SelectObject(IntPtr dc,IntPtr obj);
  [DllImport("gdi32.dll")]static extern bool DeleteObject(IntPtr obj);
  protected override bool ShowWithoutActivation{get{return true;}}
  protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x080800A0;return p;}}
  protected override void OnPaintBackground(PaintEventArgs e){} // Surface is presented as one frame.
  protected override void OnPaint(PaintEventArgs e){}
  public Badge(){
   FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;
   Size=new Size(900,160);StartPosition=FormStartPosition.Manual;
   label.ForeColor=Color.White;label.Font=new Font("Segoe UI",16);
   label.TextChanged+=(s,e)=>{textDirty=true;dirty=true;};
   label.FontChanged+=(s,e)=>{textDirty=true;dirty=true;};
   label.ForeColorChanged+=(s,e)=>{textDirty=true;dirty=true;};
   SizeChanged+=(s,e)=>{textDirty=true;dirty=true;};
   VisibleChanged+=(s,e)=>{dirty=true;nextFrame=0;lastStep=0;};
  }
  public void SetMicrophoneState(bool active,bool ready,int value,long receivedAt,long now){
   value=Math.Max(0,Math.Min(100,value));
   if(micActive!=active||micReady!=ready||rawLevel!=value)dirty=true;
   if(micActive!=active){shownLevel=0;lastStep=now;}
   micActive=active;micReady=active&&ready;rawLevel=value;sampleAt=receivedAt;
   if(!active)shownLevel=0;
  }
  static void Configure(Graphics graphics){
   graphics.SmoothingMode=SmoothingMode.AntiAlias;
   graphics.PixelOffsetMode=PixelOffsetMode.HighQuality;
   graphics.CompositingQuality=CompositingQuality.HighQuality;
  }
  void BuildText(){
   if(textFrame==null||textFrame.Width!=Width||textFrame.Height!=Height){
    if(textFrame!=null)textFrame.Dispose();if(frame!=null)frame.Dispose();
    textFrame=new Bitmap(Math.Max(1,Width),Math.Max(1,Height),PixelFormat.Format32bppPArgb);
    frame=new Bitmap(textFrame.Width,textFrame.Height,PixelFormat.Format32bppPArgb);
   }
   using(var graphics=Graphics.FromImage(textFrame)){
    graphics.Clear(Color.Transparent);Configure(graphics);
    if(!string.IsNullOrEmpty(label.Text)&&Width>24&&Height>56){
     using(var path=new GraphicsPath())using(var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Near}){
      path.AddString(label.Text,label.Font.FontFamily,(int)label.Font.Style,label.Font.SizeInPoints*graphics.DpiY/72f,new RectangleF(12,48,Width-24,Height-52),format);
      using(var shadow=new Pen(Color.FromArgb(230,0,0,0),3f))using(var ink=new SolidBrush(label.ForeColor)){
       shadow.LineJoin=LineJoin.Round;graphics.DrawPath(shadow,path);graphics.FillPath(ink,path);
      }
     }
    }
   }
   textDirty=false;TextBuildCount++;
  }
  void DrawMicrophone(Graphics graphics){
   if(!micActive||Width<160||Height<56)return;
   float x=Width/2f-64,y=12;
   Color accent=micReady?Color.FromArgb(103,235,188):Color.FromArgb(190,198,207);
   using(var mic=new GraphicsPath()){
    mic.AddArc(x,y,12,12,180,180);mic.AddLine(x+12,y+6,x+12,y+14);mic.AddArc(x,y+8,12,12,0,180);mic.CloseFigure();
    using(var black=new Pen(Color.FromArgb(230,0,0,0),5f))using(var pen=new Pen(accent,2f)){
     black.StartCap=black.EndCap=pen.StartCap=pen.EndCap=LineCap.Round;
     graphics.DrawPath(black,mic);graphics.DrawPath(pen,mic);
     graphics.DrawArc(black,x-4,y+7,20,20,0,180);graphics.DrawArc(pen,x-4,y+7,20,20,0,180);
     graphics.DrawLine(black,x+6,y+27,x+6,y+32);graphics.DrawLine(pen,x+6,y+27,x+6,y+32);
     graphics.DrawLine(black,x+1,y+32,x+11,y+32);graphics.DrawLine(pen,x+1,y+32,x+11,y+32);
    }
   }
   for(int i=0;i<10;i++){
    float height=5+i*2f,left=Width/2f-36+i*8,top=42-height;
    bool lit=micReady&&shownLevel>=(i+1)*8;
    Color color=lit?(i>=8?Color.FromArgb(255,207,105):accent):Color.FromArgb(125,156,166,177);
    using(var outline=new Pen(Color.FromArgb(225,0,0,0),3f))using(var fill=new SolidBrush(color)){
     graphics.DrawRectangle(outline,left,top,4,height);graphics.FillRectangle(fill,left,top,4,height);
    }
   }
  }
  internal Bitmap ComposeFrame(){
   if(textDirty||textFrame==null)BuildText();
   using(var graphics=Graphics.FromImage(frame)){
    graphics.CompositingMode=CompositingMode.SourceCopy;graphics.DrawImageUnscaled(textFrame,0,0);
    graphics.CompositingMode=CompositingMode.SourceOver;Configure(graphics);DrawMicrophone(graphics);
   }
   return frame;
  }
  void Present(Bitmap image){
   IntPtr memory=CreateCompatibleDC(IntPtr.Zero),bitmap=IntPtr.Zero,old=IntPtr.Zero;
   if(memory==IntPtr.Zero)throw new System.ComponentModel.Win32Exception();
   try{
    bitmap=image.GetHbitmap(Color.FromArgb(0));old=SelectObject(memory,bitmap);
    var position=new SurfacePoint(Left,Top);var origin=new SurfacePoint(0,0);
    var size=new SurfaceSize{width=image.Width,height=image.Height};
    var blend=new SurfaceBlend{operation=0,flags=0,alpha=255,format=1};
    if(!UpdateLayeredWindow(Handle,IntPtr.Zero,ref position,ref size,memory,ref origin,0,ref blend,2))throw new System.ComponentModel.Win32Exception();
    FrameCount++;
   }finally{if(old!=IntPtr.Zero)SelectObject(memory,old);if(bitmap!=IntPtr.Zero)DeleteObject(bitmap);DeleteDC(memory);}
  }
  public void RenderOverlay(long now,bool force){
   if(!Visible||IsDisposed)return;
   double target=micActive&&micReady&&sampleAt>0&&now-sampleAt<=600?rawLevel:0;
   double elapsed=lastStep==0?33:Math.Max(0,Math.Min(100,now-lastStep));lastStep=now;
   double previous=shownLevel;
   shownLevel+= (target-shownLevel)*(1-Math.Exp(-elapsed/(target>shownLevel?70:180)));
   if(Math.Abs(target-shownLevel)<.1)shownLevel=target;
   if(Math.Abs(shownLevel-previous)>.01)dirty=true;
   if(!force&&now<nextFrame)return;
   if(!force&&!dirty)return;
   Present(ComposeFrame());dirty=false;nextFrame=now+33;
  }
  protected override void Dispose(bool disposing){
   if(disposing){if(textFrame!=null)textFrame.Dispose();if(frame!=null)frame.Dispose();label.Font.Dispose();label.Dispose();}
   base.Dispose(disposing);
  }
 }
}
