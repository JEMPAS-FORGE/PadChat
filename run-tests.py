from pathlib import Path
import json,runpy
ROOT=Path(__file__).resolve().parent
CHECK=ROOT/"Tests"
results=[]
for name in ('test-padchat.py','test-runtime.py','test-options.py','test-invites.py','test-voice-options.py','test-voice-setup.py','test-speech-recovery.py'):
 print('Running',name,flush=True)
 runpy.run_path(str(CHECK/name));results.append({'name':name,'passed':True})

l=runpy.run_path(str(CHECK/'test-runtime.py'))['l']
l.execute('''
combat=false;focus=nil;P:SetText('');P:Close();P.db.controllerSetupSeen=nil
C_GamePad={GetAllDeviceIDs=function() return {1} end,GetDeviceRawState=function() return {vendorID=1118,productID=654} end}
P.db.bindings={keyboard='F9',button='AUTO',modifier='NONE'}
P:ControllerOnboarding();assert(P.optionsFrame:IsShown() and P.db.controllerSetupSeen)
P:OptionsAction('down');P:OptionsAction('next')
P.optionsFrame:Hide();P:ControllerOnboarding();assert(not P.optionsFrame:IsShown())
-- Custom choices and native touchpad opening are not replaced.
P.db.bindings.button='PADLSTICK';P.db.controllerSetupSeen=nil;P:ControllerOnboarding();assert(not P.optionsFrame:IsShown())
C_GamePad.GetDeviceRawState=function() return {vendorID=1356} end
P.db.bindings.button='AUTO';P:ControllerOnboarding();assert(not P.optionsFrame:IsShown() and P:ControllerOpenBinding()=='PADBACK')
for _,size in ipairs({{640,360},{800,450},{1280,720},{1920,1080}}) do
 for _,panel in ipairs({{780,480},{690,445},{740,640}}) do
  local scale=P.PanelFit(panel[1],panel[2],size[1],size[2])
  assert(panel[1]*scale<=size[1]-24+.001 and panel[2]*scale<=size[2]-24+.001)
 end
end
-- Status accepted only for the outstanding addon-owned request.
P:ShowVoiceOptions();P:PrepareVoiceProbe();local token=P.voiceProbeToken
assert(P.pttBox:GetText():find(';TEST$'))
P:ReadVoiceStatus('PCVS1;999;ready;20;');assert(focus==P.pttBox)
P:ReadVoiceStatus('PCVS1;'..token..';test;140;hello%20world')
assert(not focus and not P.pttBox:IsShown() and P.voiceInfo:GetText():find('100%%') and P.voiceInfo:GetText():find('nothing sent'))
local before=#sent;P:PrepareVoiceProbe();P:ReadVoiceStatus('PCVS1;'..P.voiceProbeToken..';test;20;hello%7Cbad');assert(#sent==before)
P:PTTReleaseFocus();P.voiceOptionsFrame:Hide()
local external=CreateFrame('EditBox');external:SetText('private draft');external:SetFocus();P:PrepareVoiceProbe();assert(focus==external and external:GetText()=='private draft');external:ClearFocus()
P:PrepareVoiceProbe();assert(P.pttBox:GetText():find(';NORMAL$'));P:PTTReleaseFocus()
P:ShowVoiceOptions();P.pendingVoice.review=true;assert(P:SaveVoiceOptions(P.pendingVoice));assert(P.db.voiceSettingsWire:find('^PCV3;') and P.db.voiceSettingsWire:find(';1;$'))
''')
results.append({'name':'responsive panels, controller onboarding, status ownership, test non-delivery, review settings','passed':True})
(ROOT/'TEST-REPORT.json').write_text(json.dumps(results,indent=2))
print('All exact candidate addon and improvements tests passed.')
