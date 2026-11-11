from pathlib import Path
from reportlab.pdfgen import canvas
from reportlab.lib.pagesizes import A4
from reportlab.lib.colors import HexColor
from reportlab.lib.styles import ParagraphStyle
from reportlab.platypus import Paragraph
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from pypdf import PdfReader

r=Path(__file__).resolve().parent
for name,file in [('UI','segoeui.ttf'),('Bold','segoeuib.ttf')]:pdfmetrics.registerFont(TTFont(name,str(Path('C:/Windows/Fonts')/file)))
path=r/'docs/PadChat-Installation-Guide.pdf';path.parent.mkdir(exist_ok=True)
c=canvas.Canvas(str(path),pagesize=A4);c.setTitle('PadChat Preview 15 - installation and voice startup');c.setAuthor('PadChat')
w,h=A4;ink=HexColor('#183545');gold=HexColor('#B07C2C')
body=ParagraphStyle('body',fontName='UI',fontSize=10.5,leading=16,textColor=ink)
heading=ParagraphStyle('heading',parent=body,fontName='Bold',fontSize=13,leading=18)
release='https://github.com/JEMPAS-FORGE/PadChat/releases/tag/v0.2.2-preview.15'
installer=release.replace('/tag/','/download/')+'/PadChat-Setup.exe'
def page(n,title,sub):
 c.setFillColor(ink);c.rect(0,h-153,w,153,fill=1,stroke=0)
 c.setFillColor(HexColor('#E3B76E'));c.setFont('Bold',12);c.drawString(46,h-35,'PADCHAT  /  PREVIEW 15')
 c.setFillColor(HexColor('#FFFFFF'));c.setFont('Bold',25);c.drawString(46,h-82,title)
 c.setFont('UI',10);c.drawString(46,h-108,sub)
 c.setFillColor(gold);c.rect(46,h-171,46,3,fill=1,stroke=0)
 c.setFont('UI',9);c.setFillColor(ink);c.drawString(46,37,'WoW Forever 1.60.x  |  Windows 10/11 x64  |  Local English speech');c.drawRightString(w-46,37,str(n)+' / 3')
 return h-197
def para(text,y,style=body):
 p=Paragraph(text,style);_,height=p.wrap(w-92,h)
 assert y-height>66,(text,y,height)
 p.drawOn(c,46,y-height);return y-height-11
def section(title,text,y):return para(text,para(title,y,heading))

y=page(1,'Install both parts','Controller keyboard + optional free voice typing')
y=para('For speech, use the Windows installer. CurseForge installs the in-game addon only; it cannot install or launch the companion.',y)
y=section('1. Close WoW and download Setup',f'<link href="{installer}" color="#A16F23"><b>Download PadChat-Setup.exe</b></link> from the <link href="{release}" color="#A16F23">official Preview 15 release</link>. If you download the complete ZIP, choose Extract All first. You do not need Python, ChatGPT, an API key or a paid speech account.',y)
y=section('2. Select the Forever folder','Choose the client folder containing <b>Wow.exe</b>, normally <b>World of Warcraft\\_forever_</b>. Select the client folder, not Interface/AddOns. Setup checks the game version. Version-checked older Forever WowB.exe clients remain supported.',y)
y=section('3. Keep voice and automatic startup enabled','Leave <b>Install voice typing</b> and <b>Start with Windows</b> enabled, then install. On update, existing microphone, PTT and speech preferences are retained. If Windows asks for game-folder permission, use the same Windows account.',y)
y=section('4. Choose your microphone','Select your headset microphone in the companion window, save, and close that window. The models are bundled and speech is processed locally. Start WoW and enable PadChat in the AddOns list.',y)
c.showPage()
y=page(2,'Set up and speak','Use keyboard, mouse or controller')
y=section('Open Guided setup','Click PadChat beside the minimap, then <b>Help &gt; Guided setup</b>. The controller can open the PadChat keyboard with touchpad click or a configured opening button. F9 is optional.',y)
y=section('Choose your controls','Under <b>Voice</b>, enable voice, choose your microphone and select <b>Bind push-to-talk</b>. Press the key or supported mouse button you want. New keyboard users prefer F10 or another unused key shown in Options; existing F8/custom bindings are kept. Controllers use Share/Create/Back or the saved button and modifier.',y)
y=section('Save, reload, then test safely','Save the settings and reload when safe. In Help, run the microphone/PTT test; it records for the test but sends nothing to chat. Close any Windows microphone/binding dialog before trying PTT in game.',y)
y=section('Tap to choose, hold to talk','Tap PTT to cycle available channels. Hold it and wait for green <b>READY - SPEAK NOW</b>. The meter responds to the microphone. Say one message, then release to send, or to review if you selected review-first mode. Check the destination shown in the overlay.',y)
y=section('Find the other options','<b>Controls:</b> opening bindings and Reload UI. <b>Voice:</b> PTT, microphone and send mode. <b>Appearance:</b> overlay size, position and meter. <b>Help:</b> tests, restart and explicit failed-message recovery. D-pad moves between buttons; X/A activates.',y)
c.showPage()
y=page(3,'Startup and recovery','No ChatGPT required to keep voice running')
y=section('When Windows and WoW start','The small startup launcher waits for your selected Forever client, then starts the installed companion. It does not use your microphone while waiting. It closes the speech helper after the game exits and starts it on the next game launch.',y)
y=section('If the companion exits unexpectedly','The launcher retries with a bounded delay. Unfinished dictation is discarded and never replayed automatically. Wait for readiness, release PTT fully, and start a fresh message. This does not fix every possible Windows, device or permission problem.',y)
y=section('If you deliberately exit voice','Choosing <b>Exit PadChat voice</b> in the tray also stops automatic recovery for that Windows session. Open <b>PadChat Voice</b> from Windows Start to resume. If you disabled startup in Task Manager &gt; Startup apps, enable it there or start PadChat manually.',y)
y=section('If you see PC-VOICE-BRIDGE','Close chat/search and binding dialogs and keep WoW selected. After an addon-only update, close WoW and run the <b>matching current Setup</b> with voice enabled. It replaces old managed startup/menu shortcuts. Avoid launching old companion copies. Re-running Setup repairs missing installed files without deleting preferences.',y)
y=para(f'Technical state: <b>%LOCALAPPDATA%\\PadChat\\launcher-status.json</b>. If it says <b>another-companion-running</b>, exit the older companion from its tray menu and open the current Start-menu entry. For help, use <link href="https://github.com/JEMPAS-FORGE/PadChat/issues" color="#A16F23">GitHub issues</link>; share the release and error code, not private chat text.',y)
y=para('Preview status: tested with automated and isolated Windows process checks against Forever 1.60.1.70888 / Interface 16001. The movement fix still awaits post-update live confirmation. Reboot and clean second-PC checks remain outstanding.',y,ParagraphStyle('small',parent=body,fontSize=9,leading=13))
c.save()
pdf=PdfReader(path);assert len(pdf.pages)==3
assert len([a for p in pdf.pages for a in p.get('/Annots',[])])>=3
print(path)
