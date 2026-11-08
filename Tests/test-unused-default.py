from pathlib import Path
import runpy
root=Path(__file__).resolve().parent
l=runpy.run_path(str(root/'test-runtime.py'))['l']
l.execute(r'''
local prior=PadChatDB;local action=GetBindingAction
local base,overrides={},{}
function GetBindingAction(key,useOverrides) return (useOverrides and overrides[key]) or base[key] or '' end
local function fresh(open)
 PadChatDB={bindings={keyboard=open or 'F9',button='AUTO',modifier='NONE'}};P:InitDB();return P.db.voiceOptions
end
local s=fresh();assert(s.keyName=='F10' and s.key==121 and s.enabled)
base.F10='TOGGLEBAG1';P:ApplyVoiceBindings();assert(P.db.voiceOptions.keyName=='F7')
s=fresh();assert(s.keyName=='F7' and s.enabled and base.F10=='TOGGLEBAG1')
overrides.F7='CLICK AnotherAddon:LeftButton';s=fresh('F6');assert(s.keyName=='F11')
for _,k in ipairs({'F10','F7','F6','F11','F12'}) do base[k]='GAME_ACTION' end
s=fresh();assert(not s.enabled);P:ApplyVoiceBindings();assert(not bindings.F10 or bindings.F10.name~='PadChatVoiceKey')
function GetBindingAction() error('binding API unavailable') end
s=fresh();assert(not s.enabled)
GetBindingAction=action
local chosen=fresh();chosen.kind='mouse';chosen.key=5;chosen.keyName='BUTTON4';assert(P:SaveVoiceOptions(chosen));assert(not P.db.voiceOptions.autoDefault);P:ApplyVoiceBindings();assert(P.db.voiceOptions.keyName=='BUTTON4')
-- An existing saved binding, even old F8, is a player choice, never migrated.
for _,v in ipairs({{kind='mouse',key=5,keyName='BUTTON4',mods=0},{kind='keyboard',key=119,keyName='F8',mods=0},{kind='keyboard',key=118,keyName='F7',mods=1}}) do
 v.mic='My saved headset';v.enabled=true;v.startup='OFF';v.pad='SHARE';v.padMod='NONE'
 PadChatDB={bindings={keyboard='F9',button='AUTO',modifier='NONE'},voiceOptions=v};P:InitDB()
 assert(P.db.voiceOptions==v and v.mic=='My saved headset' and v.startup=='OFF')
end
PadChatDB=prior;P:InitDB()
''')
print('PASS: fresh F10 preference, native/override/open-key collision fallback, all-bound/API-error refusal, and saved F8/mouse/modifier preservation. No real game bindings changed.')
