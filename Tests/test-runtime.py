from pathlib import Path
import sys
root=Path(__file__).resolve().parent.parent
from lupa.lua51 import LuaRuntime
l=LuaRuntime(unpack_returned_tuples=True)
l.execute('''
P={}; widgets={}; clock=1; combat=false; focus=nil; bindings={}; sent={}; hooks={}
UIParent={}; UISpecialFrames={}; SlashCmdList={}; C_Timer={After=function(_,fn) fn() end}
function GetTime() return clock end
function SendChatMessage() end
function InCombatLockdown() return combat end
function GetCurrentKeyBoardFocus() return focus end
function IsInGroup() return false end
function IsInRaid() return false end
function IsInGuild() return false end
function GetChannelList() return 4,'General - Test',false end
function GetChannelName(id) return id==4 and 4 or 0 end
DEFAULT_CHAT_FRAME={AddMessage=function() end}
function ClearOverrideBindings(owner) for key,value in pairs(bindings) do if value.owner==owner then bindings[key]=nil end end end
function SetOverrideBindingClick(owner,priority,key,name,button) assert(not combat);bindings[key]={name=name,owner=owner} end
function SetOverrideBinding(owner,priority,key,name) assert(not combat);bindings[key]={name=name,owner=owner} end
function hooksecurefunc(name,fn) hooks[name]=fn end
function RegisterStateDriver(frame,state,condition) frame.driver={state,condition} end
local Frame={}
Frame.__index=Frame
for _,method in ipairs({'SetParent','ClearAllPoints','SetPoint','SetFont','SetFontObject','SetAllPoints','SetFrameStrata','EnableMouse','SetJustifyH','SetHeight','SetWidth','SetTexture','SetAutoFocus','SetMaxLetters','SetAlpha','RegisterForClicks','HighlightText','RegisterEvent','SetColorTexture','EnableGamePadStick','SetTextColor','EnableKeyboard','RegisterForDrag'}) do Frame[method]=function() end end
function Frame:SetScript(key,fn) self.scripts[key]=fn end
function Frame:GetScript(key) return self.scripts[key] end
function Frame:SetPoint(...) self.point={...} end
function Frame:SetFrameRef(key,frame) self.refs=self.refs or {};self.refs[key]=frame end
function Frame:GetFrameRef(key) return self.refs[key] end
function Frame:ClearBindings() ClearOverrideBindings(self) end
function Frame:GetName() return self.name end
function Frame:SetSize(w,h) self.w=w;self.h=h end
function Frame:SetFrameLevel(level) self.level=level end
function Frame:GetFrameLevel() return self.level or 3 end
function Frame:SetHighlightTexture(texture) self.highlight=texture end
function Frame:SetTexture(texture) self.texture=texture end
function Frame:GetSize() return self.w,self.h end
function Frame:SetAttribute(key,value) assert(not combat);self.attrs[key]=value end
function Frame:RegisterForClicks(...) self.clicks={...} end
function Frame:Show() self.shown=true end
function Frame:IsShown() return self.shown end
function Frame:Hide() local shown=self.shown;self.shown=false;if shown and self.scripts and self.scripts.OnHide then self.scripts.OnHide(self) end end
function Frame:SetText(text) self.text=text end
function Frame:GetText() return self.text or '' end
function Frame:SetFocus() focus=self end
function Frame:ClearFocus() if focus==self then focus=nil end end
function Frame:CreateTexture() return setmetatable({},Frame) end
function Frame:CreateFontString() return setmetatable({},Frame) end
function CreateFrame(kind,name,parent,template)
 local f=setmetatable({name=name,scripts={},attrs={}},Frame);widgets[name or (#widgets+1)]=f;return f
end
''')
for name in ['Core','Model','Options','VoiceOptions','VoiceSetup','Minimap','Layout','UI','VoicePTT','Invites','Events']:
 l.execute((root/'PadChat'/f'{name}.lua').read_text(encoding='utf-8-sig'),'PadChat',l.globals().P)
