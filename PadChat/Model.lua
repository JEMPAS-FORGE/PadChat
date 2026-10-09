local _,P=...
local letters={'qwertyuiop','asdfghjkl','zxcvbnm'}
local numbers={'1234567890','!?.,:-@()','[]+*/=#'}
local common={'thanks','hello','omw','ready','mana','heal','sorry','please','group','quest','dungeon','tank','wait','yes','no','good','need','help'}
local Model={};Model.__index=Model
function P:NewModel(text)
 return setmetatable({text=text or '',row=2,col=5,shift=false,numbers=false,stickDir=nil,stickAt=0,stickBlocked=false},Model)
end
function Model:Rows(words)
 local rows={};local prefix=self.text:match('([%a]+)$') or ''
 local found,seen={},{}
 local function add(word,score)
  if not seen[word] and (prefix=='' or word:sub(1,#prefix)==string.lower(prefix)) then seen[word]=true;found[#found+1]={word,score} end
 end
 for i,word in ipairs(common) do add(word,30-i) end
 for word,count in pairs(words or {}) do add(word,30+count) end
 table.sort(found,function(a,b) return a[2]==b[2] and a[1]<b[1] or a[2]>b[2] end)
 rows[1]={};for i=1,math.min(5,#found) do rows[1][i]={label=found[i][1],word=found[i][1]} end
 if #rows[1]==0 then rows[1][1]={label='No suggestion',empty=true} end
 for r,chars in ipairs(self.numbers and numbers or letters) do
  rows[r+1]={};for i=1,#chars do local ch=chars:sub(i,i);rows[r+1][i]={label=self.shift and ch:upper() or ch,char=ch} end
 end
 rows[5]={{label='Space',action='space'},{label=self.shift and 'SHIFT' or 'Shift',action='shift'},
  {label=self.numbers and 'ABC' or '123',action='symbols'},{label='Invite',action='invite'},{label='Send',action='send'},
  {label='Options',action='options'},{label='Close',action='close'}}
 self.rows=rows;self.col=math.min(self.col,#rows[self.row]);return rows
end
function Model:Move(dx,dy)
 self.row=math.max(1,math.min(5,self.row+dy));self.col=math.max(1,math.min(#self.rows[self.row],self.col+dx))
end
function Model:Insert(text)
 if #self.text+#text>220 then return false end
 self.text=self.text..text;return true
end
function Model:Delete()
 local i=#self.text;while i>1 and self.text:byte(i)>=128 and self.text:byte(i)<192 do i=i-1 end
 self.text=self.text:sub(1,i-1)
end
function Model:Select(now)
 self.stickBlocked=true;self.stickDir=nil;self.freezeUntil=now+.14
 local key=self.rows[self.row][self.col]
 if key.char then self:Insert(self.shift and key.char:upper() or key.char);self.shift=false
 elseif key.word then
  local prefix=self.text:match('[%a]+$')
  local base=prefix and self.text:sub(1,#self.text-#prefix) or self.text
  if #base+#key.word+1<=220 then self.text=base..key.word..' ' end
 elseif key.action=='space' then self:Insert(' ')
 elseif key.action=='shift' then self.shift=not self.shift
 elseif key.action=='symbols' then self.numbers=not self.numbers
 else return key.action end
end
function Model:Stick(x,y,now)
 if type(x)~='number' or type(y)~='number' or x~=x or y~=y then return end
 local magnitude=math.max(math.abs(x),math.abs(y))
 if magnitude<.35 then self.stickBlocked=false;self.stickDir=nil;return end
 if self.stickBlocked or now<(self.freezeUntil or 0) or magnitude<.6 then return end
 local dir=math.abs(x)>=math.abs(y) and (x>0 and 'right' or 'left') or (y>0 and 'up' or 'down')
 if dir~=self.stickDir then self.stickDir=dir;self.stickAt=now+.48
 elseif now<self.stickAt then return
 else self.stickAt=now+.18 end
 self:Move(dir=='left' and -1 or dir=='right' and 1 or 0,dir=='up' and -1 or dir=='down' and 1 or 0)
 return true
end
