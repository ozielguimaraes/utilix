#!/usr/bin/env python3

"""
Gera PDFs em formato livreto PRONTO PARA IMPRIMIR.

Saídas:
- booklet_duplex_auto.pdf      (frente/verso automático)
- booklet_manual_frente.pdf   (primeiro lado)
- booklet_manual_verso.pdf    (segundo lado)

Uso:
python booklet_ready_print.py arquivo.pdf
"""

import sys
import subprocess
import math


# -------------------------
# garante dependências
# -------------------------
def ensure(pkg):
    try:
        __import__(pkg)
    except ImportError:
        print(f"Instalando dependência: {pkg}")
        subprocess.check_call([sys.executable, "-m", "pip", "install", pkg])


ensure("pypdf")
ensure("reportlab")


# agora importa
from pypdf import PdfReader, PdfWriter, Transformation
from reportlab.lib.pagesizes import A4


# -------------------------
# valida args
# -------------------------
if len(sys.argv) < 2:
    print("Uso: python booklet_ready_print.py arquivo.pdf")
    sys.exit(1)

input_file = sys.argv[1]

reader = PdfReader(input_file)
total = len(reader.pages)

PAGE_W, PAGE_H = A4


# -------------------------
# calcula ordem livreto
# -------------------------
target = math.ceil(total / 4) * 4
indexes = list(range(total)) + [None] * (target - total)

left, right = 0, target - 1
order = []

while left < right:
    order.extend([right, left, left + 1, right - 1])
    left += 2
    right -= 2


# -------------------------
# monta páginas 2-em-1 no A4
# -------------------------
def build_pdf(sequence, filename):
    writer = PdfWriter()
    scale = 0.5

    for i in range(0, len(sequence), 2):
        sheet = writer.add_blank_page(width=PAGE_W, height=PAGE_H)

        pair = sequence[i:i + 2]

        for pos, idx in enumerate(pair):
            if idx is None or idx >= total:
                continue

            page = reader.pages[idx]

            # copia a página para não acumular transformações
            page = page.clone()

            x = 0 if pos == 0 else PAGE_W / 2
            y = 0

            page.add_transf_
