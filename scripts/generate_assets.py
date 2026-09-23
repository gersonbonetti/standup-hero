"""Rebuild the original pixel icon and quiet PCM notification tones (stdlib only)."""
from pathlib import Path
import math
import struct
import wave

assets = Path(__file__).resolve().parent.parent / 'Assets'
assets.mkdir(exist_ok=True)

def icon_image(size):
    # A lime standing figure and an upward arrow on an ink-blue tile.
    def pixel(x, y):
        x, y = x * 16 // size, y * 16 // size
        if (x in (0, 15) and (y < 2 or y > 13)) or (y in (0, 15) and (x < 2 or x > 13)):
            return (0, 0, 0, 0)
        color = (29, 34, 49, 255)
        if 4 <= x <= 6 and 2 <= y <= 4: color = (234, 186, 145, 255)
        if 3 <= x <= 7 and 6 <= y <= 9: color = (167, 230, 135, 255)
        if y in (10, 11, 12, 13) and x in (3, 4, 6, 7): color = (167, 230, 135, 255)
        if (x == 11 and 4 <= y <= 11) or (y == 5 and x in (10, 12)) or (y == 6 and x in (9, 13)):
            color = (121, 187, 208, 255)
        return color
    pixels = bytearray()
    mask = bytearray()
    stride = ((size + 31) // 32) * 4
    for y in range(size - 1, -1, -1):
        row = bytearray(stride)
        for x in range(size):
            r, g, b, a = pixel(x, y)
            pixels.extend((b, g, r, a))
            if not a: row[x // 8] |= 0x80 >> (x % 8)
        mask.extend(row)
    return struct.pack('<IiiHHIIiiII', 40, size, size * 2, 1, 32, 0, len(pixels), 0, 0, 0, 0) + pixels + mask

sizes = [16, 32, 48, 64, 128, 256]
images = [icon_image(s) for s in sizes]
offset = 6 + 16 * len(sizes)
entries = bytearray()
for size, data in zip(sizes, images):
    entries.extend(struct.pack('<BBBBHHII', size % 256, size % 256, 0, 0, 1, 32, len(data), offset))
    offset += len(data)
(assets / 'StandUpHero.ico').write_bytes(struct.pack('<HHH', 0, 1, len(sizes)) + entries + b''.join(images))

for name, notes in {'rise': [523.25, 659.25, 783.99], 'fall': [783.99, 659.25, 523.25], 'bell': [880], 'arcade': [523.25, 783.99, 1046.5, 783.99]}.items():
    rate = 22050
    samples = bytearray()
    duration = 0.7 if name == 'bell' else 0.17
    for frequency in notes:
        for i in range(int(rate * duration)):
            t = i / rate
            envelope = min(t / 0.015, 1) * min((duration - t) / 0.05, 1) * math.exp(-2 * t)
            value = math.sin(2 * math.pi * frequency * t)
            if name == 'bell': value = (value + 0.25 * math.sin(2 * math.pi * frequency * 2.76 * t)) / 1.25
            if name == 'arcade': value = (value + 0.2 * math.sin(6 * math.pi * frequency * t)) / 1.2
            samples.extend(struct.pack('<h', int(value * envelope * 6500)))
        samples.extend(b'\0\0' * int(rate * 0.035))
    with wave.open(str(assets / (name + '.wav')), 'wb') as wav:
        wav.setparams((1, 2, rate, 0, 'NONE', 'not compressed'))
        wav.writeframes(samples)
