"""Generate our original two-meter icon with Python's standard library only."""

from pathlib import Path
import struct
import zlib


def chunk(kind, data):
    return struct.pack(">I", len(data)) + kind + data + struct.pack(">I", zlib.crc32(kind + data))


def image(size):
    rows = bytearray()
    for y in range(size):
        rows.append(0)
        for x in range(size):
            u, v = (x + 0.5) / size, (y + 0.5) / size
            color = (0, 0, 0, 0)
            if 0.06 < u < 0.94 and 0.06 < v < 0.94:
                color = (24, 39, 58, 255)
            if 0.24 < u < 0.44 and 0.28 < v < 0.77:
                color = (75, 213, 190, 255)
            if 0.56 < u < 0.76 and 0.43 < v < 0.77:
                color = (255, 179, 91, 255)
            rows.extend(color)
    return (
        b"\x89PNG\r\n\x1a\n"
        + chunk(b"IHDR", struct.pack(">IIBBBBB", size, size, 8, 6, 0, 0, 0))
        + chunk(b"IDAT", zlib.compress(bytes(rows), 9))
        + chunk(b"IEND", b"")
    )


def main():
    sizes = (16, 24, 32, 48, 64, 256)
    images = [image(size) for size in sizes]
    offset = 6 + 16 * len(sizes)
    entries = bytearray()
    for size, data in zip(sizes, images):
        entries.extend(struct.pack("<BBBBHHII", size % 256, size % 256, 0, 0, 1, 32, len(data), offset))
        offset += len(data)
    Path(__file__).with_name("AIUsageBar.ico").write_bytes(
        struct.pack("<HHH", 0, 1, len(sizes)) + entries + b"".join(images)
    )


if __name__ == "__main__":
    main()
