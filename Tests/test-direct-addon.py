from pathlib import Path
import json,urllib.parse,zlib
from lupa.lua51 import LuaRuntime
root=Path(__file__).resolve().parent
l=LuaRuntime(unpack_returned_tuples=True)
l.execute('''
P={}; clock=1; focus=nil; combat=false; sent={}; widgets={}; bindings={}; broken=false
UIParent={}; GameFontNormal={}; GameFontNormalLarge={}; LE_PARTY_CATEGORY_INSTANCE=2
C_Timer={After=function(_,fn) fn() end}
function GetTime() return clock end
function GetCurrentKeyBoardFocus() return focus end
function InCombatLockdown() return combat end
function IsInGroup() return true end; function IsInRaid() return true end;function IsInGuild() return true end
function GetChannelName(id) return id==4 and 4 or 0 end
function SetOverrideBindingClick(owner,_,key,name) assert(not combat);bindings[key]=name end
function SetOverrideBinding(owner,_,key,name) assert(not combat);bindings[key]=name end
local F={};F.__index=F
for _,n in ipairs({'SetSize','SetPoint','SetFontObject','SetAutoFocus','SetMaxLetters','SetAlpha','RegisterForClicks','RegisterEvent','SetFrameStrata','SetAllPoints','HighlightText','SetTextColor'}) do F[n]=function() end end
function F:SetScript(k,v) self.scripts[k]=v end
function F:GetScript(k) return self.scripts[k] end
function F:GetName() return self.name end
function F:Show() self.shown=true end;function F:Hide() self.shown=false end
function F:IsShown() return self.shown end
function F:SetText(value) self.text=value;if self.scripts.OnTextChanged then self.scripts.OnTextChanged(self) end end
function F:GetText() return self.text or '' end
function F:SetFocus() focus=self end;function F:ClearFocus() if focus==self then focus=nil end end
function F:CreateFontString() return setmetatable({scripts={}},F) end
function P.NewFrame(_,name) local f=setmetatable({scripts={},name=name},F);widgets[name or #widgets+1]=f;return f end
function P:IsOpen() return self.open or false end;function P:GetText() return self.draft or '' end
function P:Close() self.open=false end
C_ChatInfo={SendChatMessage=function(body,kind,lang,target)
 assert(not focus,'Direct send stole chat focus');if broken then error('fixture API restriction') end
 sent[#sent+1]={body=body,kind=kind,target=target}
end}
''')
for filename in ('VoicePTT.lua','VoiceDirect.lua'):
 l.execute("assert(loadstring(...))('PadChat',P)",(root.parent/'PadChat'/filename).read_text(encoding='utf-8-sig'))
l.execute('P:InitPTT();assert(bindings["CTRL-SHIFT-F1"]=="PadChatPTTDirectSend")')
def packet(command,body,token='1',id='7'):
 return f'PCVD1;{id};{token};{command};{urllib.parse.quote(body,safe="-._~")};{zlib.adler32((command+chr(10)+body).encode())}'
def prepare(kind='SAY',target=None,token='1'):
 l.execute('local token,kind,target=...;clock=tonumber(token);P:PTTReleaseFocus();P:RememberVoiceChannel(kind,target);P:PreparePTT()',token,kind,target)
 assert l.eval('P.pttBox:GetText()').startswith('CKPTT_READY3:')
def stage(command,body,token='1'):
 l.execute('P.pttBox:SetText(...)',packet(command,body,token))
 assert l.eval('P.pttBox:GetText()')==f'PCVD_ACK1:{token};7;READY'
def commit():l.execute('P:PTTReleaseFocus();widgets.PadChatPTTDirectSend.scripts.OnClick()')
for kind,command,target in [('SAY','/s',None),('PARTY','/p',None),('WHISPER','/w Friend-Realm','Friend-Realm'),('CHANNEL','/4',4)]:
 for combat in (False,True):
  l.globals().combat=combat;prepare(kind,target);stage(command,'Hello café; turn right / not a command')
  l.execute("for i=1,50 do P.pttBox:SetText(P.pttBox:GetText()..'w') end")
  assert l.eval('P.pttBox:GetText()')=='PCVD_ACK1:1;7;READY'
  before=l.eval('#sent');commit();assert l.eval('#sent')==before+1
  assert l.eval('sent[#sent].kind')==kind and l.eval('sent[#sent].body')=='Hello café; turn right / not a command'
  assert l.eval('P.pttBox:GetText()')=='PCVD_ACK1:1;7;ISSUED;0'
  commit();assert l.eval('#sent')==before+1
l.globals().combat=False
for wire in [packet('/s','hello',token='999'),packet('/run','hello'),packet('/s','bad\nmessage'),packet('/s','bad|message'),packet('/s','x'*250),packet('/s','hello')+'w',packet('/s','hello').replace('hello','other'), 'PCVD1;7;1;/s;%ZZ;12']:
 prepare();l.execute('P.pttBox:SetText(...)',wire);assert l.eval('P.directVoicePending') is None
 before=l.eval('#sent');commit();assert l.eval('#sent')==before
for reason in ('expired','changed-channel','other-focus','menu','open-keyboard','api-error'):
 prepare();stage('/s','hello');l.execute('P:PTTReleaseFocus()')
 if reason=='expired':l.execute('clock=5')
 elif reason=='changed-channel':l.execute("P:RememberVoiceChannel('PARTY')")
 elif reason=='other-focus':l.execute('focus={draft="keep"}')
 elif reason=='menu':l.execute('P.optionsFrame={IsShown=function() return true end}')
 elif reason=='open-keyboard':l.execute('P.open=true')
 else:l.execute('broken=true')
 before=l.eval('#sent');l.execute('widgets.PadChatPTTDirectSend.scripts.OnClick()');assert l.eval('#sent')==before
 assert l.eval('P.directVoicePending') is None
 assert l.eval('P.pttBox:GetText()').endswith('ERROR;0' if reason=='api-error' else 'REFUSED;0')
 if reason=='other-focus':assert l.eval('focus.draft')=='keep'
 l.execute('P.open=false;P.optionsFrame=nil;broken=false;focus=nil')
prepare();stage('/s','hello');l.execute('GamepadMode={FrameControlsManager={isUIFocused=true}}');commit()
assert l.eval('P.pttBox:GetText()')=='PCVD_ACK1:1;7;ISSUED;1'
l.execute('P:PTTReleaseFocus();assert(not focus and not P.directVoiceToken);P:ReturnPTTToGame()')
assert l.eval('P.pttBox:GetText()').startswith('CKPTT_EXIT:')
prepare();l.execute('P:PTTReleaseFocus();P:PreparePTT("character")');assert l.eval('P.pttBox:GetText()').startswith('CKPTT_READY2:')
result={'passed':True,'actualPublicCandidateBridgeAndNewDirectModule':True,'checks':['Say/party/whisper/channel routing','mock combat input callback','UTF-8 and delimiter checksum','50 repeated W text edits cannot corrupt staged ack','one send per token, no duplicate retry','stale/wrong token, command injection, malformed/oversize input refusal','expiry, changed destination, external draft/menu/keyboard and API error refusal','invite remains legacy'],'noGameInputOrSending':True,'liveApiRestrictionsVerified':False}
(root/'DIRECT-ADDON-TEST.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps(result,indent=2))
