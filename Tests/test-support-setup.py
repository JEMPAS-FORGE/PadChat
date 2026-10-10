from pathlib import Path
import runpy

root=Path(__file__).resolve().parent.parent
l=runpy.run_path(str(root/'Tests/test-runtime.py'))['l']
l.execute('''
combat=false;focus=nil
P:ShowVoiceOptions()
local saved=P.db.voiceOptions
saved.key=119;saved.keyName='F8';saved.kind='keyboard';saved.pad='SHARE';saved.padMod='NONE';saved.enabled=true
P.voiceOptionsFrame:Hide();P:ShowVoiceOptions()
assert(P.voiceSavedSummary:GetText():find('Saved in WoW: F8'))
assert(P:VoiceTestInstructions():find('F8 or Share / Create / Back'))
-- Pending edits must not masquerade as an applied PTT or test shortcut.
P.pendingVoice.key=6;P.pendingVoice.keyName='BUTTON5';P.pendingVoice.kind='mouse';P.pendingVoice.padMod='LTRIGGER'
P:UpdateVoiceOptions()
assert(P.voiceKey.text:GetText():find('BUTTON5'))
assert(P.voiceSavedSummary:GetText():find('Saved in WoW: F8'))
assert(P.voiceSavedSummary:GetText():find('Unsaved changes'))
P.voiceTest.scripts.OnClick()
assert(P.voiceInfo:GetText():find('Hold F8 or Share / Create / Back'))
assert(not P.voiceInfo:GetText():find('BUTTON5'))
assert(P.voiceInfo:GetText():find('configuration windows'))
local copy={} for k,v in pairs(saved) do copy[k]=v end
P.db.voiceGuideStep=4
P.voicePrevious.scripts.OnClick();assert(P.db.voiceGuideStep==3)
P:PreviousVoiceGuide();assert(P.db.voiceGuideStep==2)
P:PreviousVoiceGuide();assert(P.db.voiceGuideStep==1)
P:PreviousVoiceGuide();assert(P.db.voiceGuideStep==1)
for k,v in pairs(copy) do assert(saved[k]==v) end
P.db.voiceGuideStep=nil;P:PreviousVoiceGuide();assert(not P.db.voiceGuideStep)
P.db.voiceGuideStep='bad';P:PreviousVoiceGuide();assert(P.db.voiceGuideStep=='bad');P.db.voiceGuideStep=nil
saved.enabled=false;assert(P:VoiceTestInstructions():find('Saved voice is Off'));saved.enabled=true
saved.padMod='LTRIGGER';assert(P:VoiceTestInstructions():find('L2 / LT') and P:VoiceTestInstructions():find('hold modifier first'))
saved.pad='NONE';saved.padMod='NONE';assert(not P:VoiceTestInstructions():find(' or '))
-- A rejected probe explains the blocker and preserves foreign text/focus.
local external=CreateFrame('EditBox');external:SetText('private draft');external:SetFocus()
local sentBefore=#sent
P:PrepareVoiceProbe();assert(focus==external and external:GetText()=='private draft')
assert(P.voiceInfo:GetText():find('text box has focus') and P.voiceCheckDeadline==nil)
external:ClearFocus();P.voiceCapture=true;P:PrepareVoiceProbe()
assert(P.voiceInfo:GetText():find('binding capture'));P.voiceCapture=false
assert(#sent==sentBefore)
P:PrepareVoiceProbe();local token=P.voiceProbeToken
P:ReadVoiceStatus('PCVS1;'..token..';ready;23;')
assert(P.voiceInfo:GetText():find('Last check') and P.voiceInfo:GetText():find('Last reported mic level'))
-- Actual declared control rectangles fit and do not collide with each other.
for i,a in ipairs(P.voiceControls) do
 local ax,ay=a.point[2],-a.point[3]
 assert(ax>=0 and ay>=0 and ax+a.w<=740 and ay+a.h<=750)
 for j,b in ipairs(P.voiceControls) do if j>i then
  local bx,by=b.point[2],-b.point[3]
  assert(ax+a.w<=bx or bx+b.w<=ax or ay+a.h<=by or by+b.h<=ay)
 end end
end
P:PTTReleaseFocus();P.voiceOptionsFrame:Hide()
''')
print('PASS: named saved PTT versus pending edits, safe test instructions, modifier/disabled controls, previous-step preservation, rejected focus/capture feedback, no delivery, and truthful last-check status.')
