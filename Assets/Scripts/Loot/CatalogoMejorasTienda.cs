using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight
{
    public enum TipoMejoraTienda : byte
    {
        // Universales
        VidaMaxima = 0,
        PescadoAsado = 1,
        DanoUniversal = 2,
        CooldownUniversal = 3,
        AreaHabilidades = 4,
        CooldownDefinitiva = 5,
        GolpeCritico = 6,

        // Gato de Fuego
        Fuego_DanoQuemadura = 10,
        Fuego_Area = 11,
        Fuego_Duracion = 12,
        Fuego_SupernovaGigante = 13,

        // Gato de Hielo
        Hielo_DuracionCongelacion = 20,
        Hielo_AreaVentisca = 21,
        Hielo_PotenciaGlacial = 22,
        Hielo_TormentaEterna = 23,

        // Gato Eléctrico
        Elec_Voltaje = 30,
        Elec_AreaTeleport = 31,
        Elec_CooldownDash = 32,
        Elec_RayoCataclismico = 33
    }

    public static class CatalogoMejorasTienda
    {
        public static TipoPersonaje ObtenerClaseCompatible(TipoMejoraTienda tipo)
        {
            switch (tipo)
            {
                case TipoMejoraTienda.Fuego_DanoQuemadura:
                case TipoMejoraTienda.Fuego_Area:
                case TipoMejoraTienda.Fuego_Duracion:
                case TipoMejoraTienda.Fuego_SupernovaGigante:
                    return TipoPersonaje.Fuego;

                case TipoMejoraTienda.Hielo_DuracionCongelacion:
                case TipoMejoraTienda.Hielo_AreaVentisca:
                case TipoMejoraTienda.Hielo_PotenciaGlacial:
                case TipoMejoraTienda.Hielo_TormentaEterna:
                    return TipoPersonaje.Hielo;

                case TipoMejoraTienda.Elec_Voltaje:
                case TipoMejoraTienda.Elec_AreaTeleport:
                case TipoMejoraTienda.Elec_CooldownDash:
                case TipoMejoraTienda.Elec_RayoCataclismico:
                    return TipoPersonaje.Electrico;

                default:
                    return TipoPersonaje.Ninguno;
            }
        }

        public static int ObtenerCostoNivel(TipoMejoraTienda tipo, int nivelActual)
        {
            if (tipo == TipoMejoraTienda.PescadoAsado) return 20;

            switch (nivelActual)
            {
                case 0: return 25;
                case 1: return 40;
                case 2: return 60;
                case 3: return 85;
                case 4: return 120;
                default: return 999;
            }
        }

        public static void ObtenerInfoMejora(TipoMejoraTienda tipo, int nivelActual, out string nombre, out string desc, out TipoPersonaje claseReq)
        {
            claseReq = ObtenerClaseCompatible(tipo);
            switch (tipo)
            {
                case TipoMejoraTienda.VidaMaxima:
                    nombre = "CORAZÓN VITAL";
                    desc = (nivelActual >= 5)
                        ? "Vida Máxima al tope (300 HP)."
                        : $"+30 Vida Máxima Permanente (Hasta 300 HP).\nNivel actual: {nivelActual}/5";
                    break;
                case TipoMejoraTienda.PescadoAsado:
                    nombre = "PESCADO ASADO";
                    desc = "Restaura 60 Puntos de Vida de inmediato al consumirlo.";
                    break;
                case TipoMejoraTienda.DanoUniversal:
                    nombre = "PODER ARCANO";
                    desc = (nivelActual >= 5)
                        ? "Poder al nivel máximo (5/5)."
                        : $"+8% Daño General en todas tus habilidades.\nNivel actual: {nivelActual}/5";
                    break;
                case TipoMejoraTienda.CooldownUniversal:
                    nombre = "RELOJ ARCANO";
                    desc = (nivelActual >= 5)
                        ? "Enfriamiento al nivel máximo (5/5)."
                        : $"+7% Reducción de Enfriamiento en habilidades.\nNivel actual: {nivelActual}/5";
                    break;
                case TipoMejoraTienda.AreaHabilidades:
                    nombre = "AMPLITUD MÍSTICA";
                    desc = (nivelActual >= 5)
                        ? "Área al nivel máximo (5/5)."
                        : $"+12% Tamaño y área de todas tus habilidades.\nNivel actual: {nivelActual}/5";
                    break;
                case TipoMejoraTienda.CooldownDefinitiva:
                    nombre = "ENERGÍA CELESTIAL";
                    desc = (nivelActual >= 5)
                        ? "Enfriamiento de Ulti al tope (5/5)."
                        : $"+10% Reducción de recarga en tu Habilidad Definitiva.\nNivel actual: {nivelActual}/5";
                    break;
                case TipoMejoraTienda.GolpeCritico:
                    nombre = "ZARPA FULMINANTE";
                    desc = (nivelActual >= 5)
                        ? "Críticos al nivel máximo (5/5)."
                        : $"+8% Probabilidad de Golpe Crítico (1.8x daño).\nNivel actual: {nivelActual}/5";
                    break;

                // Fuego
                case TipoMejoraTienda.Fuego_DanoQuemadura:
                    nombre = "COMBUSTIÓN FEROZ";
                    desc = (nivelActual >= 5)
                        ? "Combustión al nivel máximo (5/5)."
                        : $"+6 Daño por tick a las quemaduras de fuego.\nNivel actual: {nivelActual}/5";
                    break;
                case TipoMejoraTienda.Fuego_Area:
                    nombre = "LLAMARADA EXPANSIVA";
                    desc = (nivelActual >= 5)
                        ? "Área al nivel máximo (5/5)."
                        : $"+15% Alcance y radio del lanzallamas y bombas.\nNivel actual: {nivelActual}/5";
                    break;
                case TipoMejoraTienda.Fuego_Duracion:
                    nombre = "IGNICIÓN PROLONGADA";
                    desc = (nivelActual >= 5)
                        ? "Duración al nivel máximo (5/5)."
                        : $"+1.0s Duración del fuego en suelo y armadura.\nNivel actual: {nivelActual}/5";
                    break;
                case TipoMejoraTienda.Fuego_SupernovaGigante:
                    nombre = "SUPERNOVA TITÁNICA";
                    desc = (nivelActual >= 5)
                        ? "Supernova Titánica al máximo (5/5)."
                        : $"+25% Radio y explosión de la Supernova.\nNivel actual: {nivelActual}/5";
                    break;

                // Hielo
                case TipoMejoraTienda.Hielo_DuracionCongelacion:
                    nombre = "CERO ABSOLUTO";
                    desc = (nivelActual >= 5)
                        ? "Congelación al nivel máximo (5/5)."
                        : $"+0.6s Duración de congelación y ralentización.\nNivel actual: {nivelActual}/5";
                    break;
                case TipoMejoraTienda.Hielo_AreaVentisca:
                    nombre = "ESFERA Y RAYO GLACIAL";
                    desc = (nivelActual >= 5)
                        ? "Esfera y Rayo al nivel máximo (5/5)."
                        : $"+15% Tamaño de Bola de Hielo y Alcance de Rayo Glacial.\nNivel actual: {nivelActual}/5";
                    break;
                case TipoMejoraTienda.Hielo_PotenciaGlacial:
                    nombre = "FRAGMENTACIÓN GLACIAL";
                    desc = (nivelActual >= 5)
                        ? "Potencia al nivel máximo (5/5)."
                        : $"+12% Daño contra objetivos congelados/lentos.\nNivel actual: {nivelActual}/5";
                    break;
                case TipoMejoraTienda.Hielo_TormentaEterna:
                    nombre = "VENTISCA ETERNA";
                    desc = (nivelActual >= 5)
                        ? "Ventisca Eterna al máximo (5/5)."
                        : $"+20% Radio de Era de Hielo y +1.5s duración.\nNivel actual: {nivelActual}/5";
                    break;

                // Eléctrico
                case TipoMejoraTienda.Elec_Voltaje:
                    nombre = "ALTO VOLTAJE";
                    desc = (nivelActual >= 5)
                        ? "Voltaje al nivel máximo (5/5)."
                        : $"+15% Daño y anchura de rayos y espada eléctrica.\nNivel actual: {nivelActual}/5";
                    break;
                case TipoMejoraTienda.Elec_AreaTeleport:
                    nombre = "SOBRECARGA TELEPORT";
                    desc = (nivelActual >= 5)
                        ? "Sobrecarga al nivel máximo (5/5)."
                        : $"+20% Radio de impacto eléctrico en origen/destino.\nNivel actual: {nivelActual}/5";
                    break;
                case TipoMejoraTienda.Elec_CooldownDash:
                    nombre = "HIPER-VELOCIDAD";
                    desc = (nivelActual >= 5)
                        ? "Velocidad al nivel máximo (5/5)."
                        : $"Reduce 0.3s el enfriamiento del dash eléctrico.\nNivel actual: {nivelActual}/5";
                    break;
                case TipoMejoraTienda.Elec_RayoCataclismico:
                    nombre = "RAYO CATACLÍSMICO";
                    desc = (nivelActual >= 5)
                        ? "Rayo Catacl羅smico al máximo (5/5)."
                        : $"+30% Ancho del Rayo y carga 20% más rápido.\nNivel actual: {nivelActual}/5";
                    break;

                default:
                    nombre = "MEJORA MÍSTICA";
                    desc = "Mejora especial de la run.";
                    break;
            }
        }
    }
}
