local _,P=...
local keys={'F9','F6','F7','F12','CTRL-F9','SHIFT-F9','ALT-F9'}
local buttons={'AUTO','NONE','PADBACK','PADLSTICK','PADRSTICK','PADSOCIAL','PADFORWARD'}
local modifiers={'NONE','PADLTRIGGER','PADRTRIGGER','PADLSHOULDER','PADRSHOULDER'}
local names={AUTO='Automatic',NONE='None',PADBACK='Touchpad / Back',PADSOCIAL='Share / Create',
 PADFORWARD='Options / Start',PADLSTICK='L3 / left stick',PADRSTICK='R3 / right stick',
 PADLTRIGGER='L2 / LT',PADRTRIGGER='R2 / RT',PADLSHOULDER='L1 / LB',PADRSHOULDER='R1 / RB'}
local function contains(list,value) for _,v in ipairs(list) do if v==value then return true end end end
local function nextChoice(list,value) for i,v in ipairs(list) do if v==value then return list[i%#list+1] end end;return list[1] end
function P:InitOptions()
 local s=type(self.db.bindings)=='table' and self.db.bindings or {};self.db.bindings=s
 if not contains(keys,s.keyboard) then s.keyboard='F9' end
 if not contains(buttons,s.button) then s.button='AUTO' end
 if not contains(modifiers,s.modifier) then s.modifier='NONE' end
 if s.button=='AUTO' or s.button=='NONE' then s.modifier='NONE' end
 if self.InitVoiceOptions then self:InitVoiceOptions() end
end
function P:ControllerOpenBinding()
 local s=self.db.bindings
 if s.button=='AUTO' then return self:ControllerBindingMode()=='sony' and 'PADBACK' or nil end
 if s.button~='NONE' then return s.button,s.modifier~='NONE' and s.modifier or nil end
end
function P:BindingDescription()
 local button,modifier=self:ControllerOpenBinding()
 return self.db.bindings.keyboard,button and ((modifier and names[modifier]..' + ' or '')..names[button]) or 'None'
end
function P:BindingWarning()
 local s=self.db.bindings;local button,modifier=self:ControllerOpenBinding();local warnings={}
 if button=='PADSOCIAL' or button=='PADBACK' and self:ControllerBindingMode()=='xbox' then
  warnings[#warnings+1]='Share / Back is also used by the optional voice companion. Do not use both shortcuts together.'
 end
 if modifier then warnings[#warnings+1]='Hold '..names[modifier]..' first, then press '..names[button]..'. The chosen opening button is reserved while PadChat is enabled.' end
 if GetBindingAction then
  for _,key in ipairs({s.keyboard,button}) do
   local action=GetBindingAction(key,false)
   if action and action~='' and action~='PADCHAT_TOGGLE' and not action:find('PadChat',1,true) then
    warnings[#warnings+1]=key..' is already assigned to '..action..'. PadChat will override it while enabled.'
   end
  end
 end
 if s.button=='AUTO' and not button then warnings[#warnings+1]='No automatic opening button for this controller. Choose L3 or R3 and an optional held trigger, then Save. Share / Back remains available for voice.' end
 return #warnings>0 and table.concat(warnings,'\n') or 'No existing binding conflicts detected.'
end
function P:SaveOpeningBindings(key,button,modifier)
 if InCombatLockdown() then return false,'Change bindings outside combat.' end
 if not contains(keys,key) or not contains(buttons,button) or not contains(modifiers,modifier) then return false,'Unsupported binding.' end
 if button=='AUTO' or button=='NONE' then modifier='NONE' end
 self:Close()
 self.db.bindings={keyboard=key,button=button,modifier=modifier}
 self.openDevice=nil;self.openPrimaryDown=true
 if self.ApplyOpenBindings then self:ApplyOpenBindings() end
 if self.UpdateOptions then self:UpdateOptions() end
 return true
end
function P:MappedButtonDown(state,binding)
 if not state or not state.buttons or not C_GamePad or not C_GamePad.ButtonBindingToIndex then return false end
 local ok,index=pcall(C_GamePad.ButtonBindingToIndex,binding)
 if not ok or type(index)~='number' then return false end
 return state.buttons[index+1]==true
end
function P:PollOpeningChord(dt)
 local button,modifier=self:ControllerOpenBinding()
 if not modifier then self.openPrimaryDown=nil;self.openPoll=0;return end
 self.openPoll=(self.openPoll or 0)+dt;if self.openPoll<.03 then return end;self.openPoll=0
 if not C_GamePad or not C_GamePad.GetDeviceMappedState or not C_GamePad.GetActiveDeviceID then return end
 local id=C_GamePad.GetActiveDeviceID()
 local ok,state=pcall(C_GamePad.GetDeviceMappedState,id)
 if not ok or not state then self.openPrimaryDown=true;self.openDevice=nil;return end
 local down=self:MappedButtonDown(state,button)
 -- Reconnects/device changes cannot turn an already held button into a click.
 if self.openDevice~=id then self.openDevice=id;self.openPrimaryDown=down;return end
 local pressed=down and not self.openPrimaryDown;self.openPrimaryDown=down
 if pressed and self:MappedButtonDown(state,modifier) and not InCombatLockdown()
  and not self.conflictingAddon and not self.tvActionFailed
  and not (self.optionsFrame and self.optionsFrame:IsShown()) and not (self.voiceOptionsFrame and self.voiceOptionsFrame:IsShown()) and not GetCurrentKeyBoardFocus() then self.openingRelease=modifier;PadChat_Toggle() end
end
local function text(parent,value,size)
 local label=parent:CreateFontString(nil,'OVERLAY');label:SetFont('Fonts\\FRIZQT__.TTF',size or 16,'OUTLINE');label:SetText(value);label:SetTextColor(1,.89,.66);return label
end
local function button(parent,value,x,y,width,fn)
 local b=P.NewFrame('Button',nil,parent);b:SetSize(width,36);b:SetPoint('TOPLEFT',x,y)
 local bg=b:CreateTexture(nil,'BACKGROUND');bg:SetAllPoints();bg:SetColorTexture(.30,.22,.12,1);b.fill=bg
 b.text=text(b,value);b.text:SetPoint('CENTER');b:SetScript('OnClick',fn);return b
end
function P:BuildOptions()
 if self.optionsFrame then return end
 local f=self.NewFrame('Frame','PadChatOptions',UIParent);self.optionsFrame=f
 f:SetSize(690,445);self:FitPanel(f,690,445);f:SetFrameStrata('FULLSCREEN_DIALOG');f:EnableMouse(true)
 local bg=f:CreateTexture(nil,'BACKGROUND');bg:SetAllPoints();bg:SetTexture('Interface\\DialogFrame\\UI-DialogBox-Background');bg:SetAlpha(.98)
 table.insert(UISpecialFrames,'PadChatOptions')
 local title=text(f,'PadChat Options',24);title:SetPoint('TOPLEFT',20,-20)
 self.optionClose=button(f,'Close',586,-14,84,function() f:Hide() end)
 local _,_,controls=self:ControllerHelp();local help=text(f,'D-pad: choose / change   '..controls);self.optionHelp=help;help:SetPoint('TOPLEFT',20,-63);help:SetSize(650,28);help:SetJustifyH('LEFT')
 self.optionKey=button(f,'',20,-100,310,function() self.pendingOptions.keyboard=nextChoice(keys,self.pendingOptions.keyboard);self:UpdateOptions(true) end)
 self.optionButton=button(f,'',20,-148,310,function() self.pendingOptions.button=nextChoice(buttons,self.pendingOptions.button);self:UpdateOptions(true) end)
 self.optionModifier=button(f,'',346,-148,324,function()
  if self.pendingOptions.button=='AUTO' or self.pendingOptions.button=='NONE' then self.optionWarning:SetText('Choose a controller button before adding a held modifier.');return end
  self.pendingOptions.modifier=nextChoice(modifiers,self.pendingOptions.modifier);self:UpdateOptions(true)
 end)
 self.optionWarning=text(f,'',14);self.optionWarning:SetPoint('TOPLEFT',20,-205);self.optionWarning:SetSize(650,95);self.optionWarning:SetJustifyH('LEFT')
 self.optionVoice=button(f,'Voice settings: binding, microphone and startup',20,-315,650,function() self:ShowVoiceOptions() end)
 self.optionSave=button(f,'Save opening controls',20,-385,235,function()
  local s=self.pendingOptions;local ok,why=self:SaveOpeningBindings(s.keyboard,s.button,s.modifier)
  if not ok then self.optionWarning:SetText(why) else self:Print('Opening controls saved. '..self:BindingWarning()) end
 end)
 self.optionReset=button(f,'Reset defaults',270,-385,190,function()
  local ok,why=self:SaveOpeningBindings('F9','AUTO','NONE');if not ok then self.optionWarning:SetText(why) end
 end)
 self.optionControls={self.optionKey,self.optionButton,self.optionModifier,self.optionSave,self.optionReset,self.optionClose,self.optionVoice}
 self.optionOwner=self.NewFrame('Frame',nil,nil,'SecureHandlerStateTemplate')
 self.optionOwner:SetFrameRef('panel',f)
 self.optionOwner:SetAttribute('_onstate-combat',[[if newstate=='active' then self:ClearBindings();self:GetFrameRef('panel'):Hide();local voice=self:GetFrameRef('voice');if voice then voice:Hide() end end]])
 RegisterStateDriver(self.optionOwner,'combat','[combat] active; inactive')
 self.optionActions={}
 for key,action in pairs({PADDUP='up',PADDDOWN='down',PADDLEFT='previous',PADDRIGHT='next',PAD1='activate',PAD2='close'}) do
  local control=self.NewFrame('Button','PadChatOptionAction'..key);control:RegisterForClicks('AnyUp')
  control:SetScript('OnClick',function() self:OptionsAction(action) end);self.optionActions[key]=control
 end
 f:SetScript('OnHide',function()
  self.pendingOptions=nil
  if not InCombatLockdown() then ClearOverrideBindings(self.optionOwner) end
 end);f:Hide()
end
function P:HighlightOption()
 for i,control in ipairs(self.optionControls) do
  local selected=i==self.optionIndex
  control.fill:SetColorTexture(selected and 1 or .30,selected and .76 or .22,selected and .23 or .12,1)
  control.text:SetTextColor(selected and .12 or 1,selected and .06 or .89,selected and .01 or .66)
 end
end
function P:OptionsAction(action)
 if self.voiceOptionsFrame and self.voiceOptionsFrame:IsShown() then self:VoiceOptionsAction(action);return end
 if InCombatLockdown() or not self.pendingOptions or not self.optionsFrame:IsShown() then return end
 if action=='close' then self.optionsFrame:Hide();return end
 if action=='up' or action=='down' then
  self.optionIndex=(self.optionIndex-1+(action=='up' and -1 or 1))%#self.optionControls+1
 elseif action=='previous' or action=='next' then
  local field=self.optionIndex==1 and 'keyboard' or self.optionIndex==2 and 'button' or self.optionIndex==3 and 'modifier'
  if field then
   if field=='modifier' and (self.pendingOptions.button=='AUTO' or self.pendingOptions.button=='NONE') then
    self.optionWarning:SetText('Choose a controller button before adding a held modifier.');return
   end
   local choices=field=='keyboard' and keys or field=='button' and buttons or modifiers
   local index=1;for i,value in ipairs(choices) do if value==self.pendingOptions[field] then index=i end end
   self.pendingOptions[field]=choices[(index-1+(action=='previous' and -1 or 1))%#choices+1]
   self:UpdateOptions(true)
  end
 elseif action=='activate' then
  local control=self.optionControls[self.optionIndex];local fn=control:GetScript('OnClick');if fn then fn(control) end
 end
 if self.optionsFrame:IsShown() then self:HighlightOption() end
end
function P:UpdateOptions(pending)
 if not self.optionsFrame then return end
 if not pending then local s=self.db.bindings;self.pendingOptions={keyboard=s.keyboard,button=s.button,modifier=s.modifier} end
 local s=self.pendingOptions
 self.optionKey.text:SetText('Keyboard: '..s.keyboard)
 self.optionButton.text:SetText('Controller: '..names[s.button])
 self.optionModifier.text:SetText('Hold first: '..names[s.modifier])
 -- Check the pending selection without committing it.
 local saved=self.db.bindings;self.db.bindings=s;self.optionWarning:SetText(self:BindingWarning());self.db.bindings=saved
end
function P:ShowOptions()
 if InCombatLockdown() then self:Print('Open Options outside combat.');return end
 if self.conflictingAddon then self:Print('Disable older keyboard add-ons first.');return end
 self:Close();self:BuildOptions();local _,_,help=self:ControllerHelp();self.optionHelp:SetText('D-pad: choose / change   '..help);self:UpdateOptions();self.optionIndex=1;self:HighlightOption();self.optionsFrame:Show()
 for key,control in pairs(self.optionActions) do SetOverrideBindingClick(self.optionOwner,true,key,control:GetName(),'LeftButton') end
end

function P:ControllerOnboarding()
 if not self.db or self.db.controllerSetupSeen or self.db.bindings.button~='AUTO' or self:ControllerOpenBinding() then return end
 if InCombatLockdown() or GetCurrentKeyBoardFocus() or self.open or self.conflictingAddon then return end
 if not C_GamePad or not C_GamePad.GetAllDeviceIDs then return end
 local ok,ids=pcall(C_GamePad.GetAllDeviceIDs)
 if not ok or type(ids)~='table' or #ids==0 then return end
 self.db.controllerSetupSeen=true;self:ShowOptions()
 self.optionWarning:SetText('Choose an opening button using the D-pad and A / X. Add an optional held trigger, then Save opening controls. Share / Back is for voice. Your combat bindings are kept.')
end
