from pathlib import Path
import runpy
root=Path(__file__).resolve().parent.parent
l=runpy.run_path(str(root/'Tests/test-runtime.py'))['l']
l.execute(r'''
combat=false;focus=nil;reloads=0
function GetServerTime() return 1800000000 end
function ReloadUI() reloads=reloads+1 end
P:ShowVoiceOptions()
local saved=P.db.voiceOptions
local oldKey=saved.key
P.pendingVoice.key=6;P.pendingVoice.keyName='BUTTON5';P.pendingVoice.kind='mouse'
local sentBefore=#sent
-- Controller can navigate to and activate recovery without a working PTT key.
local index
for i,c in ipairs(P.voiceControls) do if c==P.voiceRestart then index=i end end
assert(index)
P.voiceIndex=index;P:VoiceOptionsAction('activate')
assert(reloads==0 and P.voiceRestartConfirm and P.voiceRestart.text:GetText():find('Confirm'))
-- Confirmation times out; merely coming back later cannot restart.
clock=clock+9;P:VoiceOptionsAction('activate');assert(reloads==0)
combat=true;assert(not P:RestartVoiceFromGame() and reloads==0);combat=false
local external=CreateFrame('EditBox');external:SetText('keep my typed draft');external:SetFocus()
assert(not P:RestartVoiceFromGame() and external:GetText()=='keep my typed draft' and reloads==0)
external:ClearFocus();P.voiceCapture=true;assert(not P:RestartVoiceFromGame());P.voiceCapture=false
P:VoiceOptionsAction('activate')
assert(reloads==1 and P.db.voiceRecoveryWire=='PCVR1;1800000000;1')
assert(P.db.voiceOptions==saved and saved.key==oldKey and #sent==sentBefore)
assert(not P.pendingVoice and not P.voiceRestartConfirm and not P.voiceOptionsFrame:IsShown())
-- Reopening requires confirmation again, even after a previous reload request.
P:ShowVoiceOptions();P.voiceRestart.scripts.OnClick();assert(reloads==1)
P.voiceRestart.scripts.OnClick();assert(reloads==2 and P.db.voiceRecoveryWire=='PCVR1;1800000000;2')
assert(#sent==sentBefore)
''')
print('PASS: controller-selectable recovery, explicit expiring confirmation, combat/capture/foreign-draft refusal, bounded request, reload-only restart, unsaved/saved settings isolation and no send.')
