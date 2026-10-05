local name,P=...
BINDING_HEADER_PADCHAT='PadChat'
BINDING_NAME_PADCHAT_TOGGLE='Open / close controller keyboard'
local owner=P.NewFrame('Frame')
local toggle=P.NewFrame('Button','PadChatToggle');toggle:RegisterForClicks('AnyUp');toggle:SetScript('OnClick',PadChat_Toggle)
local noop=P.NewFrame('Button','PadChatShare');noop:RegisterForClicks('AnyDown','AnyUp')
function P:ControllerBindingMode()
 if not C_GamePad or not C_GamePad.GetAllDeviceIDs or not C_GamePad.GetDeviceRawState then return nil end
 local ok,ids=pcall(C_GamePad.GetAllDeviceIDs);if not ok or type(ids)~='table' then return nil end
 local found
 for _,id in ipairs(ids) do
  local good,raw=pcall(C_GamePad.GetDeviceRawState,id)
  if good and raw and raw.vendorID and raw.vendorID~=0 then
   local mode=raw.vendorID==1356 and 'sony' or raw.vendorID==1118 and raw.productID==654 and 'xbox'
   if not mode or found and found~=mode then return nil end
   found=mode
  end
 end
 return found
end
local function bindOpen()
 if InCombatLockdown() or P.tvActionFailed then return end
 ClearOverrideBindings(owner)
 SetOverrideBindingClick(owner,true,'F9',toggle:GetName(),'LeftButton')
 SetOverrideBindingClick(owner,true,'CTRL-SHIFT-F12',toggle:GetName(),'LeftButton')
 -- Raw hardware IDs distinguish touchpad from Xbox Back, even when glyphs
 -- are overridden. Xbox Back remains exclusively Share push-to-talk.
 local mode=P:ControllerBindingMode()
 if mode=='sony' then
  SetOverrideBindingClick(owner,true,'PADBACK',toggle:GetName(),'LeftButton')
  -- Native PlayStation Share is Social; the companion reads the physical
  -- button separately. Consume WoW's action without changing saved bindings.
  SetOverrideBindingClick(owner,true,'PADSOCIAL',noop:GetName(),'LeftButton')
 elseif mode=='xbox' then SetOverrideBindingClick(owner,true,'PADBACK',noop:GetName(),'LeftButton') end
end
P.openBindingOwner=owner
local function sent(text,kind,language,target)
 if not P.db then return end
 P.justSent=true;P:Learn(text);P:RememberVoiceChannel(kind,target)
end
SLASH_PADCHAT1='/padchat';SLASH_PADCHAT2='/pc'
SlashCmdList.PADCHAT=function(text)
 if P.conflictingAddon then P:Print('Disable older ControllerKeyboard add-ons and restart WoW.');return end
 C_Timer.After(0,function()
  if text=='bindings' then
   P:Print('Controller: %s. Back: %s. Share/Social: %s.',P:ControllerBindingMode() or 'unknown',GetBindingAction('PADBACK',true) or '',GetBindingAction('PADSOCIAL',true) or '')
  elseif text=='clear' then P:SetText('')
  elseif text=='channel' then P:CycleVoiceChannel()
  else PadChat_Toggle() end
 end)
end
-- Backwards-compatible private commands for the existing local voice helper.
SLASH_PADCHATVOICE1='/ck'
SlashCmdList.PADCHATVOICE=function(text)
 if P.conflictingAddon then return end
 local cmd,arg=(text or ''):match('^(%S+)%s*(.-)$')
 if cmd=='voiceinvite' then P.justSent=P:InviteByVoice(arg) or false
 elseif cmd=='invitekey' then P.justSent=P:InviteByKey(arg) or false
 elseif cmd=='reply' and arg~='' then P:SelectVoiceReply(arg)
 else P:Print('Open: /padchat or F9. Tap F8: channel. Hold F8: speak.') end
end
for _,event in ipairs({'ADDON_LOADED','PLAYER_LOGIN','PLAYER_LOGOUT','PLAYER_REGEN_DISABLED','PLAYER_REGEN_ENABLED','PLAYER_ENTERING_WORLD','GAME_PAD_ACTIVE_CHANGED','GAME_PAD_CONNECTED','GAME_PAD_DISCONNECTED','GROUP_ROSTER_UPDATE','ADDON_ACTION_BLOCKED','ADDON_ACTION_FORBIDDEN'}) do owner:RegisterEvent(event) end
owner:SetScript('OnEvent',function(_,event,addon)
 if P.conflictingAddon then return end
 if event=='ADDON_LOADED' and addon==name then P:InitDB()
 elseif event=='PLAYER_LOGIN' then
  local loaded=C_AddOns and C_AddOns.IsAddOnLoaded or IsAddOnLoaded
  if loaded and (loaded('ControllerKeyboard') or loaded('ControllerKeyboardTouchpad')) then
   P.conflictingAddon=true;P:Print('Disable ControllerKeyboard and ControllerKeyboardTouchpad, then restart WoW to use PadChat.');return
  end
  P:BuildUI();P:InitPTT();bindOpen()
  if SendChatMessage then hooksecurefunc('SendChatMessage',sent) end
  if C_ChatInfo and C_ChatInfo.SendChatMessage then hooksecurefunc(C_ChatInfo,'SendChatMessage',sent) end
  P:Print('Ready. Touchpad / F9: keyboard. Tap F8 or Share: channel; hold: voice.')
 elseif event=='PLAYER_REGEN_DISABLED' then if P.open then P:Close() end
 elseif event=='PLAYER_REGEN_ENABLED' then
  if P.cleanupPending then P.cleanupPending=nil;ClearOverrideBindings(P.keyboardOwner);P.sendButton:Hide() end
  bindOpen()
 elseif event=='PLAYER_ENTERING_WORLD' or event=='GAME_PAD_ACTIVE_CHANGED' or event=='GAME_PAD_CONNECTED' or event=='GAME_PAD_DISCONNECTED' or event=='GROUP_ROSTER_UPDATE' then C_Timer.After(.15,bindOpen)
 elseif (event=='ADDON_ACTION_BLOCKED' or event=='ADDON_ACTION_FORBIDDEN') and addon==name then
  P.tvActionFailed=true;P:Print('Keyboard paused after a blocked game action. Reload to retry.')
  C_Timer.After(0,function() if not InCombatLockdown() then P:Close() end end)
 elseif event=='PLAYER_LOGOUT' then P.db.draft=P:GetText() end
end)
