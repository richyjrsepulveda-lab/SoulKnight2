# SoulKnight 2 - Proyecto Unity (Editable + Build Jugable)

Repositorio oficial de **SoulKnight 2** (Cooperativo Multijugador 2D en Unity con Netcode for GameObjects).

---

## 🛠️ ¿Cómo descargar y editar este proyecto en otro PC con Unity?

Este repositorio contiene **todo el proyecto editable de Unity** (`Assets/`, `Packages/`, `ProjectSettings/`).

### Opción A: Descargar desde este repositorio (Recomendado)
1. En la página principal de GitHub ([https://github.com/richyjrsepulveda-lab/SoulKnight2](https://github.com/richyjrsepulveda-lab/SoulKnight2)), haz clic en el botón verde **`<> Code`** y selecciona **`Download ZIP`** (o clónalo con `git clone https://github.com/richyjrsepulveda-lab/SoulKnight2.git`).
2. También puedes descargar **`SoulKnight2_ProyectoEditable_Unity.zip`** directamente desde la sección de [**Releases (v1.0.0)**](https://github.com/richyjrsepulveda-lab/SoulKnight2/releases/tag/v1.0.0).
3. Descomprime la carpeta en tu PC antes de quedarte sin internet.

### Cómo abrirlo en Unity Hub:
1. Abre **Unity Hub**.
2. Haz clic en **Add (Añadir)** → **Add project from disk (Añadir proyecto desde disco)**.
3. Selecciona la carpeta descomprimida `SoulKnight2` (la carpeta que contiene `Assets`, `Packages` y `ProjectSettings`).
4. Ábrelo con **Unity 2022.3.25f1** (o cualquier versión 2022.3 LTS).
   - *Nota importante si mañana no tendrás internet*: Abre el proyecto **una primera vez mientras aún tengas internet** para que Unity descargue en caché los paquetes (`com.unity.netcode.gameobjects`) y genere la carpeta `Library/`. Una vez abierto la primera vez, podrás editarlo y compilarlo **100% sin internet**.

---

## 📂 Estructura del Proyecto Editable
- `Assets/Scripts/`: Todo el código fuente en C# (`Personajes`, `Combate`, `Enemigos`, `Jefes`, `Red`, `UI`, `Loot`, `Progresion`, `Editor`).
- `Assets/Escenas/`: Todas las escenas (`MenuPrincipal`, `PedirNombre`, `MenuJugar`, `CrearSala`, `UnirseSala`, `Lobby`, `Juego`).
- `Assets/Prefabs/` y `Assets/Resources/`: Prefabs configurados de personajes, jefes, enemigos, proyectiles y red.
- `Assets/Arte/`: Sprites e iconos del juego.
- **Menú útil en el Editor de Unity**: En la barra superior de Unity tienes el menú **`SoulKnight`** (`Reconstruir Todo` y `Compilar Build Standalone`).

---

## 🎮 Descargar solo el Juego Compilado (.exe)
En la sección [**Releases (v1.0.0)**](https://github.com/richyjrsepulveda-lab/SoulKnight2/releases/tag/v1.0.0) encontrarás dos archivos `.zip`:
1. **`SoulKnight2_Juego.zip`** → El juego ya compilado (`SoulKnight2.exe`) listo para jugar.
2. **`SoulKnight2_ProyectoEditable_Unity.zip`** → El proyecto fuente completo listo para abrir y editar en Unity.
