using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SoulKnight.Editor
{
    public static class CortadorAudioGato
    {
        public static void AnalizarRayoHielo()
        {
            string rutaRelativa = "Assets/Resources/Audio/SFX/RayoHielo.mp3";
            AssetDatabase.ImportAsset(rutaRelativa, ImportAssetOptions.ForceUpdate);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(rutaRelativa);
            if (clip == null)
            {
                Debug.LogError("[AnalizarRayoHielo] No se pudo cargar " + rutaRelativa);
                return;
            }
            Debug.Log($"[AnalizarRayoHielo] Duración={clip.length:F2}s, Canales={clip.channels}, Frecuencia={clip.frequency}, TotalSamples={clip.samples}");
            
            int channels = clip.channels;
            int frequency = clip.frequency;
            int totalSamples = clip.samples * channels;
            float[] samples = new float[totalSamples];
            clip.GetData(samples, 0);

            int ventanaSamples = frequency / 10 * channels; // 100ms
            int totalVentanas = totalSamples / ventanaSamples;
            for (int w = 0; w < totalVentanas; w++)
            {
                float max = 0f;
                int inicio = w * ventanaSamples;
                for (int i = 0; i < ventanaSamples; i++)
                {
                    float val = Mathf.Abs(samples[inicio + i]);
                    if (val > max) max = val;
                }
                if (max > 0.05f)
                {
                    Debug.Log($"[AnalizarRayoHielo] Tiempo {w * 0.1f:F1}s: Max={max:F3}");
                }
            }
        }

        public static void CortarAudio3Tiempos()
        {
            string rutaRelativa = "Assets/Resources/Audio/SFX/gatoHit3Tiempos.mp3";
            AssetDatabase.ImportAsset(rutaRelativa, ImportAssetOptions.ForceUpdate);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(rutaRelativa);

            if (clip == null)
            {
                Debug.LogError("[CortadorAudioGato] No se pudo cargar " + rutaRelativa);
                return;
            }

            int channels = clip.channels;
            int frequency = clip.frequency;
            int totalSamples = clip.samples * channels;
            float[] samples = new float[totalSamples];
            clip.GetData(samples, 0);

            Debug.Log($"[CortadorAudioGato] Clip cargado: Duración={clip.length:F2}s, Canales={channels}, Frecuencia={frequency}, TotalSamples={clip.samples}");

            // Analizar energía en ventanas de 25ms para encontrar los 3 picos / ráfagas
            int ventanaSamples = frequency / 40 * channels;
            int totalVentanas = totalSamples / ventanaSamples;
            float[] energiaVentana = new float[totalVentanas];
            float maxEnergia = 0f;

            for (int w = 0; w < totalVentanas; w++)
            {
                float suma = 0f;
                int inicio = w * ventanaSamples;
                for (int i = 0; i < ventanaSamples; i++)
                {
                    float val = Mathf.Abs(samples[inicio + i]);
                    if (val > suma) suma = val;
                }
                energiaVentana[w] = suma;
                if (suma > maxEnergia) maxEnergia = suma;
            }

            Debug.Log($"[CortadorAudioGato] Max energía = {maxEnergia}");

            // Identificar los 3 tramos activos separados por valles de silencio
            // Umbral de silencio relativo
            float umbral = Mathf.Max(0.04f, maxEnergia * 0.12f);
            List<(int startSample, int endSample)> tramos = new List<(int, int)>();

            bool enTramo = false;
            int tramoInicio = 0;
            int silencioConsecutivo = 0;
            int umbralSilencioVentanas = 8; // ~200ms de silencio para considerar fin de tramo

            for (int w = 0; w < totalVentanas; w++)
            {
                float e = energiaVentana[w];
                if (e >= umbral)
                {
                    if (!enTramo)
                    {
                        enTramo = true;
                        tramoInicio = Mathf.Max(0, (w - 2) * ventanaSamples);
                    }
                    silencioConsecutivo = 0;
                }
                else
                {
                    if (enTramo)
                    {
                        silencioConsecutivo++;
                        if (silencioConsecutivo >= umbralSilencioVentanas || w == totalVentanas - 1)
                        {
                            enTramo = false;
                            int tramoFin = Mathf.Min(totalSamples, (w - silencioConsecutivo + 3) * ventanaSamples);
                            if (tramoFin - tramoInicio > frequency * channels * 0.15f) // al menos 150ms
                            {
                                tramos.Add((tramoInicio, tramoFin));
                                Debug.Log($"[CortadorAudioGato] Tramo detectado: {tramoInicio / (float)(frequency * channels):F2}s a {tramoFin / (float)(frequency * channels):F2}s");
                            }
                            silencioConsecutivo = 0;
                        }
                    }
                }
            }

            // Si por alguna razón de ruido continuo no se separaron exactamente en 3, dividir el audio en 3 tercios equitativos con recorte
            if (tramos.Count < 3)
            {
                Debug.LogWarning($"[CortadorAudioGato] Se detectaron {tramos.Count} tramos por umbral. Usando división en 3 tercios de tiempos.");
                tramos.Clear();
                int tercio = totalSamples / 3;
                for (int t = 0; t < 3; t++)
                {
                    tramos.Add((t * tercio, (t + 1) * tercio));
                }
            }

            // Tomar los 3 primeros / más significativos tramos
            string carpetaSalida = "Assets/Resources/Audio/SFX";
            for (int i = 0; i < 3 && i < tramos.Count; i++)
            {
                int start = tramos[i].startSample;
                int end = tramos[i].endSample;
                int length = end - start;

                float[] slice = new float[length];
                Array.Copy(samples, start, slice, 0, length);

                string nombreArchivo = $"GatoDano{i + 1}.wav";
                string rutaGuardado = Path.Combine(carpetaSalida, nombreArchivo);
                GuardarComoWav(rutaGuardado, slice, channels, frequency);
                Debug.Log($"[CortadorAudioGato] ¡Guardado exitosamente: {nombreArchivo}! ({length / (float)(frequency * channels):F2}s)");
            }

            AssetDatabase.Refresh();
        }

        private static void GuardarComoWav(string rutaArchivo, float[] samples, int channels, int frequency)
        {
            using (var fs = new FileStream(rutaArchivo, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                int sampleCount = samples.Length;
                int byteRate = frequency * channels * 2;
                short blockAlign = (short)(channels * 2);

                // Header RIFF
                bw.Write(new char[4] { 'R', 'I', 'F', 'F' });
                bw.Write(36 + sampleCount * 2);
                bw.Write(new char[4] { 'W', 'A', 'V', 'E' });

                // Subchunk "fmt "
                bw.Write(new char[4] { 'f', 'm', 't', ' ' });
                bw.Write(16); // Tamaño subchunk PCM
                bw.Write((short)1); // AudioFormat = 1 (PCM)
                bw.Write((short)channels);
                bw.Write(frequency);
                bw.Write(byteRate);
                bw.Write(blockAlign);
                bw.Write((short)16); // BitsPerSample

                // Subchunk "data"
                bw.Write(new char[4] { 'd', 'a', 't', 'a' });
                bw.Write(sampleCount * 2);

                for (int i = 0; i < sampleCount; i++)
                {
                    short pcm = (short)Mathf.Clamp(samples[i] * 32767f, -32768f, 32767f);
                    bw.Write(pcm);
                }
            }
        }
    }
}
