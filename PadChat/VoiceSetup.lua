local _,P=...
function P:VoiceBindingWarnings(s)
 local messages={};local keys={self:VoiceBindingKey(s)}
 local native={START='PADFORWARD',NORTH='PAD4',LSTICK='PADLSTICK',RSTICK='PADRSTICK',LSHOULDER='PADLSHOULDER',RSHOULDER='PADRSHOULDER',SHARE=self:ControllerBindingMode()=='sony' and 'PADSOCIAL' or 'PADBACK'}
 if s.pad~='NONE' then keys[#keys+1]=native[s.pad] end
 if not s.enabled then return '' end
 for _,key in ipairs(keys) do
  local ok,action=pcall(GetBindingAction,key,false)
  if ok and type(action)=='string' and action~='' and not action:find('PadChat',1,true) then
   messages[#messages+1]=key..' is bound to '..action..'. PTT reserves the primary button, including with a held modifier.'
  end
 end
 return table.concat(messages,'\n')
end
function P:VoiceConflictSignature(s)
 return self:VoiceBindingKey(s)..';'..s.pad..';'..s.padMod..';'..self:VoiceBindingWarnings(s)
end
function P:SaveVoiceWithWarnings()
 local s=self.pendingVoice;if not s then return end
 local warning=self:VoiceBindingWarnings(s);local signature=self:VoiceConflictSignature(s)
 if warning~='' and self.voiceConflictAccepted~=signature then
  self.voiceConflictAccepted=signature;self.voiceSave.text:SetText('Accept conflicts & reload')
  self.voiceInfo:SetText(warning..'\nChoose another binding, or activate Accept conflicts & reload to keep it.');return
 end
 local ok,why=self:SaveVoiceOptions(s);if not ok then self.voiceInfo:SetText(why);return end
 if self.db.voiceGuideStep then self.db.voiceGuideStep=4 end
 self:EndVoiceCapture();self.voiceOptionsFrame:Hide();ReloadUI()
end
function P:VoiceGuideText()
 local step=self.db.voiceGuideStep
 if step==1 then return 'Setup 1/4: choose a Windows microphone using Microphone. Refresh if needed; then choose Setup next.' end
 if step==2 then return 'Setup 2/4: choose Bind push-to-talk or Controller / Hold first. Keep current controls with Setup next; check conflicts before saving.' end
 if step==3 then return 'Setup 3/4: Save & reload UI somewhere safe. This applies the microphone and PTT; setup resumes here after reload.' end
 if step==4 then return 'Setup 4/4: '..self:VoiceTestInstructions() end
 return 'Choose Guided setup for microphone, PTT, apply and a safe voice test.'
end
function P:PreviousVoiceGuide()
 local step=self.db.voiceGuideStep
 if type(step)~='number' or step<1 or step>4 then self.voiceInfo:SetText('Choose Guided setup to begin. Back returns to PadChat Options.');return end
 -- Going back changes only the guide, never saved microphone/PTT choices.
 self.db.voiceGuideStep=math.max(1,step-1)
 self.voiceGuide.text:SetText('Setup '..self.db.voiceGuideStep..'/4: next')
 self.voiceInfo:SetText(self:VoiceGuideText())
end
function P:NextVoiceGuide()
 local step=self.db.voiceGuideStep
 if not step or self.db.voiceGuideComplete then self.db.voiceGuideComplete=nil;step=1
 elseif step==1 then
  if self.pendingVoice.mic=='' then self.voiceInfo:SetText('Select a microphone first. Setup does not guess or switch your input.');return end
  step=2
 elseif step==2 then local why=self:VoiceShortcutProblem(self.pendingVoice);if why then self.voiceInfo:SetText(why);return end;step=3
 end
 self.db.voiceGuideStep=step;self.voiceGuide.text:SetText('Setup '..step..'/4: next')
 self.voiceInfo:SetText(self:VoiceGuideText())
end
function P:VoiceGuideChecked(state,words)
 if state=='test' and words~='' and self.db.voiceGuideStep==4 then
  self.db.voiceGuideStep=nil;self.db.voiceGuideComplete=true
  self.voiceGuide.text:SetText('Setup complete / restart')
 end
end
function P:BeginVoiceWords()
 if self.voiceCapture then return end
 self.voiceWordsEditing=true;self.voiceWordsFrame:Show();self.voiceWordsBox:SetText(self.pendingVoice.vocabulary or '');self.voiceWordsBox:SetFocus()
end
function P:EndVoiceWords(save)
 if save then
  local text=self.voiceWordsBox:GetText();local ok,why=self:ValidVoiceWords(text)
  if not ok then self.voiceInfo:SetText(why);return end
  self.pendingVoice.vocabulary=text
 end
 if self.voiceWordsBox and GetCurrentKeyBoardFocus()==self.voiceWordsBox then self.voiceWordsBox:ClearFocus() end
 self.voiceWordsEditing=false;if self.voiceWordsFrame then self.voiceWordsFrame:Hide() end
end
function P:ValidVoiceWords(text)
 if type(text)~='string' or #text>600 or text:find('[%c/;|"\\]') then return false,'Use comma-separated words/names, at most 600 bytes; no commands or control characters.' end
 local count=0
 for term in text:gmatch('[^,]+') do
  term=term:match('^%s*(.-)%s*$')
  if term~='' then count=count+1;if #term>60 or term:gsub('[\128-\255]',''):find("[^%w .'-]") then return false,'Use short words or player names, separated by commas.' end end
 end
 if count>32 then return false,'Use at most 32 custom words or names.' end
 return true
end
function P:BuildVoiceSetupControls(label,button)
 local f=self.voiceOptionsFrame
 self.voiceGuide=button(f,'Guided setup',340,-16,180,function() self:NextVoiceGuide() end)
 self.voiceWords=button(f,'WoW words / names',540,-16,180,function() self:BeginVoiceWords() end)
 table.insert(self.voiceControls,self.voiceGuide);table.insert(self.voiceControls,self.voiceWords)
 self.voicePrevious=button(f,'Previous setup step',270,-690,240,function() self:PreviousVoiceGuide() end)
 table.insert(self.voiceControls,self.voicePrevious)
 local edit=self.NewFrame('Frame',nil,f);self.voiceWordsFrame=edit;edit:SetAllPoints();edit:SetFrameStrata('TOOLTIP');edit:EnableMouse(true)
 local shade=edit:CreateTexture(nil,'BACKGROUND');shade:SetAllPoints();shade:SetColorTexture(.08,.06,.03,.98)
 label(edit,'Optional recognition hints',24,-80,690,40,22)
 label(edit,'Comma-separated WoW words or player names (600 bytes / 32 terms).\nThese are local recognition hints, not automatic replacements or invite targets.\nType with the keyboard; empty keeps the built-in WoW vocabulary. Enter saves; Escape cancels.',24,-132,690,100)
 local box=self.NewFrame('EditBox',nil,edit);self.voiceWordsBox=box;box:SetSize(680,40);box:SetPoint('TOPLEFT',24,-250);box:SetFontObject(GameFontNormal);box:SetAutoFocus(false);box:SetMaxLetters(600)
 box:SetScript('OnEnterPressed',function() self:EndVoiceWords(true) end);box:SetScript('OnEscapePressed',function() self:EndVoiceWords(false) end)
 button(edit,'Keep hints',24,-325,200,function() self:EndVoiceWords(true) end);button(edit,'Cancel',500,-325,200,function() self:EndVoiceWords(false) end);edit:Hide()
end
