local _,P=...
function P:PositionMinimapButton()
 if not self.minimapButton then return end
 -- 225 is already occupied by common quest voice add-ons. Start on the left
 -- edge instead, while preserving a position the player explicitly dragged.
 local saved=self.db.minimapAngle
 local angle=math.rad(type(saved)=='number' and saved==saved and saved or 180)
 local width=Minimap.GetWidth and Minimap:GetWidth() or 156
 local radius=(type(width)=='number' and width or 156)/2+5
 self.minimapButton:ClearAllPoints()
 self.minimapButton:SetPoint('CENTER',Minimap,'CENTER',math.cos(angle)*radius,math.sin(angle)*radius)
end
function P:InitMinimapButton()
 if self.minimapButton or not Minimap then return end
 local b=self.NewFrame('Button','PadChatMinimapButton',Minimap);self.minimapButton=b
 b:SetSize(32,32);b:SetFrameStrata('MEDIUM')
 b:SetFrameLevel(math.max(8,(Minimap.GetFrameLevel and Minimap:GetFrameLevel() or 3)+5))
 b:EnableMouse(true);b:RegisterForClicks('LeftButtonUp','RightButtonUp');b:RegisterForDrag('LeftButton')
 b:SetHighlightTexture('Interface\\Minimap\\UI-Minimap-ZoomButton-Highlight')
 local bg=b:CreateTexture(nil,'BACKGROUND');bg:SetSize(23,23);bg:SetPoint('CENTER',0,0);bg:SetColorTexture(.08,.06,.03,1)
 -- PadChat's current Warcraft-themed branding distinguishes it from quest voice icons.
 local icon=b:CreateTexture(nil,'ARTWORK');icon:SetSize(23,23);icon:SetPoint('CENTER',0,0)
 icon:SetTexture('Interface\\AddOns\\PadChat\\PadChatIconWarcraft');b.icon=icon
 -- The stock tracking border keeps the button readable beside other add-ons.
 local border=b:CreateTexture(nil,'OVERLAY');border:SetSize(54,54);border:SetPoint('TOPLEFT',-1,1);border:SetTexture('Interface\\Minimap\\MiniMap-TrackingBorder')
 b:SetScript('OnClick',function() if not self.minimapDragged then self:ShowOptions() end;self.minimapDragged=false end)
 b:SetScript('OnEnter',function()
  if GameTooltip then GameTooltip:SetOwner(b,'ANCHOR_LEFT');GameTooltip:AddLine('PadChat');GameTooltip:AddLine('Click: Options and voice settings',1,1,1);GameTooltip:AddLine('Drag: move around the minimap',1,1,1);GameTooltip:Show() end
 end)
 b:SetScript('OnLeave',function() if GameTooltip then GameTooltip:Hide() end end)
 b:SetScript('OnDragStart',function()
  if InCombatLockdown() then return end
  self.minimapDragged=true
  b:SetScript('OnUpdate',function()
   local mx,my=Minimap:GetCenter();local x,y=GetCursorPosition();local scale=Minimap:GetEffectiveScale()
   if mx and my and scale and scale>0 then self.db.minimapAngle=math.deg(math.atan2(y/scale-my,x/scale-mx));self:PositionMinimapButton() end
  end)
 end)
 b:SetScript('OnDragStop',function() b:SetScript('OnUpdate',nil) end)
 self:PositionMinimapButton();b:Show()
end
