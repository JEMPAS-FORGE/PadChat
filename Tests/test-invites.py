from pathlib import Path
import sys
root=Path(__file__).resolve().parent.parent
from lupa.lua51 import LuaRuntime
l=LuaRuntime(unpack_returned_tuples=True)
l.execute('''
CK={}; function GetLocale() return 'enUS' end
function wipe(t) for k in pairs(t) do t[k]=nil end end
''')
for name in ('Core.lua','Model.lua','Options.lua','UI.lua','VoicePTT.lua','Invites.lua'):
    l.execute((root/'PadChat'/name).read_text(encoding='utf-8'),'ControllerKeyboard',l.globals().CK)
l.execute('''
CK:InitDB();CK.standalone=true
function CK:Refresh() end
function CK:Print(fmt,...) self.notice=string.format(fmt,...) end
function CK:VoiceChannelNotice(msg) self.notice=msg end
function CK:KnownNames() return {'Recent Sender','Ragnek Themad'} end
function UnitName(unit) if unit=='player' then return 'Self Player' end end
function UnitIsPlayer() return false end
function UnitFactionGroup() return 'Horde' end
function UnitGUID(unit) if unit=='player' then return 'self' end end
function RegionalUniqueNamesEnabled() return true end
combat=false;function InCombatLockdown() return combat end
WOW_PROJECT_ID=99;WOW_PROJECT_CLASSIC=2;BNET_CLIENT_WOW='WoW'
local friendNames={{name='Online Friend',connected=true},{name='Offline Friend',connected=false}}
C_FriendList={GetNumFriends=function() return #friendNames end,GetFriendInfoByIndex=function(i) return friendNames[i] end}
sent={};C_PartyInfo={CanFormCrossFactionParties=function() return false end,
 IsPartyFull=function() return false end,InviteUnit=function(name) sent[#sent+1]='character:'..name end}
local function game(id,name)
 return {gameAccountID=id,characterName=name,isOnline=true,clientProgram='WoW',wowProjectID=99,
 isInCurrentRegion=true,playerGuid='guid'..id,realmID=4,factionName='Horde'}
end
accounts={
 {bnetAccountID=10,battleTag='BigDog#123',games={game(101,'Ragnek Themad')}},
 {bnetAccountID=11,battleTag='OfflinePal#456',games={}},
 {bnetAccountID=12,battleTag='OtherGame#123',games={{gameAccountID=103,isOnline=true,clientProgram='D3'}}},
 {bnetAccountID=13,battleTag='Multi#100',games={game(104,'First Alt'),game(105,'Second Alt')}},
 {bnetAccountID=14,battleTag='BigDog#999',games={game(106,'Other Player')}},
}
function BNGetNumFriends() return #accounts end
C_BattleNet={GetFriendAccountInfo=function(i) return accounts[i] end,
 GetFriendNumGameAccounts=function(i) return #accounts[i].games end,
 GetFriendGameAccountInfo=function(i,j) return accounts[i].games[j] end,
 InviteFriend=function(id) sent[#sent+1]='bn:'..id end}
local names=CK:QueryInviteNames('')
assert(#names==10)
assert(CK.inviteLabels['Offline Friend']:find('Offline'))
assert(CK.inviteLabels['b11g0']:find('Offline'))
assert(CK.inviteLabels['b12g103']:find('another game'))
assert(#CK:QueryInviteNames('Bad\\n/quit')==0)
assert(#CK:QueryInviteNames('Ragnek')==3)
assert(CK:QueryInviteNames('BigDog#123')[1]=='b10g101')
assert(CK:ConfirmInviteTarget('b10g101'))
assert(CK:BuildMacroText()=='/ck invitekey b10g101')
assert(#sent==0) -- selecting never invites
assert(not CK:ConfirmInviteTarget('b11g0'))
assert(not CK:ConfirmInviteTarget('Offline Friend'))
assert(not CK:ConfirmInviteTarget('b999g999'))
assert(CK:ConfirmInviteTarget('Online Friend'))
assert(CK:BuildMacroText()=='/invite Online-Friend')
assert(#sent==0)
assert(CK:InviteByVoice('Ragnek Themad.'));assert(sent[#sent]=='bn:101')
assert(CK:InviteByVoice('BigDog#123'));assert(sent[#sent]=='bn:101')
local count=#sent
assert(not CK:InviteByVoice('BigDog'));assert(#sent==count and CK.notice:find('More than one'))
assert(not CK:InviteByVoice('Multi'));assert(#sent==count)
assert(not CK:InviteByVoice('Nobody'));assert(#sent==count)
assert(not CK:InviteByVoice('OfflinePal'));assert(#sent==count)
assert(not CK:InviteByVoice('OtherGame'));assert(#sent==count)
assert(not CK:InviteByVoice('BigDog/quit'));assert(#sent==count)
assert(CK:InviteByVoice('Online Friend'));assert(sent[#sent]=='character:Online-Friend')
assert(CK:InviteByVoice('Second Alt'));assert(sent[#sent]=='bn:105')
-- Friend order changes: invite uses stable account + game ID, never an index.
accounts[1],accounts[5]=accounts[5],accounts[1]
assert(CK:InviteByKey('b10g101'));assert(sent[#sent]=='bn:101')
count=#sent;accounts[5].games[1].isOnline=false
assert(not CK:InviteByKey('b10g101'));assert(#sent==count)
combat=true;assert(not CK:InviteByVoice('Second Alt'));assert(#sent==count)
combat=false;accounts[4].games[2].wowProjectID=1
assert(not CK:InviteByVoice('Second Alt'));assert(#sent==count)
print('Passed all-friends roster, offline/other-game labels, exact voice resolution, duplicate and multi-account refusal, selection without sending, stable IDs, stale-state and combat checks.')
''')
