# Documento de Diseño de Juego (GDD) - SoulKnight 2 🎮

Este documento recopila las mecánicas de juego, personajes jugables, habilidades, enemigos, jefes y balance matemático de **SoulKnight 2**.

---

## 1. Clases de Personajes Jugables (Héroes Felinos)

Cada personaje cuenta con estadísticas base, un ataque básico (LMB), dos habilidades de combate (E, Q) y una habilidad definitiva (R) desbloqueable en la run:

### 1.1 Gato de Fuego (Luchador de Área / Piromante)
- **Vida Base:** 110 HP | **Velocidad:** 6.0 | **Rol:** Daño constante en cono y control zonal.
- **Ataque Básico (LMB):** *Bola de Fuego*. Lanza proyectiles ardientes que explotan al impactar a enemigos o muros, infligiendo daño radial.
- **Habilidad 1 (E):** *Lanzallamas*. Emisión continua de llamas en cono frente al personaje que quema progresivamente a todos los objetivos en alcance.
- **Habilidad 2 (Q):** *Armadura de Fuego / Rastro Ardiente*. Otorga reducción de daño temporal y deja una estela de llamas en el suelo (`RastroFuego.cs`) que quema a los enemigos que la pisan y a los atacantes cuerpo a cuerpo.
- **Habilidad Definitiva (R):** *Supernova Felina*. Detonación volcánica en 360 grados que repele enemigos, genera proyectiles triples de magma y carboniza el área de impacto.

### 1.2 Gato de Hielo (Control de Masas / Francotirador)
- **Vida Base:** 95 HP | **Velocidad:** 6.2 | **Rol:** Ralentización, congelamiento y daño penetrante lineal.
- **Ataque Básico (LMB):** *Fragmento de Hielo*. Disparo veloz de esquirlas congelantes con probabilidad de ralentizar.
- **Habilidad 1 (E):** *Rayo de Hielo Continuo*. Haz láser continuo de escarcha (`RayoHielo`) calibrado para salir desde el hocico del gato, perforando enemigos en línea recta con daño continuo e inducción de estado congelado.
- **Habilidad 2 (Q):** *Escudo de Escarcha / Nova Gélida*. Escudo protector que absorbe daño y estalla en un anillo helado al agotarse.
- **Habilidad Definitiva (R):** *Era de Hielo* (`ZonaEraHielo.cs`). Invoca una tormenta de ventisca masiva procedural de radio 10.0 con partículas en espiral. Congela de inmediato a todos los enemigos en el área y les inflige daño sostenido por segundo escalado con el nivel del héroe.

### 1.3 Gato Eléctrico (Asesino Ágil / Alta Movilidad)
- **Vida Base:** 85 HP | **Velocidad:** 7.0 | **Rol:** Ráfagas de daño crítico, evasión y reposicionamiento instantáneo.
- **Ataque Básico (LMB):** *Tajo de Plasma / Zarpazo Chispeante*. Ataque veloz cuerpo a cuerpo que salta de un objetivo a otro cercano con arcos de plasma.
- **Habilidad 1 (E):** *Descarga Eléctrica / Rayo de Plasma*. Disparo de alto voltaje que aturde momentáneamente al objetivo principal.
- **Habilidad 2 (Q):** *Teletransporte con Rayo Divino Dual* (`EfectoRayoTeleport.cs`). El gato se desvanece de su posición original e impacta instantáneamente en la posición del cursor; un relámpago celestial golpea el punto de origen y otro el de destino, dañando a todos los enemigos adyacentes.
- **Habilidad Definitiva (R):** *Tempestad de Chispas / Dash Estilo Zenitsu*. Desplazamiento hiper-rápido invulnerable que atraviesa la sala cortando a todos los enemigos en trayectoria con daño crítico masivo.

---

## 2. Progresión y Escalado

