local _,P=...
-- Feature checks, rather than a client name, select the available API. Never
-- inspect or persist a value Blizzard marks secret during chat lockdown.
function P.IsReadable(value)
 return not (issecretvalue and issecretvalue(value))
end
function P.IsString(value)
 return P.IsReadable(value) and type(value)=='string'
end
function P.RegisterOptionalEvent(frame,event)
 return pcall(frame.RegisterEvent,frame,event)
end
function P:GeneralChannelID()
 if C_ChatInfo and C_ChatInfo.GetGeneralChannelLocalID then
  local id=C_ChatInfo.GetGeneralChannelLocalID()
  if self.IsReadable(id) and type(id)=='number' and id>0 then return id end
 end
 if not GetChannelList then return end
 local channels={GetChannelList()}
 local general=GENERAL or 'General'
 for i=1,#channels,3 do
  local id,name=channels[i],channels[i+1]
  if self.IsReadable(id) and self.IsString(name) and (name==general or name:sub(1,#general+3)==general..' - ') then return id end
 end
end
function P:CharacterFriends()
 local friends={}
 if C_FriendList and C_FriendList.GetNumFriends and C_FriendList.GetFriendInfoByIndex then
  for i=1,C_FriendList.GetNumFriends() do
   local info=C_FriendList.GetFriendInfoByIndex(i)
   if info and self.IsString(info.name) and self.IsReadable(info.connected) then friends[#friends+1]=info end
  end
 elseif GetNumFriends and GetFriendInfo then
  for i=1,GetNumFriends() do
   local name,_,_,_,connected=GetFriendInfo(i)
   if self.IsString(name) and self.IsReadable(connected) then friends[#friends+1]={name=name,connected=connected} end
  end
 end
 return friends
end
function P:ReadableGameAccount(game)
 if not self.IsReadable(game) or type(game)~='table' then return false end
 for _,key in ipairs({'gameAccountID','isOnline','isAppearOffline','clientProgram','wowProjectID','isInCurrentRegion','playerGuid','realmID','factionName','classID','characterName'}) do
  if not self.IsReadable(game[key]) then return false end
 end
 return true
end
