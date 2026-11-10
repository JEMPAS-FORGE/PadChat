
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