l.execute('''
P:InitDB();P.openBindingOwner.scripts.OnEvent(P.openBindingOwner,'PLAYER_LOGIN');P:Open();assert(P.open)
assert(bindings.PAD1.name=='PadChatActionPAD1')
assert(bindings.PADFORWARD.name=='PadChatSend')
P.model.row=2;P.model.col=1;P:Act('select');assert(P:GetText()=='q')
P:Act('space');P:Act('delete');assert(P:GetText()=='q')
P:Act('channel');assert(P:VoiceDestination()=='/y')
P:PrepareSend();assert(P.sendButton.attrs.macrotext=='/y q')
P.model.row=5;P.model.col=5;P:Refresh();assert(bindings.PAD1.name=='PadChatSend')
P:Close();assert(not P.open and not bindings.PAD1 and P.db.draft=='q')
P:Open();assert(P:GetText()=='q');P:SetText('');P:Close()
-- Exercise real close entry points, not just calling P:Close directly.
P:SetText('keep this draft');P:Open();P.closeButton.scripts.OnClick()
assert(not P.open and not bindings.PAD1 and not P.sendButton.shown and P:GetText()=='keep this draft')
P:Open();assert(UISpecialFrames[1]=='PadChatKeyboard');P.frame:Hide() -- Escape special-frame path
assert(not P.open and not bindings.PAD2 and P:GetText()=='keep this draft')
for _,edge in ipairs({'release','legacy','both'}) do
 P:Open();local b=P.buttons.PAD2
 if edge=='both' then b.scripts.OnClick(b,'LeftButton',true) end
 if edge=='legacy' then b.scripts.OnClick(b,'LeftButton') else b.scripts.OnClick(b,'LeftButton',false) end
 assert(not P.open and not bindings.PAD1 and not P.sendButton.shown)
end
-- A paired press/release must type once; release-only clients must also type.
P:SetText('');P:Open();P.model.row=2;P.model.col=1;P:Refresh()
local select=P.buttons.PAD1;select.scripts.OnClick(select,'LeftButton',true);select.scripts.OnClick(select,'LeftButton',false)
assert(P:GetText()=='q');select.scripts.OnClick(select,'LeftButton',false);assert(P:GetText()=='qq');P:Close()
-- Footer geometry leaves a gap below every button at either layout.
for _,symbols in ipairs({false,true}) do
 P.model.numbers=symbols;P:Refresh()
 local bottom=-P.keys[5][1].point[3]+P.keys[5][1].h
 assert(-P.hint.point[3]>=bottom+16)
 assert(-P.hint2.point[3]>=-P.hint.point[3]+20)
 assert(-P.keyboardHint.point[3]+20<=P.frame.h-12)
end
-- Keyboard-only dictation retains the companion protocol.
P:RememberVoiceChannel('SAY');P:PreparePTT();assert(P.pttBox.text=='CKPTT_READY2:1;/s;SAY');P:PTTReleaseFocus()
P:RememberVoiceChannel('PARTY');P:PreparePTT();assert(P.pttBox.text:find('CKPTT_DENIED:'));P:PTTReleaseFocus()
P:RememberVoiceChannel('WHISPER','Bad|name');assert(P:VoiceDestination()==nil)
function RegionalUniqueNamesEnabled() return true end
P:RememberVoiceChannel('REPLY','Friendly Player','WHISPER');assert(P:VoiceDestination()=='/w Friendly-Player')
P:Open();assert(P:VoiceDestination()=='/w Friendly-Player');P:SetText('draft');P:PreparePTT()
assert(P:GetText()=='draft' and P.pttBox.text:find('CKPTT_DENIED:'));P:PTTReleaseFocus();P:Close()
-- Combat entry cannot mutate secure attributes or bindings.
combat=true;P:Open();assert(not P.open);P:PrepareSend();combat=false
-- Closing keeps drafts and a normal new send prepares the correct command.
P:SetText('thank you');P:RememberVoiceChannel('GUILD');assert(not P:BuildMacroText())
P:RememberVoiceChannel('SAY');assert(P:BuildMacroText()=='/s thank you')
P:Open();P.justSent=true;P.sendButton.scripts.PostClick();assert(not P.open and P:GetText()=='')
-- Exercise the secure action's real press/release decision. WoW falls back to
-- ActionButtonUseKeyDown when an individual button has no explicit preference.
-- A release-only Send must execute once with either global preference, for a
-- mouse click or a keyboard/controller binding, and leave no chat focus.
local function secureClick(button,down,isKeyPress,globalKeyDown)
 local phase=down and 'AnyDown' or 'AnyUp';local registered=false
 for _,click in ipairs(button.clicks) do if click==phase then registered=true end end
 if not registered then return end
 button.scripts.PreClick(button,'LeftButton',down)
 local use=button.attrs.useOnKeyDown
 if use==nil then use=globalKeyDown end
 if not isKeyPress then use=false end
 if (down and use) or (not down and not use) then
  if button.attrs.macrotext~='' then
   sent[#sent+1]=button.attrs.macrotext
   hooks.SendChatMessage(button.attrs.macrotext:sub(4),'SAY')
  end
 end
 button.scripts.PostClick(button,'LeftButton',down)
end
for _,globalKeyDown in ipairs({false,true}) do
 for _,isKeyPress in ipairs({false,true}) do
  P:RememberVoiceChannel('SAY');P:SetText('secure send test');P:Open()
  local count=#sent
  secureClick(P.sendButton,true,isKeyPress,globalKeyDown)
  secureClick(P.sendButton,false,isKeyPress,globalKeyDown)
  assert(#sent==count+1 and sent[#sent]=='/s secure send test')
  assert(not P.open and P:GetText()=='' and not focus)
 end
end
P:Open();combat=true
local secure=assert(loadstring(P.keyboardOwner.attrs['_onstate-combat']))
setfenv(secure,{self=P.keyboardOwner,newstate='active'});secure()
assert(not bindings.PAD1 and not bindings.PADFORWARD and not P.sendButton.shown)
P:Close();assert(P.cleanupPending);combat=false
-- Native Share and touchpad are separate; glyph-only Xbox overrides must not
-- make Back open the keyboard. Mixed/unknown devices keep their original keys.
local devices={{vendorID=1356,productID=1476}}
C_GamePad={GetAllDeviceIDs=function() local ids={} for i in ipairs(devices) do ids[#ids+1]=i end return ids end,
 GetDeviceRawState=function(i) return devices[i] end}
local event=P.openBindingOwner.scripts.OnEvent
event(P.openBindingOwner,'GAME_PAD_CONNECTED')
assert(bindings.PADBACK.name=='PadChatToggle' and bindings.PADSOCIAL.name=='PadChatShare')
assert(widgets.PadChatShare.scripts.OnClick==nil)
devices={{vendorID=1118,productID=654,labelStyle='Shapes'}}
event(P.openBindingOwner,'GAME_PAD_ACTIVE_CHANGED')
assert(bindings.PADBACK.name=='PadChatShare' and not bindings.PADSOCIAL)
devices={{vendorID=1356,productID=1476},{vendorID=1118,productID=654}}
event(P.openBindingOwner,'GAME_PAD_CONNECTED');assert(not bindings.PADBACK and not bindings.PADSOCIAL)
devices={{vendorID=999,productID=1}}
event(P.openBindingOwner,'GAME_PAD_CONNECTED');assert(not bindings.PADBACK)
devices={{vendorID=1356,productID=1476}};combat=true
event(P.openBindingOwner,'GAME_PAD_CONNECTED');assert(not bindings.PADBACK)
combat=false;event(P.openBindingOwner,'PLAYER_REGEN_ENABLED')
assert(bindings.PADSOCIAL.name=='PadChatShare')
''')
print('Passed full addon startup, controller bindings, draft persistence, send preparation/cleanup, voice compatibility, whisper addresses, channel guards and combat refusal.')

l.execute("if P.optionsFrame then P.optionsFrame:Hide() end; P:Close();focus=nil;combat=false")
