from pathlib import Path
from reportlab.pdfgen import canvas
from reportlab.lib.colors import HexColor
from reportlab.platypus import Paragraph
from reportlab.lib.styles import ParagraphStyle
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
OUT = Path(__file__).resolve().parent
W, H = 612, 792
PAPER, INK, ORANGE, MUTED = '#EEE9DC', '#202521', '#D94B25', '#535B51'
fonts = Path('C:/Windows/Fonts')
pdfmetrics.registerFont(TTFont('Display', str(fonts/'impact.ttf')))
pdfmetrics.registerFont(TTFont('Body', str(fonts/'calibri.ttf')))
pdfmetrics.registerFont(TTFont('Bold', str(fonts/'calibrib.ttf')))
pdfmetrics.registerFont(TTFont('Mono', str(fonts/'consola.ttf')))
c = canvas.Canvas(str(OUT/'Pocket-Drift-v3.1-Beta-Information-Packet.pdf'), pagesize=(W,H))
c.setTitle('Pocket Drift Overdrive | v3.1 Beta Information Packet')
c.setAuthor('Casper of Branded Creations')

def rect(x,y,w,h,color):
    c.setFillColor(HexColor(color)); c.rect(x,H-y-h,w,h,stroke=0,fill=1)

def text(s,x,y,size=12,font='Body',color=INK):
    c.setFillColor(HexColor(color)); c.setFont(font,size); c.drawString(x,H-y-size,s)

def para(s,x,y,w=516,size=12,color=INK,leading=None):
    st=ParagraphStyle('p',fontName='Body',fontSize=size,leading=leading or size*1.4,textColor=HexColor(color))
    p=Paragraph(s,st); _,height=p.wrap(w,H); p.drawOn(c,x,H-y-height); return y+height

def photo(name,x,y,w,h):
    im=Image.open(ROOT/'website'/'assets'/name)
    iw,ih=im.size; scale=min(w/iw,h/ih); dw,dh=iw*scale,ih*scale
    c.drawImage(str(ROOT/'website'/'assets'/name),x+(w-dw)/2,H-y-h+(h-dh)/2,dw,dh,mask='auto')

def base(n,label):
    rect(0,0,W,H,PAPER); rect(0,0,12,H,ORANGE)
    text('BRANDED CREATIONS / 2026',48,27,9,'Mono')
    text(label,365,27,9,'Mono')
    rect(48,742,516,1,'#B8BBAD')
    text('POCKET DRIFT: OVERDRIVE / v3.1 BETA',48,755,8,'Mono')
    text(f'{n:02d} / 04',517,755,8,'Mono')

base(1,'PUBLIC BETA PREVIEW')
text('POCKET DRIFT',46,69,73,'Display')
text('OVERDRIVE',48,151,48,'Display',ORANGE)
text('MINIATURE MOTORSPORT. WORKSHOP ATTITUDE.',48,218,11,'Mono')
rect(48,253,516,264,INK); photo('circuit.png',48,253,516,264)
text('IN-ENGINE DEVELOPMENT SCENE CAPTURE',48,523,8,'Mono',MUTED)
rect(48,555,516,83,ORANGE)
text('v3.1 BETA / PUBLIC RELEASE',65,567,12,'Bold','#FFFFFF')
text('NOVEMBER 7, 2026',65,587,31,'Display','#FFFFFF')
para('A miniature arcade racing project set on a workshop bench. Meet the world, the cars, and the creator behind the next stage of development.',48,657,516,13)
text('CREATED BY CASPER OF BRANDED CREATIONS, 2026',48,718,9,'Mono')
c.showPage()

