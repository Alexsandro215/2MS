# -*- coding: utf-8 -*-
"""Reconstruye BIOGRAFIAS - TRABAJO.docx desde el original,
insertando los documentos de diseno actualizados."""
import re, shutil, sys
import docx
from docx.shared import Pt, Inches
from docx.oxml.ns import qn
from docx.oxml import OxmlElement

SRC = r"E:\TMS\BIOGRAFIAS 8de10.docx"
DST = r"E:\TMS\BIOGRAFIAS 8de10 - TRABAJO.docx"
D   = r"E:\TMS\docs"

shutil.copyfile(SRC, DST)
doc = docx.Document(DST)
body = doc.element.body

def ptext(el):
    return "".join(n.text or "" for n in el.iter(qn('w:t')))

def find(pred, start=0):
    kids = list(body)
    for i in range(start, len(kids)):
        el = kids[i]
        if el.tag == qn('w:p') and pred(ptext(el).strip()):
            return i
    return None

# ---------- formato ----------
def set_borders(tbl):
    b = OxmlElement('w:tblBorders')
    for e in ('top','left','bottom','right','insideH','insideV'):
        x = OxmlElement('w:'+e)
        x.set(qn('w:val'),'single'); x.set(qn('w:sz'),'4'); x.set(qn('w:color'),'999999')
        b.append(x)
    tbl._tbl.tblPr.append(b)

def shade_cell(c, col):
    sh = OxmlElement('w:shd'); sh.set(qn('w:val'),'clear'); sh.set(qn('w:fill'),col)
    c._tc.get_or_add_tcPr().append(sh)

def shade_par(p, col):
    sh = OxmlElement('w:shd'); sh.set(qn('w:val'),'clear'); sh.set(qn('w:fill'),col)
    p._p.get_or_add_pPr().append(sh)

def left_bar(p):
    pbdr = OxmlElement('w:pBdr'); lb = OxmlElement('w:left')
    lb.set(qn('w:val'),'single'); lb.set(qn('w:sz'),'18')
    lb.set(qn('w:space'),'8'); lb.set(qn('w:color'),'A0A0A0')
    pbdr.append(lb); p._p.get_or_add_pPr().append(pbdr)

INLINE = re.compile(r'(\*\*.+?\*\*|`[^`]+`|\*[^*]+\*|~~.+?~~)')

def add_runs(p, text, size=None):
    for tok in INLINE.split(text):
        if not tok: continue
        if tok.startswith('**') and tok.endswith('**') and len(tok)>4:
            r = p.add_run(tok[2:-2]); r.bold = True
        elif tok.startswith('~~') and tok.endswith('~~') and len(tok)>4:
            r = p.add_run(tok[2:-2]); r.font.strike = True
        elif tok.startswith('`') and tok.endswith('`') and len(tok)>2:
            r = p.add_run(tok[1:-1]); r.font.name='Consolas'; r.font.size=Pt(9)
            r._element.rPr.rFonts.set(qn('w:cs'),'Consolas')
        elif tok.startswith('*') and tok.endswith('*') and len(tok)>2:
            r = p.add_run(tok[1:-1]); r.italic = True
        else:
            r = p.add_run(tok)
        if size: r.font.size = Pt(size)

