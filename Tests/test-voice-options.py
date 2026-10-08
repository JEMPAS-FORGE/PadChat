from pathlib import Path
import runpy

root=Path(__file__).resolve().parent.parent
runtime=runpy.run_path(str(root/'Tests/test-runtime.py'))['l']
runtime.execute('''
local saved=PadChatDB
PadChatDB={bindings={keyboard='F9',button='AUTO',modifier='NONE'},draft='keep this'}
P:InitDB()
assert(P.db.voiceOptions.key==119 and P.db.voiceOptions.mic=='')
local reloads=0;function ReloadUI() reloads=reloads+1 end
function IsControlKeyDown() return false end
function IsShiftKeyDown() return false end
function IsAltKeyDown() return false end
C_VoiceChat={GetAvailableInputDevices=function() return {
 {displayName='Microphone (USB)',deviceID='private-device-id'},
 {displayName='Microphone (USB)',deviceID='duplicate'},
 {displayName='Microphone (Built-in)',deviceID='other'},
 {displayName='',deviceID='invalid'}} end}
local m=P:GetVoiceMicrophones();assert(#m==2 and m[1]=='Microphone (Built-in)')
P:ShowOptions();P.optionIndex=7;P:OptionsAction('activate')
assert(P.voiceOptionsFrame:IsShown() and not P.optionsFrame:IsShown())
assert(P:GetText()=='keep this')
-- Exercise actual mouse clicks: WoW passes a button name, not a numeric delta.
P.voiceMic.scripts.OnClick(P.voiceMic,'LeftButton');assert(P.pendingVoice.mic=='Microphone (Built-in)')
P.voiceMic.scripts.OnClick(P.voiceMic,'LeftButton');assert(P.pendingVoice.mic=='Microphone (USB)')
P.voicePadMod.scripts.OnClick(P.voicePadMod,'LeftButton');assert(P.pendingVoice.padMod=='LTRIGGER')
P.voiceStartup.scripts.OnClick(P.voiceStartup,'LeftButton');assert(P.pendingVoice.startup=='ON')
P.voiceStartup.scripts.OnClick(P.voiceStartup,'LeftButton');assert(P.pendingVoice.startup=='OFF')
P.voiceStartup.scripts.OnClick(P.voiceStartup,'LeftButton');assert(P.pendingVoice.startup=='KEEP')
P.voiceKey.scripts.OnClick();clock=clock+1
P.voiceCaptureFrame.scripts.OnKeyDown(P.voiceCaptureFrame,'F9')
assert(P.voiceCapture and P.pendingVoice.key==119) -- opening collision refused
P.voiceCaptureFrame.scripts.OnKeyDown(P.voiceCaptureFrame,'ESCAPE');assert(not P.voiceCapture)
P:BeginVoiceCapture();clock=clock+1
P.voiceCaptureFrame.scripts.OnMouseDown(P.voiceCaptureFrame,'Button5')
assert(not P.voiceCapture and P.pendingVoice.kind=='mouse' and P.pendingVoice.key==6)
assert(P:VoiceBindingKey(P.pendingVoice)=='BUTTON5')
assert(P.db.voiceOptions.kind=='keyboard') -- pending edits do not save
combat=true;P.voiceSave.scripts.OnClick();assert(reloads==0 and P.db.voiceSettingsWire==nil);combat=false
P.voiceSave.scripts.OnClick();assert(reloads==1 and P.db.voiceSettingsWire=='PCV3;mouse;6;0;Microphone %28USB%29;1;KEEP;SHARE;LTRIGGER;0;')
assert(not P.voiceOptionsFrame:IsShown() and not bindings.PAD1)
assert(bindings.BUTTON5.name=='PadChatVoiceKey')
P:InitDB();assert(P.db.voiceOptions.kind=='mouse' and P.db.voiceOptions.key==6)
P:ShowVoiceOptions();P.voiceIndex=2;P:VoiceOptionsAction('previous')
assert(P.pendingVoice.mic=='Microphone (Built-in)')
P:VoiceOptionsAction('close');assert(not P.voiceOptionsFrame:IsShown() and P.optionsFrame:IsShown())
assert(P.db.voiceOptions.mic=='Microphone (USB)') -- cancel preserves choice
P.optionsFrame:Hide()
-- Missing enumeration cannot clear an existing input. No speech/chat API is used.
C_VoiceChat=nil;P:ShowVoiceOptions();P.voiceMic.scripts.OnClick(P.voiceMic,'LeftButton');assert(P.pendingVoice.mic=='Microphone (USB)')
P:BeginVoiceCapture();P.voiceOptionsFrame:Hide();assert(not P.voiceCapture and P.pendingVoice==nil)
P:ShowVoiceOptions();local channel=P:VoiceDestination();P:CycleVoiceChannel();assert(P:VoiceDestination()==channel)
P:PreparePTT();assert(not focus)
PadChat_Toggle();assert(not P.voiceOptionsFrame:IsShown() and not bindings.PAD1)
-- A modifier key is not a shortcut by itself; Ctrl+Shift protocol is reserved.
P:ShowVoiceOptions();P:BeginVoiceCapture();clock=clock+1
P:VoiceCaptureKey('LSHIFT');assert(P.voiceCapture)
function IsControlKeyDown() return true end
function IsShiftKeyDown() return true end
P:VoiceCaptureKey('F8');assert(P.voiceCapture and P.pendingVoice.key==6)
P:EndVoiceCapture();P.voiceOptionsFrame:Hide()
-- UTF-8 mic names are encoded as data, never Lua source or a command.
local v=P.db.voiceOptions;v.mic='Mic é;"';assert(P:SaveVoiceOptions(v))
assert(P.db.voiceSettingsWire:find('%%C3%%A9%%3B%%22'))
-- Minimap click opens options; dragging persists only an angle.
Minimap=CreateFrame('Frame','TestMinimap');function Minimap:GetCenter() return 100,100 end
function Minimap:GetEffectiveScale() return 2 end
function GetCursorPosition() return 200,360 end
-- The minimap can become available after PLAYER_LOGIN. World entry retries
-- creation through the actual event handler rather than relying on that order.
P.openBindingOwner.scripts.OnEvent(P.openBindingOwner,'PLAYER_ENTERING_WORLD')
assert(P.minimapButton:IsShown())
assert(P.minimapButton.level>=8)
assert(P.minimapButton.icon.texture=='Interface\\\\AddOns\\\\PadChat\\\\PadChatIconWarcraft')
assert(P.minimapButton.point[4]<0 and math.abs(P.minimapButton.point[5])<.01)
-- The quest voice button's default 225-degree placement must not overlap.
local r=83;local qx,qy=math.cos(math.rad(225))*r,math.sin(math.rad(225))*r
local dx,dy=P.minimapButton.point[4]-qx,P.minimapButton.point[5]-qy
assert(dx*dx+dy*dy>32*32)
P.minimapButton.scripts.OnClick();assert(P.optionsFrame:IsShown());P.optionsFrame:Hide()
P.minimapButton.scripts.OnDragStart();P.minimapButton.scripts.OnUpdate();assert(math.abs(P.db.minimapAngle-90)<.01)
P.minimapButton.scripts.OnDragStop();assert(P.minimapButton.scripts.OnUpdate==nil)
P.minimapButton.scripts.OnClick();assert(not P.optionsFrame:IsShown()) -- no click after drag
combat=true;P.minimapButton.scripts.OnClick();assert(not P.optionsFrame:IsShown());combat=false
PadChatDB=saved
''')
print('Passed in-game voice binding capture, mouse clicks, controller navigation, microphone refresh and persistence, protocol encoding, cancelled edits, combat guard, settings reload, no chat while configuring, and minimap click/drag.')
