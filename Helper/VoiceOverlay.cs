using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

partial class VoicePTT {
 [StructLayout(LayoutKind.Sequential)] struct OverlayRect {public int left,top,right,bottom;}
 [StructLayout(LayoutKind.Sequential)] struct OverlayPoint {public int x,y;}
 [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr window,out OverlayRect rect);
 [DllImport("user32.dll")] static extern bool ClientToScreen(IntPtr window,ref OverlayPoint point);
 [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr window);
 [DllImport("user32.dll")] static extern bool IsIconic(IntPtr window);
 static long overlayFollowAt;
 static Rectangle VoiceOverlayBounds(Rectangle client,Rectangle work) {
  Rectangle area=Rectangle.Intersect(client,work);
  if(area.Width<1||area.Height<1)area=work;
  if(area.Width<1||area.Height<1)return Rectangle.Empty;
  int margin=Math.Min(12,Math.Max(0,Math.Min(area.Width,area.Height)/4));
  int width=Math.Min(900,Math.Max(1,area.Width-margin*2));
  int height=Math.Min(160,Math.Max(1,area.Height-margin*2));
  int top=Math.Min(area.Top+Math.Max(margin,Math.Min(55,area.Height/8)),area.Bottom-margin-height);
  return new Rectangle(area.Left+(area.Width-width)/2,top,width,height);
 }
 static bool PositionVoiceOverlay() {
  IntPtr foreground=GetForegroundWindow();
  IntPtr game=IsGame(foreground)?foreground:IsGame(target)?target:IntPtr.Zero;
  if(game!=IntPtr.Zero&&(!IsWindowVisible(game)||IsIconic(game)))return false;
  var screen=Screen.FromHandle(game!=IntPtr.Zero?game:foreground);
  Rectangle work=screen.WorkingArea,client=work;
  OverlayRect rect;var origin=new OverlayPoint();
  // Both native calls use this helper thread's coordinate context. Do not mix
  // physical DWM rectangles with DPI-virtualised WinForms coordinates.
  if(game!=IntPtr.Zero&&GetClientRect(game,out rect)&&ClientToScreen(game,ref origin))
   client=new Rectangle(origin.x,origin.y,Math.Max(0,rect.right-rect.left),Math.Max(0,rect.bottom-rect.top));
  Rectangle bounds=VoiceOverlayBounds(client,work);
  if(bounds.IsEmpty)return false;
  if(badge.Bounds!=bounds)badge.Bounds=bounds;
  return true;
 }
 static void ShowVoiceOverlay() {
  // Badge.ShowWithoutActivation and WS_EX_NOACTIVATE preserve the game focus.
  // Bounds change only this application's window, never the game or display.
  try{if(PositionVoiceOverlay())badge.Show();else badge.Hide();}
  catch{try{badge.Hide();}catch{}} // Display failures must not cancel speech.
 }
 static void MaintainVoiceOverlay() {
  if(badge==null||badge.IsDisposed||!badge.Visible||Now<overlayFollowAt)return;
  overlayFollowAt=Now+100;
  try{if(!PositionVoiceOverlay())badge.Hide();}catch{badge.Hide();}
 }
 static void TestVoiceOverlay() {
  ShowVoiceOverlay(); // No badge exists in self-test: display failure is isolated.
  var cases=new[]{
   new[]{new Rectangle(0,0,1920,1080),new Rectangle(0,0,1920,1040)},
   new[]{new Rectangle(1920,0,2560,1440),new Rectangle(1920,0,2560,1400)},
   new[]{new Rectangle(-1920,0,1920,1080),new Rectangle(-1920,0,1920,1040)},
   new[]{new Rectangle(0,-1440,2560,1440),new Rectangle(0,-1440,2560,1400)},
   new[]{new Rectangle(2200,170,1000,700),new Rectangle(1920,0,2560,1400)},
   new[]{new Rectangle(1800,-100,1200,850),new Rectangle(1920,0,1280,984)},
   new[]{new Rectangle(200,100,640,360),new Rectangle(0,0,1280,984)},
   new[]{new Rectangle(200,100,320,200),new Rectangle(0,0,1280,984)},
   new[]{new Rectangle(0,0,10,10),new Rectangle(0,0,1280,984)},
   new[]{new Rectangle(9000,9000,1920,1080),new Rectangle(0,0,1920,1040)}
  };
  foreach(var pair in cases) {
   var area=Rectangle.Intersect(pair[0],pair[1]);if(area.Width<1||area.Height<1)area=pair[1];
   var bounds=VoiceOverlayBounds(pair[0],pair[1]);
   if(bounds.Width<1||bounds.Height<1||!area.Contains(bounds)||bounds.Width>900||bounds.Height>160)
    throw new Exception("Voice overlay escaped the game/monitor work area");
  }
  var right=VoiceOverlayBounds(cases[1][0],cases[1][1]);
  var left=VoiceOverlayBounds(cases[2][0],cases[2][1]);
  if(right.Left<1920||left.Right>0||right==left)throw new Exception("Voice overlay retained the primary-monitor anchor");
  if(!VoiceOverlayBounds(Rectangle.Empty,Rectangle.Empty).IsEmpty)throw new Exception("No-monitor geometry accepted");
  using(var form=new Badge()) {
   var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
   var cp=(CreateParams)typeof(Badge).GetProperty("CreateParams",flags).GetValue(form,null);
   if((cp.ExStyle&0x080000A0)!=0x080000A0||!(bool)typeof(Badge).GetProperty("ShowWithoutActivation",flags).GetValue(form,null))
    throw new Exception("Voice overlay can take focus or intercept clicks");
  }
  int checks=0;
  foreach(int x in new[]{-3840,-1920,0,1920,3840})foreach(int width in new[]{320,640,1920,3840})foreach(int height in new[]{200,360,1080,2160}) {
   var work=new Rectangle(x,-height,width,height);var client=new Rectangle(x+20,-height+20,Math.Max(1,width-40),Math.Max(1,height-40));
   var bounds=VoiceOverlayBounds(client,work);if(!client.Contains(bounds))throw new Exception("Overlay resize/negative-origin matrix failed");checks++;
  }
  Console.WriteLine("PASS: overlay game/monitor placement: primary, right, negative left/above, windowed, spanning, small window, work-area changes and removed-monitor fallback. 80 resize/origin cases plus non-activation/click-through/tool-window styles. Geometry tests; mixed-DPI/fullscreen live acceptance remains open.");
 }
}
