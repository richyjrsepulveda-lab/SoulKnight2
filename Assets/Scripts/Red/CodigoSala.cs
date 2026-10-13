using System;
using System.Text;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Sistema de Encriptación y Ofuscación de Códigos de Sala para Soul Knight 2:
    /// Transforma la dirección IP y puerto del Host en un código secreto abstracto
    /// (ej. SK-9F8K-4A2D-7M) para no exponer IPs públicas en pantalla y facilitar la conexión.
    /// </summary>
    public static class CodigoSala
    {
        private const string Alfabeto = "23456789ABCDEFGHJKMNPQRSTUVWXYZ"; // 32 caracteres (Base32 sin 0, O, 1, I, L)
        private static readonly byte[] MascaraSecreta = new byte[] { 0x6E, 0xB4, 0x29, 0xD1, 0x5C, 0xA7 };

        /// <summary>
        /// Convierte una IP y puerto en un código secreto con formato SK-XXXX-XXXX-XX
        /// </summary>
        public static string EncriptarIP(string direccionIP, ushort puerto)
        {
            try
            {
                if (string.IsNullOrEmpty(direccionIP)) return "SK-DESCONOCIDO";

                string ipLimpia = direccionIP.Trim();
                if (ipLimpia.Contains(":"))
                {
                    ipLimpia = ipLimpia.Split(':')[0];
                }

                // Manejo de localhost
                if (ipLimpia.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                {
                    ipLimpia = "127.0.0.1";
                }

                string[] partes = ipLimpia.Split('.');
                if (partes.Length != 4)
                {
                    return "SK-INVALIDO";
                }

                byte[] bytes = new byte[6];
                for (int i = 0; i < 4; i++)
                {
                    if (!byte.TryParse(partes[i], out bytes[i])) return "SK-INVALIDO";
                }

                // Puerto en 2 bytes
                bytes[4] = (byte)(puerto >> 8);
                bytes[5] = (byte)(puerto & 0xFF);

                // Ofuscar con la máscara XOR
                for (int i = 0; i < 6; i++)
                {
                    bytes[i] ^= MascaraSecreta[i];
                }

                // Codificar a Base32 (48 bits -> 10 caracteres)
                ulong bits = 0;
                for (int i = 0; i < 6; i++)
                {
                    bits = (bits << 8) | bytes[i];
                }

                char[] chars = new char[10];
                for (int i = 9; i >= 0; i--)
                {
                    int index = (int)(bits & 0x1F);
                    chars[i] = Alfabeto[index];
                    bits >>= 5;
                }

                string rawCode = new string(chars);
                // Formato estético: SK-XXXX-XXXX-XX
                return $"SK-{rawCode.Substring(0, 4)}-{rawCode.Substring(4, 4)}-{rawCode.Substring(8, 2)}";
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CodigoSala] Error al encriptar: {ex.Message}");
                return "SK-ERROR";
            }
        }

        /// <summary>
        /// Desencripta un código secreto de vuelta a la dirección IP y puerto del host.
        /// Si el usuario introduce una IP directa (ej. 192.168.1.10:7777), la devuelve limpia.
        /// </summary>
        public static bool DesencriptarCodigo(string codigoOEntrada, out string ipHost, out ushort puerto)
        {
            ipHost = "";
            puerto = Global.Puerto;

            if (string.IsNullOrEmpty(codigoOEntrada)) return false;

            string limpio = codigoOEntrada.Trim().Replace(" ", "").ToUpperInvariant();

            // Si el usuario introdujo una IP normal o localhost directamente
            if (limpio.Contains(".") || limpio.Equals("LOCALHOST", StringComparison.OrdinalIgnoreCase))
            {
                if (limpio.Contains(":"))
                {
                    string[] p = limpio.Split(':');
                    ipHost = p[0];
                    if (ushort.TryParse(p[1], out ushort pt)) puerto = pt;
                }
                else
                {
                    ipHost = limpio;
                }
                return true;
            }

            // Quitar prefijo "SK-" o "SK" si existe
            if (limpio.StartsWith("SK-")) limpio = limpio.Substring(3);
            else if (limpio.StartsWith("SK")) limpio = limpio.Substring(2);

            // Quitar guiones
            limpio = limpio.Replace("-", "");

            if (limpio.Length != 10)
            {
                return false;
            }

            try
            {
                ulong bits = 0;
                for (int i = 0; i < 10; i++)
                {
                    int idx = Alfabeto.IndexOf(limpio[i]);
                    if (idx < 0) return false;
                    bits = (bits << 5) | (ulong)idx;
                }

                byte[] bytes = new byte[6];
                for (int i = 5; i >= 0; i--)
                {
                    bytes[i] = (byte)(bits & 0xFF);
                    bits >>= 8;
                }

                // Revertir máscara XOR
                for (int i = 0; i < 6; i++)
                {
                    bytes[i] ^= MascaraSecreta[i];
                }

                ipHost = $"{bytes[0]}.{bytes[1]}.{bytes[2]}.{bytes[3]}";
                puerto = (ushort)((bytes[4] << 8) | bytes[5]);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CodigoSala] Error al desencriptar: {ex.Message}");
                return false;
            }
        }
    }
}
