from pathlib import Path
import hashlib, json, re, runpy

root = Path(__file__).resolve().parent.parent
l = runpy.run_path(str(root / 'Tests/test-runtime.py'))['l']
api_path = root / 'API/RestrictedFrames.lua'
api = api_path.read_text(encoding='utf-8-sig')
baseline = root / 'Tests/Fixtures/Options-before-combat-fix.lua'
installed = baseline.read_text(encoding='utf-8-sig')
old = re.search(r"SetAttribute\('_onstate-combat',\[\[(.*?)\]\]\)", installed, re.S).group(1)

# Use the pinned implementation's actual validation and handle methods, with
# only the native engine's handle lookup/protection facts supplied by fixtures.
support = api[api.index('local function GetPossiblyForbiddenHandleFrame'):api.index('-- "GETTER" methods')]
hide = api[api.index('function HANDLE:Hide('):api.index('function HANDLE:SetID(')]
refs = api[api.index('function HANDLE:GetFrameRef('):api.index('function HANDLE:GetEffectiveAttribute(')]
clear = api[api.index('function HANDLE:ClearBindings('):api.index('function HANDLE:ClearBinding(')]
l.execute('''
HANDLE={};handleFrames={};frameHandles={};restrictedHide=false
local Frame=getmetatable(P.optionOwner)
local normalHide=Frame.Hide
function Frame:Hide()
 assert(not combat or not self.protected or restrictedHide,'Insecure protected Hide during combat')
 normalHide(self)
end
function frameHandle(frame)
 if not frameHandles[frame] then
  local handle=newproxy(true);getmetatable(handle).__index=HANDLE
  frameHandles[frame]=handle;handleFrames[handle]=frame
 end
 return frameHandles[frame]
end
function GetFrameHandleFrame(handle) local frame=handleFrames[handle];return frame,frame and frame.protected end
function IsFrameHandle(handle) return handleFrames[handle]~=nil end
function AddReferencedFrame() end
function CheckForbidden() return false end
function PropagateForbiddenToReferencedFrames() error('Unexpected forbidden propagation') end
function scrub(value) return value end
LOCAL_CHECK_Frame={
 IsProtected=function(frame) return frame.protected end,
 GetAttribute=function(frame,key)
  local ref=key:match('^frameref%-(.+)$')
  if ref then local target=frame.refs and frame.refs[ref];return target and frameHandle(target) end
  return frame.attrs[key]
 end,
 Hide=function(frame) restrictedHide=true;frame:Hide();restrictedHide=false end,
 SetAttribute=function(frame,key,value) frame.attrs[key]=value end,
}
P.optionOwner.protected=true;P.keyboardOwner.protected=true;P.sendButton.protected=true
''')
l.execute(support + hide + refs + clear)
l.globals().oldCombatSnippet = old
staged = (root / 'PadChat/Options.lua').read_text(encoding='utf-8-sig')
l.globals().stagedCombatSnippet = re.search(r"SetAttribute\('_onstate-combat',\[\[(.*?)\]\]\)", staged, re.S).group(1)
l.execute('''
combat=false;P:ShowOptions();P:BuildVoiceOptions()
-- These regular panels can be hidden by the ordinary event handler in combat.
assert(not P.optionsFrame.protected and not P.voiceOptionsFrame.protected)
P.optionOwner.refs={panel=P.optionsFrame,voice=P.voiceOptionsFrame}
local old=assert(loadstring(oldCombatSnippet))
setfenv(old,{self=frameHandle(P.optionOwner),newstate='active'})
combat=true
local ok,why=pcall(old)
assert(not ok and tostring(why):find('Invalid frame handle'),tostring(why))
combat=false
local staged=assert(loadstring(stagedCombatSnippet))
setfenv(staged,{self=frameHandle(P.optionOwner),newstate='active'})
combat=true;staged();combat=false
-- Execute the corrected production snippet through the same real validator.
local current=assert(loadstring(P.optionOwner.attrs['_onstate-combat']))
setfenv(current,{self=frameHandle(P.optionOwner),newstate='active'})
local send=assert(loadstring(P.keyboardOwner.attrs['_onstate-combat']))
setfenv(send,{self=frameHandle(P.keyboardOwner),newstate='active'})
for _,panel in ipairs({'closed','keyboard','options','voice'}) do
 combat=false;P:Close();if P.optionsFrame then P.optionsFrame:Hide() end;if P.voiceOptionsFrame then P.voiceOptionsFrame:Hide() end
 P:SetText('keep combat draft')
 if panel=='keyboard' then P:Open()
 elseif panel=='options' then P:ShowOptions()
 elseif panel=='voice' then P:ShowVoiceOptions() end
 combat=true;current();send()
 P.openBindingOwner.scripts.OnEvent(P.openBindingOwner,'PLAYER_REGEN_DISABLED')
 assert(not P.open and not P.optionsFrame:IsShown() and not P.voiceOptionsFrame:IsShown() and not P.sendButton:IsShown())
 assert(P:GetText()=='keep combat draft')
 for _,binding in pairs(bindings) do assert(binding.owner~=P.optionOwner and binding.owner~=P.keyboardOwner) end
 combat=false;P.openBindingOwner.scripts.OnEvent(P.openBindingOwner,'PLAYER_REGEN_ENABLED')
 assert(not P.cleanupPending and not P.tvActionFailed)
 P:Open();assert(P.open);P:Close()
end
''')
report = {'pinnedSource': '9465cb273b5513495d8ecc12fbb19930dd6b8957',
          'apiFileSha256': hashlib.sha256(api_path.read_bytes()).hexdigest(),
          'oldPublicCombatSnippetReproducesInvalidHandle': True,
          'correctedSnippetPassesPinnedValidator': True,
          'stagedInstalledSnippetPassesPinnedValidator': True,
          'closedKeyboardOptionsVoiceCombatCyclesPass': True,
          'liveCombatVerified': False}
(root / 'COMBAT-TEST-REPORT.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print('PASS: old installed combat snippet reproduces Invalid frame handle; corrected owner/Hide paths pass pinned validator, draft/binding/focus recovery checks. Live gameplay remains unverified.')
