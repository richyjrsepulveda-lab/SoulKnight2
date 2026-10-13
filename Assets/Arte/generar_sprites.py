import os
from PIL import Image, ImageDraw
import math

os.makedirs('Assets/Arte/Enemigos', exist_ok=True)
os.makedirs('Assets/Arte/Mapas', exist_ok=True)

def safe_putpixel(img, x, y, col):
    if 0 <= x < img.width and 0 <= y < img.height:
        img.putpixel((x, y), col)

def safe_blend(img, x, y, col):
    if 0 <= x < img.width and 0 <= y < img.height:
        bg = img.getpixel((x, y))
        a = col[3] / 255.0
        r = int(col[0] * a + bg[0] * (1.0 - a))
        g = int(col[1] * a + bg[1] * (1.0 - a))
        b = int(col[2] * a + bg[2] * (1.0 - a))
        out_a = min(255, int(col[3] + bg[3] * (1.0 - a)))
        img.putpixel((x, y), (r, g, b, out_a))

# =========================================================================
# 1. ENEMIGO MAGO (40 x 48) - Mago Oscuro / Cultista Arcano
# =========================================================================
w, h = 40, 48
img = Image.new('RGBA', (w, h), (0, 0, 0, 0))

c_outline = (18, 10, 28, 255)
c_robe_dark = (45, 22, 65, 255)
c_robe = (78, 42, 112, 255)
c_robe_light = (118, 68, 162, 255)
c_gold = (235, 185, 55, 255)
c_gold_d = (165, 115, 25, 255)
c_face = (15, 12, 20, 255)
c_eye = (65, 240, 255, 255)
c_wood = (85, 50, 35, 255)
c_orb = (255, 45, 125, 255)
c_orb_c = (255, 190, 225, 255)

# Báculo a la izquierda (x: 5..8, y: 12..45)
for y in range(14, 45):
    safe_putpixel(img, 6, y, c_outline)
    safe_putpixel(img, 7, y, c_wood)
    safe_putpixel(img, 8, y, (120, 75, 50, 255))
    safe_putpixel(img, 9, y, c_outline)

# Cabeza de báculo y orbe flotante
for ox in range(-4, 5):
    for oy in range(-4, 5):
        d2 = ox*ox + oy*oy
        if d2 <= 16:
            col = c_orb_c if d2 <= 4 else (c_orb if d2 <= 11 else (160, 20, 80, 255))
            safe_putpixel(img, 7 + ox, 9 + oy, col)
# Destellos mágicos del orbe
safe_putpixel(img, 7, 3, (255, 180, 220, 220))
safe_putpixel(img, 2, 9, (255, 180, 220, 220))
safe_putpixel(img, 12, 9, (255, 180, 220, 220))
safe_putpixel(img, 7, 15, (255, 180, 220, 220))

# Capa y túnica
for y in range(18, 46):
    sp = int((y - 18) * 0.42)
    x1 = 24 - 7 - sp
    x2 = 24 + 7 + sp
    for x in range(x1, x2 + 1):
        if x == x1 or x == x2 or y == 45:
            safe_putpixel(img, x, y, c_outline)
        else:
            t = (x - x1) / float(max(1, x2 - x1))
            if t < 0.22 or t > 0.78:
                col = c_robe_dark
            elif 0.38 <= t <= 0.62:
                col = c_robe_light
            else:
                col = c_robe
            if y >= 43:
                col = c_gold if (y == 43 or (x % 3 == 0)) else c_gold_d
            safe_putpixel(img, x, y, col)

# Capucha (Hood)
for y in range(7, 21):
    rad = int(7.5 - abs(y - 14) * 0.5)
    for x in range(24 - rad, 24 + rad + 1):
        if x == 24 - rad or x == 24 + rad or y == 7:
            safe_putpixel(img, x, y, c_outline)
        else:
            safe_putpixel(img, x, y, c_robe)

# Interior oscuro capucha y ojos
for y in range(12, 18):
    for x in range(21, 28):
        safe_putpixel(img, x, y, c_face)

