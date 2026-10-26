# -*- coding: utf-8 -*-
"""Genera "guion/GUION - Historia Principal.docx" desde los .md de guion/.

Lee guion/NN-*.md en orden y les da formato de guion: personaje centrado,
dialogo sangrado, acotaciones en cursiva, marcadores de juego en color y
bloques de diseno sombreados. Las convenciones estan en guion/README.md.

    python tools/build_guion.py
"""
import glob
import os
import re

import docx
from docx.enum.text import WD_ALIGN_PARAGRAPH, WD_BREAK
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.shared import Inches, Pt, RGBColor

_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DIR = os.path.join(_ROOT, "guion")
SALIDA = os.path.join(DIR, "GUION - Historia Principal.docx")

FUENTE = "Courier New"
AZUL = RGBColor(0x1F, 0x4E, 0x79)
GRIS = RGBColor(0x59, 0x59, 0x59)

# **NOMBRE**  o  **NOMBRE** *(acotacion)*
RE_PERSONAJE = re.compile(r"^\*\*([A-ZÁÉÍÓÚÜÑ0-9?¿][A-ZÁÉÍÓÚÜÑ0-9 .,'’?¿\-/]*)\*\*(?:\s+\*\((.+)\)\*)?\s*$")
RE_ACOTACION = re.compile(r"^\*\((.+)\)\*\s*$")
RE_MARCADOR = re.compile(r"^\*\*\[(.+?)\]\*\*\s*(.*)$")
RE_INLINE = re.compile(r"(\*\*.+?\*\*|\*.+?\*)")


def add_runs(p, texto, size=None, color=None, italic=False, bold=False):
    """Texto con **negrita** y *cursiva* en linea."""
    for trozo in RE_INLINE.split(texto):
        if not trozo:
            continue
        b, i = bold, italic
        if trozo.startswith("**") and trozo.endswith("**") and len(trozo) > 4:
            trozo, b = trozo[2:-2], True
        elif trozo.startswith("*") and trozo.endswith("*") and len(trozo) > 2:
            trozo, i = trozo[1:-1], True
        r = p.add_run(trozo)
        r.bold, r.italic = b, i
        if size:
            r.font.size = Pt(size)
        if color:
            r.font.color.rgb = color


def sombrear(p, color="EEF2F7"):
    sh = OxmlElement("w:shd")
    sh.set(qn("w:val"), "clear")
    sh.set(qn("w:fill"), color)
    p._p.get_or_add_pPr().append(sh)


def formato(p, izq=0.0, der=0.0, antes=0, despues=0, centrado=False, junto=False):
    f = p.paragraph_format
    f.left_indent, f.right_indent = Inches(izq), Inches(der)
    f.space_before, f.space_after = Pt(antes), Pt(despues)
    f.keep_with_next = junto
    if centrado:
        p.alignment = WD_ALIGN_PARAGRAPH.CENTER


def tabla(doc, filas):
    celdas = [[c.strip() for c in f.strip().strip("|").split("|")] for f in filas]
    celdas = [f for f in celdas if not all(re.fullmatch(r":?-{2,}:?", c) for c in f)]
    if not celdas:
        return
    ancho = max(len(f) for f in celdas)
    t = doc.add_table(rows=len(celdas), cols=ancho)
    t.style = "Table Grid"
    for i, fila in enumerate(celdas):
        for j in range(ancho):
            p = t.cell(i, j).paragraphs[0]
            add_runs(p, fila[j] if j < len(fila) else "", size=9, bold=(i == 0))


