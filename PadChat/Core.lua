local name,P=...
PadChat=P
P.name=name
-- Construct without a parent argument, then parent explicitly. Forever's
-- native controller navigation hooks parented frame creation.
function P.NewFrame(kind,name,parent,template)
 local f=CreateFrame(kind,name,nil,template)
 if parent then f:SetParent(parent) end
 return f
end
function P:Print(message,...)
 DEFAULT_CHAT_FRAME:AddMessage('|cff7ee7d6PadChat|r: '..string.format(message,...))
end
function P.Normalize(value) return string.lower(value or '') end
function P:InitDB()
 PadChatDB=type(PadChatDB)=='table' and PadChatDB or {};self.db=PadChatDB
 local entries={}
 if type(self.db.words)=='table' then
  for word,count in pairs(self.db.words) do
   if type(word)=='string' and #word>=2 and #word<=30 and not word:find('[%c|]')
    and type(count)=='number' and count==count and count>0 and count<math.huge then
    entries[#entries+1]={word,math.min(1000000,math.floor(count))}
   end
  end
 end
 table.sort(entries,function(a,b) return a[2]==b[2] and a[1]<b[1] or a[2]>b[2] end)
 self.db.words={};for i=1,math.min(500,#entries) do self.db.words[entries[i][1]]=entries[i][2] end
 if type(self.db.draft)~='string' or #self.db.draft>1000 or self.db.draft:find('[%z\1-\8\11\12\14-\31]') then self.db.draft='' end
 self:InitOptions()
 self.model=self:NewModel(self.db.draft);self.open=false
end
function P:IsOpen() return self.open end
function P:GetText() return self.model and self.model.text or '' end
function P:SetText(text) self.model.text=text;self.db.draft=text;self:Refresh() end
function P:GetChatAttr(key)
 local v=self.voiceLastChannel or {chatType='SAY'}
 if key=='chatType' then return v.chatType end
 if key=='tellTarget' or key=='channelTarget' then return v.target end
end
function P:KnownNames()
 local names={}
 for _,v in ipairs(self.voiceRecentWhispers or {}) do names[#names+1]=v.name end
 return names
end
function P:Learn(text)
 for word in string.lower(text):gmatch("[%a][%a\128-\255'-]*") do
  if #word>=2 and #word<=30 then self.db.words[word]=math.min(1000000,(self.db.words[word] or 0)+1) end
 end
 -- Keep the personal dictionary bounded.
 local entries={};for word,count in pairs(self.db.words) do entries[#entries+1]={word,count} end
 table.sort(entries,function(a,b) return a[2]>b[2] end)
 for i=501,#entries do self.db.words[entries[i][1]]=nil end
end