# Ojos cyan resplandecientes
safe_putpixel(img, 22, 14, c_eye)
safe_putpixel(img, 23, 14, c_eye)
safe_putpixel(img, 25, 14, c_eye)
safe_putpixel(img, 26, 14, c_eye)
safe_blend(img, 22, 13, (65, 240, 255, 150))
safe_blend(img, 26, 13, (65, 240, 255, 150))

# Cinturón de runas
for x in range(18, 31):
    safe_putpixel(img, x, 29, c_gold_d)
    safe_putpixel(img, x, 30, c_gold)
safe_putpixel(img, 24, 30, (255, 50, 80, 255))
safe_putpixel(img, 24, 31, (255, 150, 150, 255))

img.save('Assets/Arte/Enemigos/EnemigoMago.png')
print('EnemigoMago.png guardado')

# =========================================================================
# 2. ENEMIGO SLIME (34 x 30) - Slime Ácido / Gelatina Voraz
# =========================================================================
w, h = 34, 30
img = Image.new('RGBA', (w, h), (0, 0, 0, 0))

c_sl_out = (15, 45, 25, 255)
c_sl_dark = (35, 115, 60, 255)
c_sl_mid = (65, 185, 95, 255)
c_sl_light = (135, 245, 150, 255)
c_sl_core = (25, 80, 45, 255)
c_sl_eye = (245, 255, 245, 255)
c_sl_pupil = (20, 40, 25, 255)

cx, cy = 17, 18
rx, ry = 14, 10

for y in range(4, 28):
    for x in range(2, 32):
        # Forma de gota aplastada / blob
        dy = (y - cy) / float(ry)
        dx = (x - cx) / float(rx)
        # Squash y punta arriba
        if dy < 0:
            dx *= (1.0 - dy * 0.35)
        dist = dx*dx + dy*dy
        if dist <= 1.0:
            if dist > 0.85 or y == 27:
                safe_putpixel(img, x, y, c_sl_out)
            elif dist > 0.55:
                safe_putpixel(img, x, y, c_sl_dark)
            elif dist > 0.22:
                safe_putpixel(img, x, y, c_sl_mid)
            else:
                safe_putpixel(img, x, y, c_sl_light)

# Brillo superior izquierdo
for bx in range(9, 14):
    for by in range(10, 14):
        if (bx-11)**2 + (by-12)**2 <= 4:
            safe_putpixel(img, bx, by, (220, 255, 225, 255))

# Ojos saltones
for ox, eye_x in [(-4, 12), (4, 21)]:
    for ey in range(14, 19):
        for ex in range(eye_x - 2, eye_x + 3):
            d2 = (ex - eye_x)**2 + (ey - 16)**2
            if d2 <= 4:
                safe_putpixel(img, ex, ey, c_sl_eye)
    # Pupila negra con mirada hostil
    safe_putpixel(img, eye_x + (1 if ox > 0 else 0), 16, c_sl_pupil)
    safe_putpixel(img, eye_x + (1 if ox > 0 else 0), 17, c_sl_pupil)

img.save('Assets/Arte/Enemigos/EnemigoSlime.png')
print('EnemigoSlime.png guardado')

# =========================================================================
# 3. PROYECTIL MAGO (24 x 24) - Orbe de Energía Sombría / Mágica
# =========================================================================
w, h = 24, 24
img = Image.new('RGBA', (w, h), (0, 0, 0, 0))
cx, cy = 12, 12

for y in range(w):
    for x in range(h):
        d = math.sqrt((x - cx)**2 + (y - cy)**2)
        if d <= 9.5:
            if d <= 3.0:
                col = (255, 230, 255, 255) # Centro blanco puro
            elif d <= 5.5:
                col = (245, 90, 210, 255)  # Magenta brillante
            elif d <= 7.5:
                col = (150, 40, 190, 230)  # Púrpura medio
            else:
                col = (60, 15, 100, 160)   # Halo sombrío
            safe_putpixel(img, x, y, col)

# Destellos en cruz
for i in range(-10, 11):
    safe_blend(img, cx + i, cy, (255, 120, 220, max(0, 180 - abs(i)*16)))
    safe_blend(img, cx, cy + i, (255, 120, 220, max(0, 180 - abs(i)*16)))

