local _,P=...
local actions={PADDLEFT='left',PADDRIGHT='right',PADDUP='up',PADDDOWN='down',PAD1='select',PAD2='close',PAD3='delete',PAD4='space',PADLSHOULDER='channel',PADRSHOULDER='suggest',PADLTRIGGER='shift',PADRTRIGGER='symbols'}
local repeats={left=true,right=true,up=true,down=true,delete=true}
local function label(parent,text,size)
 local f=parent:CreateFontString(nil,'OVERLAY');f:SetFont('Fonts\\FRIZQT__.TTF',size or 18,'OUTLINE');f:SetText(text);return f
end
local function border(parent,inset,r,g,b)
 for _,edge in ipairs({'TOP','BOTTOM','LEFT','RIGHT'}) do
  local t=parent:CreateTexture(nil,'BORDER');t:SetColorTexture(r,g,b,1)
  if edge=='TOP' or edge=='BOTTOM' then
   t:SetHeight(2);t:SetPoint(edge..'LEFT',parent,edge..'LEFT',inset,edge=='TOP' and -inset or inset);t:SetPoint(edge..'RIGHT',parent,edge..'RIGHT',-inset,edge=='TOP' and -inset or inset)
  else
   t:SetWidth(2);t:SetPoint('TOP'..edge,parent,'TOP'..edge,edge=='LEFT' and inset or -inset,-inset);t:SetPoint('BOTTOM'..edge,parent,'BOTTOM'..edge,edge=='LEFT' and inset or -inset,inset)
  end
 end
end
function P:BuildUI()
 if self.frame then return end
 local f=self.NewFrame('Frame','PadChatKeyboard',UIParent);self.frame=f
 f:SetSize(780,420);f:SetPoint('BOTTOM',UIParent,'BOTTOM',0,165);f:SetFrameStrata('DIALOG');f:EnableMouse(true)
 local bg=f:CreateTexture(nil,'BACKGROUND');bg:SetAllPoints();bg:SetTexture('Interface\\DialogFrame\\UI-DialogBox-Background');bg:SetAlpha(.98)
 border(f,0,.58,.40,.15);border(f,4,.28,.18,.07)
 self.title=label(f,'PadChat',24);self.title:SetPoint('TOPLEFT',20,-17);self.title:SetTextColor(1,.82,.35)
 self.channel=label(f,'Say',18);self.channel:SetPoint('TOPRIGHT',-20,-20);self.channel:SetTextColor(1,.82,.35)
 self.preview=label(f,'',20);self.preview:SetPoint('TOPLEFT',20,-56);self.preview:SetSize(740,44);self.preview:SetJustifyH('LEFT')
 self.keys={}
 for row=1,5 do
  self.keys[row]={}
  for col=1,10 do
   local b=self.NewFrame('Button',nil,f);b:SetSize(58,49)
   b.fill=b:CreateTexture(nil,'BACKGROUND');b.fill:SetAllPoints()
   border(b,0,.48,.32,.12)
   b.text=label(b,'',22);b.text:SetPoint('CENTER')
   b:SetScript('OnClick',function() P.model.row=row;P.model.col=col;P:Act('select') end)
   self.keys[row][col]=b
  end
 end
 self.hint=label(f,'D-pad / stick: move   X: select   Square: delete   Triangle: space',15);self.hint:SetPoint('BOTTOM',0,48);self.hint:SetTextColor(.85,.73,.50)
 local hint2=label(f,'L1: channel   R1: words   L2: shift   R2: 123   Options: send   Circle: close',14);hint2:SetPoint('BOTTOM',0,25);hint2:SetTextColor(.85,.73,.50)
 self.sendButton=self.NewFrame('Button','PadChatSend',nil,'SecureActionButtonTemplate')
 self.sendButton:SetAttribute('type','macro');self.sendButton:RegisterForClicks('AnyUp')
 self.sendButton:SetSize(140,49);self.sendButton:SetFrameStrata('FULLSCREEN_DIALOG')
 self.sendButton:SetScript('PreClick',function() P:PrepareSend() end)
 self.sendButton:SetScript('PostClick',function() if P.justSent then P:SetText('');P:Close('sent') end end)
 self.sendButton:Hide()
 self.keyboardOwner=self.NewFrame('Frame',nil,nil,'SecureHandlerStateTemplate');self.buttons={}
 self.keyboardOwner:SetFrameRef('send',self.sendButton)
 self.keyboardOwner:SetAttribute('_onstate-combat',[[
  if newstate=='active' then
   self:ClearBindings()
   self:GetFrameRef('send'):Hide()
  end
 ]])
 RegisterStateDriver(self.keyboardOwner,'combat','[combat] active; inactive')
 for key,action in pairs(actions) do
  local b=self.NewFrame('Button','PadChatAction'..key);b:RegisterForClicks('AnyDown','AnyUp')
  b:SetScript('OnClick',function(_,_,down)
   if not P.open then return end
   if down then P:Act(action);if repeats[action] then P.repeatAction=action;P.repeatAt=GetTime()+.45 end
   elseif P.repeatAction==action then P.repeatAction=nil end
  end);self.buttons[key]=b
 end
 if f.EnableGamePadStick then
  f:EnableGamePadStick(true)
  f:SetScript('OnGamePadStick',function(_,stick,x,y)
   if stick=='Left' or stick=='Movement' then P.stickEvents=true;if P.model:Stick(x,y,GetTime()) then P:Refresh() end end
  end)
 end
 f:SetScript('OnUpdate',function(_,dt)
  if not P.open then return end
  if P.repeatAction and GetTime()>=P.repeatAt then P:Act(P.repeatAction);P.repeatAt=GetTime()+.18 end
  P.pollAt=(P.pollAt or 0)+dt
  if not P.stickEvents and P.pollAt>=.03 and C_GamePad and C_GamePad.GetDeviceMappedState then
   P.pollAt=0;local id=C_GamePad.GetActiveDeviceID();local state=id and C_GamePad.GetDeviceMappedState(id)
   local stick=state and state.sticks and state.sticks[1]
   if stick and P.model:Stick(stick.x or 0,stick.y or 0,GetTime()) then P:Refresh() end
  end
 end)
 f:Hide()
