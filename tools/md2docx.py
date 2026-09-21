# -*- coding: utf-8 -*-
"""Inserta manual-tecnico.md dentro del docx de biografias, sustituyendo
la seccion FUNDAMENTOS DEL SISTEMA DE COMBATE."""
import re, copy, sys
import docx
from docx.shared import Pt, RGBColor, Inches
from docx.oxml.ns import qn
from docx.oxml import OxmlElement
from docx.enum.text import WD_ALIGN_PARAGRAPH

import os
_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DOCX = os.path.join(_ROOT, "BIOGRAFIAS 8de10 - TRABAJO.docx")
MD   = os.path.join(_ROOT, "docs", "manual-tecnico.md")

doc = docx.Document(DOCX)
body = doc.element.body

def ptext(el):
    return "".join(n.text or "" for n in el.iter(qn('w:t')))

kids = list(body)
i_start = i_end = None
for idx, el in enumerate(kids):
    if el.tag == qn('w:p'):
        t = ptext(el).strip()
        if t == "FUNDAMENTOS DEL SISTEMA DE COMBATE" and i_start is None:
            i_start = idx
        elif t.startswith("LISTA COMPLETA DE ARMAS") and i_start is not None and i_end is None:
            i_end = idx
assert i_start is not None and i_end is not None, "anclas no encontradas"

anchor_h1 = kids[i_start]          # se conserva: ancla del indice
to_delete = kids[i_start+1:i_end]  # todo lo de dentro se sustituye
for el in to_delete:
    body.remove(el)

# ---------- utilidades de formato ----------

def set_borders(tbl):
    tblPr = tbl._tbl.tblPr
    borders = OxmlElement('w:tblBorders')
    for edge in ('top','left','bottom','right','insideH','insideV'):
        e = OxmlElement('w:' + edge)
        e.set(qn('w:val'), 'single')
        e.set(qn('w:sz'), '4')
        e.set(qn('w:color'), '999999')
        borders.append(e)
    tblPr.append(borders)

def shade(cell, hexcolor):
    tcPr = cell._tc.get_or_add_tcPr()
    sh = OxmlElement('w:shd')
    sh.set(qn('w:val'), 'clear')
    sh.set(qn('w:fill'), hexcolor)
    tcPr.append(sh)

def para_shade(p, hexcolor):
    pPr = p._p.get_or_add_pPr()
    sh = OxmlElement('w:shd')
    sh.set(qn('w:val'), 'clear')
    sh.set(qn('w:fill'), hexcolor)
    pPr.append(sh)

def left_bar(p):
    pPr = p._p.get_or_add_pPr()
    pbdr = OxmlElement('w:pBdr')
    lb = OxmlElement('w:left')
    lb.set(qn('w:val'), 'single'); lb.set(qn('w:sz'), '18')
    lb.set(qn('w:space'), '8'); lb.set(qn('w:color'), 'A0A0A0')
    pbdr.append(lb)
    pPr.append(pbdr)

INLINE = re.compile(r'(\*\*.+?\*\*|`[^`]+`|\*[^*]+\*)')

def add_runs(p, text, base_size=None, mono=False):
    for tok in INLINE.split(text):
        if not tok:
            continue
        if tok.startswith('**') and tok.endswith('**') and len(tok) > 4:
            r = p.add_run(tok[2:-2]); r.bold = True
        elif tok.startswith('`') and tok.endswith('`') and len(tok) > 2:
            r = p.add_run(tok[1:-1]); r.font.name = 'Consolas'
            r.font.size = Pt(9)
            r._element.rPr.rFonts.set(qn('w:cs'), 'Consolas')
        elif tok.startswith('*') and tok.endswith('*') and len(tok) > 2:
            r = p.add_run(tok[1:-1]); r.italic = True
        else:
            r = p.add_run(tok)
        if mono:
            r.font.name = 'Consolas'
        if base_size:
            r.font.size = Pt(base_size)
    return p

def new_para(style=None):
    p = doc.add_paragraph(style=style) if style else doc.add_paragraph()
    return p

# ---------- parser markdown ----------

lines = open(MD, encoding='utf-8').read().split('\n')
i = 0
made = []           # elementos creados, en orden
skipped_title = False

def register(p_or_t):
    made.append(p_or_t)

