// Native Sony input through SDL's HID/gamepad backend. No controller output,
// remapping, rumble, or synthetic gamepad events are generated here.
using System;
using System.Runtime.InteropServices;

partial class VoicePTT {
 [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] static extern bool SDL_Init(uint flags);
 [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] static extern bool SDL_SetHint(string name,string value);
 [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)] static extern void SDL_SetGamepadEventsEnabled([MarshalAs(UnmanagedType.I1)] bool enabled);
 [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)] static extern void SDL_SetJoystickEventsEnabled([MarshalAs(UnmanagedType.I1)] bool enabled);
 [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)] static extern void SDL_UpdateGamepads();
 [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)] static extern IntPtr SDL_GetGamepads(out int count);
 [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)] static extern void SDL_free(IntPtr memory);
 [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)] static extern IntPtr SDL_OpenGamepad(uint id);
 [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)] static extern void SDL_CloseGamepad(IntPtr pad);
 [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)] static extern ushort SDL_GetGamepadVendor(IntPtr pad);
 [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)] static extern int SDL_GetGamepadType(IntPtr pad);
 [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] static extern bool SDL_GamepadConnected(IntPtr pad);
 [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] static extern bool SDL_GetGamepadButton(IntPtr pad,int button);
 [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)] static extern void SDL_QuitSubSystem(uint flags);
 static bool sdlAttempted,sdlReady;static IntPtr sonyPad;static uint sonyId;static int sonyCount;static long sonyScanAt;
 static bool IsSonyGamepad(ushort vendor,int type){return vendor==0x054c&&(type==5||type==6);}
 static uint NormalizeSDL(bool share,bool start,bool triangle){return (share?0x100u:0u)|(start?0x200u:0u)|(triangle?0x8u:0u);}
 static bool ReadNativeSony(out bool connected,out uint buttons){
  connected=false;buttons=0;
  if(!sdlAttempted){
   sdlAttempted=true;
   try{
    SDL_SetHint("SDL_JOYSTICK_ALLOW_BACKGROUND_EVENTS","1");
    sdlReady=SDL_Init(0x2000); // gamepad subsystem also initialises joysticks
    if(sdlReady){SDL_SetGamepadEventsEnabled(false);SDL_SetJoystickEventsEnabled(false);}
   }catch(DllNotFoundException){}catch(EntryPointNotFoundException){}catch(BadImageFormatException){}
  }
  if(!sdlReady)return false; // older installs can still use the legacy fallback
  SDL_UpdateGamepads();
  bool lost=sonyPad!=IntPtr.Zero&&!SDL_GamepadConnected(sonyPad);
  if(lost){SDL_CloseGamepad(sonyPad);sonyPad=IntPtr.Zero;sonyScanAt=0;}
  if(Now>=sonyScanAt){
   sonyScanAt=Now+500;int count;IntPtr ids=SDL_GetGamepads(out count);uint candidate=0;sonyCount=0;
   try{
    for(int i=0;ids!=IntPtr.Zero&&i<count;i++){
     uint id=unchecked((uint)Marshal.ReadInt32(ids,i*4));
     IntPtr pad=id==sonyId&&sonyPad!=IntPtr.Zero?sonyPad:SDL_OpenGamepad(id);
     if(pad==IntPtr.Zero)continue;
     if(IsSonyGamepad(SDL_GetGamepadVendor(pad),SDL_GetGamepadType(pad))){sonyCount++;candidate=id;}
     if(pad!=sonyPad)SDL_CloseGamepad(pad);
    }
   }finally{if(ids!=IntPtr.Zero)SDL_free(ids);}
   if(sonyCount!=1||candidate!=sonyId){if(sonyPad!=IntPtr.Zero)SDL_CloseGamepad(sonyPad);sonyPad=IntPtr.Zero;}
   if(sonyCount==1&&sonyPad==IntPtr.Zero){sonyId=candidate;sonyPad=SDL_OpenGamepad(candidate);}
  }
  if(sonyCount>1){inputSource="ambiguous";return true;}
  if(sonyPad==IntPtr.Zero||!SDL_GamepadConnected(sonyPad)){inputSource="none";return true;}
  inputSource="SDL:"+sonyId;connected=true;
  // SDL's mapped Back is Create/Share, Start is Options, North is Triangle.
  // Guide, microphone, and touchpad are deliberately not voice triggers.
  buttons=NormalizeSDL(SDL_GetGamepadButton(sonyPad,4),SDL_GetGamepadButton(sonyPad,6),SDL_GetGamepadButton(sonyPad,3));
  return true;
 }
 static void CloseNativeSony(){
  if(!sdlReady)return;
  if(sonyPad!=IntPtr.Zero)SDL_CloseGamepad(sonyPad);
  sonyPad=IntPtr.Zero;SDL_QuitSubSystem(0x2000|0x200);sdlReady=false;
 }
 static void TestNativeSony(){
  if(!IsSonyGamepad(0x054c,5)||!IsSonyGamepad(0x054c,6)||IsSonyGamepad(0x045e,6)||IsSonyGamepad(0x054c,0))throw new Exception("Native controller identity isolation failed");
  for(int mask=0;mask<8;mask++){
   uint normalized=NormalizeSDL((mask&1)!=0,(mask&2)!=0,(mask&4)!=0);
   if(Chord(normalized)!=((mask&1)!=0)||KeyboardToggleChord(normalized)!=(mask==6))throw new Exception("SDL Share / Options / Triangle isolation failed");
  }
  Console.WriteLine("Native Sony identity and mapped Share/toggle tests passed.");
 }
}
