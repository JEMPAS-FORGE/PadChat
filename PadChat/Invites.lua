local _, CK = ...

local function clean(name)
    if not CK.IsString(name) or #name>120 or name:find('[%c/;|]') then return end
    name=name:match('^%s*(.-)%s*$')
    if name~='' then return name end
end
local function norm(name)
    return CK.Normalize(name or ''):gsub('[%s%p]','')
end
local function characterAddress(name)
    name=clean(name)
    if not name then return end
    if RegionalUniqueNamesEnabled and RegionalUniqueNamesEnabled() then name=name:gsub(' ','-') end
    return name
end

function CK:InviteUnavailable(game)
    if not self:ReadableGameAccount(game) then return 'Friend details unavailable' end
    if not game or not game.isOnline or game.isAppearOffline then return 'Offline' end
    if game.clientProgram~=(BNET_CLIENT_WOW or 'WoW') then return 'Playing another game' end
    if game.wowProjectID~=WOW_PROJECT_ID then return 'Different WoW version' end
    if not game.isInCurrentRegion then return 'Different region' end
    if not game.playerGuid or not game.realmID or game.realmID==0 then return 'Not in a character yet' end
    if WOW_PROJECT_ID==WOW_PROJECT_CLASSIC and GetRealmID and game.realmID~=GetRealmID() then return 'Different realm' end
    local faction=UnitFactionGroup and UnitFactionGroup('player')
    local cross=C_PartyInfo and C_PartyInfo.CanFormCrossFactionParties and C_PartyInfo.CanFormCrossFactionParties()
    local quest=C_QuestSession and C_QuestSession.Exists and C_QuestSession.Exists()
    if faction and game.factionName~=faction and (not cross or quest) then return 'Different faction' end
    if C_GameRules and C_GameRules.GetActiveGameMode and Enum and Enum.GameMode then
        local modes={[14]=Enum.GameMode.Plunderstorm,[15]=Enum.GameMode.WoWHack}
        if (modes[game.classID] or Enum.GameMode.Standard)~=C_GameRules.GetActiveGameMode() then return 'Different game mode' end
    end
    if UnitGUID and game.playerGuid==UnitGUID('player') then return 'This is you' end
    if IsInGroup and IsInGroup() and UnitGUID and GetNumGroupMembers then
        local unit=IsInRaid and IsInRaid() and 'raid' or 'party'
        for i=1,GetNumGroupMembers() do
            if UnitGUID(unit..i)==game.playerGuid then return 'Already in your group' end
        end
    end
end

