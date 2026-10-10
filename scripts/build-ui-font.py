"""Adapt the existing desktop bitmap atlas to a local native font (no font substitution).
Run with .local/font-tools/bin/python. Generated asset stays under .local.
"""
from pathlib import Path
import xml.etree.ElementTree as ET
from PIL import Image
from fontTools.fontBuilder import FontBuilder
from fontTools.pens.ttGlyphPen import TTGlyphPen
root = Path(__file__).resolve().parents[1]
source = root / 'engine/apple/SourceAssets/BitmapFonts'
font = ET.parse(source / 'sans_11px.fnt').getroot()
atlas = Image.open(source / 'sans_11px_0.png').convert('RGBA')
scale = 64
base = int(font.find('common').get('base'))
chars = [{k:int(v) for k,v in c.attrib.items()} for c in font.find('chars')]
chars = [c for c in chars if c['id'] >= 0]
order = ['.notdef'] + [f'uni{c["id"]:04X}' for c in chars]
glyphs = {'.notdef': TTGlyphPen(None).glyph()}
metrics = {'.notdef': (6*scale,0)}
for c, name in zip(chars,order[1:]):
    pen = TTGlyphPen(None)
    for y in range(c['height']):
        x=0
        while x < c['width']:
            if atlas.getpixel((c['x']+x,c['y']+y))[3] < 128:
                x+=1; continue
            start=x
            while x < c['width'] and atlas.getpixel((c['x']+x,c['y']+y))[3] >=128: x+=1
            left=(start+c['xoffset'])*scale; right=(x+c['xoffset'])*scale
            top=(base-y-c['yoffset'])*scale; bottom=top-scale
            pen.moveTo((left,bottom)); pen.lineTo((left,top)); pen.lineTo((right,top)); pen.lineTo((right,bottom)); pen.closePath()
    glyphs[name]=pen.glyph(); metrics[name]=(c['xadvance']*scale,c['xoffset']*scale)
# UIKit secure fields use masking characters absent from the legacy atlas.
# Supply small native pixel symbols so passwords never appear visually empty.
cmap={c['id']:name for c,name in zip(chars,order[1:])}
for codepoint, rows in {
    0x2022: ["01110","11111","11111","11111","01110"],
    0x25CF: ["01110","11111","11111","11111","01110"],
    0x2610: ["1111111","1000001","1000001","1000001","1000001","1000001","1111111"],
    0x2611: ["1111111","1000001","1000011","1100111","1111101","1011001","1111111"],
}.items():
    if codepoint in cmap: continue
    name=f'uni{codepoint:04X}'; pen=TTGlyphPen(None)
    for y,row in enumerate(rows):
        for x,value in enumerate(row):
            if value!='1': continue
            left=(x+1)*scale; bottom=(8-y)*scale
            pen.moveTo((left,bottom)); pen.lineTo((left,bottom+scale)); pen.lineTo((left+scale,bottom+scale)); pen.lineTo((left+scale,bottom)); pen.closePath()
    order.append(name); cmap[codepoint]=name; glyphs[name]=pen.glyph(); metrics[name]=((len(rows[0])+2)*scale,scale)
builder=FontBuilder(11*scale,isTTF=True)
builder.setupGlyphOrder(order)
builder.setupCharacterMap(cmap)
builder.setupGlyf(glyphs); builder.setupHorizontalMetrics(metrics)
builder.setupHorizontalHeader(ascent=11*scale,descent=-2*scale)
builder.setupNameTable({'familyName':'EO Desktop Pixel','styleName':'Regular','uniqueFontIdentifier':'EODesktopPixel-Regular-1','fullName':'EO Desktop Pixel Regular','psName':'EODesktopPixel-Regular','version':'Version 1.0'})
builder.setupOS2(sTypoAscender=11*scale,sTypoDescender=-2*scale,usWinAscent=11*scale,usWinDescent=2*scale)
builder.setupPost(); builder.setupMaxp()
out=root/'.local/ui-fonts/EODesktopPixel.ttf'; out.parent.mkdir(exist_ok=True)
builder.save(out)
print(f'Built {len(chars)} desktop glyphs plus {len(cmap)-len(chars)} native UI symbols')
