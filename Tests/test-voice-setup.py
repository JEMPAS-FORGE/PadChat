from pathlib import Path
import runpy
root=Path(__file__).resolve().parent.parent
l=runpy.run_path(str(root/'Tests/test-runtime.py'))['l']
l.execute('''
P:ShowVoiceOptions();P.pendingVoice.pad='NONE';P.pendingVoice.mic='Test Microphone'
local base={F8='OPENALLBAGS'}
function GetBindingAction(key,overrides) assert(overrides==false);return base[key] or '' end
local reloads=0;function ReloadUI() reloads=reloads+1 end
local draft=P:GetText();local before=#sent
P:SaveVoiceWithWarnings();assert(reloads==0 and P.voiceSave.text:GetText()=='Accept conflicts & reload')
assert(P.voiceInfo:GetText():find('OPENALLBAGS'))
P.voiceOptionsFrame:Hide();P:ShowVoiceOptions();P.pendingVoice.pad='NONE'
P:SaveVoiceWithWarnings();assert(reloads==0) -- dismissal cannot retain consent
base.F8='TOGGLEBAG1';P:SaveVoiceWithWarnings();assert(reloads==0 and P.voiceInfo:GetText():find('TOGGLEBAG1'))
P:SaveVoiceWithWarnings();assert(reloads==1 and base.F8=='TOGGLEBAG1')
P:ShowVoiceOptions();P.pendingVoice.pad='NONE';P.pendingVoice.mods=1
assert(P:VoiceBindingWarnings(P.pendingVoice)=='') -- Ctrl-F8 is separate
P.pendingVoice.pad='NORTH';P.pendingVoice.padMod='LTRIGGER';base.PAD4='ACTIONBUTTON3'
assert(P:VoiceBindingWarnings(P.pendingVoice):find('ACTIONBUTTON3'))
P.pendingVoice.enabled=false;assert(P:VoiceBindingWarnings(P.pendingVoice)=='');P.pendingVoice.enabled=true
P.pendingVoice.pad='NONE';P.pendingVoice.mods=0;base={}
for _,bad in ipairs({'/invite somebody','bad~name','bad`name','bad;name',string.rep('x',601),string.rep('a,',32)..'a'}) do assert(not P:ValidVoiceWords(bad),bad) end
assert(P:ValidVoiceWords("Thunder Bluff, Friend-Realm, é"))
P:BeginVoiceWords();P.voiceWordsBox:SetText('Thunder Bluff, Friend-Realm');P:EndVoiceWords(true)
assert(not focus and P.pendingVoice.vocabulary=='Thunder Bluff, Friend-Realm')
P:BeginVoiceWords();P.voiceWordsBox:SetText('discard me');P:VoiceOptionsAction('close')
assert(not P.voiceWordsEditing and P.pendingVoice.vocabulary=='Thunder Bluff, Friend-Realm')
P:NextVoiceGuide();assert(P.db.voiceGuideStep==1)
P.pendingVoice.mic='';P:NextVoiceGuide();assert(P.db.voiceGuideStep==1)
P.pendingVoice.mic='Test Microphone';P:NextVoiceGuide();assert(P.db.voiceGuideStep==2)
P.pendingVoice.key=27;P:NextVoiceGuide();assert(P.db.voiceGuideStep==2)
P.pendingVoice.key=119;P:NextVoiceGuide();assert(P.db.voiceGuideStep==3)
P:SaveVoiceWithWarnings();assert(reloads==2 and P.db.voiceGuideStep==4)
assert(P.db.voiceSettingsWire:find('Thunder Bluff%%2C Friend%-Realm$'))
P:InitDB();P:ShowVoiceOptions();assert(P.voiceInfo:GetText():find('Setup 4/4'))
P:PrepareVoiceProbe();local token=P.voiceProbeToken
P:ReadVoiceStatus('PCVS1;'..(tonumber(token)+1)..';test;20;hello');assert(P.db.voiceGuideStep==4)
P:ReadVoiceStatus('PCVS1;'..token..';ready;20;hello');assert(P.db.voiceGuideStep==4)
P:PrepareVoiceProbe();token=P.voiceProbeToken
P:ReadVoiceStatus('PCVS1;'..token..';test;20;');assert(P.db.voiceGuideStep==4)
P:PrepareVoiceProbe();token=P.voiceProbeToken
P:ReadVoiceStatus('PCVS1;'..token..';test;20;hello');assert(P.db.voiceGuideComplete and not P.db.voiceGuideStep)
assert(#sent==before and P:GetText()==draft)
P.voiceOptionsFrame:Hide()
''')
print('Passed conflict acknowledgement/invalidation, guided setup/resume/owned non-delivery test, vocabulary encoding/edit cancellation and controller access.')
