# Historial de Cambios y Versiones - SoulKnight 2 📜

Este registro consolida la evolución técnica y de diseño de **SoulKnight 2**, sirviendo de referencia cronológica para cualquier agente o desarrollador que consulte el repositorio.

---

## Versión Actual: v1.2.0 (Octubre 2026 - Commit `aa3ada1`)

### 🛒 Tienda de la Run Completa y Mercado Felino (`Shop.dc.html` / `PanelTiendaRun`)
- **Diseño Retro 1:1:** Implementación de interfaz de tienda temática con tarjetas dinámicas con halo circular elemental, marcos de estilo pixel art y animación de entrada.
- **Interacción con el Gato Tendero:**
  - Bocadillo interactivo con frases variadas, márgenes adaptables y text wrap.
  - Efectos de sonido temáticos: maullidos y ronroneos del gato tendero al interactuar, y sonido de caja registradora (*cachín*) al comprar.
  - Ronroneo sutil del gato jugador cuando permanece quieto.
- **Mejoras Dinámicas:** Catálogo de 6 rasgos/mejoras con descripciones multinivel (Nivel 1 a 5) y subtítulo de Mejoras Permanentes en la run. Precios con auto-ajuste y sin truncamiento visual.

### 🎨 Arquitectura Visual Pixel Art Unificada y Tipografías
- **Tipografía `Jersey 15`:** Letra C limpia, números retro legibles y ajuste óptico sin pixel-bleeding en tarjetas de tienda y menús.
- **Tipografía `Pixelify Sans`:** Estandarizada en menús de Buscar Sala, Crear Sala, Menú Jugar y HUD general.
- **Componentes `EstiloUIUX`:** Renderizado exacto de trazo y sombra pixel art en títulos, botones con PPU estandarizado a 100 y cajas de texto con overflow vertical/horizontal calibrado para todas las resoluciones.

### ⚡ Netcode y Combate Mejorado
- **Piloto de Predicción 0ms para Proyectiles:** Predicción local instantánea para proyectiles como *Bola de Fuego* con validación autoritativa en el Host y Object Pooling para rendimiento óptimo.
- **Correcciones de Red en Personaje Eléctrico:** Daño calibrado de dash a 90, sincronización perfecta en multiplayer de colisiones y resolución del lag de ataques en clientes remotos.
- **Cadencia de Rayo de Hielo:** Frecuencia de impactos aumentada a 50 ticks por segundo para sensación de daño continuo pesado (`danoRayo = 4`, `intervaloTickRayo = 0.02`).
- **Estados de Daño:** Daño periódico de quemadura y veneno estandarizado a 2 por tick, con efectos visuales de quemadura y ataudes optimizados en el bioma de desierto.

### 🗺️ Navegación y Mapa en Pantalla Completa
- **Apertura de Mapa con Tecla M o Clic:** Mapa completo desplegable durante la run sin pausa brusca de juego.
- **Niebla de Guerra y Pasillos:** Pasillos ocultos revelados limpiamente sin sombras superpuestas; confinamiento de monedas de oro en el suelo para evitar que salgan de los límites de las habitaciones.
- **Salas Secundarias y Cofres de Oro:** Generación de recámaras laterales sin salida que contienen cofres interactivos protegidos por oleadas adicionales.

### ⚙️ Menú de Opciones y Teclado
- **Apartado de Video:** Ciclo de resoluciones de pantalla sincronizado, opción de pantalla completa y modo de daño acumulativo opcional para simplificar los números de combate.
- **Tecla ESC Fija:** Protección para evitar reasignar accidentalmente la tecla ESC, manteniéndola siempre como atajo para pausar y retroceder menús.

---

## Versión v1.1.0 (Octubre 2026 - Commit `bc084bf`)

