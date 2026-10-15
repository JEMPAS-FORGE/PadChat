from pathlib import Path
import runpy
root=Path(__file__).resolve().parent
l=runpy.run_path(str(root/'test-runtime.py'))['l']
l.execute(r'''
local saved=PadChatDB
for _,bad in ipairs({'broken',123,true}) do PadChatDB=bad;P:InitDB();assert(type(P.db)=='table' and P:GetText()=='') end
PadChatDB={draft=123,words='bad',bindings={keyboard='F6',button='NONE',modifier='NONE'}}
P:InitDB();assert(P:GetText()=='' and P.db.bindings.keyboard=='F6');P.model:Rows(P.db.words)
PadChatDB={draft='keep valid draft',words={good=3,invalid='bad',negative=-4,nan=0/0,infinite=math.huge,[123]=4}}
P:InitDB();assert(P:GetText()=='keep valid draft' and P.db.words.good==3 and P.db.words.invalid==nil)
P.model:Rows(P.db.words)
for i=1,700 do P.db.words['word'..i]=i end
P:InitDB();local count=0;for _ in pairs(P.db.words) do count=count+1 end;assert(count==500)
PadChatDB=saved;P:InitDB();focus=nil;combat=false;P:Close()
assert(P:SaveOpeningBindings('F9','NONE','NONE'))
local v=P.db.voiceOptions;v.kind='keyboard';v.key=117;v.keyName='F6';v.mods=0;v.pad='SHARE';v.padMod='NONE'
assert(P:SaveVoiceOptions(v))
local old=P.db.bindings
assert(not P:SaveOpeningBindings('F6','NONE','NONE') and P.db.bindings==old)
assert(not P:SaveOpeningBindings('F9','PADSOCIAL','PADLTRIGGER') and P.db.bindings==old)
assert(P:SaveOpeningBindings('F9','PADLSTICK','PADLTRIGGER'))
v.pad='LSTICK';assert(not P:SaveVoiceOptions(v));v.pad='SHARE'
assert(P:SaveOpeningBindings('F9','AUTO','NONE'))
''')
scope=runpy.run_path(str(root/'test-direct-addon.py'));l=scope['l']
for point in ('prepared','staged','released','expired'):
 scope['prepare']()
 if point!='prepared':scope['stage']('/s','discard this')
 if point=='released':l.execute('P:PTTReleaseFocus()')
 if point=='expired':l.execute('clock=50')
 before=l.eval('#sent')
 l.execute("widgets.PadChatPTTAbort.scripts.OnClick();assert(not P.directVoiceToken and not P.directVoicePending)")
 for _ in range(3):
  l.execute("P:PrepareVoiceProbe();assert(P.pttBox:GetText():find('^PCVQ1;'));P:ReadVoiceStatus('PCVS1;'..P.voiceProbeToken..';ready;0;')")
 assert l.eval('#sent')==before
 # A new probe also supersedes an abandoned token if the abort input was lost.
 scope['prepare']();scope['stage']('/s','never replay')
 l.execute("P:PrepareVoiceProbe();assert(not P.directVoiceToken and not P.directVoicePending and P.pttBox:GetText():find('^PCVQ1;'))")
 assert l.eval('#sent')==before
 l.execute('P:PTTReleaseFocus()')
# The intentional release between stage and commit still sends exactly once.
scope['prepare']();scope['stage']('/s','normal release');before=l.eval('#sent');scope['commit']();assert l.eval('#sent')==before+1
print('PASS: corrupted SavedVariables, bounded learned words, symmetric opening/PTT conflicts, explicit abort/fresh-probe recovery and preserved release-before-commit.')
