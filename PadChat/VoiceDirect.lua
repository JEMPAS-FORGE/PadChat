local _, P = ...
-- Companion handoff uses the small existing bridge edit box, never OPENCHAT.
-- Sending occurs only in the normal input-bound button callback. Do not call
-- it from timers/text-change callbacks, bypass restrictions or retry a send.
local previousPrepare, previousInit, previousRelease = P.PreparePTT, P.InitPTT, P.PTTReleaseFocus
local channels = {['/s']='SAY',['/y']='YELL',['/p']='PARTY',['/ra']='RAID',
 ['/g']='GUILD',['/o']='OFFICER',['/i']='INSTANCE_CHAT',['/rw']='RAID_WARNING',['/e']='EMOTE'}
local function checksum(value)
 local a,b=1,0
 for i=1,#value do a=(a+value:byte(i))%65521;b=(b+a)%65521 end
 return string.format('%.0f',b*65536+a)
end
local function decode(value)
 if value:gsub('%%[0-9A-F][0-9A-F]',''):find('[^A-Za-z0-9_.~-]') then return end
 return value:gsub('%%([0-9A-F][0-9A-F])',function(v) return string.char(tonumber(v,16)) end)
end
local function destination(command)
 if channels[command] then return channels[command] end
 local target=command:match('^/w ([^%s/|;%c]+)$')
 if target and #target<=100 then return 'WHISPER',target end
 local id=tonumber(command:match('^/([1-9]%d?)$'))
 if id and id>=1 and id<=99 then return 'CHANNEL',tostring(id) end
end
function P:DirectVoiceMarker(value)
 self.directVoiceMarker=value;self.directVoiceWriting=true
 self.pttBox:SetText(value);self.directVoiceWriting=false
 self.pttBox:HighlightText()
end
function P:ReceiveDirectVoice(value)
 if self.directVoiceWriting or not self.directVoiceToken then return end
 local id,token,command,encoded,sum=value:match('^PCVD1;(%d+);([0-9.]+);([^;]+);([^;]*);(%d+)$')
 local valid=id and #id<=10 and token==self.directVoiceToken and
  self.directVoiceDeadline and GetTime()<=self.directVoiceDeadline and #value<=2200
 local body=valid and decode(encoded)
 local kind,target
 if valid then kind,target=destination(command) end
 if not body or #body==0 or #body>245-#command-1 or body:find('[%c|]') or
  body:match('^%s*$') or not kind or checksum(command..'\n'..body)~=sum then
  -- Held keys may type into the invisible transport. Keep acknowledgements
  -- immutable; an incomplete/malformed payload can never become a send.
  self:DirectVoiceMarker(self.directVoiceMarker);return
 end
 if self.directVoicePending or self.directVoiceConsumed then
  self:DirectVoiceMarker(self.directVoiceMarker);return
 end
 self.directVoicePending={id=id,token=token,command=command,body=body,kind=kind,
  target=target,expires=GetTime()+3}
 self:DirectVoiceMarker('PCVD_ACK1:'..token..';'..id..';READY')
end
function P:CommitDirectVoice()
 local pending=self.directVoicePending
 if not pending then return end
 -- Refuse a different input field, stale channel, menu, or changed draft.
 local focus=GetCurrentKeyBoardFocus and GetCurrentKeyBoardFocus()
 local command=self:VoiceDestination()
 local problem=GetTime()>pending.expires or command~=pending.command or
  focus and focus~=self.pttBox or self:IsOpen() or self.tvActionFailed or
  self.voiceOptionsFrame and self.voiceOptionsFrame:IsShown() or
  self.optionsFrame and self.optionsFrame:IsShown()
 self.directVoicePending=nil;self.directVoiceConsumed=true
 self:PTTReleaseFocus()
 local state='REFUSED'
 local send=C_ChatInfo and C_ChatInfo.SendChatMessage or SendChatMessage
 if not problem and type(send)=='function' then
  -- Consume before invoking: even an error after issuance must not retry.
  local ok=pcall(send,pending.body,pending.kind,nil,pending.target)
  state=ok and 'ISSUED' or 'ERROR'
 end
 self:DirectVoiceMarker('PCVD_ACK1:'..pending.token..';'..pending.id..';'..state..';0')
 -- A newly opened chat/search draft must retain its focus on refusal. The
 -- companion will treat an unavailable acknowledgement as uncertain, not retry.
 if not focus or focus==self.pttBox then
  self.pttBox:Show();self.pttBox:SetFocus();self.pttBox:HighlightText()
  local manager=GamepadMode and GamepadMode.FrameControlsManager
  self:DirectVoiceMarker('PCVD_ACK1:'..pending.token..';'..pending.id..';'..state..';'..(manager and manager.isUIFocused and '1' or '0'))
 end
end
function P:PTTReleaseFocus()
 previousRelease(self)
 if self.directVoiceConsumed then self.directVoiceToken=nil;self.directVoicePending=nil end
end
function P:PreparePTT(invite)
 self.directVoicePending=nil;self.directVoiceToken=nil;self.directVoiceConsumed=nil
 previousPrepare(self,invite)
 -- Invites retain their independently guarded legacy path.
 if invite or not self.pttBox:IsShown() then return end
 local token,command,label=self.pttBox:GetText():match('^CKPTT_READY2:([0-9.]+);([^;]+);(.+)$')
 if token and destination(command) then
  self.directVoiceToken=token;self.directVoiceDeadline=GetTime()+3
  self:DirectVoiceMarker('CKPTT_READY3:'..token..';'..command..';'..label)
 end
end
function P:InitPTT()
 previousInit(self)
 if self.directVoiceButton or not self.pttBox then return end
 self.pttBox:SetMaxLetters(2400)
 local changed=self.pttBox:GetScript('OnTextChanged')
 self.pttBox:SetScript('OnTextChanged',function(box,...)
  if self.directVoiceToken then self:ReceiveDirectVoice(box:GetText())
  elseif changed then changed(box,...) end
 end)
 local button=self.NewFrame('Button','PadChatPTTDirectSend')
 button:RegisterForClicks('AnyUp')
 button:SetScript('OnClick',function() self:CommitDirectVoice() end)
 self.directVoiceButton=button
 local function bind()
  if not InCombatLockdown() and not self.tvActionFailed then
   SetOverrideBindingClick(self.pttOwner,true,'CTRL-SHIFT-F1',button:GetName())
  end
 end
 local events=self.pttOwner:GetScript('OnEvent')
 self.pttOwner:SetScript('OnEvent',function(...)
  if events then events(...) end
  C_Timer.After(.1,bind)
 end)
 bind()
end
