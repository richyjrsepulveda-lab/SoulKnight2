# Just a Cat Adventure – Sprites de mazmorra (5 biomas)

Biomas (todos mazmorras): `stone`, `plant`, `ice`, `fire`, `desert`.
Tamaño de tile: **16x16 px**. Todo es PNG con transparencia, sin escalar.

## Importar (cualquier motor)
- Filter Mode: **Point / Nearest**. Compresión: **ninguna**. Mipmaps: **off**.
- Pixels Per Unit: **16** (1 tile = 1 unidad). Si el juego es más grande, escala por un entero (x2, x3, x4), nunca decimales.
- Cada archivo es un sprite completo (no hay que cortar nada). También hay un atlas por bioma si prefieres usar Tilemap.
- `manifest.json` lista cada sprite con tamaño, frames, fps, pivote, collider y si hace daño.

## Estructura
```
manifest.json
<bioma>/
  <bioma>_tileset_atlas.png      atlas 256x48 (celdas de 16px)
  tiles/   <bioma>_floor_00..07, <bioma>_floor_shadow_00..01, <bioma>_void
  walls/   <bioma>_wall_top_00..15, <bioma>_wall_face_{a,b,cracked,decor}, <bioma>_wall_face_torch_f0..f3 (+ _strip)
  props/   obstáculos y decoración
preview/   una sala de ejemplo por bioma
```

## Cómo armar una sala (reglas simples)
1. **Suelo**: usa `floor_00..03` como base (la mayoría), `floor_04..07` esporádicos (grietas, detalles).
2. **Muro, parte de arriba** (`wall_top_XX`): es autotile de 4 vecinos. Para cada celda de muro calcula
   `mask = N*1 + E*2 + S*4 + O*8`, donde cada letra vale 1 si el vecino en esa dirección también es muro (top). Usa `wall_top_{mask:02d}`.
   Ejemplos: 15 = interior completo, 0 = bloque aislado.
3. **Muro, cara frontal** (`wall_face_*`): se coloca en la celda de **suelo** que queda justo debajo de una celda de muro (se ve la pared de frente). Es sólida. Mezcla `a`/`b`, un `cracked` o `decor` de vez en cuando y una `torch` cada ~4-5 tiles (animada, 4 frames, 8 fps).
4. **Sombra**: en la fila de suelo justo debajo de la cara del muro usa `floor_shadow_00`, y en la siguiente `floor_shadow_01`.
5. **Vacío**: `void` para todo lo que queda fuera de la sala (fondo).

## Obstáculos y props (por bioma)
Pivote: abajo-centro. Los colliders del manifest están en píxeles desde la esquina superior izquierda del sprite.
| archivo | tamaño | comportamiento |
|---|---|---|
| `pillar` | 16x32 | sólido |
| `boulder_s` / `boulder_l` | 16x16 / 32x32 | sólido |
| `crate` / `barrel` | 16x16 | sólido, destruible (en `fire` el barril es explosivo) |
| `spikes_off` / `spikes_on` (+ `spikes_strip`, 2 frames) | 16x16 | trampa, daña solo cuando está activa |
| `pit` (stone), `poison` (plant), `icewater` (ice), `lava` (fire), `quicksand` (desert) | 16x16 x4 frames, 5 fps | zona de peligro, tile continuo sin bordes |
| `bones` (stone) | 16x16 | decoración sin colisión |
| `thornbush` (plant) / `cactus` (desert) | 16x16 | sólido, daña al tocar |
| `crystal` (ice) | 16x16 | sólido |
| `brazier` (fire) | 16x16 x4 frames, 8 fps | sólido, animado |

## Limitaciones conocidas
- El autotile de muros usa 4 vecinos (16 piezas): las esquinas interiores diagonales quedan con una muesca pequeña. Si no te gusta, se agregan 4 piezas extra.
- Sin puertas todavía.
