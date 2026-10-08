local _, CK = ...
-- This bridge only reads Blizzard chat state. Native typing/sending is done by
-- the user's PTT helper through normal keyboard input, including in combat.
local prefix = {SAY='/s', YELL='/y', PARTY='/p', RAID='/ra', GUILD='/g',
    OFFICER='/o', INSTANCE_CHAT='/i', RAID_WARNING='/rw', EMOTE='/e'}

function CK:RememberVoiceChannel(kind, target, replyType)
    if not kind then return end
    local previous=self.voiceLastChannel
    -- Keep the selected Reply destination after our own successful whisper.
    if previous and previous.chatType=='REPLY' and previous.target==target and previous.replyType==kind then return end
    self.voiceLastChannel = {chatType=kind, target=target, replyType=replyType}
end

function CK:VoiceReplyTarget()
    local name,kind
    if ChatFrameUtil and ChatFrameUtil.GetLastTellTarget then name,kind=ChatFrameUtil.GetLastTellTarget()
    elseif ChatEdit_GetLastTellTarget then name,kind=ChatEdit_GetLastTellTarget() end
    kind=kind or 'WHISPER'
    if type(name)~='string' or name=='' or #name>100 or name:find('[%c/|;]') then return end
    if kind~='WHISPER' then return end
    return name,kind
end

local function whisperAddress(name)
    -- Forever accepts First-Last as the unambiguous full character name.
    -- Use an explicit recipient, never /r, which could switch recipients if
    -- another whisper arrives between the handshake and native text entry.
    if type(name)~='string' or name=='' or #name>100 or name:find('[%c/|;]') then return end
    if name:find('%s') then
        if not (RegionalUniqueNamesEnabled and RegionalUniqueNamesEnabled()) or not name:match('^[^%s]+ [^%s]+$') then return end
        name=name:gsub(' ','-')
    end
    return name
end