while i < len(lines):
    ln = lines[i]
    s = ln.strip()

    # titulo de documento -> se omite (ya existe el H1 de seccion)
    if s.startswith('# ') and not skipped_title:
        skipped_title = True
        i += 1
        continue

    # regla horizontal
    if s == '---':
        i += 1
        continue

    # bloque de codigo / diagrama
    if s.startswith('```'):
        i += 1
        buf = []
        while i < len(lines) and not lines[i].strip().startswith('```'):
            buf.append(lines[i].rstrip('\n'))
            i += 1
        i += 1
        while buf and not buf[0].strip(): buf.pop(0)
        while buf and not buf[-1].strip(): buf.pop()
        p = new_para()
        pf = p.paragraph_format
        pf.space_before = Pt(6); pf.space_after = Pt(6)
        pf.left_indent = Inches(0.15)
        para_shade(p, 'F2F2F2')
        for k, bl in enumerate(buf):
            r = p.add_run(bl)
            r.font.name = 'Consolas'; r.font.size = Pt(8)
            r._element.rPr.rFonts.set(qn('w:cs'), 'Consolas')
            t = r._element.find(qn('w:t'))
            if t is not None:
                t.set(qn('xml:space'), 'preserve')
            if k < len(buf) - 1:
                r.add_break()
        register(p)
        continue

    # tabla
    if s.startswith('|') and i + 1 < len(lines) and re.match(r'^\|[\s:\-|]+\|$', lines[i+1].strip()):
        header = [c.strip() for c in s.strip('|').split('|')]
        i += 2
        rows = []
        while i < len(lines) and lines[i].strip().startswith('|'):
            rows.append([c.strip() for c in lines[i].strip().strip('|').split('|')])
            i += 1
        ncols = len(header)
        has_header = any(h.strip() for h in header)
        tbl = doc.add_table(rows=(1 if has_header else 0), cols=ncols)
        set_borders(tbl)
        tbl.autofit = True
        for c, htxt in enumerate(header if has_header else []):
            cell = tbl.rows[0].cells[c]
            cell.text = ''
            p = cell.paragraphs[0]
            p.paragraph_format.space_after = Pt(2)
            add_runs(p, htxt if htxt else ' ', base_size=9)
            for r in p.runs: r.bold = True
            shade(cell, 'E8E8E8')
        for rowdata in rows:
            cells = tbl.add_row().cells
            for c in range(ncols):
                val = rowdata[c] if c < len(rowdata) else ''
                cell = cells[c]
                cell.text = ''
                p = cell.paragraphs[0]
                p.paragraph_format.space_after = Pt(2)
                add_runs(p, val if val else ' ', base_size=9)
        register(tbl)
        sp = new_para(); sp.paragraph_format.space_after = Pt(4)
        register(sp)
        continue

    # encabezados  (# -> H2, ## -> H3, ### -> H4, #### -> H5)
    m = re.match(r'^(#{1,6})\s+(.*)$', s)
    if m:
        lvl = len(m.group(1))
        style = {1: 'Heading 2', 2: 'Heading 3', 3: 'Heading 4',
                 4: 'Heading 5', 5: 'Heading 6', 6: 'Heading 6'}[lvl]
        p = new_para(style=style)
        add_runs(p, m.group(2))
        register(p)
        i += 1
        continue

    # cita / nota destacada
    if s.startswith('>'):
        buf = []
        while i < len(lines) and lines[i].strip().startswith('>'):
            buf.append(re.sub(r'^\s*>\s?', '', lines[i]))
            i += 1
        for bl in buf:
            bs = bl.strip()
            if not bs:
                continue
            hm = re.match(r'^(#{1,6})\s+(.*)$', bs)
            p = new_para()
            pf = p.paragraph_format
            pf.left_indent = Inches(0.25)
            pf.space_before = Pt(2); pf.space_after = Pt(2)
            left_bar(p)
            if hm:
                add_runs(p, hm.group(2), base_size=10)
                for r in p.runs: r.bold = True
            else:
                bs2 = re.sub(r'^[-*]\s+', '\u2022 ', bs)
                add_runs(p, bs2, base_size=10)
                for r in p.runs:
                    if not r.bold: r.italic = True
            register(p)
        continue

    # listas
    if re.match(r'^[-*]\s+', s):
        p = new_para(style='List Bullet')
        p.paragraph_format.space_after = Pt(2)
        add_runs(p, re.sub(r'^[-*]\s+', '', s))
        register(p)
        i += 1
        continue
    if re.match(r'^\d+\.\s+', s):
        p = new_para(style='List Paragraph')
        p.paragraph_format.space_after = Pt(2)
        add_runs(p, s)
        register(p)
        i += 1
        continue

    # linea vacia
    if not s:
        i += 1
        continue

    # parrafo normal
    p = new_para()
    p.paragraph_format.space_after = Pt(4)
    add_runs(p, s)
    register(p)
    i += 1

# ---------- mover lo creado al lugar correcto ----------
prev = anchor_h1
for obj in made:
    el = obj._tbl if hasattr(obj, '_tbl') else obj._p
    body.remove(el)
    prev.addnext(el)
    prev = el

doc.save(DOCX)
print("OK")
print("elementos insertados:", len(made))