function CK:InviteRoster()
    local list,byKey={},{}
    if C_ChatInfo and C_ChatInfo.InChatMessagingLockdown and C_ChatInfo.InChatMessagingLockdown() then return list end
    local function add(entry)
        if not entry or not clean(entry.label) then return end
        local key=entry.key:lower()
        if byKey[key] then return end
        byKey[key]=entry; list[#list+1]=entry
    end
    local function character(name,reason)
        name=clean(name)
        if not name or (UnitName and name==UnitName('player')) then return end
        add({key=name,label=name..(reason and ' ('..reason..')' or ''),name=name,
            address=characterAddress(name),aliases={name},reason=reason})
    end
    for _,unit in ipairs({'target','mouseover'}) do
        if UnitIsPlayer and UnitIsPlayer(unit) and not (UnitIsUnit and UnitIsUnit(unit,'player'))
            and not (UnitInParty and UnitInParty(unit)) and not (UnitInRaid and UnitInRaid(unit)) then
            local name,realm
            if UnitFullName then name,realm=UnitFullName(unit) elseif UnitName then name=UnitName(unit) end
            if self.IsString(name) and self.IsReadable(realm) then character(realm and realm~='' and name..'-'..realm or name) end
        end
    end
    -- Include offline character friends too. Do not use the online-only chat roster here.
    for _,friend in ipairs(self:CharacterFriends()) do character(friend.name,not friend.connected and 'Offline' or nil) end
    for _,name in ipairs(self:KnownNames()) do character(name) end
    for _,entry in ipairs(self.voiceRecentWhispers or {}) do character(entry.name) end
    if C_BattleNet and C_BattleNet.GetFriendAccountInfo and BNGetNumFriends then
        for i=1,BNGetNumFriends() do
            local account=C_BattleNet.GetFriendAccountInfo(i)
            if self.IsReadable(account) and type(account)=='table' and self.IsReadable(account.bnetAccountID) then
                local tag=clean(account.battleTag) or clean(account.accountName) or 'Battle.net friend'
                local games={}
                local count=C_BattleNet.GetFriendNumGameAccounts and C_BattleNet.GetFriendNumGameAccounts(i) or 0
                for j=1,count do
                    local game=C_BattleNet.GetFriendGameAccountInfo(i,j)
                    if self:ReadableGameAccount(game) then games[#games+1]=game end
                end
                if #games==0 and self.IsReadable(account.gameAccountInfo) then
                    if account.gameAccountInfo==nil then games[1]={}
                    elseif self:ReadableGameAccount(account.gameAccountInfo) then games[1]=account.gameAccountInfo end
                end
                for _,game in ipairs(games) do
                    local id=tonumber(game.gameAccountID) or 0
                    local accountID=tonumber(account.bnetAccountID) or 0
                    local reason=self:InviteUnavailable(game)
                    if id<=0 then reason=reason or 'Character unavailable' end
                    local name=clean(game.characterName)
                    local label=tag..(name and ' - '..name or '')..(reason and ' ('..reason..')' or '')
                    local aliases={tag,tag:match('^([^#]+)') or tag}
                    if name then aliases[#aliases+1]=name end
                    if clean(account.accountName) then aliases[#aliases+1]=account.accountName end
                    add({key='b'..accountID..'g'..id,label=label,name=name or tag,aliases=aliases,
                        gameID=id,accountID=accountID,reason=reason,guid=game.playerGuid})
                end
            end
        end
    end
    return list
end

function CK:QueryInviteNames(prefix)
    local list,labels={},{}
    if prefix and prefix~='' and not clean(prefix) then self.inviteLabels={};return list end
    local typed=clean(prefix or '') or ''
    local query=norm(typed)
    local exact=false
    for _,entry in ipairs(self:InviteRoster()) do
        local match=query==''
        for _,alias in ipairs(entry.aliases) do
            local value=norm(alias)
            if value==query then exact=true end
            if value:sub(1,#query)==query then match=true end
        end
        if match then list[#list+1]=entry.key;labels[entry.key]=entry.label end
    end
    if typed~='' and not exact then list[#list+1]=typed;labels[typed]=typed end
    self.inviteLabels=labels
    return list
end

local oldConfirm=CK.ConfirmInviteTarget
function CK:ConfirmInviteTarget(key)
    self.inviteBattleNet=nil
    for _,entry in ipairs(self:InviteRoster()) do
        if entry.key==key then
            if entry.reason then self:Print('%s: %s',entry.name,entry.reason);return false end
            if entry.gameID then
                self.invitePicking=nil;self.inviteChoices=nil
                local draft='/invite '..entry.label
                self:SetText(draft)
                self.inviteBattleNet={key=key,draft=draft}
                self.inviteReady=true;self:Refresh()
                return true
            end
            return oldConfirm(self,entry.address)
        end
    end
    -- A disappeared Battle.net entry must never turn into a character name.
    if tostring(key):match('^b%d+g%d+$') then self:Print('Friend status changed. Open Invite player again.');return false end
    return oldConfirm(self,characterAddress(key))
end

local oldMacro=CK.BuildMacroText
function CK:BuildMacroText()
    local pending=self.inviteBattleNet
    if pending and self.inviteReady and self:GetText()==pending.draft then return '/ck invitekey '..pending.key end
    return oldMacro(self)
end

function CK:InviteNotice(message)
    self:Print('%s',message)
    if self.VoiceChannelNotice then self:VoiceChannelNotice(message) end
end

function CK:ExecuteInvite(entry)
    if not entry then self:InviteNotice('Friend status changed. Select them again.');return false end
    if entry.reason then self:InviteNotice(entry.name..': '..entry.reason);return false end
    if InCombatLockdown and InCombatLockdown() then self:InviteNotice('Invite when out of combat.');return false end
    if C_PartyInfo and C_PartyInfo.IsPartyFull and C_PartyInfo.IsPartyFull() then self:InviteNotice('Your group is full.');return false end
    if entry.gameID then
        if not (C_BattleNet and C_BattleNet.InviteFriend) then self:InviteNotice('Battle.net invites are unavailable.');return false end
        C_BattleNet.InviteFriend(entry.gameID)
    else
        local invite=C_PartyInfo and C_PartyInfo.InviteUnit or InviteUnit
        if not invite or not entry.address then return false end
        invite(entry.address)
    end
    self:InviteNotice('Invitation requested for '..entry.name)
    return true
end

function CK:InviteByKey(key)
    -- Resolve again at the moment of sending; cached account indexes can move.
    for _,entry in ipairs(self:InviteRoster()) do
        if entry.key==key then return self:ExecuteInvite(entry) end
    end
    self:InviteNotice('Friend status changed. Select them again.')
    return false
end

function CK:InviteByVoice(name)
    name=clean(name)
    if not name then self:InviteNotice('Say invite followed by a friend or character name.');return false end
    local query=norm(name)
    if query=='' then return false end
    local matches,seen={},{}
    for _,entry in ipairs(self:InviteRoster()) do
        for _,alias in ipairs(entry.aliases) do
            if norm(alias)==query then
                -- The same online character can be both a WoW friend and a Battle.net friend.
                local identity=entry.gameID and ('bn:'..entry.accountID..':'..entry.gameID) or ('character:'..norm(entry.address))
                if not seen[identity] then matches[#matches+1]=entry;seen[identity]=true end
                break
            end
        end
    end
    -- A friend may appear once in the character roster and once as the same
    -- Battle.net character. Prefer the account entry so the spoken name stays
    -- unambiguous and uses Blizzard's friend invite route.
    local battleNetCharacters={}
    for _,entry in ipairs(matches) do
        if entry.gameID and entry.name then battleNetCharacters[norm(entry.name)]=true end
    end
    if next(battleNetCharacters) then
        local unique={}
        for _,entry in ipairs(matches) do
            if entry.gameID or not battleNetCharacters[norm(entry.name)] then unique[#unique+1]=entry end
        end
        matches=unique
    end
    if #matches==0 then self:InviteNotice('No matching friend. Open Invite player to choose or type a name.');return false end
    if #matches>1 then self:InviteNotice('More than one match. Say their full character name or choose in Invite player.');return false end
    return self:ExecuteInvite(matches[1])
end