function CK:VoiceReplyChoices()
    local choices,seen={},{}
    local function add(name,kind)
        if kind~='WHISPER' then return false end
        local address=whisperAddress(name)
        if not address or seen[address:lower()] then return false end
        seen[address:lower()]=true
        choices[#choices+1]={chatType='REPLY',target=name,replyType='WHISPER',label='Reply to '..name}
        return true
    end
    local name,kind=self:VoiceReplyTarget()
    local nextTell=ChatFrameUtil and ChatFrameUtil.GetNextTellTarget or ChatEdit_GetNextTellTarget
    local visited={}
    for i=1,20 do
        if type(name)~='string' or name=='' then break end
        local key=tostring(kind)..':'..name:lower()
        if visited[key] then break end
        visited[key]=true;add(name,kind)
        if not nextTell then break end
        name,kind=nextTell(name,kind)
    end
    -- Preserve senders already seen this session, even if the native recent
    -- list is displaced by more whispers. Names only; no messages are stored.
    for _,entry in ipairs(self.voiceRecentWhispers or {}) do add(entry.name,entry.kind) end
    local selected=self.voiceLastChannel
    if selected and selected.chatType=='REPLY' then add(selected.target,selected.replyType) end
    return choices
end

function CK:RememberIncomingVoiceWhisper(name)
    local address=whisperAddress(name)
    if not address then return end
    local recent=self.voiceRecentWhispers or {}
    for i=#recent,1,-1 do
        if whisperAddress(recent[i].name):lower()==address:lower() then table.remove(recent,i) end
    end
    table.insert(recent,1,{name=name,kind='WHISPER'})
    while #recent>12 do table.remove(recent) end
    self.voiceRecentWhispers=recent
end

function CK:SelectVoiceReply(name)
    if not whisperAddress(name) then self:VoiceChannelNotice('Choose a full in-game character name');return false end
    self:RememberIncomingVoiceWhisper(name)
    self:RememberVoiceChannel('REPLY',name,'WHISPER')
    self:VoiceChannelNotice('Voice chat: Reply to '..name..'\nHold Share to speak; release to send')
    return true
end

function CK:QuickGeneralChannel()
    local channels = {GetChannelList()}
    for i=1,#channels,3 do
        local id,name=channels[i],channels[i+1]
        if type(name)=='string' and (name=='General' or name:match('^General%s*%-')) then
            return id
        end
    end
end

function CK:VoiceChannelNotice(message)
    if not self.pttNotice then return end
    self.pttNotice.text:SetText(message)
    self.pttNotice:Show()
    self.pttNoticeToken=(self.pttNoticeToken or 0)+1
    local token=self.pttNoticeToken
    C_Timer.After(3,function() if CK.pttNoticeToken==token then CK.pttNotice:Hide() end end)
end

function CK:CycleVoiceChannel()
    if self.voiceOptionsFrame and self.voiceOptionsFrame:IsShown() or self.optionsFrame and self.optionsFrame:IsShown() then return end
    if self.tvActionFailed or (GetCurrentKeyBoardFocus and GetCurrentKeyBoardFocus()) then return end
    local current=self.voiceLastChannel or {chatType='SAY'}
    local general=self:QuickGeneralChannel()
    local choices={{chatType='SAY',label='Say'},{chatType='YELL',label='Yell'}}
    if general then choices[#choices+1]={chatType='CHANNEL',target=general,label='General (/'..general..')'} end
    if IsInGroup() then choices[#choices+1]={chatType='PARTY',label='Party'} end
    if IsInRaid() then choices[#choices+1]={chatType='RAID',label='Raid'} end
    if IsInGuild() then choices[#choices+1]={chatType='GUILD',label='Guild'} end
    for _,reply in ipairs(self:VoiceReplyChoices()) do choices[#choices+1]=reply end
    local index=0
    for i,v in ipairs(choices) do
        local sameTarget=v.chatType~='CHANNEL' or tonumber(v.target)==tonumber(current.target)
        if v.chatType=='REPLY' then sameTarget=v.target==current.target and v.replyType==current.replyType end
        if v.chatType==current.chatType and sameTarget then index=i;break end
    end
    local nextChannel=choices[index%#choices+1]
    self:RememberVoiceChannel(nextChannel.chatType,nextChannel.target,nextChannel.replyType)
    local label=nextChannel.label
    if current.chatType=='YELL' and not general then label=label..' â€” General unavailable here' end
    self:VoiceChannelNotice('Chat: '..label..'\nTap your voice shortcut: channel | Hold: talk')
end

function CK:VoiceDestination()
    local v = self.voiceLastChannel or {chatType='SAY'}
    local kind, target = v.chatType, v.target
    if kind=='REPLY' then
        if v.replyType~='WHISPER' then return nil,'Choose an in-game whisper recipient first' end
        local address=whisperAddress(target)
        if not address then return nil,'This whisper recipient cannot be addressed safely' end
        return '/w '..address,'Reply to '..target
    end
    if kind=='PARTY' and not IsInGroup() then return nil,'You are no longer in a party' end
    if (kind=='RAID' or kind=='RAID_WARNING') and not IsInRaid() then return nil,'You are no longer in a raid' end
    if (kind=='GUILD' or kind=='OFFICER') and not IsInGuild() then return nil,'No guild is available' end
    if kind=='INSTANCE_CHAT' and not IsInGroup(LE_PARTY_CATEGORY_INSTANCE) then return nil,'No instance group is available' end
    if kind=='WHISPER' then
        local address=whisperAddress(target)
        if not address then return nil,'Select a whisper recipient first' end
        return '/w '..address, 'Whisper '..target
    end
    if kind=='CHANNEL' then
        local id=tonumber(target)
        if not id or id<1 or id>99 or id~=math.floor(id) or (GetChannelName(id) or 0)==0 then
            return nil,'The previous chat channel is unavailable'
        end
        local general=GetChannelList and self:QuickGeneralChannel()
        return '/'..id, general==id and 'General' or 'Channel '..id
    end
    if not prefix[kind] then return nil,'This chat destination is not supported' end
    return prefix[kind],kind
end

function CK:PTTReleaseFocus()
    if self.pttBox then self.pttBox:ClearFocus(); self.pttBox:Hide() end
end

function CK:ReturnPTTToGame()
    -- Read navigation state; the companion invokes the native TOGGLEUIFOCUS
    -- binding only when necessary. Never call protected navigation from Lua.
    local focus=GetCurrentKeyBoardFocus and GetCurrentKeyBoardFocus()
    if focus and focus~=self.pttBox then
        local name=focus.GetName and focus:GetName()
        if not name or not name:match('^ChatFrame%d+EditBox$') or focus:GetText()~='' then return end
    end
    local manager=GamepadMode and GamepadMode.FrameControlsManager
    local active=manager and manager.isUIFocused
    self.pttBox:SetText('CKPTT_EXIT:'..tostring(GetTime())..'|'..(active and '1' or '0'))
    self.pttBox:Show();self.pttBox:SetFocus();self.pttBox:HighlightText()
end

function CK:PreparePTT(invite)
    if self.voiceOptionsFrame and self.voiceOptionsFrame:IsShown() or self.optionsFrame and self.optionsFrame:IsShown() then return end
    -- Never overwrite an existing input field/draft or reopen the full keyboard.
    local focus=GetCurrentKeyBoardFocus and GetCurrentKeyBoardFocus()
    if focus and focus~=self.pttBox then return end
    if self.tvActionFailed then return end
    local cmd,label
    if invite=='character' then cmd,label='/invite','Character invitation'
    elseif invite then cmd,label='/ck voiceinvite','Friend invitation'
    else cmd,label=self:VoiceDestination() end
    if self:IsOpen() and self:GetText()~='' then cmd=nil;label='Finish or clear the existing keyboard draft first' end
    if cmd and self:IsOpen() and not InCombatLockdown() then self:Close('voice') end
    local marker
    -- Pipes start WoW formatting escapes: "|Reply" loses its separator as |R.
    -- Version 2 uses semicolons, which are forbidden in whisper addresses.
    if cmd then marker='CKPTT_READY2:'..tostring(GetTime())..';'..cmd..';'..label
    else marker='CKPTT_DENIED:'..tostring(GetTime())..'|'..label end
    self.pttBox:Show()
    self.pttBox:SetText(marker)
    self.pttBox:SetFocus()
    self.pttBox:HighlightText()
end

function CK:InitPTT()
    if self.pttOwner or InCombatLockdown() then return end
    local owner=CK.NewFrame('Frame')
    self.pttOwner=owner
    local notice=CK.NewFrame('Frame',nil,UIParent)
    notice:SetSize(750,90);notice:SetPoint('TOP',UIParent,'TOP',0,-125)
    notice:SetFrameStrata('TOOLTIP')
    notice.text=notice:CreateFontString(nil,'OVERLAY','GameFontNormalLarge')
    notice.text:SetAllPoints();notice:Hide();self.pttNotice=notice
    local box=CK.NewFrame('EditBox',nil,UIParent)
    box:SetSize(1,1);box:SetPoint('BOTTOMLEFT',UIParent,'BOTTOMLEFT',0,0)
    box:SetFontObject(GameFontNormal);box:SetAutoFocus(false);box:SetMaxLetters(2400);box:SetAlpha(0)
    box:SetScript('OnKeyDown',function(_,key)
        if key=='F10' then CK:PreparePTT() elseif key=='F11' then CK:PTTReleaseFocus() end
    end)
    box:SetScript('OnEscapePressed',function() CK:PTTReleaseFocus() end)
    box:SetScript('OnTextChanged',function() CK:ReadVoiceStatus(box:GetText()) end)
    box:Hide();self.pttBox=box
    local prepare=CK.NewFrame('Button','PadChatPTTPrepare')
    prepare:RegisterForClicks('AnyUp');prepare:SetScript('OnClick',function() CK:PreparePTT() end)
    local finish=CK.NewFrame('Button','PadChatPTTFinish')
    finish:RegisterForClicks('AnyUp');finish:SetScript('OnClick',function() CK:PTTReleaseFocus() end)
    local noop=CK.NewFrame('Button','PadChatPTTNoop')
    noop:RegisterForClicks('AnyDown','AnyUp')
    local cycle=CK.NewFrame('Button','PadChatPTTCycle')
    cycle:RegisterForClicks('AnyUp');cycle:SetScript('OnClick',function() CK:CycleVoiceChannel() end)
    local exit=CK.NewFrame('Button','PadChatPTTExit')
    exit:RegisterForClicks('AnyUp');exit:SetScript('OnClick',function() CK:ReturnPTTToGame() end)
    local invite=CK.NewFrame('Button','PadChatPTTInvite')
    invite:RegisterForClicks('AnyUp');invite:SetScript('OnClick',function() CK:PreparePTT(true) end)
    local characterInvite=CK.NewFrame('Button','PadChatPTTCharacterInvite')
    characterInvite:RegisterForClicks('AnyUp');characterInvite:SetScript('OnClick',function() CK:PreparePTT('character') end)
    local probe=CK.NewFrame('Button','PadChatVoiceProbe');probe:RegisterForClicks('AnyUp');probe:SetScript('OnClick',function() CK:PrepareVoiceProbe() end)
    local function bind()
        if InCombatLockdown() or CK.tvActionFailed then return end
        SetOverrideBindingClick(owner,true,'CTRL-SHIFT-F2',probe:GetName())
        SetOverrideBindingClick(owner,true,'CTRL-SHIFT-F10',prepare:GetName())
        SetOverrideBindingClick(owner,true,'CTRL-SHIFT-F11',finish:GetName())
        SetOverrideBinding(owner,true,'CTRL-SHIFT-F9','OPENCHAT')
        SetOverrideBindingClick(owner,true,'CTRL-SHIFT-F8',cycle:GetName())
        SetOverrideBindingClick(owner,true,'CTRL-SHIFT-F7',exit:GetName())
        SetOverrideBinding(owner,true,'CTRL-SHIFT-F6','TOGGLEUIFOCUS')
        SetOverrideBindingClick(owner,true,'CTRL-SHIFT-F4',invite:GetName())
        SetOverrideBindingClick(owner,true,'CTRL-SHIFT-F3',characterInvite:GetName())
        -- Create is reserved for voice. Touchpad click remains PADBACK.
        -- Device-aware voice reservations are owned by Events/VoiceOptions.
    end
    owner:RegisterEvent('PLAYER_ENTERING_WORLD');owner:RegisterEvent('PLAYER_REGEN_ENABLED')
    owner:RegisterEvent('GAME_PAD_ACTIVE_CHANGED')
    owner:RegisterEvent('CHAT_MSG_WHISPER')
    owner:SetScript('OnEvent',function(_,event,message,sender)
        if event=='CHAT_MSG_WHISPER' then CK:RememberIncomingVoiceWhisper(sender)
        else C_Timer.After(.1,bind) end
    end)
    bind()
end

function CK:PrepareVoiceProbe()
 local focus=GetCurrentKeyBoardFocus and GetCurrentKeyBoardFocus()
 if focus and focus~=self.pttBox or self.voiceCapture then return end
 self.voiceProbeToken=tostring(GetTime());self.voiceProbeDeadline=GetTime()+3
 local mode=self.voiceOptionsFrame and self.voiceOptionsFrame:IsShown() and 'TEST' or 'NORMAL'
 self.pttBox:SetText('PCVQ1;'..self.voiceProbeToken..';'..mode)
 self.pttBox:Show();self.pttBox:SetFocus();self.pttBox:HighlightText()
end
function CK:ReadVoiceStatus(value)
 if type(value)~='string' or #value>2200 then return end
 local token,state,level,words=value:match('^PCVS1;([0-9.]+);([a-z]+);(%d+);([A-Za-z0-9%% ._-]*)$')
 if not token or token~=self.voiceProbeToken or not self.voiceProbeDeadline or GetTime()>self.voiceProbeDeadline or not ({ready=true,loading=true,disabled=true,error=true,test=true})[state] then return end
 words=words:gsub('%%(%x%x)',function(hex) return string.char(tonumber(hex,16)) end)
 if words:find('[%c|]') then return end
 self.voiceProbeDeadline=nil;self.voiceProbeToken=nil;self.voiceCheckDeadline=nil
 self.voiceCompanionStatus='Companion: '..state..' | Microphone level: '..math.min(100,tonumber(level))..'%'
 if words~='' then self.voiceCompanionStatus=self.voiceCompanionStatus..'\nTest recognised: '..words..' (nothing sent)' end
 if self.voiceInfo then self.voiceInfo:SetText(self.voiceCompanionStatus) end
 self:PTTReleaseFocus()
end
