from pathlib import Path
import runpy

root=Path(__file__).resolve().parent.parent
runtime=runpy.run_path(str(root/'Tests/test-runtime.py'))['l']
runtime.execute('''
function GetBindingAction(key) return key=='F6' and 'TARGETNEARESTFRIEND' or '' end
function SetCVar() error('Options must not alter global gamepad modifiers') end
P:SetText('retained draft')
assert(P:SaveOpeningBindings('F6','PADSOCIAL','PADLTRIGGER'))
assert(bindings.F6.name=='PadChatToggle' and not bindings.F9)
assert(bindings.PADSOCIAL.name=='PadChatShare') -- reserved; polling supplies the chord
assert(P:BindingWarning():find('TARGETNEARESTFRIEND'))
assert(P:BindingWarning():find('optional voice companion'))
assert(not P:SaveOpeningBindings('F8','PADBACK','NONE')) -- reserved voice shortcut
assert(not P:SaveOpeningBindings('CTRL-SHIFT-F9','PADBACK','NONE')) -- protocol key
assert(not P:SaveOpeningBindings('F6','BOGUS','NONE'))
combat=true;assert(not P:SaveOpeningBindings('F9','AUTO','NONE'));combat=false
assert(P.db.bindings.keyboard=='F6' and P:GetText()=='retained draft')
local state={buttons={}};local id=1
C_GamePad.ButtonBindingToIndex=function(binding) return ({PADSOCIAL=13,PADLTRIGGER=8})[binding] end
C_GamePad.GetDeviceMappedState=function() return state end
C_GamePad.GetActiveDeviceID=function() return id end
P:PollOpeningChord(.04) -- initialise with released buttons
state.buttons[9]=true;P:PollOpeningChord(.04);assert(not P.open) -- L2 alone
state.buttons[14]=true;P:PollOpeningChord(.04);assert(P.open) -- then Share
P.buttons.PADLTRIGGER.scripts.OnClick(P.buttons.PADLTRIGGER,'LeftButton',false)
assert(not P.model.shift) -- releasing the opening modifier must not toggle Shift
P:PollOpeningChord(.2);assert(P.open) -- no repeats while held
state.buttons[14]=false;P:PollOpeningChord(.04)
state.buttons[14]=true;P:PollOpeningChord(.04);assert(not P.open and P:GetText()=='retained draft')
state.buttons={};P:PollOpeningChord(.04)
state.buttons[14]=true;P:PollOpeningChord(.04);assert(not P.open) -- Share first
state.buttons[9]=true;P:PollOpeningChord(.04);assert(not P.open) -- pressing L2 later is not a new click
state.buttons={};P:PollOpeningChord(.04)
combat=true;state.buttons[9]=true;state.buttons[14]=true;P:PollOpeningChord(.04);assert(not P.open)
combat=false;P:PollOpeningChord(.04);assert(not P.open) -- cannot open from held combat input
state.buttons={};P:PollOpeningChord(.04)
focus={};state.buttons[9]=true;state.buttons[14]=true;P:PollOpeningChord(.04);assert(not P.open)
focus=nil;P:PollOpeningChord(.04);assert(not P.open) -- text focus remains untouched
id=2;P:PollOpeningChord(.04);assert(not P.open) -- reconnect with buttons held
state.buttons={};P:PollOpeningChord(.04)
state.buttons[9]=true;state.buttons[14]=true;P:PollOpeningChord(.04);assert(P.open)
P:ShowOptions();assert(not P.open and P.optionsFrame:IsShown() and P:GetText()=='retained draft')
assert(bindings.PAD1.name=='PadChatOptionActionPAD1' and bindings.PAD2.name=='PadChatOptionActionPAD2')
local original=P.pendingOptions.keyboard
P.optionActions.PADDRIGHT.scripts.OnClick();assert(P.pendingOptions.keyboard~=original)
P.optionActions.PADDLEFT.scripts.OnClick();assert(P.pendingOptions.keyboard==original)
P.optionActions.PADDDOWN.scripts.OnClick();assert(P.optionIndex==2)
P.optionActions.PAD1.scripts.OnClick();assert(P.pendingOptions.button~='PADSOCIAL')
P.optionActions.PAD2.scripts.OnClick();assert(not P.optionsFrame:IsShown() and not bindings.PAD1 and not bindings.PAD2)
assert(P.db.bindings.keyboard=='F6' and P.db.bindings.button=='PADSOCIAL') -- dismissed changes do not save
P:ShowOptions();P.optionIndex=4;P:OptionsAction('activate')
assert(P.db.bindings.keyboard=='F6') -- save entry reachable
P.optionsFrame:Hide();assert(not bindings.PAD1)
P:Open();P.model.row=5;P.model.col=6;P:Act('select');assert(P.optionsFrame:IsShown() and not P.open)
P:OptionsAction('close');P:Open();P.model.row=5;P.model.col=7;P:Act('select');assert(not P.open and not bindings.PAD1)
P:ShowOptions();combat=true;P:OptionsAction('activate');assert(P.optionsFrame:IsShown());combat=false
P.optionsFrame:Hide();assert(P.pendingOptions==nil)
P:ShowOptions();PadChat_Toggle();assert(not P.optionsFrame:IsShown())
local saved=PadChatDB
P:InitDB();assert(P.db.bindings.keyboard=='F6' and P:GetText()=='retained draft')
assert(P:SaveOpeningBindings('F9','AUTO','NONE'))
assert(bindings.F9.name=='PadChatToggle' and not bindings.F6)
assert(P:GetText()=='retained draft')
PadChatDB={bindings={keyboard='F8',button='BROKEN',modifier='BROKEN'},draft='original'}
P:InitDB();assert(P.db.bindings.keyboard=='F9' and P.db.bindings.button=='AUTO' and P:GetText()=='original')
PadChatDB=saved
''')
print('Passed saved options, conflict notices, reserved keys, modifier-first chords, hold/release, combat, text focus, reconnects, options dismissal, defaults and corrupted settings recovery.')
