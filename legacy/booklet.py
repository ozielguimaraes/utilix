from pypdf import PdfReader, PdfWriter
import math
import sys

input_file = sys.argv[1]
output_file = "booklet_" + input_file

reader = PdfReader(input_file)
writer = PdfWriter()

total = len(reader.pages)

# múltiplo de 4 (livreto exige isso)
target = math.ceil(total / 4) * 4
blanks = target - total

pages = list(reader.pages)

# adiciona páginas em branco se precisar
for _ in range(blanks):
    writer.add_blank_page(
        width=pages[0].mediabox.width,
        height=pages[0].mediabox.height
    )

pages += [None] * blanks

left = 0
right = target - 1

order = []

while left < right:
    order.extend([right, left, left + 1, right - 1])
    left += 2
    right -= 2

for i in order:
    if i < total:
        writer.add_page(reader.pages[i])
    else:
        writer.add_blank_page(
            width=pages[0].mediabox.width,
            height=pages[0].mediabox.height
        )

with open(output_file, "wb") as f:
    writer.write(f)

print(f"Gerado: {output_file}")
