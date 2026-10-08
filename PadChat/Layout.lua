local _,P=...
-- WoW coordinates are logical UI units. Fit without changing the player's UI scale.
function P.PanelFit(width,height,viewWidth,viewHeight)
 local scale=math.min(1,(viewWidth-24)/width,(viewHeight-24)/height)
 return math.max(.15,scale)
end
function P:FitPanel(frame,width,height,bottom)
 local function fit()
  if InCombatLockdown() then return end
  local w=UIParent.GetWidth and UIParent:GetWidth() or 1024
  local h=UIParent.GetHeight and UIParent:GetHeight() or 768
  local scale=P.PanelFit(width,height,w,h)
  if frame.SetScale then frame:SetScale(scale) end
  frame:ClearAllPoints()
  if bottom and height*scale+bottom<=h-12 then frame:SetPoint('BOTTOM',UIParent,'BOTTOM',0,bottom/scale)
  else frame:SetPoint('CENTER',UIParent,'CENTER',0,0) end
 end
 frame.padchatFit=fit;fit()
end
function P:RefitPanels()
 for _,frame in ipairs({self.frame,self.optionsFrame,self.voiceOptionsFrame,self.pttNotice}) do
  if frame and frame.padchatFit then frame.padchatFit() end
 end
end
function P:ControllerHelp()
 if self:ControllerBindingMode()=='sony' then return 'X: select   Square: delete   Triangle: space','L1: channel   R1: words   L2: shift   R2: symbols   Options: send   Circle: close','X: activate   Circle: back' end
 return 'A: select   X: delete   Y: space','LB: channel   RB: words   LT: shift   RT: symbols   Start: send   B: close','A: activate   B: back'
end
