from pypdf import PdfReader, PdfWriter
import math
import sys

input_file = sys.argv[1]

reader = PdfReader(input_file)
total = len(reader.pages)

# livreto precisa múltiplo de 4
target = math.ceil(total / 4) * 4
pages = list(range(total)) + [None] * (target - total)


def blank_page(writer, ref_page):
    writer.add_blank_page(
        width=ref_page.mediabox.width,
        height=ref_page.mediabox.height
    )


# -------------------------
# calcula ordem de livreto
# -------------------------
left = 0
right = target - 1
booklet_order = []

while left < right:
    booklet_order.extend([right, left, left + 1, right - 1])
    left += 2
    right -= 2


def create_pdf(order, filename):
    writer = PdfWriter()
    ref = reader.pages[0]

    for idx in order:
        if idx is not None and idx < total:
            writer.add_page(reader.pages[idx])
        else:
            blank_page(writer, ref)

    with open(filename, "wb") as f:
        writer.write(f)


# -------------------------
# 1) arquivo único duplex
# -------------------------
create_pdf(booklet_order, "booklet_duplex_auto.pdf")


# -------------------------
# 2) separa frente/verso
# -------------------------
front = booklet_order[::2]  # ímpares
back = booklet_order[1::2]  # pares

create_pdf(front, "booklet_frente_manual.pdf")
create_pdf(back, "booklet_verso_manual.pdf")

print("\nArquivos gerados:")
print("booklet_duplex_auto.pdf")
print("booklet_frente_manual.pdf")
print("booklet_verso_manual.pdf")
