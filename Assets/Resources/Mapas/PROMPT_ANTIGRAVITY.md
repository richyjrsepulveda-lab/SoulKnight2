Agrega a mi juego 2D top-down estos sprites de mazmorra. NO los edites, NO los redibujes, NO los reescales con suavizado: úsalos tal cual.

Lee primero `LEEME.md` y `manifest.json` (están junto a este archivo). Reglas:
1. Importa todos los PNG con Filter = Point/Nearest, sin compresión, sin mipmaps, 16 pixels per unit.
2. Cada bioma (stone, plant, ice, fire, desert) debe poder elegirse por sala y usa SOLO los archivos con su prefijo.
3. Genera las salas con las reglas de LEEME.md: suelo (floor_00..07), muros con autotile `wall_top_{mask:02d}` (mask = N*1+E*2+S*4+O*8), cara frontal `wall_face_*` debajo de cada muro, sombras `floor_shadow_*` y `void` fuera de la sala.
4. Las animaciones usan las tiras `*_strip.png` (frames de 16x16 en horizontal) con los fps del manifest.
5. Obstáculos: colócalos con los colliders del manifest. `solid` bloquea movimiento, `damage` hace daño.
6. No cambies nada de la UI ni de los menús existentes.
Cuando termines, dime qué archivos tocaste y muéstrame una captura de una sala de cada bioma.
