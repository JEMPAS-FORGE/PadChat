from pathlib import Path
import sys
root=Path(__file__).resolve().parent.parent
from lupa.lua51 import LuaRuntime
lua=LuaRuntime(unpack_returned_tuples=True)
lua.execute('P={}; function GetTime() return 1 end')
lua.execute((root/'PadChat/Model.lua').read_text(), 'PadChat',lua.globals().P)
lua.execute('''
local m=P:NewModel('');m:Rows({})
assert(m.row==2 and m.col==5)
m:Move(100,0);assert(m.col==10)
m:Move(0,1);assert(m.row==3 and m.col==9)
m:Move(0,1);assert(m.row==4 and m.col==7)
m:Move(0,100);assert(m.row==5 and m.col==7)
m:Move(-100,-100);assert(m.row==1 and m.col==1)
m.row=2;m.col=1;m:Select(0);assert(m.text=='q')
-- Drift after selecting cannot move the key until the stick centres.
assert(not m:Stick(.9,.05,.3) and m.col==1)
m:Stick(0,0,.4);assert(m:Stick(.9,.05,.5) and m.col==2)
assert(not m:Stick(.9,.05,.6) and m.col==2)
assert(m:Stick(.9,.05,1.0) and m.col==3)
m:Stick(0,0,1.1);assert(m.col==3)
assert(not m:Stick(.59,.1,1.2));assert(not m:Stick(0/0,.1,1.3))
m.shift=true;m:Rows({});m.row=2;m.col=1;m:Select(2);assert(m.text=='qQ' and not m.shift)
m.text='th';m:Rows({});m.row=1;m.col=1;assert(m.rows[1][1].word=='thanks');m:Select(3);assert(m.text=='thanks ')
m.text='';m.numbers=true;m:Rows({});m.row=2;m.col=1;m:Select(4);assert(m.text=='1')
m.text='caf'..string.char(195,169);m:Delete();assert(m.text=='caf')
m.text=string.rep('a',220);assert(not m:Insert('x') and #m.text==220)
m.text='';m:Rows({customword=50});assert(m.rows[1][1].word=='customword')
''')
print('Passed deterministic navigation, stick hysteresis/repeat, confirmation drift lock, shift/symbols, suggestions, Unicode backspace and length limit.')
# Compile every source with Lua 5.1 (the game's dialect).
for path in (root/'PadChat').glob('*.lua'):
    lua.execute('assert(loadstring(...))',path.read_text())
print('All PadChat Lua sources parse under Lua 5.1.')
