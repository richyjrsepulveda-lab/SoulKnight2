using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SoulKnight.Editor
{
    public static class CortadorAudioGato
    {
        public static void ProcesarAudioEraHielo()
        {
            string rutaM4a = "Assets/Resources/Audio/SFX/EraHieloHit1.m4a";
            AssetDatabase.ImportAsset(rutaM4a, ImportAssetOptions.ForceUpdate);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(rutaM4a);
            if (clip != null)
            {
                Debug.Log($"[ProcesarAudioEraHielo] M4A cargado con éxito: {clip.length:F2}s. Convirtiendo a WAV...");
                float[] samples = new float[clip.samples * clip.channels];
                clip.GetData(samples, 0);
                GuardarComoWav("Assets/Resources/Audio/SFX/EraHieloHit1.wav", samples, clip.channels, clip.frequency);
                AssetDatabase.Refresh();
                Debug.Log("[ProcesarAudioEraHielo] ¡EraHieloHit1.wav guardado exitosamente!");
            }
            else
            {
                Debug.LogWarning("[ProcesarAudioEraHielo] Unity no pudo importar .m4a directamente. Verificando HitEraDeHielo.mp3...");
                string rutaMp3 = "Assets/Resources/Audio/SFX/HitEraDeHielo.mp3";
                AssetDatabase.ImportAsset(rutaMp3, ImportAssetOptions.ForceUpdate);
                AudioClip clipMp3 = AssetDatabase.LoadAssetAtPath<AudioClip>(rutaMp3);
                if (clipMp3 != null)
                {
                    float[] samples = new float[clipMp3.samples * clipMp3.channels];
                    clipMp3.GetData(samples, 0);
                    GuardarComoWav("Assets/Resources/Audio/SFX/EraHieloHit1.wav", samples, clipMp3.channels, clipMp3.frequency);
                    AssetDatabase.Refresh();
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