def construir():
    doc = docx.Document()
    estilo = doc.styles["Normal"]
    estilo.font.name = FUENTE
    estilo.element.rPr.rFonts.set(qn("w:eastAsia"), FUENTE)
    estilo.font.size = Pt(11)
    for s in doc.sections:
        s.left_margin, s.right_margin = Inches(1.3), Inches(1.0)

    portada = doc.add_paragraph()
    formato(portada, antes=200, centrado=True)
    add_runs(portada, "E.N.M.A.\nHISTORIA PRINCIPAL\n\nGuion", size=20, bold=True)

    archivos = sorted(glob.glob(os.path.join(DIR, "[0-9][0-9]-*.md")))
    for ruta in archivos:
        texto = open(ruta, encoding="utf-8").read()
        texto = re.sub(r"<!--.*?-->", "", texto, flags=re.S)
        lineas = texto.split("\n")
        en_dialogo = False
        i = 0
        while i < len(lineas):
            l = lineas[i].rstrip()
            i += 1
            if not l.strip():
                en_dialogo = False
                continue

            if l.startswith("# "):
                p = doc.add_paragraph()
                p.add_run().add_break(WD_BREAK.PAGE)
                formato(p, despues=18, centrado=True)
                add_runs(p, l[2:], size=16, bold=True)
                continue
            if l.startswith("## "):
                p = doc.add_paragraph()
                formato(p, antes=18, despues=6, junto=True)
                add_runs(p, l[3:].upper(), size=12, bold=True, color=AZUL)
                continue
            if l.startswith("### "):
                p = doc.add_paragraph()
                formato(p, antes=14, despues=6, junto=True)
                add_runs(p, l[4:], bold=True)
                sombrear(p, "E7E6E6")
                continue
            if l.startswith("#### "):
                p = doc.add_paragraph()
                formato(p, antes=10, despues=4, junto=True)
                add_runs(p, l[5:], bold=True, italic=True)
                continue
            if l.strip() == "---":
                continue

            # Bloque de diseno: lineas seguidas que empiezan por ">"
            if l.startswith(">"):
                bloque = [l]
                while i < len(lineas) and lineas[i].startswith(">"):
                    bloque.append(lineas[i].rstrip())
                    i += 1
                bloque = [b[1:].lstrip() if b.startswith(">") else b for b in bloque]
                j = 0
                while j < len(bloque):
                    b = bloque[j]
                    if b.startswith("|"):
                        filas = []
                        while j < len(bloque) and bloque[j].startswith("|"):
                            filas.append(bloque[j])
                            j += 1
                        tabla(doc, filas)
                        continue
                    j += 1
                    if not b.strip():
                        continue
                    p = doc.add_paragraph()
                    vineta = re.match(r"^\s*[-*]\s+(.*)$", b)
                    formato(p, izq=0.6 if vineta else 0.3, der=0.3, despues=2)
                    add_runs(p, ("• " + vineta.group(1)) if vineta else b, size=10)
                    sombrear(p)
                continue

            if l.startswith("|"):
                filas = [l]
                while i < len(lineas) and lineas[i].startswith("|"):
                    filas.append(lineas[i].rstrip())
                    i += 1
                tabla(doc, filas)
                continue

            m = RE_MARCADOR.match(l)
            if m:
                p = doc.add_paragraph()
                formato(p, antes=6, despues=4)
                add_runs(p, "[" + m.group(1) + "]", bold=True, color=AZUL, size=10)
                if m.group(2):
                    add_runs(p, " " + m.group(2))
                en_dialogo = False
                continue

            m = RE_PERSONAJE.match(l)
            if m:
                p = doc.add_paragraph()
                formato(p, izq=2.0, antes=8, junto=True)
                add_runs(p, m.group(1), bold=True)
                if m.group(2):
                    add_runs(p, " (" + m.group(2) + ")", italic=True, color=GRIS)
                en_dialogo = True
                continue

            if en_dialogo:
                m = RE_ACOTACION.match(l)
                p = doc.add_paragraph()
                if m:
                    formato(p, izq=1.6, der=1.6, junto=True)
                    add_runs(p, "(" + m.group(1) + ")", italic=True, color=GRIS)
                else:
                    formato(p, izq=1.0, der=1.0)
                    add_runs(p, l)
                continue

            vineta = re.match(r"^\s*[-*]\s+(.*)$", l)
            p = doc.add_paragraph()
            formato(p, izq=0.3 if vineta else 0.0, antes=4, despues=4)
            add_runs(p, ("• " + vineta.group(1)) if vineta else l)

    doc.save(SALIDA)
    print(f"{len(archivos)} archivos -> {SALIDA}")


if __name__ == "__main__":
    construir()