# ---------- conversor markdown -> elementos ----------
def convert(md_path, base_level, skip_title=True):
    """Devuelve lista de objetos (parrafos/tablas) recien creados al final del doc."""
    lines = open(md_path, encoding='utf-8').read().split('\n')
    made = []
    i = 0
    title_skipped = not skip_title
    HMAP = {1:base_level, 2:base_level+1, 3:base_level+2, 4:base_level+3,
            5:base_level+4, 6:base_level+4}
    while i < len(lines):
        s = lines[i].strip()

        if s.startswith('# ') and not title_skipped:
            title_skipped = True; i += 1; continue

        if s == '---':
            i += 1; continue

        if s.startswith('```'):
            i += 1; buf = []
            while i < len(lines) and not lines[i].strip().startswith('```'):
                buf.append(lines[i].rstrip()); i += 1
            i += 1
            while buf and not buf[0].strip(): buf.pop(0)
            while buf and not buf[-1].strip(): buf.pop()
            p = doc.add_paragraph()
            pf = p.paragraph_format
            pf.space_before = Pt(6); pf.space_after = Pt(6); pf.left_indent = Inches(0.15)
            shade_par(p, 'F2F2F2')
            for k, bl in enumerate(buf):
                r = p.add_run(bl); r.font.name='Consolas'; r.font.size=Pt(8)
                r._element.rPr.rFonts.set(qn('w:cs'),'Consolas')
                t = r._element.find(qn('w:t'))
                if t is not None: t.set(qn('xml:space'),'preserve')
                if k < len(buf)-1: r.add_break()
            made.append(p); continue

        if s.startswith('|') and i+1 < len(lines) and re.match(r'^\|[\s:\-|]+\|$', lines[i+1].strip()):
            header = [c.strip() for c in s.strip('|').split('|')]
            i += 2; rows = []
            while i < len(lines) and lines[i].strip().startswith('|'):
                rows.append([c.strip() for c in lines[i].strip().strip('|').split('|')]); i += 1
            n = len(header); has_h = any(h.strip() for h in header)
            tbl = doc.add_table(rows=(1 if has_h else 0), cols=n)
            set_borders(tbl); tbl.autofit = True
            if has_h:
                for c, ht in enumerate(header):
                    cell = tbl.rows[0].cells[c]; cell.text = ''
                    pp = cell.paragraphs[0]; pp.paragraph_format.space_after = Pt(2)
                    add_runs(pp, ht or ' ', 9)
                    for r in pp.runs: r.bold = True
                    shade_cell(cell, 'E8E8E8')
            for rd in rows:
                cells = tbl.add_row().cells
                for c in range(n):
                    cell = cells[c]; cell.text = ''
                    pp = cell.paragraphs[0]; pp.paragraph_format.space_after = Pt(2)
                    add_runs(pp, (rd[c] if c < len(rd) else '') or ' ', 9)
            made.append(tbl)
            sp = doc.add_paragraph(); sp.paragraph_format.space_after = Pt(4); made.append(sp)
            continue

        m = re.match(r'^(#{1,6})\s+(.*)$', s)
        if m:
            lvl = min(HMAP[len(m.group(1))], 6)
            p = doc.add_paragraph(style='Heading %d' % lvl)
            add_runs(p, m.group(2)); made.append(p); i += 1; continue

        if s.startswith('>'):
            buf = []
            while i < len(lines) and lines[i].strip().startswith('>'):
                buf.append(re.sub(r'^\s*>\s?','', lines[i])); i += 1
            for bl in buf:
                bs = bl.strip()
                if not bs: continue
                hm = re.match(r'^(#{1,6})\s+(.*)$', bs)
                p = doc.add_paragraph()
                pf = p.paragraph_format
                pf.left_indent = Inches(0.25); pf.space_before = Pt(2); pf.space_after = Pt(2)
                left_bar(p)
                if hm:
                    add_runs(p, hm.group(2), 10)
                    for r in p.runs: r.bold = True
                else:
                    add_runs(p, re.sub(r'^[-*]\s+','\u2022 ', bs), 10)
                    for r in p.runs:
                        if not r.bold: r.italic = True
                made.append(p)
            continue

        if re.match(r'^[-*]\s+', s):
            p = doc.add_paragraph(style='List Bullet')
            p.paragraph_format.space_after = Pt(2)
            add_runs(p, re.sub(r'^[-*]\s+','', s)); made.append(p); i += 1; continue

        if re.match(r'^\d+\.\s+', s):
            p = doc.add_paragraph(style='List Paragraph')
            p.paragraph_format.space_after = Pt(2)
            add_runs(p, s); made.append(p); i += 1; continue

        if not s:
            i += 1; continue

        p = doc.add_paragraph(); p.paragraph_format.space_after = Pt(4)
        add_runs(p, s); made.append(p); i += 1
    return made

def move_after(made, anchor_el):
    prev = anchor_el
    for obj in made:
        el = obj._tbl if hasattr(obj,'_tbl') else obj._p
        body.remove(el); prev.addnext(el); prev = el
    return prev

def delete_between(i_from_excl, i_to_excl):
    kids = list(body)
    for el in kids[i_from_excl+1:i_to_excl]:
        body.remove(el)

def new_h1(text, after_el):
    p = doc.add_paragraph(style='Heading 1')
    p.add_run(text)
    body.remove(p._p); after_el.addnext(p._p)
    return p._p

# =========================================================
# 1) ESTRUCTURA DIARIA DEL TIEMPO  ->  dia.md
# =========================================================
i_day  = find(lambda t: t == "ESTRUCTURA DIARIA DEL TIEMPO")
i_next = find(lambda t: t.startswith("BASES DEL JUEGO"), i_day+1)
assert i_day is not None and i_next is not None, "anclas dia"
anchor_day = list(body)[i_day]
delete_between(i_day, i_next)
move_after(convert(D + r"\dia.md", 2), anchor_day)
print("dia.md insertado")

# =========================================================
# 2) FUNDAMENTOS DEL SISTEMA DE COMBATE -> manual-tecnico.md
#    + secciones nuevas detras
# =========================================================
i_comb = find(lambda t: t == "FUNDAMENTOS DEL SISTEMA DE COMBATE")
i_end  = find(lambda t: t.startswith("LISTA COMPLETA DE ARMAS"), i_comb+1)
assert i_comb is not None and i_end is not None, "anclas combate"
anchor_comb = list(body)[i_comb]
delete_between(i_comb, i_end)
last = move_after(convert(D + r"\manual-tecnico.md", 2), anchor_comb)
print("manual-tecnico.md insertado")

for titulo, archivo in [
    ("PASIVAS Y RECUERDOS",            "pasivas.md"),
    ("EFECTOS EMOCIONALES (E.EM.)",    "emociones.md"),
    ("LAS RUNS",                        "runs.md"),
]:
    h = new_h1(titulo, last)
    last = move_after(convert(D + "\\" + archivo, 2), h)
    print(titulo, "insertado")

doc.save(DST)
print("\nGUARDADO")
