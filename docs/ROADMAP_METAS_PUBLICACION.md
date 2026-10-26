# Hoja de Ruta y Metas de Publicación - SoulKnight 2 🚀

Este documento define la planificación estratégica, las metas de publicación y las prioridades de desarrollo para llevar a **SoulKnight 2** desde su estado actual de prototipo avanzado hasta su lanzamiento comercial en plataformas digitales.

---

## 1. Estado Actual del Proyecto (Alpha Avanzada / Beta Interna)

El núcleo jugable (Core Gameplay Loop) se encuentra funcional y estable:
- ✅ 3 Clases Felinas jugables completas: Fuego, Hielo y Eléctrico con habilidades Q, E, R (Ulti) y ataques primarios.
- ✅ 5 Biomas procedurales artesanales generados proceduralmente a 32 PPU sin cortes de textura.
- ✅ Bucle de Run de 10 niveles organizado en 3 recámaras con peleas de Jefes en los niveles 3, 6 y 10.
- ✅ Santuario del Bucle Infinito (Nivel 10.1+) para extender la partida con dificultad ilimitada.
- ✅ Multijugador LAN sólido con descubrimiento automático UDP y códigos de sala.
- ✅ Sistema de Audio integral con BGM dinámico según bioma/jefe y efectos de sonido reactivos.
- ✅ Tienda secreta en mazmorra con compras mediante tecla F.
- ✅ Sistema de metaprogresión (Maestría 1-20) y progresión temporal en run (Niveles 1-10).

---

## 2. Hitos de Publicación (Milestones)

### 🎯 Hito 1: Pulido y Preparación de Demo para Steam / Itch.io (PC)
*Objetivo: Lograr una versión comercial impecable para jugadores de teclado/ratón y mandos.*

- [ ] **Soporte Completo de Gamepad / Controles:**
  - Integrar mapeo nativo para mandos Xbox, PlayStation y Nintendo Switch (Dual-stick shooter: stick izquierdo para movimiento, stick derecho para apuntado, gatillos para ataque y ulti).
- [ ] **Steamworks Integration:**
  - Reemplazar la dependencia de IP directa por **Steam Lobbies y Steam Datagram Relay (SDR)** utilizando Netcode Transport para Steam (Facepunch o Steamworks.NET) para invitar amigos con 1 clic desde la lista de Steam.
- [ ] **Nuevas Clases de Personajes (Completar el hexágono elemental):**
  - Gato de Planta / Tierra (Invocador de raíces y esporas venenosas).
  - Gato de Sombra / Veneno (Asesino con dagas espectrales y humo tóxico).
  - Gato de Luz / Celestial (Soporte con auras de curación y prismas de refracción).
- [ ] **Variedad de Armas secundarias en mazmorra:**
  - Además de las habilidades del personaje, permitir equipar armas arrojadizas o espadas secundarias encontradas en cofres dorados.

---

### 🎯 Hito 2: Contenido Expandido y Rejugabilidad
*Objetivo: Ampliar la profundidad roguelike para garantizar más de 50 horas de juego.*

- [ ] **Árbol de Talentos Global (Santuario del Lobby):**
  - Inversión del oro excedente recolectado entre partidas en mejoras pasivas permanentes (probabilidad crítica, velocidad de recarga, reducción de enfriamientos).
- [ ] **Modificadores de Reto (Pacto del Caos / Ascensión):**
  - Sistema de dificultad incremental similar a las Ascensiones de *Slay the Spire* o el Pacto de Castigo de *Hades* (enemigos con escudos, salas con lava periódica, jefes con habilidades adicionales).
- [ ] **Bestiario y Códice Felino:**
  - Sección en el menú principal para consultar la historia, lore felino, estadísticas de enemigos y coleccionables desbloqueados.

---

### 🎯 Hito 3: Publicación Multiplataforma (Móvil y Consolas)
*Objetivo: Expandir el alcance a audiencias móviles y consolas portátiles (Nintendo Switch / Steam Deck).*

- [ ] **Interfaz Adaptable para Pantallas Táctiles:**
  - Controles virtuales dual-stick con respuesta háptica y auto-apuntado dinámico asistido.
- [ ] **Optimización para Steam Deck (Verificación Oficial):**
  - Ajuste de escalado de interfaz a resolución 1280x800 a 60 FPS fijos con consumo menor a 8W de batería.
- [ ] **Cross-Play:**
  - Capacidad de juego cruzado entre usuarios de PC y dispositivos móviles en la misma sala.

---

## 3. Criterios de Calidad de Lanzamiento (Checklist Final)

1. **Rendimiento:** 60 FPS estables sin micro-tirones (0 stutter) durante explosiones masivas o la definitiva *Era de Hielo*.
2. **Latencia de Red:** Comportamiento fluido con pings de hasta 120ms sin teletransportaciones erráticas gracias a la interpolación en `ClientNetworkTransform`.
3. **Estabilidad:** 0 excepciones no controladas (`NullReferenceException` o `MissingReferenceException`) registradas en el log durante runs completas de más de 30 minutos.
4. **Claridad Visual:** Contornos definidos y paletas desaturadas de fondo para que los proyectiles y proyectiles enemigos siempre resalten en cualquier bioma.