img.save('Assets/Arte/Enemigos/ProyectilMago.png')
print('ProyectilMago.png guardado')

# =========================================================================
# 4. COLUMNA HIELO (32 x 48)
# =========================================================================
w, h = 32, 48
img = Image.new('RGBA', (w, h), (0, 0, 0, 0))
for y in range(4, 46):
    for x in range(6, 26):
        # Gradiente y estrías de hielo
        t = (x - 6) / 20.0
        if x == 6 or x == 25 or y == 4 or y == 45:
            col = (20, 50, 80, 255)
        elif 0.35 <= t <= 0.65:
            col = (210, 245, 255, 255)
        elif t < 0.35:
            col = (110, 185, 225, 255)
        else:
            col = (70, 130, 180, 255)
        # Grietas de hielo
        if (x + y * 2) % 17 == 0:
            col = (255, 255, 255, 255)
        safe_putpixel(img, x, y, col)
# Capitel y base ancha
for x in range(4, 28):
    for y in [3, 4, 44, 45, 46]:
        safe_putpixel(img, x, y, (150, 220, 255, 255))
        if x in (4, 27) or y in (3, 46):
            safe_putpixel(img, x, y, (20, 50, 80, 255))

img.save('Assets/Arte/Mapas/ColumnaHielo.png')
print('ColumnaHielo.png guardado')

# =========================================================================
# 5. COLUMNA MAGMA (32 x 48)
# =========================================================================
w, h = 32, 48
img = Image.new('RGBA', (w, h), (0, 0, 0, 0))
for y in range(4, 46):
    for x in range(6, 26):
        t = (x - 6) / 20.0
        if x == 6 or x == 25 or y == 4 or y == 45:
            col = (25, 15, 20, 255)
        elif 0.4 <= t <= 0.6:
            col = (70, 45, 55, 255)
        else:
            col = (45, 28, 35, 255)
        # Vena de magma ardiente
        if abs(x - (14 + int(3 * math.sin(y * 0.3)))) <= 1:
            col = (255, 180, 40, 255) if abs(x - (14 + int(3 * math.sin(y * 0.3)))) == 0 else (240, 70, 20, 255)
        safe_putpixel(img, x, y, col)
for x in range(4, 28):
    for y in [3, 4, 44, 45, 46]:
        safe_putpixel(img, x, y, (55, 32, 40, 255))
        if x in (4, 27) or y in (3, 46):
            safe_putpixel(img, x, y, (25, 15, 20, 255))

img.save('Assets/Arte/Mapas/ColumnaMagma.png')
print('ColumnaMagma.png guardado')

# =========================================================================
# 6. PORTAL RUNICO SALIDA (48 x 48)
# =========================================================================
w, h = 48, 48
img = Image.new('RGBA', (w, h), (0, 0, 0, 0))
cx, cy = 24, 24
for y in range(h):
    for x in range(w):
        d = math.sqrt((x - cx)**2 + (y - cy)**2)
        if d <= 22:
            if d > 18:
                # Anillo de piedra rúnica
                col = (70, 75, 90, 255)
                if int(d) == 21 or int(d) == 19:
                    col = (40, 45, 55, 255)
                safe_putpixel(img, x, y, col)
            else:
                # Vórtice interdimensional brillante
                t = d / 18.0
                angle = math.atan2(y - cy, x - cx)
                spiral = math.sin(angle * 4.0 - d * 0.5)
                if spiral > 0.3:
                    col = (130, 230, 255, 240)
                elif spiral > -0.3:
                    col = (60, 130, 240, 220)
                else:
                    col = (25, 40, 120, 200)
                if d < 4:
                    col = (255, 255, 255, 255)
                safe_putpixel(img, x, y, col)
# Runas brillantes en el borde
for a in range(8):
    ang = a * (math.pi / 4.0)
    rx = int(cx + 20 * math.cos(ang))
    ry = int(cy + 20 * math.sin(ang))
    safe_putpixel(img, rx, ry, (255, 220, 100, 255))

img.save('Assets/Arte/Mapas/PortalSalida.png')
print('PortalSalida.png guardado')