base(2,'THE GAME')
text('A BIG WORLD',48,76,51,'Display')
text('ON A SMALL BENCH.',48,132,45,'Display')
para('Pocket Drift: Overdrive brings a miniature racing world to a familiar setting: the workshop. Oversized tools establish the scale, while the circuit brings the atmosphere of a motorsport venue.',48,207,516,15)
rect(48,303,516,1,'#B8BBAD')
text('01',48,325,33,'Display',ORANGE)
text('THE WORKSHOP CIRCUIT',112,328,19,'Bold')
para('Sweeping corners, painted curbs, pit garages, grandstands, and floodlights give the track its identity. Workshop props make the cars feel small without losing the race-day atmosphere.',112,362,452,13)
text('02',48,450,33,'Display',ORANGE)
text('THE DRIVING DIRECTION',112,453,19,'Bold')
para('The project is built around arcade drifting and combat-racing ideas. Driving feel, race flow, collisions, recovery, and performance are priorities for testing and refinement.',112,487,452,13)
text('03',48,575,33,'Display',ORANGE)
text('THE NEXT CHAPTER',112,578,19,'Bold')
para('The v3.1 Beta public release is scheduled for November 7, 2026. This packet introduces the project; supported platforms, controls, known issues, and access instructions should accompany the released build.',112,612,452,13)
c.showPage()

base(3,'THE GARAGE')
text('MEET THE GRID.',48,76,55,'Display')
para('Three original stylized car designs. Three distinct visual personalities.',48,151,516,15)
rect(48,205,516,281,INK); photo('cars.png',48,205,516,281)
text('BLENDER STUDIO RENDER / NOT GAMEPLAY',48,494,8,'Mono',MUTED)
for x,num,name,desc in [(48,'07','SCRAPPER','Red bodywork and a mid-engine GT silhouette.'),(227,'23','VOLT DOZER','Ivory paint with a fastback profile.'),(406,'81','NEON VIPER','Teal paint and a long-hood coupe silhouette.')]:
    rect(x,534,158,2,INK); text(num,x,550,40,'Display',ORANGE)
    text(name,x,601,19,'Display'); para(desc,x,633,151,12)
text('Vehicle handling and balance remain subject to development.',48,711,10,'Body',MUTED)
c.showPage()

base(4,'CREATOR & TESTING')
text('FROM THE CREATOR.',48,76,48,'Display')
text('CASPER',48,144,39,'Display',ORANGE)
text('BRANDED CREATIONS / 2026',48,193,12,'Mono')
para("I'm Casper of Branded Creations, the creator behind Pocket Drift: Overdrive. I'm building a miniature racing world and sharing the process as it develops, from car and circuit design to the changes shaped by player feedback.",48,230,516,14)
rect(48,341,516,92,INK)
text('PUBLIC CONTACT',65,353,10,'Mono','#EEE9DC')
text('404kidzworld',65,373,31,'Display','#EEE9DC')
text('Contact handle / platform link to be added',65,411,9,'Body','#EEE9DC')
text('HELP SHAPE THE NEXT LAPS',48,465,23,'Display')
para('When you try the beta, tell us what feels clear, what feels frustrating, and what you would change first. Useful feedback includes:',48,504,516,13)
for y,s in [(556,'01   Your build version, device, and input method'),(584,'02   The issue and the steps needed to reproduce it'),(612,'03   What you expected and what actually happened')]:
    text(s,48,y,11,'Mono')
rect(48,652,516,2,ORANGE)
text('v3.1 BETA PUBLIC RELEASE / NOVEMBER 7, 2026',48,668,12,'Bold')
para('Download destination to be announced. This packet contains no active download link. Beta software is in development and may change.',48,691,516,10,color=MUTED)
c.save()

import fitz
doc=fitz.open(OUT/'Pocket-Drift-v3.1-Beta-Information-Packet.pdf')
thumbs=[]
for i,page in enumerate(doc):
    pix=page.get_pixmap(matrix=fitz.Matrix(1,1)); file=OUT/f'preview-{i+1}.png'; pix.save(str(file))
    thumbs.append(Image.open(file).convert('RGB'))
    assert page.get_text().strip(), f'Empty page {i+1}'
sheet=Image.new('RGB',(W*2,H*2),'white')
for i,im in enumerate(thumbs): sheet.paste(im,((i%2)*W,(i//2)*H))
sheet.save(OUT/'packet-overview.png')
assert len(doc)==4
print('Created four-page PDF and page previews.')
