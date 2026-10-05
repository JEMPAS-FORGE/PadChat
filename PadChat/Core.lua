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
 PadChatDB=PadChatDB or {};self.db=PadChatDB
 self.db.words=self.db.words or {};self.db.draft=self.db.draft or ''
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
  if #word>=2 and #word<=30 then self.db.words[word]=(self.db.words[word] or 0)+1 end
 end
 -- Keep the personal dictionary bounded.
 local entries={};for word,count in pairs(self.db.words) do entries[#entries+1]={word,count} end
 table.sort(entries,function(a,b) return a[2]>b[2] end)
 for i=501,#entries do self.db.words[entries[i][1]]=nil end
end