### 2.1 Nivel en Partida (Run Actual: Niveles 1 al 10)
- **Mecánica:** Todo héroe arranca la run en **Nivel 1** con 0 XP.
- **Obtención de XP:** Al derrotar enemigos normales, élites y jefes.
- **Fórmula de Daño:** Cada nivel otorga un multiplicador de daño global:
  $$\text{Multiplicador} = 1.0 + (\text{NivelActual} - 1) \times 0.12$$
  *(Un héroe en Nivel 10 inflige un +108% de daño base en todas sus habilidades).*
- **Desbloqueo de Habilidades:**
  - Nivel 1: Ataque básico (LMB).
  - Nivel 2: Primera habilidad (E).
  - Nivel 4: Segunda habilidad (Q).
  - Nivel 6: Habilidad Definitiva / Ultimate (R).

### 2.2 Maestría Permanente (Metaprogresión: Niveles 1 al 20)
- **Mecánica:** Al finalizar una sala o run, un porcentaje de la experiencia obtenida se suma a la **Maestría del Personaje** jugado.
- **Tope Máximo:** Nivel 20 de Maestría.
- **Beneficio:** Habilita variantes de habilidades en el catálogo de loadout del Lobby para personalizar el estilo de juego antes de entrar a la mazmorra.

---

## 3. Economía y Consumibles Felinos

### 3.1 Pescados Curativos
En lugar de pociones genéricas de alquimia, el juego utiliza pescados temáticos:
- **Poco Pescado:** Resto esquelético que conserva solo la cabeza. Cura **+20 HP**.
- **Medio Pescado:** Mitad del cuerpo en buen estado y la otra esqueleto. Cura **+50 HP**.
- **Pescado Completo:** Pescado fresco entero de gran calidad. Cura **+80 HP**.

### 3.2 Moneda y Tienda Secreta (`PuestitoTienda.cs`)
- Los enemigos derrotados sueltan monedas de oro (`RecogerOro`).
- En salas intermedias de la mazmorra aparece el puesto de tienda atendido por un mercader felino.
- Los jugadores pueden acercarse y pulsar **F** para abrir la tienda interactiva y adquirir mejoras temporales de la run, aumento de vida máxima o pescados de restauración.

---

## 4. Estructura de la Mazmorra y Jefes

Una run completa se estructura en **3 Recámaras temáticas (10 Niveles en total)**:

| Nivel | Recámara | Tipo de Sala Final | Jefe o Encuentro |
|---|---|---|---|
| **Nivel 1** | Recámara 1 | Cámara de Guardianes | 3 Oleadas de enemigos élite |
| **Nivel 2** | Recámara 1 | Cámara de Guardianes | 3 Oleadas pesadas reforzadas |
| **Nivel 3** | Recámara 1 | Sala de Jefe | **Jefe Slime Rey** |
| **Nivel 4** | Recámara 2 | Cámara de Guardianes | Guardianes élite del nuevo bioma |
| **Nivel 5** | Recámara 2 | Cámara de Guardianes | Oleadas continuas reforzadas |
| **Nivel 6** | Recámara 2 | Sala de Jefe | **Jefe Gran Brujo** |
| **Nivel 7** | Recámara 3 | Cámara de Guardianes | Guardianes oscuros con trampas zonales |
| **Nivel 8** | Recámara 3 | Cámara de Guardianes | Oleadas dobles coordinadas |
| **Nivel 9** | Recámara 3 | Cámara de Guardianes | Emboscada de élites antes del clímax |
| **Nivel 10** | Recámara 3 | Sala de Jefe Final | **Jefe Golem Colosal Ancestral** |
| **Nivel 10.1** | Santuario de Decisión | Elección de Destino | **Portal Bucle Infinito (Loop 1+)** o **Portal Salir al Lobby** |

---

## 5. Comportamiento de Jefes (Estilo Souls-like 2D)
- Poseen una barra de vida prominente superior estilo Dark Souls (`PanelJefeDarkSouls`).
- Cambian de fase al 50% de HP con patrones acelerados de proyectiles, embestidas o invocación de súbditos.
- Música exclusiva de combate de jefes (`MusicaJefe1`) que se activa al cerrar la sala y se apaga tras derrotarlo.
