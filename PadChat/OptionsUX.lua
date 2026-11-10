local _,P=...
local function copy(t) local r={} for k,v in pairs(t or {}) do r[k]=v end return r end
local function same(a,b,keys) for _,k in ipairs(keys) do if a[k]~=b[k] then return false end end return true end
local openingKeys={'keyboard','button','modifier'}
local voiceKeys={'kind','key','mods','keyName','mic','enabled','startup','pad','padMod','review','vocabulary'}
local appearanceKeys={'size','position','meter','sound'}
local openNames={AUTO='Automatic',NONE='None',PADBACK='Touchpad / Back',PADSOCIAL='Share / Create',PADFORWARD='Options / Start',PADLSTICK='L3 / left stick',PADRSTICK='R3 / right stick',PADLTRIGGER='L2 / LT',PADRTRIGGER='R2 / RT',PADLSHOULDER='L1 / LB',PADRSHOULDER='R1 / RB'}
local padNames={SHARE='Share / Create / Back',START='Options / Start',NORTH='Triangle / Y',LSTICK='L3',RSTICK='R3',LSHOULDER='L1 / LB',RSHOULDER='R1 / RB',NONE='None',LTRIGGER='L2 / LT',RTRIGGER='R2 / RT'}
local function choices(values,names) local r={} for _,v in ipairs(values) do r[#r+1]={value=v,label=names and names[v] or tostring(v)} end return r end
function P:AppearanceSettings()
 local s=copy(self.db.appearance)
 if s.size~=14 and s.size~=16 and s.size~=20 and s.size~=24 then s.size=16 end
 if s.position~='CENTER' and s.position~='BOTTOM' then s.position='TOP' end
 s.meter=s.meter~=false;s.sound=s.sound==true;return s
end
local function label(f,value,x,y,w,h,size)
 local t=f:CreateFontString(nil,'OVERLAY');t:SetFont('Fonts\\FRIZQT__.TTF',size or 16,'OUTLINE');t:SetTextColor(1,.89,.66);t:SetText(value);t:SetPoint('TOPLEFT',x,y);t:SetSize(w,h);t:SetJustifyH('LEFT');t:SetJustifyV('TOP');return t
end
function P:UXButton(f,value,x,y,w,fn,page)
 local b=self.NewFrame('Button',nil,f);b:SetSize(w,38);b:SetPoint('TOPLEFT',x,y);b.optionRect={x=x,y=-y,w=w};b.uxPage=page
 local bg=b:CreateTexture(nil,'BACKGROUND');bg:SetAllPoints();bg:SetColorTexture(.30,.22,.12,1);b.fill=bg
 b.text=label(b,value,10,-6,w-20,28,17)
 b:SetScript('OnEnter',function() self:UXFocus(b) end)
 b:SetScript('OnClick',function() if InCombatLockdown() then return end;self:UXFocus(b);fn(b) end)
 return b
end
function P:UXFocus(control)
 if not self.optionsFrame or not self.optionsFrame:IsShown() or self.voiceCapture or self.voiceWordsEditing then return end
 local controls=self.uxModal and self.uxModal.controls or self.uxControls
 for i,c in ipairs(controls or {}) do if c==control then
  if self.uxModal then self.uxModal.index=i;self.uxModal.anchor=c.optionRect.x+c.optionRect.w/2
   if self.uxModal.kind=='picker' and c.uxPickIndex then self.uxPicker.selected=c.uxPickIndex end
  else self.uxIndex=i;self.uxAnchor=c.optionRect.x+c.optionRect.w/2 end
  self:UXHighlight();return
 end end
end
function P:UXHighlight()
 local controls=self.uxModal and self.uxModal.controls or self.uxControls
 local selected=self.uxModal and self.uxModal.index or self.uxIndex
 for i,b in ipairs(controls or {}) do
  local on=i==selected;b.fill:SetColorTexture(on and 1 or .30,on and .76 or .22,on and .23 or .12,1)
  b.text:SetTextColor(on and .12 or 1,on and .06 or .89,on and .01 or .66)
 end
end
function P:UXDirty()
 return self.pendingOptions and (not same(self.pendingOptions,self.db.bindings,openingKeys) or not same(self.pendingVoice,self.db.voiceOptions,voiceKeys) or not same(self.pendingAppearance,self:AppearanceSettings(),appearanceKeys)) or false
end
function P:UXRefresh()
 if not self.pendingOptions then return end
 local o,v,a=self.pendingOptions,self.pendingVoice,self.pendingAppearance
 self.optionKey.text:SetText('Open keyboard: '..o.keyboard)
 self.optionButton.text:SetText('Controller: '..(openNames[o.button] or o.button))
 self.optionModifier.text:SetText('Hold first: '..(openNames[o.modifier] or o.modifier))
 self.voiceKey.text:SetText('Bind push-to-talk: '..self:VoiceBindingKey(v))
 self.voiceMic.text:SetText(v.mic=='' and 'Microphone: keep current input' or 'Microphone: '..v.mic)
 self.voicePad.text:SetText('Voice button: '..(padNames[v.pad] or v.pad));self.voicePadMod.text:SetText('Hold first: '..(padNames[v.padMod] or v.padMod))
 self.voiceEnabled.text:SetText('Voice: '..(v.enabled and 'On' or 'Off'));self.voiceStartup.text:SetText('Windows startup: '..(v.startup=='KEEP' and 'Keep current' or v.startup))
 self.voiceReview.text:SetText('Sending: '..(v.review and 'Review first' or 'On release'))
 self.appearanceSize.text:SetText('Text size: '..a.size);self.appearancePosition.text:SetText('Position: '..a.position)
 self.appearanceMeter.text:SetText('Mic meter: '..(a.meter and 'On' or 'Off'));self.appearanceSound.text:SetText('Ready sound: '..(a.sound and 'On' or 'Off'))
 self.uxDirtyLabel:SetText(self:UXDirty() and 'Unsaved changes - Save & reload to apply' or 'Saved settings - D-pad moves; X / A selects; Circle / B goes back')
 self.uxControls={}
 for _,b in ipairs(self.uxAllControls) do
  if not b.uxPage or b.uxPage==self.uxPage then b:Show();self.uxControls[#self.uxControls+1]=b else b:Hide() end
 end
 self.uxIndex=math.max(1,math.min(self.uxIndex or 1,#self.uxControls));self:UXHighlight()
end
function P:UpdateOptions() self:UXRefresh() end
function P:UpdateVoiceOptions() self:UXRefresh() end
function P:UpdateSavedVoiceSummary() self:UXRefresh() end
function P:UXDismissModal()
 self.uxModal=nil;self.uxPicker=nil;if self.uxModalFrame then self.uxModalFrame:Hide() end;self:UXHighlight()
end
function P:UXModalButton(index,title,x,y,w,fn)
 self.uxModalPool=self.uxModalPool or {}
 local b=self.uxModalPool[index]
 if not b then b=self:UXButton(self.uxModalFrame,title,x,y,w,function(control) control.uxAction() end);self.uxModalPool[index]=b end
 b.uxAction=fn;b.uxPickIndex=nil;b:ClearAllPoints();b:SetPoint('TOPLEFT',x,y);b:SetSize(w,38);b.optionRect={x=x,y=-y,w=w};b.text:SetText(title);b.text:SetSize(w-20,28);b:Show()
 return b
end
function P:UXDialog(title,message,actions)
 self:UXDismissModal();local f=self.uxModalFrame
 self.uxModalTitle:SetText(title);self.uxModalText:SetText(message)
 for _,b in ipairs(self.uxModalButtons or {}) do b:Hide() end
 self.uxModalButtons={};local controls={};local count=#actions
 for i,a in ipairs(actions) do
  local w=math.floor(700/count)-12
  local b=self:UXModalButton(i,a[1],30+(i-1)*(w+12),-410,w,function() self:UXDismissModal();a[2]() end)
  controls[#controls+1]=b;self.uxModalButtons[#self.uxModalButtons+1]=b
 end
 self.uxModal={kind='dialog',controls=controls,index=#controls};f:Show();self:UXHighlight()
end
function P:UXPick(title,entries,current,apply)
 if #entries==0 then self.voiceInfo:SetText('No choices are available. Your saved selection is kept.');return end
 self:UXDismissModal();self.uxPicker={entries=entries,current=current,apply=apply,title=title,selected=1}
 for i,e in ipairs(entries) do if e.value==current then self.uxPicker.selected=i;break end end
 self:UXDrawPicker()
end
function P:UXDrawPicker()
 local p=self.uxPicker;local page=math.floor((p.selected-1)/6);local first=page*6+1
 self.uxModalTitle:SetText(p.title)
 self.uxModalText:SetText('D-pad up/down: choose   Left/right: page   X / A: select   Circle / B: cancel\nSelected choice is marked with *. Page '..(page+1)..' / '..math.ceil(#p.entries/6))
 for _,b in ipairs(self.uxModalButtons or {}) do b:Hide() end
 self.uxModalButtons={};local controls={}
 for i=first,math.min(#p.entries,first+5) do
  local entry=p.entries[i];local ix=i
  local b=self:UXModalButton(i-first+1,(entry.value==p.current and '* ' or '')..entry.label,30,-(135+(i-first)*43),700,function()
   local apply=p.apply;self.uxPicker=nil;self:UXDismissModal();apply(entry.value);self:UXRefresh()
  end)
  controls[#controls+1]=b;self.uxModalButtons[#self.uxModalButtons+1]=b;b.uxPickIndex=ix
 end
 local cancel=self:UXModalButton(#controls+1,'Cancel',530,-410,200,function() self.uxPicker=nil;self:UXDismissModal() end)
 controls[#controls+1]=cancel;self.uxModalButtons[#self.uxModalButtons+1]=cancel
 self.uxModal={kind='picker',controls=controls,index=p.selected-first+1};self.uxModalFrame:Show();self:UXHighlight()
end
function P:UXPickField(title,list,object,field,names)
 self:UXPick(title,choices(list,names),object[field],function(value) object[field]=value
  if field=='button' and (value=='AUTO' or value=='NONE') then object.modifier='NONE' end
  if field=='pad' and value=='NONE' then object.padMod='NONE' end
  self.voiceConflictAccepted=nil
 end)
end
function P:UXSave()
 local o,v=self.pendingOptions,self.pendingVoice
 local problem=self:VoiceShortcutProblem(v,o);if problem then self.voiceInfo:SetText(problem);return false end
 local ok,why=self:ValidVoiceWords(v.vocabulary or '');if not ok then self.voiceInfo:SetText(why);return false end
 local old=self.db.bindings;self.db.bindings=copy(o)
 local saved,reason=self:SaveVoiceOptions(v)
 if not saved then self.db.bindings=old;self.voiceInfo:SetText(reason);return false end
 self.db.appearance=copy(self.pendingAppearance)
 local a=self.db.appearance;self.db.appearanceWire=table.concat({'PCA1',a.size,a.position,a.meter and '1' or '0',a.sound and '1' or '0'},';')
 if self.db.voiceGuideStep==3 then self.db.voiceGuideStep=4 end
 self:ApplyOpenBindings();return true
end
function P:UXSaveReload()
 if InCombatLockdown() then return end
 local v=self.pendingVoice;local warning=self:VoiceBindingWarnings(v)
 local saved=self.db.bindings;self.db.bindings=self.pendingOptions;local openWarning=self:BindingWarning();self.db.bindings=saved
 if openWarning~='No existing binding conflicts detected.' then warning=warning..'\n'..openWarning end
 if warning:match('%S') and not self.uxAcceptWarnings then
  self:UXDialog('Binding notice',warning,{{'Accept & save',function() self.uxAcceptWarnings=true;self:UXSaveReload() end},{'Stay',function() end}});return
 end
 self.uxAcceptWarnings=nil
 if not self:UXSave() then return end
 self.uxAllowHide=true;self.optionsFrame:Hide();ReloadUI()
end
function P:UXLeave(after)
 if not self:UXDirty() then after();return end
 self:UXDialog('Keep your changes?','Save applies your choices and reloads the interface. Discard keeps the saved settings. Stay returns to editing.',{
  {'Save & reload',function() self:UXSaveReload() end},
  {'Discard',function() self.pendingOptions=copy(self.db.bindings);self.pendingVoice=copy(self.db.voiceOptions);self.pendingAppearance=self:AppearanceSettings();after() end},
  {'Stay',function() end}})
end
function P:UXClose()
 self:UXLeave(function() self.uxAllowHide=true;self.optionsFrame:Hide() end)
end
function P:UXReload()
 self:UXLeave(function() self:UXDialog('Reload UI?','The interface will reload. Finish dictation first. Saved settings and drafts are kept.',{
  {'Reload UI',function() if GetCurrentKeyBoardFocus and GetCurrentKeyBoardFocus() then self.voiceInfo:SetText('Close the active text box first.');return end;self.uxAllowHide=true;self.optionsFrame:Hide();ReloadUI() end},
  {'Stay',function() end}}) end)
end
function P:UXSetPage(page)
 self.uxPage=page;self.uxIndex=({Controls=1,Voice=2,Appearance=3,Help=4})[page] or 1;self.uxAnchor=nil;self.voiceRecoveryRequested=nil;self:UXDismissModal();self:UXRefresh()
 self.uxTitle:SetText('PadChat Options - '..page)
 self.voiceInfo:SetText(page=='Appearance' and 'Overlay changes apply with Save & reload. Speech model, microphone and recognition speed are kept.' or page=='Help' and 'Voice tests send nothing. Recovery copies the last failed message from this companion session; check chat before sending it again.' or page=='Voice' and 'Choose a microphone or controller button from a list. Bind push-to-talk captures your keyboard / mouse shortcut.' or 'Choose how to open the PadChat keyboard. Your voice shortcut is configured on Voice.')
end
function P:UXAction(action)
 if InCombatLockdown() or not self.pendingOptions or not self.optionsFrame:IsShown() then return end
 if self.voiceCapture then if action=='close' then self:EndVoiceCapture('Binding cancelled.') end return end
 if self.voiceWordsEditing then if action=='close' then self:EndVoiceWords(false) end return end
 if action=='previous' then action='left' elseif action=='next' then action='right' end
 if self.uxModal then
  if action=='close' then self.uxPicker=nil;self:UXDismissModal();return end
  local m=self.uxModal
  if m.kind=='picker' and action~='activate' then
   local p=self.uxPicker;local delta=action=='up' and -1 or action=='down' and 1 or action=='left' and -6 or action=='right' and 6 or 0
   p.selected=math.max(1,math.min(#p.entries,p.selected+delta));self:UXDrawPicker();return
  elseif action=='activate' then local b=m.controls[m.index];b:GetScript('OnClick')(b)
  else m.index,m.anchor=self:NextOptionControl(m.controls,m.index,action,m.anchor);self:UXHighlight() end
  return
 end
 if action=='close' then self:UXClose();return end
 if action=='activate' then local b=self.uxControls[self.uxIndex];if b then b:GetScript('OnClick')(b) end
 else self.uxIndex,self.uxAnchor=self:NextOptionControl(self.uxControls,self.uxIndex,action,self.uxAnchor);self:UXHighlight() end
end
function P:OptionsAction(action) self:UXAction(action) end
function P:VoiceOptionsAction(action) self:UXAction(action) end
function P:BuildOptions() self:BuildSettingsUX() end
function P:BuildVoiceOptions() self:BuildSettingsUX() end
function P:ShowOptions() self:ShowSettingsUX('Controls') end
function P:ShowVoiceOptions() self:ShowSettingsUX('Voice') end
function P:ShowSettingsUX(page)
 if InCombatLockdown() then self:Print('Open Options outside combat.');return end
 if self.conflictingAddon then self:Print('Disable older keyboard add-ons first.');return end
 self:Close();self:BuildSettingsUX()
 if not self.pendingOptions then self.pendingOptions=copy(self.db.bindings);self.pendingVoice=copy(self.db.voiceOptions);self.pendingAppearance=self:AppearanceSettings() end
 self.optionsFrame:Show();self:UXSetPage(page)
 for key,b in pairs(self.optionActions) do SetOverrideBindingClick(self.optionOwner,true,key,b:GetName(),'LeftButton') end
end
function P:BuildSettingsUX()
 if self.uxBuilt then return end;self.uxBuilt=true
 local f=self.NewFrame('Frame','PadChatSettings',UIParent);self.optionsFrame=f;self.voiceOptionsFrame=f
 f:SetSize(780,550);self:FitPanel(f,780,550);f:SetFrameStrata('FULLSCREEN_DIALOG');f:EnableMouse(true)
 local bg=f:CreateTexture(nil,'BACKGROUND');bg:SetAllPoints();bg:SetTexture('Interface\\DialogFrame\\UI-DialogBox-Background');bg:SetAlpha(.98)
 table.insert(UISpecialFrames,'PadChatSettings');self.uxTitle=label(f,'PadChat Options',20,-18,740,35,24)
 self.uxDirtyLabel=label(f,'',20,-468,740,22,14);self.voiceInfo=label(f,'',20,-378,740,82,16);self.optionWarning=self.voiceInfo
 self.uxAllControls={}
 local function add(name,title,x,y,w,fn,page)
  local b=self:UXButton(f,title,x,y,w,fn,page);self[name]=b;table.insert(self.uxAllControls,b);return b
 end
 for i,page in ipairs({'Controls','Voice','Appearance','Help'}) do local target=page;add('uxTab'..page,page,20+(i-1)*188,-65,176,function() self:UXSetPage(target) end) end
 add('optionKey','',20,-130,740,function() self:UXPickField('Keyboard opening shortcut',{'F9','F6','F7','F12','CTRL-F9','SHIFT-F9','ALT-F9'},self.pendingOptions,'keyboard') end,'Controls')
 add('optionButton','',20,-180,360,function() self:UXPickField('Controller opening button',{'AUTO','NONE','PADBACK','PADLSTICK','PADRSTICK','PADSOCIAL','PADFORWARD'},self.pendingOptions,'button',openNames) end,'Controls')
 add('optionModifier','',400,-180,360,function()
  if self.pendingOptions.button=='AUTO' or self.pendingOptions.button=='NONE' then self.voiceInfo:SetText('Choose a controller button first.');return end
  self:UXPickField('Held opening modifier',{'NONE','PADLTRIGGER','PADRTRIGGER','PADLSHOULDER','PADRSHOULDER'},self.pendingOptions,'modifier',openNames)
 end,'Controls')
 add('optionReset','Reset opening defaults',20,-240,360,function() self:UXDialog('Reset opening controls?','This changes the pending opening controls to F9 and Automatic. Voice controls stay unchanged.',{{'Reset',function() self.pendingOptions={keyboard='F9',button='AUTO',modifier='NONE'};self:UXRefresh() end},{'Stay',function() end}}) end,'Controls')
 add('optionReload','Reload UI',400,-240,360,function() self:UXReload() end,'Controls')
 add('voiceKey','',20,-120,740,function() self:BeginVoiceCapture() end,'Voice')
 add('voiceMic','',20,-168,740,function() local list={{value='',label='Keep current companion input'}};for _,name in ipairs(self:GetVoiceMicrophones()) do list[#list+1]={value=name,label=name} end;self:UXPick('Microphone',list,self.pendingVoice.mic,function(v) self.pendingVoice.mic=v end) end,'Voice')
 add('voicePad','',20,-216,360,function() self:UXPickField('Voice controller button',{'SHARE','START','NORTH','LSTICK','RSTICK','LSHOULDER','RSHOULDER','NONE'},self.pendingVoice,'pad',padNames) end,'Voice')
 add('voicePadMod','',400,-216,360,function() if self.pendingVoice.pad=='NONE' then return end;self:UXPickField('Held voice modifier',{'NONE','LTRIGGER','RTRIGGER','LSHOULDER','RSHOULDER'},self.pendingVoice,'padMod',padNames) end,'Voice')
 add('voiceEnabled','',20,-264,230,function() self.pendingVoice.enabled=not self.pendingVoice.enabled;self:UXRefresh() end,'Voice')
 add('voiceStartup','',270,-264,490,function() self:UXPickField('Start with Windows',{'KEEP','ON','OFF'},self.pendingVoice,'startup',{KEEP='Keep current choice',ON='On',OFF='Off'}) end,'Voice')
 add('voiceReview','',20,-312,490,function() self:UXPick('Send recognised messages',{{value=false,label='Send on release'},{value=true,label='Review, then press PTT to send'}},self.pendingVoice.review,function(v) self.pendingVoice.review=v end) end,'Voice')
 add('voiceRefresh','Refresh microphones',530,-312,230,function() self.voiceInfo:SetText(tostring(#self:GetVoiceMicrophones())..' microphone(s) found. Select Microphone to choose. Existing input is kept.');end,'Voice')
 add('appearanceSize','',20,-130,360,function() self:UXPickField('Overlay text size',{14,16,20,24},self.pendingAppearance,'size',{[14]='Small (14)',[16]='Current size (16)',[20]='Large (20)',[24]='Extra large (24)'}) end,'Appearance')
 add('appearancePosition','',400,-130,360,function() self:UXPickField('Overlay position',{'TOP','CENTER','BOTTOM'},self.pendingAppearance,'position',{TOP='Top centre',CENTER='Screen centre',BOTTOM='Bottom centre'}) end,'Appearance')
 add('appearanceMeter','',20,-180,360,function() self.pendingAppearance.meter=not self.pendingAppearance.meter;self:UXRefresh() end,'Appearance')
 add('appearanceSound','',400,-180,360,function() self.pendingAppearance.sound=not self.pendingAppearance.sound;self:UXRefresh() end,'Appearance')
 add('appearanceReset','Restore appearance',20,-240,740,function() self.pendingAppearance={size=16,position='TOP',meter=true,sound=false};self:UXRefresh() end,'Appearance')
 add('voiceTest','Test saved PTT / microphone',20,-130,360,function() self.voiceRecoveryRequested=nil;self.voiceCheckDeadline=GetTime()+12;self.voiceInfo:SetText(self:VoiceTestInstructions()) end,'Help')
 add('voiceRestart','Restart voice + reload UI',400,-130,360,function() self:UXLeave(function() self:UXDialog('Restart voice?','Finish dictation first. This restarts the companion and reloads the UI.',{{'Restart',function() self.voiceRestartConfirm=GetTime()+8;self.uxAllowHide=true;self:RestartVoiceFromGame();self.uxAllowHide=nil end},{'Stay',function() end}}) end) end,'Help')
 add('voiceRecover','Copy last failed message',20,-180,740,function() self.voiceRecoveryRequested=true;self.voiceInfo:SetText('Hold your saved PTT now to retrieve and copy the last failed message. No microphone recording or message sending. Recovery lasts only for this companion session.');end,'Help')
 -- Reuse the established binding-capture and vocabulary editor implementations.
 self.voiceControls={};self:BuildVoiceSetupControls(label,function(parent,title,x,y,w,fn) return self:UXButton(parent,title,x,y,w,fn,'Help') end)
 local function relocate(b,x,y,w) b:ClearAllPoints();b:SetPoint('TOPLEFT',x,y);b:SetSize(w,38);b.optionRect={x=x,y=-y,w=w};b.uxPage='Help';table.insert(self.uxAllControls,b) end
 relocate(self.voiceGuide,20,-230,360);relocate(self.voiceWords,400,-230,360);relocate(self.voicePrevious,20,-280,740)
 self.voiceCaptureFrame=self.NewFrame('Frame',nil,f);local capture=self.voiceCaptureFrame;capture:SetAllPoints();capture:SetFrameStrata('TOOLTIP');capture:EnableMouse(true);capture:EnableKeyboard(true)
 if capture.SetPropagateKeyboardInput then capture:SetPropagateKeyboardInput(false) end
 local shade=capture:CreateTexture(nil,'BACKGROUND');shade:SetAllPoints();shade:SetColorTexture(.08,.06,.03,.98)
 self.voiceCaptureText=label(capture,'',30,-150,720,160,18)
 capture:SetScript('OnKeyDown',function(_,key) if GetTime()>=self.voiceCaptureAt then self:VoiceCaptureKey(key) end end)
 capture:SetScript('OnMouseDown',function(_,key) if GetTime()<self.voiceCaptureAt then return end;local map={MiddleButton={4,'BUTTON3'},Button4={5,'BUTTON4'},Button5={6,'BUTTON5'}};if map[key] then self:SetVoiceCapture('mouse',map[key][1],map[key][2]) end end);capture:Hide()
 add('voiceSave','Save & reload UI',20,-500,270,function() self:UXSaveReload() end)
 add('optionClose','Close',600,-500,160,function() self:UXClose() end)
 self.optionSave=self.voiceSave;self.voiceBack=self.optionClose
 local modal=self.NewFrame('Frame',nil,f);self.uxModalFrame=modal;modal:SetAllPoints();modal:SetFrameStrata('TOOLTIP');modal:EnableMouse(true)
 local shade2=modal:CreateTexture(nil,'BACKGROUND');shade2:SetAllPoints();shade2:SetColorTexture(.08,.06,.03,.99)
 self.uxModalTitle=label(modal,'',30,-24,700,40,24);self.uxModalText=label(modal,'',30,-74,700,320,17);modal:Hide()
 self.optionOwner=self.NewFrame('Frame',nil,nil,'SecureHandlerStateTemplate');self.optionOwner:SetAttribute('_onstate-combat',[[if newstate=='active' then self:ClearBindings() end]])
 RegisterStateDriver(self.optionOwner,'combat','[combat] active; inactive');self.optionActions={}
 for key,action in pairs({PADDUP='up',PADDDOWN='down',PADDLEFT='left',PADDRIGHT='right',PAD1='activate',PAD2='close'}) do
  local b=self.NewFrame('Button','PadChatSettingsAction'..key);b:RegisterForClicks('AnyUp');b:SetScript('OnClick',function() self:UXAction(action) end);self.optionActions[key]=b
 end
 f:SetScript('OnHide',function()
  if self.uxHiding then return end;self.uxHiding=true
  self:EndVoiceCapture();self:EndVoiceWords(false);self:UXDismissModal();self.voiceRecoveryRequested=nil
  if not InCombatLockdown() then ClearOverrideBindings(self.optionOwner) end
  if not self.uxAllowHide and self:UXDirty() and not InCombatLockdown() then
   f:Show();for key,b in pairs(self.optionActions) do SetOverrideBindingClick(self.optionOwner,true,key,b:GetName(),'LeftButton') end
   self:UXClose()
  elseif self.uxAllowHide or not self:UXDirty() then self.pendingOptions=nil;self.pendingVoice=nil;self.pendingAppearance=nil end
  self.uxAllowHide=nil;self.uxHiding=nil
 end)
 self.uxAllowHide=true;f:Hide()
end

function P:VoiceRecoveryRetrieved(words,uncertain)
 if words=='' then self.voiceInfo:SetText('No failed message is available in this companion session.');return end
 local warning=uncertain and 'Delivery was uncertain: check chat before sending again.' or 'The previous attempt was stopped before sending.'
 self:UXDialog('Recovered message',warning..'\nCopied to the Windows clipboard. Nothing has been resent.\n\n'..words,{
  {'Use as draft',function()
   self:UXLeave(function() self.uxAllowHide=true;self.optionsFrame:Hide();self:Open();self:SetText(words);self:Refresh() end)
  end},
  {'Close',function() end}})
end
local oldEndWords=P.EndVoiceWords
function P:EndVoiceWords(save)
 oldEndWords(self,save)
 if self.uxBuilt then self:UXRefresh() end
end
