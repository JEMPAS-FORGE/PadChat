from pathlib import Path
import sys,json
root=Path(__file__).resolve().parent.parent

from lupa.lua51 import LuaRuntime
l=LuaRuntime(unpack_returned_tuples=True)
fixture=(root/'Tests/Fixtures/options-runtime.lua').read_text(encoding='utf-8-sig')
fixture=fixture.replace('name=name,scripts={},attrs={}','name=name,scripts={},attrs={},shown=true')
fixture=fixture.replace("'SetJustifyH'", "'SetJustifyH','SetJustifyV'")
l.execute(fixture)
l.execute('function GetBindingAction() return "" end;reloads=0;function ReloadUI() reloads=reloads+1 end')
addon=root/'PadChat'
for line in (addon/'PadChat.toc').read_text(encoding='utf-8-sig').splitlines():
    if line.endswith('.lua'):l.execute((addon/line).read_text(encoding='utf-8-sig'),'PadChat',l.globals().P)
l.execute('''
P:InitDB();P.openBindingOwner.scripts.OnEvent(P.openBindingOwner,'PLAYER_LOGIN');P:Open();P:SetText('preserved draft');P:ShowOptions()
assert(P.uxPage=='Controls' and P.optionsFrame:IsShown() and not P:UXDirty())
local saved=P.db.bindings.button
P.optionButton.scripts.OnClick();assert(P.uxModal.kind=='picker' and P.uxPicker.entries[1].value=='AUTO')
P:OptionsAction('down');P:OptionsAction('activate');assert(P.pendingOptions.button=='NONE' and P.db.bindings.button==saved and P:UXDirty())
P:UXSetPage('Voice');assert(P.pendingOptions.button=='NONE')
C_VoiceChat={GetAvailableInputDevices=function() local list={} for i=1,20 do list[#list+1]={displayName=string.format('Mic %02d',i),deviceID=tostring(i)} end return list end}
P.voiceMic.scripts.OnClick();assert(#P.uxPicker.entries==21)
P:OptionsAction('right');assert(P.uxPicker.selected==7);P:OptionsAction('down');P:OptionsAction('activate')
assert(P.pendingVoice.mic=='Mic 07' and P.db.voiceOptions.mic=='')
P.voiceMic.scripts.OnClick();assert(P.uxPicker.selected==8);P:OptionsAction('down');P:OptionsAction('close')
assert(P.pendingVoice.mic=='Mic 07')
P.voiceMic.scripts.OnClick();P.uxModal.controls[3].scripts.OnEnter();assert(P.uxPicker.selected==9)
P:OptionsAction('down');assert(P.uxPicker.selected==10);P:OptionsAction('close')
P:UXSetPage('Appearance');P.appearanceSize.scripts.OnClick();P:OptionsAction('down');P:OptionsAction('activate')
assert(P.pendingAppearance.size==20)
P.appearancePosition.scripts.OnClick();P:OptionsAction('down');P:OptionsAction('activate');assert(P.pendingAppearance.position=='CENTER')
P.appearanceSound.scripts.OnClick();assert(P.pendingAppearance.sound and not P:AppearanceSettings().sound)
P:UXClose();assert(P.uxModal.kind=='dialog' and P.uxModal.index==3 and reloads==0)
P:OptionsAction('activate');assert(not P.uxModal and P.optionsFrame:IsShown()) -- Stay
-- External Escape-like hide also protects edits.
P.optionsFrame:Hide();assert(P.optionsFrame:IsShown() and P.uxModal and P:UXDirty())
P:OptionsAction('close');assert(P:UXDirty())
-- Combat hide retains pending edits without protected binding writes.
combat=true;P.optionsFrame:Hide();assert(not P.optionsFrame:IsShown() and P.pendingAppearance.size==20);combat=false
P:ShowOptions();assert(P.pendingAppearance.size==20 and P.pendingVoice.mic=='Mic 07')
P.db.voiceGuideStep=3;P:UXSaveReload();assert(reloads==1 and P.db.appearanceWire=='PCA1;20;CENTER;1;1' and P.db.voiceGuideStep==4)
assert(P.db.voiceOptions.mic=='Mic 07' and P.db.bindings.button=='NONE' and P:GetText()=='preserved draft' and not P.optionsFrame:IsShown())
P:ShowOptions();assert(not P:UXDirty())
P.pendingAppearance.size=24;P:UXClose();P.uxModal.index=2;P:OptionsAction('activate')
assert(not P.optionsFrame:IsShown() and P.db.appearance.size==20 and reloads==1)
-- Picker/dialog controls are pooled: repeated opens do not grow native frames.
P:ShowVoiceOptions();P.voiceMic.scripts.OnClick();P:OptionsAction('close');local count=#widgets
for i=1,40 do P.voiceMic.scripts.OnClick();P:OptionsAction('right');P:OptionsAction('close') end
assert(#widgets==count and #P.uxModalPool<=7)
-- Four-way movement never changes values, sends messages, or triggers reload.
for _,page in ipairs({'Controls','Voice','Appearance','Help'}) do
 P:UXSetPage(page)
 local current=P.pendingVoice.mic;local before=reloads
 for i=1,50 do P:OptionsAction(({'up','down','left','right'})[(i-1)%4+1]) end
 assert(P.pendingVoice.mic==current and reloads==before and not P.uxModal and not P.voiceCapture)
 for _,b in ipairs(P.uxControls) do local r=b.optionRect;assert(r.x+r.w<=780 and r.y+38<=550) end
end
-- Voice capture remains isolated and cancellable.
P:UXSetPage('Voice');P.voiceKey.scripts.OnClick();assert(P.voiceCapture)
local index=P.uxIndex;P:OptionsAction('right');assert(P.voiceCapture and P.uxIndex==index)
P:OptionsAction('close');assert(not P.voiceCapture)
-- Probe v2 supplies Trade/whisper destinations without speech or sending.
P.uxAllowHide=true;P.optionsFrame:Hide();focus=nil
function GetChannelList() return 7,'Trade - City',false end
function GetChannelName(id) return id==7 and 7 or 0 end
P:RememberVoiceChannel('CHANNEL',7);P:PrepareVoiceProbe()
assert(P.pttBox:GetText()=='PCVQ2;1;NORMAL;%2F7;Trade');P:PTTReleaseFocus()
P:RememberVoiceChannel('WHISPER','Friend-Realm');P:PrepareVoiceProbe()
assert(P.pttBox:GetText():find(';%%2Fw Friend%-Realm;Whisper Friend%-Realm$'));P:PTTReleaseFocus()
P:ShowVoiceOptions();P:UXSetPage('Help');P.voiceRecover.scripts.OnClick();P:PrepareVoiceProbe()
assert(P.pttBox:GetText():find(';RECOVER;'))
local token=P.voiceProbeToken
P:ReadVoiceStatus('PCVRD1;999;0;do%20not%20accept');assert(P.voiceRecoveryRequested)
P:ReadVoiceStatus('PCVRD1;'..token..';1;Recovered%20words')
assert(not P.voiceRecoveryRequested and not focus and P.uxModalTitle:GetText()=='Recovered message')
assert(P.uxModalText:GetText():find('uncertain') and #sent==0)
P.uxModal.index=1;P:OptionsAction('activate')
assert(P.open and P:GetText()=='Recovered words' and #sent==0)
''')
report={'passed':True,'actualTocLoaded':True,'checks':['four readable pages with preserved pending edits','finite lists, current selection, paging and cancellation','Save/Discard/Stay including external hide and combat suspension','appearance wire and saved voice migration','bounded modal frame pool','200 navigation actions without setting changes','PTT capture isolation','bounded v2 Trade/whisper probe','explicit recovery, wrong-token rejection, uncertainty and editable draft without sending'],
        'liveVerified':False,'gameInput':False,'audioCaptured':False}
(root/'ADDON-REPORT.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(json.dumps(report,indent=2))
