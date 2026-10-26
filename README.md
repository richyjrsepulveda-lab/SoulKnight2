# SoulKnight 2 🐱⚔️ (Unity 2D Multiplayer Roguelike)

Repositorio oficial y base de desarrollo de **SoulKnight 2**, un juego de acción roguelike cooperativo 2D desarrollado en **Unity 2022.3 LTS** con **Netcode for GameObjects (NGO)** y transporte **Unity Transport (UTP)** en arquitectura LAN / Internet (con descubrimiento automático LAN y soporte UPnP).

---

## 🧭 ¿Qué es este proyecto y qué rumbo tiene?

SoulKnight 2 es un roguelike dinámico de acción en vista cenital (top-down) inspirado en Soul Knight y clásicos del género, pero con personalidad felina propia, progresión dual (en la run y entre runs), biomas artesanales procedurales con pixel art de alta fidelidad, cooperativo en tiempo real y combate basado en sinergias de habilidades elementales.

### 🎯 Visión y Rumbo del Juego
1. **Identidad Temática Única:**
   - Héroes felinos carismáticos con roles elementales definidos (Fuego, Hielo, Eléctrico, etc.).
   - Estética visual pixel art artesanal coherente a **32 PPU (Pixels Per Unit)**, donde cada celda de la mazmorra equivale a 1 unidad exacta de Unity, con muros monolíticos limpios, suelos continuos y bordes definidos.
   - Objetos y consumibles temáticos (como los pescaditos de vida: *Poco Pescado*, *Medio Pescado*, *Pescado Completo*).
2. **Estructura Roguelike de la Run:**
   - **3 Recámaras temáticas por Run** compuestas por un total de **10 Niveles**:
     - **Recámara 1:** Niveles 1 al 3 (Nivel 3 con Jefe).
     - **Recámara 2:** Niveles 4 al 6 (Nivel 6 con Jefe).
     - **Recámara 3:** Niveles 7 al 10 (Nivel 10 con Jefe Final de la Run).
   - **5 Biomas Procedurales Disponibles:** Piedra Ancestral, Cripta de Hielo Eterno, Forja / Basalto Ardiente, Ruinas Vegetales (Planta) y Desierto de Arena. En cada run se seleccionan 3 biomas distintos al azar para las 3 recámaras.
   - **Niveles sin jefe con Cámaras de Guardianes:** Oleadas pesadas coreografiadas anunciadas mediante portales de invocación que se cierran al limpiar la sala.
   - **Santuario del Bucle Infinito (Nivel 10.1+):** Al derrotar la run, los jugadores acceden a un santuario especial donde eligen entre salir con su victoria o entrar al portal del **Bucle Infinito (Loop)** con dificultad extrema y escalado ilimitado.
3. **Progresión Dual:**
   - **Progresión en Partida (Niveles 1 a 10):** Se reinicia cada run. Desbloquea el poder pleno del loadout, escala el daño (`+12%` por nivel) y las magnitudes de las habilidades.
   - **Maestría Permanente de Héroes (Niveles 1 a 20):** Se conserva entre runs en disco (`PlayerPrefs`). Desbloquea habilidades avanzadas en el catálogo de cada clase para configurar loadouts personalizados en el Lobby.
4. **Cooperativo sin Fricción:**
   - Multijugador LAN con auto-descubrimiento en red local por paquetes UDP (puerto 7778) y juego por código secreto/IP (puerto 7777).
   - Lobby interactivo físico donde los jugadores caminan a las salas elementales para elegir personaje y cruzan el portal norte para iniciar.
   - Mecánica de reanimación de compañeros caídos manteniendo presionada la tecla **F**.
   - HUD sincronizado en tiempo real con barras de aliados, estados y minimapa centrado con niebla de guerra progresiva.
5. **Metas de Publicación:**
   - **Fase Actual (Alpha / Beta Interna en PC):** Core gameplay pulido, 3 héroes completos, 5 biomas procedurales, loop infinito, multijugador LAN/IP sólido y sistema de audio retro completo.
   - **Hito 1 (Steam / Itch.io PC Release):** Soporte para Steam Datagram Relay / Steam Lobbies, integración de logros, 3 héroes adicionales (Planta/Tierra, Sombra, Luz), soporte total de gamepad y selector de idiomas.
   - **Hito 2 (Expansión Móvil / Consolas):** Controles táctiles virtuales (joystick dual), crossplay y servidores dedicados comunitarios.

---

## 🗺️ Mapa de la Documentación Completa

Para que cualquier agente, desarrollador o colaborador entienda el proyecto al 100%:

| Documento | Ubicación | Descripción |
|---|---|---|
| **Arquitectura y Sistemas** | [`docs/ARQUITECTURA_TECNICA.md`](docs/ARQUITECTURA_TECNICA.md) | Desglose de arquitectura C#, Netcode, networking, bucle de red y optimizaciones de memoria. |
| **Diseño de Juego (GDD)** | [`docs/GAME_DESIGN_DOCUMENT.md`](docs/GAME_DESIGN_DOCUMENT.md) | Clases de personajes, habilidades, biomas, jefes, balance y fórmulas matemáticas. |
| **Guía de Desarrollo y Comandos** | [`docs/GUIA_DESARROLLO_Y_COMANDOS.md`](docs/GUIA_DESARROLLO_Y_COMANDOS.md) | Cómo compilar, flujos de pruebas (`-autotest`), consola de códigos admin y buenas prácticas. |
| **Hoja de Ruta (Roadmap)** | [`docs/ROADMAP_METAS_PUBLICACION.md`](docs/ROADMAP_METAS_PUBLICACION.md) | Metas de lanzamiento, hitos pendientes y visión a medio/largo plazo. |
| **Registro de Cambios (Changelog)** | [`docs/CHANGELOG_HISTORIAL_CAMBIOS.md`](docs/CHANGELOG_HISTORIAL_CAMBIOS.md) | Historial detallado de versiones, commits recientes y evolución del proyecto. |

---

## ⚡ Inicio Rápido para Desarrolladores

### Requisitos
- **Unity Editor:** `2022.3.25f1` LTS (o cualquier versión `2022.3 LTS`).
- **Paquetes clave:** `com.unity.netcode.gameobjects (1.8.1)` y `com.unity.transport (1.4.0)`.

### Abrir el Proyecto en Unity Hub
1. Abre **Unity Hub**.
2. Haz clic en **Add (Añadir)** → **Add project from disk**.
3. Selecciona la carpeta raíz del repositorio (donde residen `Assets/`, `Packages/` y `ProjectSettings/`).
4. Abre el proyecto con **Unity 2022.3.25f1**.

### Compilar desde el Editor
En la barra superior de Unity dispones del menú personalizado **`SoulKnight`**:
- **`SoulKnight > Reconstruir Todo el Proyecto`**: Regenera prefabs, materiales y las 7 escenas del juego desde cero.
- **`SoulKnight > Compilar Build Standalone`**: Genera el ejecutable limpio en `C:/Users/Ricardo/Desktop/unityJuegos/SoulKnight2_Build/SoulKnight2.exe`.

---

## 📦 Descargas Rápidas (Releases)

En la sección [**Releases (v1.0.0)**](https://github.com/richyjrsepulveda-lab/SoulKnight2/releases/tag/v1.0.0) se mantienen actualizados los dos paquetes principales:
- **`SoulKnight2_Juego.zip`**: Build ejecutable compilado listo para descomprimir y jugar en Windows x64.
- **`SoulKnight2_ProyectoEditable_Unity.zip`**: Todo el proyecto fuente de Unity empaquetado para trabajar sin conexión.