### 🔊 Audio Integral y Efectos Sonoros
- **`GestorAudio.cs`:** Sistema centralizado de BGM y SFX con persistencia de volúmenes en `PlayerPrefs`.
- **BGM Dinámico:** Pistas musicales temáticas diferenciadas para Salas normales y música de Jefe (`MusicaJefe1`).
- **Efectos Felinos Realistas:** Rotación aleatoria de 3 sonidos de daño felino con variaciones de pitch para evitar monotonía.
- **SFX de Habilidades:** Sonido continuo de rayo láser para el gato de hielo, relámpagos celestiales en teletransporte eléctrico y efectos al devorar pescados curativos.

### 🎨 Pixel Art Artesanal y Biomas Procedurales (32 PPU)
- **`CatalogoBiomas.cs`:** Creación procedural en tiempo de ejecución de texturas a 32 PPU para 5 biomas: Piedra Ancestral, Cripta de Hielo, Forja de Basalto Ardiente, Ruinas Vegetales y Desierto de Arena.
- **Cuadrícula Unificada:** Solución integral para evitar cortes diagonales, fisuras y desniveles entre baldosas de suelo y muros.
- **Mejora de Visibilidad:** Paletas de fondo desaturadas, bordes blancos contrastados en enemigos y contorno nítido en proyectiles para máxima legibilidad de combate.

### ⚡ Habilidades y Combate
- **Gato de Hielo:**
  - Calibración de la salida del rayo de hielo continuo para coincidir exactamente con el hocico del felino.
  - Habilidad definitiva *Era de Hielo* (`ZonaEraHielo.cs`): radio aumentado a 10.0 con sincronización visual de ventisca procedural y escalado de daño según el nivel del personaje.
- **Gato Eléctrico:**
  - Habilidad definitiva estilo Zenitsu con desplazamiento hiper-rápido.
  - Teletransporte de plasma dual con caída de rayo en origen y destino.
- **Gato de Fuego:**
  - *Supernova Felina* como definitiva en 360 grados, escopeta triple de magma y rastro de fuego ardiente en el suelo.

### 🏰 Mazmorras, Tiendas y Estructura Roguelike
- **3 Recámaras temáticas por Run (10 Niveles):** Jefes en los niveles 3, 6 y 10.
- **Cámaras de Guardianes Élite:** Niveles intermedios sin jefe cerrados con oleadas pesadas de enemigos de élite saliendo de portales mágicos.
- **Santuario del Bucle Infinito (Nivel 10.1+):** Portales duales tras derrotar la run: uno para salir victorioso al Lobby y otro para entrar al Bucle Infinito con dificultad creciente.
- **Puesto de Tienda Intermedia:** Tienda felina en mazmorras con modal de compras pulsando **F**.

### 💻 Interfaz, Comandos y Calidad de Vida
- **Botón Cambiar Nombre:** Botón anclado abajo a la izquierda en el Menú Principal (`✎ CAMBIAR NOMBRE (Actual)`), permitiendo editar el nombre con confirmación por tecla Enter.
- **Barras de HUD:** Barra de experiencia de run (`BarraXPRunHUD`) y barra de maestría (`BarraMaestriaHUD`) con offsets limpios en `RectTransform` sin desfasamientos horizontales.
- **Consola de Comandos Admin:** Comandos ejecutables desde el menú de opciones (`lvl`, `maxlvl`, `desbloqueartodo`, `zerocd`, `god`, `heal`, `gold`, `skip`, etc.).

---

## Versión Inicial: v1.0.0 (Principios de Octubre 2026)

- Migración inicial completa desde Godot a Unity con arquitectura Netcode for GameObjects.
- Implementación de Lobby físico con cuartos elementales para selección interactiva de clases.
- Sistema de multijugador LAN con auto-descubrimiento en red local.
- Menú de pausa interactivo con minimapa en tiempo real y asignación de controles remapeables.
- Mecánica cooperativa de reanimación de aliados caídos con tecla F.
- Creación de builds automáticos standalone y scripts de prueba (`-autotest`).
