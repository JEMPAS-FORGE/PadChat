local _,P=...
-- The companion reads only this versioned, bounded settings record. WoW owns
-- its SavedVariables; Save & reload is the supported write boundary.
local pads={'SHARE','START','NORTH','LSTICK','RSTICK','LSHOULDER','RSHOULDER','NONE'}
local modifiers={'NONE','LTRIGGER','RTRIGGER','LSHOULDER','RSHOULDER'}
local padNames={SHARE='Share / Create / Back',START='Options / Start',NORTH='Triangle / Y',LSTICK='L3',RSTICK='R3',LSHOULDER='L1 / LB',RSHOULDER='R1 / RB',NONE='None',LTRIGGER='L2 / LT',RTRIGGER='R2 / RT'}
local vk={BACKSPACE=8,TAB=9,ENTER=13,SPACE=32,PAGEUP=33,PAGEDOWN=34,END=35,HOME=36,LEFT=37,UP=38,RIGHT=39,DOWN=40,INSERT=45,DELETE=46,NUMLOCK=144,SCROLLLOCK=145}
local function copy(t) local o={} for k,v in pairs(t) do o[k]=v end return o end
local function choice(list,value,delta) delta=type(delta)=='number' and delta or 1;for i,v in ipairs(list) do if v==value then return list[(i-1+delta)%#list+1] end end return list[1] end
local function contains(list,value) for _,v in ipairs(list) do if value==v then return true end end end
local function encode(s) return (s:gsub('[^A-Za-z0-9 ._%-]',function(c) return string.format('%%%02X',c:byte()) end)) end
function P:InitVoiceOptions()
 local s=self.db.voiceOptions
 if type(s)~='table' then s={kind='keyboard',key=119,mods=0,keyName='F8',mic='',enabled=true,startup='KEEP',pad='SHARE',padMod='NONE'}
  if self.voiceInitialShortcut then for k,v in pairs(self.voiceInitialShortcut) do s[k]=v end end
 end
 if (s.kind~='keyboard' and s.kind~='mouse') or type(s.key)~='number' or s.key<4 or s.key>254 or type(s.mods)~='number' or s.mods<0 or s.mods>7 or type(s.keyName)~='string' then s.kind='keyboard';s.key=119;s.mods=0;s.keyName='F8' end
 if type(s.mic)~='string' or #s.mic>512 then s.mic='' end
 if not contains(pads,s.pad) then s.pad='SHARE' end
 if not contains(modifiers,s.padMod) then s.padMod='NONE' end
 if s.pad=='NONE' then s.padMod='NONE' end
 if s.startup~='ON' and s.startup~='OFF' then s.startup='KEEP' end
 s.vocabulary=type(s.vocabulary)=='string' and s.vocabulary or '';if #s.vocabulary>600 or s.vocabulary:find('[%c/;|]') then s.vocabulary='' end
 s.enabled=s.enabled~=false;s.review=s.review==true;self.db.voiceOptions=s
end
function P:VoiceBindingKey(s)
 s=s or self.db.voiceOptions
 return ((s.mods%2==1) and 'CTRL-' or '')..((math.floor(s.mods/2)%2==1) and 'SHIFT-' or '')..(s.mods>=4 and 'ALT-' or '')..s.keyName
end
function P:VoiceShortcutProblem(s)
 if s.kind=='mouse' and s.key~=4 and s.key~=5 and s.key~=6 then return 'Choose middle mouse, Mouse 4 or Mouse 5.' end
 if s.kind=='keyboard' and (s.key==27 or s.key==16 or s.key==17 or s.key==18) then return 'Escape and modifier keys cannot be used alone.' end
 if s.key>=112 and s.key<=123 and s.mods%4==3 then return 'Ctrl+Shift+function keys are reserved for PadChat communication.' end
 if s.mods>=4 and (s.key==9 or s.key==115 or s.key==32) then return 'That shortcut is reserved by Windows.' end
 if self:VoiceBindingKey(s)==self.db.bindings.keyboard then return 'Voice and keyboard opening need different shortcuts.' end
 local open,mod=self:ControllerOpenBinding()
 local native={PADSOCIAL='SHARE',PADBACK=self:ControllerBindingMode()=='xbox' and 'SHARE' or 'TOUCHPAD',PADFORWARD='START',PADLSTICK='LSTICK',PADRSTICK='RSTICK'}
 -- The opening button is reserved even with a modifier, so sharing it is unsafe.
 if open and native[open]==s.pad then return 'Voice and keyboard opening need different controller buttons.' end
 if s.pad==s.padMod and s.pad~='NONE' then return 'Choose different primary and held controller buttons.' end
end
function P:SaveVoiceOptions(s)
 if InCombatLockdown() then return false,'Save voice options outside combat.' end
 local problem=self:VoiceShortcutProblem(s);if problem then return false,problem end
 local valid,why=self:ValidVoiceWords(s.vocabulary or '');if not valid then return false,why end
 self.db.voiceOptions=copy(s)
 self.db.voiceSettingsWire=table.concat({'PCV3',s.kind,tostring(s.key),tostring(s.mods),encode(s.mic),s.enabled and '1' or '0',s.startup,s.pad,s.padMod,s.review and '1' or '0',encode(s.vocabulary or '')},';')
 if self.ApplyVoiceBindings then self:ApplyVoiceBindings() end
 return true
end
function P:GetVoiceMicrophones()
 local result={};local seen={}
 if C_VoiceChat and C_VoiceChat.GetAvailableInputDevices then
  local ok,devices=pcall(C_VoiceChat.GetAvailableInputDevices)
  if ok and type(devices)=='table' then for _,d in ipairs(devices) do
   if type(d)=='table' and type(d.displayName)=='string' and #d.displayName>0 and #d.displayName<=512 and not seen[d.displayName] then
    seen[d.displayName]=true;result[#result+1]=d.displayName
   end
  end end
 end
 table.sort(result);return result
end
local function label(f,value,x,y,w,h,size)
 local t=f:CreateFontString(nil,'OVERLAY');t:SetFont('Fonts\\FRIZQT__.TTF',size or 14,'OUTLINE');t:SetTextColor(1,.89,.66);t:SetText(value);t:SetPoint('TOPLEFT',x,y);t:SetSize(w,h);t:SetJustifyH('LEFT');return t
end
local function button(f,value,x,y,w,fn)
 local b=P.NewFrame('Button',nil,f);b:SetSize(w,36);b:SetPoint('TOPLEFT',x,y)
 local bg=b:CreateTexture(nil,'BACKGROUND');bg:SetAllPoints();bg:SetColorTexture(.30,.22,.12,1);b.fill=bg
 b.text=label(b,value,8,-4,w-16,28);b:SetScript('OnClick',fn);return b
end
function P:VoiceCaptureKey(key)
 if not self.voiceCapture then return end
 if key=='ESCAPE' then self:EndVoiceCapture('Binding cancelled.');return end
 if key=='LSHIFT' or key=='RSHIFT' or key=='LCTRL' or key=='RCTRL' or key=='LALT' or key=='RALT' then return end
 local n=vk[key] or key:match('^F(%d+)$') and tonumber(key:sub(2))+111
 if #key==1 and key:match('[A-Z0-9]') then n=key:byte() end
 if not n or n>135 and key:match('^F') then self.voiceInfo:SetText('Use a letter, number, function key or named navigation key.');return end
 self:SetVoiceCapture('keyboard',n,key)
end
function P:SetVoiceCapture(kind,key,name)
 local s=copy(self.pendingVoice);s.kind=kind;s.key=key;s.keyName=name
 s.mods=(IsControlKeyDown and IsControlKeyDown() and 1 or 0)+(IsShiftKeyDown and IsShiftKeyDown() and 2 or 0)+(IsAltKeyDown and IsAltKeyDown() and 4 or 0)
 local why=self:VoiceShortcutProblem(s);if why then self.voiceInfo:SetText(why);return end
 self.pendingVoice=s;self:EndVoiceCapture('Binding selected. Save & reload UI to apply.');self:UpdateVoiceOptions();local warning=self:VoiceBindingWarnings(s);if warning~='' then self.voiceInfo:SetText(warning..'\nChange the binding or explicitly accept this warning when saving.') end
end
function P:EndVoiceCapture(message)
 self.voiceCapture=false
 if self.voiceCaptureFrame then self.voiceCaptureFrame:Hide() end
 if message and self.voiceInfo then self.voiceInfo:SetText(message) end
end
function P:BeginVoiceCapture()
 self.voiceCapture=true;self.voiceCaptureAt=GetTime()+.25;self.voiceCaptureFrame:Show()
 self.voiceCaptureText:SetText('Press your keyboard shortcut or middle / side mouse button.\nHold Ctrl, Alt or Shift first if needed. Escape cancels.\n\nController voice: use the Controller and Hold first controls below.')
end
function P:UpdateVoiceOptions()
 local s=self.pendingVoice;if not s then return end
 if self.voiceConflictAccepted~=self:VoiceConflictSignature(s) then self.voiceSave.text:SetText('Save & reload UI') end
 self.voiceKey.text:SetText('Bind push-to-talk: '..self:VoiceBindingKey(s))
 self.voiceMic.text:SetText(s.mic=='' and 'Microphone: keep current companion input' or 'Microphone: '..s.mic)
 self.voiceEnabled.text:SetText('Voice: '..(s.enabled and 'On' or 'Off'))
 self.voiceStartup.text:SetText('Start with Windows: '..(s.startup=='KEEP' and 'keep current' or s.startup))
 self.voicePad.text:SetText('Controller: '..padNames[s.pad])
 self.voicePadMod.text:SetText('Hold first: '..padNames[s.padMod])
 self.voiceReview.text:SetText('Send: '..(s.review and 'review, then press PTT again' or 'automatically on release'))
 for i,c in ipairs(self.voiceControls) do local on=i==self.voiceIndex;c.fill:SetColorTexture(on and 1 or .30,on and .76 or .22,on and .23 or .12,1) end
end
function P:VoiceOptionsAction(action)
 if InCombatLockdown() or not self.pendingVoice then return end
 if self.voiceWordsEditing then if action=='close' then self:EndVoiceWords(false) end return end
 if self.voiceCapture then if action=='close' then self:EndVoiceCapture('Binding cancelled.') end return end
 if action=='close' then self.voiceOptionsFrame:Hide();self:ShowOptions();return end
 if action=='up' or action=='down' then self.voiceIndex=(self.voiceIndex-1+(action=='up' and -1 or 1))%#self.voiceControls+1
 elseif action=='activate' or action=='previous' or action=='next' then
  local c=self.voiceControls[self.voiceIndex];local fn=c:GetScript('OnClick');if fn then fn(c,action=='previous' and -1 or 1) end
 end
 self:UpdateVoiceOptions()
end
function P:BuildVoiceOptions()
 if self.voiceOptionsFrame then return end
 local f=self.NewFrame('Frame','PadChatVoiceOptions',UIParent);self.voiceOptionsFrame=f;self.optionOwner:SetFrameRef('voice',f)
 f:SetSize(740,640);self:FitPanel(f,740,640);f:SetFrameStrata('FULLSCREEN_DIALOG');f:EnableMouse(true)
 local bg=f:CreateTexture(nil,'BACKGROUND');bg:SetAllPoints();bg:SetTexture('Interface\\DialogFrame\\UI-DialogBox-Background');bg:SetAlpha(.98)
 table.insert(UISpecialFrames,'PadChatVoiceOptions')
 label(f,'PadChat Voice',20,-20,300,32,24)
 local _,_,controls=self:ControllerHelp();self.voiceHelp=label(f,'D-pad: choose / change   '..controls,20,-64,700,25)
 self.voiceKey=button(f,'',20,-102,700,function() self:BeginVoiceCapture() end)
 self.voiceMic=button(f,'',20,-150,700,function(_,delta)
  local list=self:GetVoiceMicrophones();table.insert(list,1,'')
  if #list==1 then self.voiceInfo:SetText('WoW has not listed any microphones. Connect a Windows microphone and select Refresh. Your existing choice is kept.');return end
  self.pendingVoice.mic=choice(list,self.pendingVoice.mic,delta)
  self:UpdateVoiceOptions()
 end)
 self.voicePad=button(f,'',20,-198,340,function(_,delta) self.pendingVoice.pad=choice(pads,self.pendingVoice.pad,delta);if self.pendingVoice.pad=='NONE' then self.pendingVoice.padMod='NONE' end;self:UpdateVoiceOptions() end)
 self.voicePadMod=button(f,'',380,-198,340,function(_,delta) if self.pendingVoice.pad=='NONE' then return end;self.pendingVoice.padMod=choice(modifiers,self.pendingVoice.padMod,delta);self:UpdateVoiceOptions() end)
 self.voiceEnabled=button(f,'',20,-246,190,function() self.pendingVoice.enabled=not self.pendingVoice.enabled;self:UpdateVoiceOptions() end)
 self.voiceStartup=button(f,'',225,-246,340,function(_,delta) self.pendingVoice.startup=choice({'KEEP','ON','OFF'},self.pendingVoice.startup,delta);self:UpdateVoiceOptions() end)
 self.voiceRefresh=button(f,'Refresh',580,-246,140,function() local list=self:GetVoiceMicrophones();self.voiceInfo:SetText(tostring(#list)..' microphone(s) available. Click Microphone to choose.');self:UpdateVoiceOptions() end)
 self.voiceReview=button(f,'',20,-292,700,function() self.pendingVoice.review=not self.pendingVoice.review;self:UpdateVoiceOptions() end)
 self.voiceTest=button(f,'Test microphone / check companion',20,-340,700,function() self.voiceCheckDeadline=GetTime()+8;self.voiceInfo:SetText('Hold your saved PTT shortcut while this panel is open. It tests voice without sending chat. If nothing happens, open PadChat Voice from Start.') end)
 label(f,'The Windows companion must be installed and running. Recognition stays local.\nTap your voice shortcut to cycle chat; hold to speak; release to send.\nChoose a microphone available to Windows. PadChat does not change game sound.',20,-389,700,72)
 self.voiceInfo=label(f,'Save & reload UI writes these choices for the companion to apply.\nReload only when it is safe. It does not restart the game.',20,-474,700,60)
 self.voiceSave=button(f,'Save & reload UI',20,-580,230,function() self:SaveVoiceWithWarnings() end)
 self.voiceBack=button(f,'Back',580,-580,140,function() self:VoiceOptionsAction('close') end)
 self.voiceControls={self.voiceKey,self.voiceMic,self.voicePad,self.voicePadMod,self.voiceEnabled,self.voiceStartup,self.voiceRefresh,self.voiceReview,self.voiceTest,self.voiceSave,self.voiceBack}
 self:BuildVoiceSetupControls(label,button)
 self.voiceCaptureFrame=self.NewFrame('Frame',nil,f);local capture=self.voiceCaptureFrame
 capture:SetAllPoints();capture:SetFrameStrata('TOOLTIP');capture:EnableMouse(true);capture:EnableKeyboard(true)
 if capture.SetPropagateKeyboardInput then capture:SetPropagateKeyboardInput(false) end
 local shade=capture:CreateTexture(nil,'BACKGROUND');shade:SetAllPoints();shade:SetColorTexture(.08,.06,.03,.98)
 self.voiceCaptureText=label(capture,'',30,-150,680,150,18)
 capture:SetScript('OnKeyDown',function(_,key) if GetTime()>=self.voiceCaptureAt then self:VoiceCaptureKey(key) end end)
 capture:SetScript('OnMouseDown',function(_,key)
  if GetTime()<self.voiceCaptureAt then return end
  local map={MiddleButton={4,'BUTTON3'},Button4={5,'BUTTON4'},Button5={6,'BUTTON5'}}
  if map[key] then self:SetVoiceCapture('mouse',map[key][1],map[key][2]) end
 end)
 capture:Hide()
 f:SetScript('OnHide',function() self:EndVoiceCapture();self:EndVoiceWords(false);self.voiceConflictAccepted=nil;self.pendingVoice=nil;if not InCombatLockdown() then ClearOverrideBindings(self.optionOwner) end end);f:Hide()
end
function P:ShowVoiceOptions()
 if InCombatLockdown() then return end
 self:BuildOptions();self:BuildVoiceOptions();self.optionsFrame:Hide();self.pendingVoice=copy(self.db.voiceOptions);self.voiceConflictAccepted=nil;self.voiceSave.text:SetText("Save & reload UI");self.voiceIndex=1;self:UpdateVoiceOptions();local _,_,help=self:ControllerHelp();self.voiceHelp:SetText('D-pad: choose / change   '..help);self.voiceInfo:SetText(self.voiceCompanionStatus or 'Companion status not checked. Hold your saved PTT here to test without sending. Save & reload applies changed settings.');self.voiceOptionsFrame:Show();if self.db.voiceGuideStep then self.voiceGuide.text:SetText('Setup '..self.db.voiceGuideStep..'/4: next');self.voiceInfo:SetText(self:VoiceGuideText()) end
 for key,control in pairs(self.optionActions) do SetOverrideBindingClick(self.optionOwner,true,key,control:GetName(),'LeftButton') end
end
local voiceOwner=P.NewFrame('Frame')
local noop=P.NewFrame('Button','PadChatVoiceKey');noop:RegisterForClicks('AnyDown','AnyUp')
function P:ApplyVoiceBindings()
 if InCombatLockdown() then return end
 ClearOverrideBindings(voiceOwner)
 if self.db and self.db.voiceOptions and self.db.voiceOptions.enabled then
  SetOverrideBindingClick(voiceOwner,true,self:VoiceBindingKey(),noop:GetName(),'LeftButton')
  local s=self.db.voiceOptions
  local mapped={START='PADFORWARD',NORTH='PAD4',LSTICK='PADLSTICK',RSTICK='PADRSTICK',LSHOULDER='PADLSHOULDER',RSHOULDER='PADRSHOULDER'}
  -- Share is reserved by the original device-aware binding owner. Reserve
  -- other user-selected buttons too; never rewrite their saved WoW bindings.
  local primary=mapped[s.pad]
  if primary then SetOverrideBindingClick(voiceOwner,true,primary,noop:GetName(),'LeftButton') end
 end
end
function P:RefreshVoiceBindings() if self.db then self:ApplyVoiceBindings() end end
voiceOwner:RegisterEvent('PLAYER_LOGIN');voiceOwner:RegisterEvent('PLAYER_REGEN_ENABLED')
voiceOwner:SetScript('OnEvent',function() P:RefreshVoiceBindings() end)
