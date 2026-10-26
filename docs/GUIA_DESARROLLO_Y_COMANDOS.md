# Guía de Desarrollo y Comandos de Administración - SoulKnight 2 🛠️

Esta guía está destinada a cualquier desarrollador, agente de IA o tester que trabaje en el código de **SoulKnight 2**, explicando cómo compilar, cómo ejecutar pruebas automatizadas y cómo utilizar los comandos de desarrollador.

---

## 1. Configuración del Entorno de Desarrollo

### 1.1 Versión de Unity y Dependencias
- **Unity Editor:** `2022.3.25f1` LTS instalado en `C:\Program Files\Unity\Hub\Editor\2022.3.25f1\Editor\Unity.exe`.
- **Ruta del Proyecto en Disco:** `C:\Users\Ricardo\Desktop\unityJuegos\SoulKnight2`.
- **Ruta del Build Compilado:** `C:\Users\Ricardo\Desktop\unityJuegos\SoulKnight2_Build\SoulKnight2.exe`.

### 1.2 Regla de Oro para Ejecutar Unity desde PowerShell / CLI
Debido a que `Unity.exe` es una aplicación Win32 GUI, en PowerShell los comandos lanzados directamente regresan de inmediato a la consola sin esperar a que Unity termine. Para que cualquier script espere la finalización del proceso y capture el código de salida correctamente, **SIEMPRE utiliza `Start-Process -Wait -PassThru`**:

```powershell
$p = Start-Process -FilePath "C:\Program Files\Unity\Hub\Editor\2022.3.25f1\Editor\Unity.exe" `
    -ArgumentList "-batchmode -nographics -quit -projectPath `"C:\Users\Ricardo\Desktop\unityJuegos\SoulKnight2`" -executeMethod SoulKnight.Editor.GeneradorProyecto.CompilarBuildStandalone -logFile `"C:\Users\Ricardo\Desktop\unityJuegos\SoulKnight2\unity_build.log`"" `
    -Wait -PassThru

Write-Host "Unity ExitCode: $($p.ExitCode)"
```

---

## 2. Métodos de Compilación en Editor y Batchmode

En `Assets/Scripts/Editor/GeneradorProyecto.cs` residen los métodos autoritativos de reconstrucción y compilación:

1. **Reconstrucción Completa de Assets y Escenas:**
   - Menú en Unity: `SoulKnight > Reconstruir Todo el Proyecto`
   - Método C#: `SoulKnight.Editor.GeneradorProyecto.ReconstruirTodo()`
   - Regenera prefabs, texturas procedurales, re-serializa las 7 escenas y guarda los `EditorBuildSettings`.

2. **Compilación de Build Standalone de Windows x64:**
   - Menú en Unity: `SoulKnight > Compilar Build Standalone`
   - Método C#: `SoulKnight.Editor.GeneradorProyecto.CompilarBuildStandalone()`
   - Compila el ejecutable en `C:/Users/Ricardo/Desktop/unityJuegos/SoulKnight2_Build/SoulKnight2.exe`.

---

## 3. Pruebas Automatizadas en Runtime (`-autotest`)

El ejecutable `SoulKnight2.exe` y la clase [`Global.cs`](Assets/Scripts/Red/Global.cs) incorporan un parámetro de línea de comandos `-autotest`:

```powershell
$p = Start-Process -FilePath "C:\Users\Ricardo\Desktop\unityJuegos\SoulKnight2_Build\SoulKnight2.exe" `
    -ArgumentList "-batchmode -nographics -autotest -logfile `"C:\Users\Ricardo\Desktop\unityJuegos\SoulKnight2_Build\autotest.log`"" `
    -Wait -PassThru

Write-Host "AutoTest ExitCode: $($p.ExitCode)"
Get-Content "C:\Users\Ricardo\Desktop\unityJuegos\SoulKnight2_Build\autotest.log" | Select-String -Pattern "\[AUTOTEST\]"
```

### ¿Qué valida automáticamente `-autotest`?
1. Carga limpia de `MenuPrincipal` y presencia del botón `✎ CAMBIAR NOMBRE`.
2. Creación de sala multijugador local e inicio del host de Netcode.
3. Transición limpia a la escena `Lobby` y verificación de que existe **exactamente 1 instancia de personaje** (evitando duplicaciones o bucles recursivos).
4. Selección física de personaje felino (Hielo).
5. Inicio de partida y transición a la escena `Juego`.
6. Generación de mazmorra procedural y verificación de que el HUD (`HUDJugador`), barra de vida, barra de maestría y barra de experiencia de run (`BarraXPRunHUD`) se inicialicen e incrementen con la XP otorgada.
7. Si todo es correcto, el proceso termina automáticamente con **`ExitCode: 0`**. Si algo falla, termina con **`ExitCode: 1`** registrando el error en el log.

---

## 4. Consola de Códigos y Comandos de Desarrollador (`ComandosAdmin.cs`)

SoulKnight 2 cuenta con una consola interactiva integrada dentro del panel de **Opciones** (accesible desde el Menú Principal, en el Lobby o pausando durante la Run con la tecla **ESC**):

| Comando / Código | Argumento | Efecto |
|---|---|---|
| `nivel` / `lvl` / `subirnivel` | `[cantidad]` | Sube inmediatamente el nivel de la run actual (`+1` por defecto o la cantidad especificada). |
| `maxlvl` / `maxnivel` | *(ninguno)* | Lleva al personaje de inmediato al nivel máximo de run (**Nivel 10**). |
| `desbloqueartodo` / `allskills` | *(ninguno)* | Otorga maestría máxima (Nivel 20) a todos los héroes felinos para probar cualquier habilidad. |
| `zerocd` / `0cd` / `cooldowns` | *(ninguno)* | Alterna el modo **0 Cooldowns** (enfriamiento instantáneo en todas las habilidades). |
| `god` / `invencible` | *(ninguno)* | Activa o desactiva la invencibilidad para el personaje local. |
| `heal` / `curar` | *(ninguno)* | Restaura la salud completa del personaje inmediatamente. |
| `gold` / `oro` | `[cantidad]` | Otorga oro a los jugadores (`+500` por defecto). |
| `skip` / `saltarsala` / `next` | *(ninguno)* | Salta inmediatamente a la siguiente sala o al hoyo del siguiente nivel. |
| `resetmastery` / `resetmaestria`| *(ninguno)* | Reinicia las maestrías guardadas a Nivel 1 para testear el flujo de nuevo usuario. |
| `killall` / `matartodos` | *(ninguno)* | Elimina a todos los enemigos activos en la habitación actual. |

---

## 5. Buenas Prácticas para el Código del Proyecto

- **Nunca asignar `Sprite.Create(Texture2D.whiteTexture, ...)` en métodos del Editor:** Los sprites en memoria creados sin ser assets guardados en disco se serializan en las escenas de Unity con `texture: {fileID: 0}`, lo que provoca que los componentes `Image` de uGUI fallen y se muestren transparentes. Para imágenes planas, utiliza `Image.Type.Simple` con `sprite = null` (Unity usa internamente su textura blanca por defecto).
- **Ajuste de RectTransform para Barras de Progreso:** Al modificar el tamaño de una barra de relleno (`BarraVida.cs`), ajusta `anchorMin`, `anchorMax` y establece siempre `offsetMin` y `offsetMax` explícitos para evitar desplazamientos accidentales provocados por el cálculo interno de `anchoredPosition` de Unity.
- **Networking Autorizado:** Los cálculos de daño, transiciones de nivel y aparición de loot deben realizarse siempre con comprobación de `IsServer` para evitar desincronizaciones entre el Host y los clientes.
