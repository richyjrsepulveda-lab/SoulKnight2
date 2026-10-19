using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Sistema de comandos y códigos de Administrador / Desarrollador.
    /// Permite testear habilidades, niveles, maestrías, vida y salas sin fricción.
    /// Accesible desde el Menú de Opciones en Lobby, Menú Principal y en plena Run.
    /// </summary>
    public static class ComandosAdmin
    {
        public static bool ModoCeroCooldowns = false;
        public static bool ModoSinEnemigosEnSalas = false;

        public static string EjecutarComando(string comandoRaw)
        {
            if (string.IsNullOrWhiteSpace(comandoRaw))
                return "Introduce un código o comando válido.";

            string[] partes = comandoRaw.Trim().ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length == 0) return "";

            string cmd = partes[0];
            string arg = partes.Length > 1 ? partes[1] : "";

            try
            {
                switch (cmd)
                {
                    case "nivel":
                    case "lvl":
                    case "level":
                    case "subirnivel":
                    {
                        int cant = 1;
                        if (!string.IsNullOrEmpty(arg) && int.TryParse(arg, out int parsed)) cant = parsed;
                        else if (cmd == "subirnivel") cant = 1;

                        int aplicados = 0;
                        foreach (var pj in Personaje.Todos)
                        {
                            if (pj == null) continue;
                            var nivel = pj.GetComponent<NivelEnPartida>();
                            if (nivel != null)
                            {
                                int nuevoNivel = Mathf.Clamp(nivel.NivelActual.Value + cant, 1, NivelEnPartida.NivelMaximo);
                                nivel.NivelActual.Value = nuevoNivel;
                                aplicados++;
                            }
                        }
                        return aplicados > 0 
                            ? $"✓ Nivel de run modificado (+{cant} niveles aplicados)." 
                            : "✓ Nivel aplicado (se reflejará al iniciar o entrar con tu héroe).";
                    }

                    case "maxlvl":
                    case "maxnivel":
                    case "nivelmax":
                    {
                        foreach (var pj in Personaje.Todos)
                        {
                            if (pj == null) continue;
                            var nivel = pj.GetComponent<NivelEnPartida>();
                            if (nivel != null)
                            {
                                nivel.NivelActual.Value = NivelEnPartida.NivelMaximo;
                            }
                        }
                        return "✓ ¡Nivel máximo de Run (Nivel 10) otorgado!";
                    }

                    case "unlockall":
                    case "desbloqueartodo":
                    case "desbloquear":
                    case "allskills":
                    {
                        // Otorga maestría máxima a todos los héroes
                        GestorMaestria.AgregarPuntos(TipoPersonaje.Fuego, GestorMaestria.XP_MAXIMA_MAESTRIA);
                        GestorMaestria.AgregarPuntos(TipoPersonaje.Hielo, GestorMaestria.XP_MAXIMA_MAESTRIA);
                        GestorMaestria.AgregarPuntos(TipoPersonaje.Electrico, GestorMaestria.XP_MAXIMA_MAESTRIA);

                        // Y sube el nivel de run a 10
                        foreach (var pj in Personaje.Todos)
                        {
                            if (pj == null) continue;
                            var nivel = pj.GetComponent<NivelEnPartida>();
                            if (nivel != null) nivel.NivelActual.Value = NivelEnPartida.NivelMaximo;
                        }
                        return "✓ ¡TODO DESBLOQUEADO! Maestría Máxima (Nivel 20) en todos los héroes y todas las habilidades disponibles.";
                    }

                    case "maestria":
                    case "xpmaestria":
                    case "puntosmaestria":
                    {
                        int pts = 500;
                        if (!string.IsNullOrEmpty(arg) && int.TryParse(arg, out int parsed)) pts = parsed;

                        GestorMaestria.AgregarPuntos(TipoPersonaje.Fuego, pts);
                        GestorMaestria.AgregarPuntos(TipoPersonaje.Hielo, pts);
                        GestorMaestria.AgregarPuntos(TipoPersonaje.Electrico, pts);
                        return $"✓ +{pts} Puntos de Maestría otorgados a todos los personajes.";
                    }

                    case "maxmaestria":
                    case "maestriamax":
                    {
                        GestorMaestria.AgregarPuntos(TipoPersonaje.Fuego, GestorMaestria.XP_MAXIMA_MAESTRIA);
                        GestorMaestria.AgregarPuntos(TipoPersonaje.Hielo, GestorMaestria.XP_MAXIMA_MAESTRIA);
                        GestorMaestria.AgregarPuntos(TipoPersonaje.Electrico, GestorMaestria.XP_MAXIMA_MAESTRIA);
                        return "✓ ¡Maestría nivel 20 (Máxima) otorgada a Fuego, Hielo y Eléctrico!";
                    }

                    case "resetlvl":
                    case "resetnivel":
                    case "reiniciarnivel":
                    {
                        foreach (var pj in Personaje.Todos)
                        {
                            if (pj == null) continue;
                            var nivel = pj.GetComponent<NivelEnPartida>();
                            if (nivel != null)
                            {
                                nivel.NivelActual.Value = 1;
                                nivel.XPActual.Value = 0;
                                nivel.XPSiguienteNivel.Value = 100;
                                TextoFlotante.Crear(pj.transform.position + Vector3.up * 2f, "¡NIVEL 1!", Color.white);
                            }
                        }
                        return "✓ Nivel de run reiniciado a Nivel 1 (0 XP).";
                    }

                    case "resetmaestria":
                    case "resetmastery":
                    case "reiniciarmaestria":
                    {
                        PlayerPrefs.DeleteKey("maestria_xp_Fuego");
                        PlayerPrefs.DeleteKey("maestria_xp_Hielo");
                        PlayerPrefs.DeleteKey("maestria_xp_Electrico");
                        PlayerPrefs.Save();
                        GestorMaestria.AgregarPuntos(TipoPersonaje.Fuego, 0);
                        GestorMaestria.AgregarPuntos(TipoPersonaje.Hielo, 0);
                        GestorMaestria.AgregarPuntos(TipoPersonaje.Electrico, 0);
                        return "✓ Maestría de todos los personajes reiniciada a Nivel 1 (0 XP).";
                    }

                    case "resetpoderes":
                    case "resetmejoras":
                    case "resetupgrades":
                    case "resetrun":
                    {
                        foreach (var pj in Personaje.Todos)
                        {
                            if (pj == null) continue;
                            pj.ResetearPoderesRun();
                            TextoFlotante.Crear(pj.transform.position + Vector3.up * 2f, "¡PODERES REINICIADOS!", Color.white);
                        }
                        if (HUDJugador.Instancia != null)
                        {
                            HUDJugador.Instancia.LimpiarEfectosConsumibles();
                        }
                        return "✓ Todos los poderes, stats y mejoras acumuladas en la run han sido reiniciadas a valores base.";
                    }

                    case "resetall":
                    case "reiniciartodo":
                    {
                        foreach (var pj in Personaje.Todos)
                        {
                            if (pj == null) continue;
                            var nivel = pj.GetComponent<NivelEnPartida>();
                            if (nivel != null)
                            {
                                nivel.NivelActual.Value = 1;
                                nivel.XPActual.Value = 0;
                                nivel.XPSiguienteNivel.Value = 100;
                            }
                            pj.ResetearPoderesRun();
                        }
                        PlayerPrefs.DeleteKey("maestria_xp_Fuego");
                        PlayerPrefs.DeleteKey("maestria_xp_Hielo");
                        PlayerPrefs.DeleteKey("maestria_xp_Electrico");
                        PlayerPrefs.Save();
                        GestorMaestria.AgregarPuntos(TipoPersonaje.Fuego, 0);
                        GestorMaestria.AgregarPuntos(TipoPersonaje.Hielo, 0);
                        GestorMaestria.AgregarPuntos(TipoPersonaje.Electrico, 0);
                        if (HUDJugador.Instancia != null)
                        {
                            HUDJugador.Instancia.LimpiarEfectosConsumibles();
                        }
                        return "✓ ¡RESETEO COMPLETO! Nivel 1, Maestría 1 y Poderes de run devueltos a valores iniciales.";
                    }

                    case "god":
                    case "inmortal":
                    {
                        foreach (var pj in Personaje.Todos)
                        {
                            if (pj == null) continue;
                            pj.vidaMax = 9999;
                            pj.VidaMaxSincronizada.Value = 9999;
                            pj.VidaActual.Value = 9999;
                            pj.ReduccionDano.Value = 0.95f; // 95% resistencia
                        }
                        return "✓ ¡MODO DIOS ACTIVADO! (9999 Vida y 95% reducción de daño).";
                    }

                    case "curar":
                    case "heal":
                    case "vidamax":
                    {
                        foreach (var pj in Personaje.Todos)
                        {
                            if (pj == null) continue;
                            pj.CurarTotalmente();
                        }
                        return "✓ Vida restaurada al 100%.";
                    }

                    case "killall":
                    case "matartodos":
                    case "limpiarsala":
                    {
                        int eliminados = 0;
                        for (int i = EnemigoBase.Todos.Count - 1; i >= 0; i--)
                        {
                            var enemigo = EnemigoBase.Todos[i];
                            if (enemigo != null && enemigo.gameObject.activeInHierarchy)
                            {
                                enemigo.RecibirDano(999999);
                                eliminados++;
                            }
                        }
                        return $"✓ {eliminados} enemigos eliminados de la sala.";
                    }

                    case "skip":
                    case "nextlevel":
                    case "pasarnivel":
                    case "saltarnivel":
                    {
                        if (GestorPartida.Instancia != null)
                        {
                            GestorPartida.Instancia.ActivarPasajeSiguienteNivel();
                            return "✓ Pasaje / Portal al siguiente nivel activado.";
                        }
                        return "El salto de nivel solo está disponible durante una Run en mazmorra.";
                    }

                    case "skipsalas":
                    case "saltarsalas":
                    case "clearsalas":
                    case "noenemigos":
                    case "paz":
                    {
                        ModoSinEnemigosEnSalas = true;
                        int salasContadas = 0;
                        var salas = UnityEngine.Object.FindObjectsOfType<ControladorSala>();
                        foreach (var sala in salas)
                        {
                            if (sala == null) continue;
                            sala.despejada = true;
                            sala.enCombate = false;
                            sala.BloquearSala(false);
                            sala.EncenderAntorchas(true);
                            sala.AbrirMuroSalidaJefe();
                            salasContadas++;
                        }

                        int eliminados = 0;
                        for (int i = EnemigoBase.Todos.Count - 1; i >= 0; i--)
                        {
                            var enemigo = EnemigoBase.Todos[i];
                            if (enemigo != null && enemigo.gameObject.activeInHierarchy)
                            {
                                enemigo.RecibirDano(999999);
                                eliminados++;
                            }
                        }

                        if (GestorPartida.Instancia != null)
                        {
                            GestorPartida.Instancia.ActivarPasajeSiguienteNivel();
                        }

                        return $"✓ ¡Todas las salas del nivel saltadas ({salasContadas} salas)! No saldrán más enemigos al entrar.";
                    }

                    case "0cd":
                    case "nocd":
                    case "zerocd":
                    case "cerocd":
                    case "infinitocd":
                    {
                        ModoCeroCooldowns = !ModoCeroCooldowns;
                        if (ModoCeroCooldowns)
                        {
                            foreach (var pj in Personaje.Todos)
                            {
                                if (pj == null) continue;
                                pj.ReiniciarCooldowns();
                            }
                            return "✓ ¡MODO 0 COOLDOWNS ACTIVADO! Habilidades sin ningún tiempo de espera.";
                        }
                        else
                        {
                            return "✓ Modo 0 Cooldowns desactivado. Cooldowns normales restaurados.";
                        }
                    }

                    case "resetcd":
                    case "cooldown":
                    {
                        foreach (var pj in Personaje.Todos)
                        {
                            if (pj == null) continue;
                            pj.ReiniciarCooldowns();
                        }
                        return "✓ Cooldowns de todas las habilidades reiniciados a 0.";
                    }

                    case "speed":
                    case "velocidad":
                    {
                        float factor = 1.6f;
                        if (!string.IsNullOrEmpty(arg) && float.TryParse(arg, out float p)) factor = p;
                        foreach (var pj in Personaje.Todos)
                        {
                            if (pj == null) continue;
                            pj.MultiplicadorVelocidadLoot.Value = factor;
                        }
                        return $"✓ Velocidad multiplicada por {factor:F1}x.";
                    }

                    case "dano":
                    case "damage":
                    {
                        float factor = 3.0f;
                        if (!string.IsNullOrEmpty(arg) && float.TryParse(arg, out float p)) factor = p;
                        foreach (var pj in Personaje.Todos)
                        {
                            if (pj == null) continue;
                            pj.MultiplicadorDanoLoot.Value = factor;
                        }
                        return $"✓ Daño multiplicado por {factor:F1}x.";
                    }

                    case "ayuda":
                    case "help":
                    case "comandos":
                    case "codes":
                    {
                        return "Comandos disponibles:\n" +
                               "• unlockall -> Desbloquea todas las habilidades y maestría 20\n" +
                               "• nivel <n> / maxlvl -> Sube niveles de run (1-10)\n" +
                               "• maestria <n> / maxmaestria -> Otorga puntos de maestría\n" +
                               "• god / curar -> Modo Dios o curación completa\n" +
                               "• killall -> Elimina a todos los enemigos de la sala\n" +
                               "• nextlevel -> Abre el portal al siguiente piso\n" +
                               "• speed <n> / dano <n> -> Modifica velocidad o daño\n" +
                               "• resetcd -> Reinicia todos los cooldowns";
                    }

                    default:
                        return $"Código '{comandoRaw}' no reconocido. Escribe 'ayuda' para ver la lista de comandos.";
                }
            }
            catch (Exception ex)
            {
                return $"Error ejecutando comando: {ex.Message}";
            }
        }
    }
}
