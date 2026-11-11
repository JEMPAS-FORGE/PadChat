from pathlib import Path
import json
import sys
import os

baseline = Path(__file__).resolve().parent.parent
fixture = (baseline / 'Tests/test-direct-addon.py').read_text()
fixture = fixture.split('for kind,command,target in ')[0]
addon = Path(os.environ.get('PADCHAT_ADDON_TEST_ROOT', str(baseline / 'PadChat')))
fixture = fixture.replace("(root.parent/'PadChat'/filename)", '(addon/filename)')
scope = {'__file__': str(baseline / 'Tests/test-direct-addon.py'), 'addon': addon}
exec(compile(fixture, 'shipping-direct-fixture', 'exec'), scope)
lua = scope['l']
results = []
for key in 'wasd':
    lua.execute("focus=nil;P.voiceCapture=nil;P:RememberVoiceChannel('SAY');P:PrepareVoiceProbe()")
    expected = lua.eval('P.pttBox:GetText()')
    lua.execute('P.pttBox:SetText(...)', key)
    results.append({'key': key, 'protected': lua.eval('P.pttBox:GetText()') == expected,
                    'stillFocused': lua.eval('focus==P.pttBox'), 'messagesSent': lua.eval('#sent')})
print(json.dumps({'addon': str(addon), 'movementProbeChecks': results}, indent=2))
if True:
    assert all(row['protected'] for row in results), 'Held movement destroyed the readiness reply'
    lua.execute('''
local function prepare()
 focus=nil;clock=1;P.voiceCapture=nil;P.voiceOptionsFrame=nil
 P:RememberVoiceChannel('SAY');P:PrepareVoiceProbe()
 return P.pttBox:GetText(),P.voiceProbeToken
end
for _,state in ipairs({'ready','loading','disabled','error','test'}) do
 local marker,token=prepare()
 for _,bad in ipairs({'w','a','s','d',' ',marker..'www','PCVS1;wrong;ready;0;','PCVS1;'..token..';bogus;0;'}) do
  for i=1,10 do P.pttBox:SetText(bad);assert(P.pttBox:GetText()==marker) end
 end
 P.pttBox:SetText('PCVS1;'..token..';'..state..';0;')
 assert(not focus and not P.pttBox:IsShown() and not P.voiceProbeToken and not P.voiceProbeMarker)
end
-- The TEST/RECOVER reply paths must still accept complete, matching packets.
prepare();P.voiceOptionsFrame={IsShown=function()return true end};P:PrepareVoiceProbe()
assert(P.pttBox:GetText():find(';TEST;'))
P.pttBox:SetText('PCVS1;'..P.voiceProbeToken..';test;50;test%20words')
assert(P.voiceCompanionStatus:find('test words') and not focus)
prepare();P.voiceRecoveryRequested=true;P:PrepareVoiceProbe()
assert(P.pttBox:GetText():find(';RECOVER;'))
local recovered=nil;P.VoiceRecoveryRetrieved=function(_,words,uncertain) recovered={words,uncertain} end
P.pttBox:SetText('PCVRD1;'..P.voiceProbeToken..';0;unsent%20words')
assert(recovered[1]=='unsent words' and not recovered[2] and not focus)
-- Timeouts, abort, cleanup, and fresh probes cannot revive an old marker.
prepare();clock=5;P.pttBox:SetText('expired');assert(P.pttBox:GetText()=='expired')
P:PTTReleaseFocus();assert(not P.voiceProbeMarker and not P.voiceProbeToken and not P.voiceProbeDeadline)
prepare();P:AbortDirectVoice();assert(not focus and not P.voiceProbeMarker)
prepare();P.pttBox.scripts.OnKeyDown(P.pttBox,'F11');assert(not focus and not P.voiceProbeMarker)
-- Foreign inputs and binding capture remain protected, including blank inputs.
local external=P.NewFrame('EditBox','ExternalDraft');external:SetText('private draft');external:SetFocus()
P:PrepareVoiceProbe();assert(focus==external and external:GetText()=='private draft')
external:SetText('');P:PrepareVoiceProbe();assert(focus==external and external:GetText()=='')
focus=nil;P.voiceCapture=true;P:PrepareVoiceProbe();assert(not focus and not P.voiceProbeToken)
assert(#sent==0)
''')
    print('PASS: repeated WASD, malformed/stale replies, valid statuses, TEST/RECOVER, expiry, abort/F11 cleanup, foreign drafts, binding capture, and no messages sent.')