end
function P:Refresh()
 if not self.frame then return end
 self.db.draft=self:GetText();self.preview:SetText(self:GetText()=='' and 'Choose a letter or a ready word...' or self:GetText())
 local _,destination=self:VoiceDestination();self.channel:SetText(destination or 'Unavailable channel')
 local rows=self.model:Rows(self.db.words)
 for r=1,5 do for c=1,10 do
  local b=self.keys[r][c];local key=rows[r][c]
  if key then
   local width=(r==1 or r==5) and 140 or 58
   local total=#rows[r]*(width+8)-8
   b:SetSize(width,49);b:ClearAllPoints();b:SetPoint('TOPLEFT',(780-total)/2+(c-1)*(width+8),-112-(r-1)*54)
   b.text:SetText(key.label)
   if self.model.row==r and self.model.col==c then b.fill:SetColorTexture(1,.76,.23,1);b.text:SetTextColor(.12,.06,.01)
   else b.fill:SetColorTexture(.30,.22,.12,1);b.text:SetTextColor(1,.89,.66) end
   b:Show()
  else b:Hide() end
 end end
 if self.invitePicking then
  self.preview:SetText('Choose a friend with up/down; X confirms; Circle returns')
  local names={};for i=math.max(1,self.inviteIndex-1),math.min(#self.inviteChoices,self.inviteIndex+2) do
   local key=self.inviteChoices[i];names[#names+1]=(i==self.inviteIndex and '> ' or '  ')..(self.inviteLabels[key] or key)
  end
  self.preview:SetText(table.concat(names,'\n'));self.preview:SetHeight(250)
  for r=1,5 do for c=1,10 do self.keys[r][c]:Hide() end end;self.sendButton:Hide()
 else
  self.preview:SetHeight(44)
  local send=self.keys[5][5];self.sendButton:ClearAllPoints();self.sendButton:SetPoint('TOPLEFT',send,'TOPLEFT');self.sendButton:SetSize(send:GetSize())
  if self.open then self.sendButton:Show() else self.sendButton:Hide() end
 end
 if not InCombatLockdown() then
  local target=self.model.row==5 and self.model.col==5 and not self.invitePicking and self.sendButton:GetName() or self.buttons.PAD1:GetName()
  if self.open then SetOverrideBindingClick(self.keyboardOwner,true,'PAD1',target,'LeftButton') end
 end
end
function P:Open()
 if InCombatLockdown() or self.tvActionFailed then self:Print('Open the keyboard outside combat. Voice typing remains separate.');return end
 if GetCurrentKeyBoardFocus() then self:Print('Finish the current text box first.');return end
 self:BuildUI();self.open=true;self.model.stickBlocked=true;self.model.stickDir=nil;self.stickEvents=false
 self.frame:Show()
 for key,b in pairs(self.buttons) do SetOverrideBindingClick(self.keyboardOwner,true,key,b:GetName(),'LeftButton') end
 SetOverrideBindingClick(self.keyboardOwner,true,'PADFORWARD',self.sendButton:GetName(),'LeftButton')
 self:Refresh()
end
function P:Close()
 self.open=false;self.repeatAction=nil;self.invitePicking=nil;self.inviteBattleNet=nil
 if self.frame then self.frame:Hide() end
 if not InCombatLockdown() then ClearOverrideBindings(self.keyboardOwner);self.sendButton:Hide() else self.cleanupPending=true end
end
function P:Act(action)
 if not self.open or InCombatLockdown() then return end
 if self.invitePicking then
  if action=='close' then self.invitePicking=nil
  elseif action=='up' or action=='down' then self.inviteIndex=math.max(1,math.min(#self.inviteChoices,self.inviteIndex+(action=='up' and -1 or 1)))
  elseif action=='select' then self:ConfirmInviteTarget(self.inviteChoices[self.inviteIndex]) end
 else
  if action=='left' then self.model:Move(-1,0)
  elseif action=='right' then self.model:Move(1,0)
  elseif action=='up' then self.model:Move(0,-1)
  elseif action=='down' then self.model:Move(0,1)
  elseif action=='select' then local picked=self.model:Select(GetTime());if picked=='invite' then self:ChooseInvite() end
  elseif action=='space' then self.model:Insert(' ')
  elseif action=='delete' then self.model:Delete()
  elseif action=='shift' then self.model.shift=not self.model.shift
  elseif action=='symbols' then self.model.numbers=not self.model.numbers
  elseif action=='suggest' then self.model.row=1;self.model.col=1
  elseif action=='channel' then self:CycleVoiceChannel()
  elseif action=='close' then self:Close();return end
 end
 self:Refresh()
end
function P:BuildMacroText()
 if self.invitePicking then return nil end
 if self.inviteBattleNet and self:GetText()==self.inviteBattleNet.draft then return '/ck invitekey '..self.inviteBattleNet.key end
 if self.inviteReady and self:GetText():match('^/invite ') then return self:GetText() end
 local prefix=self:VoiceDestination();local text=self:GetText():gsub('[\r\n|]',' ')
 if prefix and text:match('%S') then return prefix..' '..text end
end
function P:PrepareSend()
 self.justSent=false
 if not self.open or InCombatLockdown() then return end
 self.sendButton:SetAttribute('macrotext',self:BuildMacroText() or '')
end
function P:ChooseInvite()
 self.invitePicking=true;self.inviteChoices=self:QueryInviteNames('');self.inviteIndex=1
 if #self.inviteChoices==0 then self.invitePicking=nil;self:Print('No friends or recent whisper senders are available yet.') end
end
function P:ConfirmInviteTarget(name)
 if not name or name:find('[%c/;|]') then return false end
 self.invitePicking=nil;self.inviteReady=true;self:SetText('/invite '..name);return true
end
function PadChat_Toggle() if P.open then P:Close() else P:Open() end end
