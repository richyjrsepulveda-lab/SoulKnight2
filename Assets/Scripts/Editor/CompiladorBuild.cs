using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SoulKnight.Editor
{
    public static class CompiladorBuild
    {
        public static void CompilarStandaloneWindows64()
        {
            string[] escenas = new string[]
            {
                "Assets/Escenas/MenuPrincipal.unity",
                "Assets/Escenas/PedirNombre.unity",
                "Assets/Escenas/MenuJugar.unity",
                "Assets/Escenas/CrearSala.unity",
                "Assets/Escenas/UnirseSala.unity",
                "Assets/Escenas/Lobby.unity",
                "Assets/Escenas/Juego.unity"
            };

            string carpetaDestino = @"C:\Users\Ricardo\Desktop\unityJuegos\SoulKnight2_Build";
            if (!Directory.Exists(carpetaDestino))
            {
                carpetaDestino = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "soulknight2Test");
                if (!Directory.Exists(carpetaDestino)) Directory.CreateDirectory(carpetaDestino);
            }

            string rutaExe = Path.Combine(carpetaDestino, "SoulKnight2.exe");

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = escenas,
                locationPathName = rutaExe,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            Debug.Log($"[Build] Iniciando compilación de SoulKnight2 a: {rutaExe}");
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[Build] ¡Compilación exitosa! Tamaño: {summary.totalSize} bytes en {summary.totalTime.TotalSeconds:F1}s");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"[Build] Error en compilación: {summary.totalErrors} errores. Resultado: {summary.result}");
                EditorApplication.Exit(1);
            }
        }
    }
}
