using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

/// <summary>
/// Clase responsable de la encriptación y desencriptación de datos
/// </summary>
public class EncryptionManager : MonoBehaviour
{
    [Header("Configuración de Encriptación")]
    [Tooltip("Clave para encriptar los datos. Debe tener longitud adecuada para AES (16, 24 o 32 bytes)")]
    [SerializeField] private string encryptionKey = "ThiSIsAVeRyS3cuReEnCrYpTionK3y!"; // 32 caracteres

    [Tooltip("Si es verdadero, guarda una copia sin encriptar junto a la encriptada (solo para desarrollo)")]
    [SerializeField] private bool guardarCopiaPlaintext = false;

    private void Awake()
    {
        Debug.Log("EncryptionManager inicializado");

        // Verificar longitud de la clave
        byte[] keyBytes = Encoding.UTF8.GetBytes(encryptionKey);
        if (keyBytes.Length != 16 && keyBytes.Length != 24 && keyBytes.Length != 32)
        {
            Debug.LogWarning($"La clave de encriptación tiene longitud {keyBytes.Length} bytes, que no es óptima para AES. Se ajustará automáticamente.");
        }
    }

    /// <summary>
    /// Encripta y guarda un JSON en el dispositivo
    /// </summary>
    /// <param name="jsonData">Datos en formato JSON para encriptar</param>
    /// <param name="rutaArchivo">Ruta donde guardar el archivo encriptado</param>
    /// <returns>True si la operación fue exitosa, False en caso contrario</returns>
    public bool GuardarJSONEncriptado(string jsonData, string rutaArchivo)
    {
        try
        {
            // Validar que tenemos datos válidos
            if (string.IsNullOrEmpty(jsonData))
            {
                Debug.LogError("No se pueden encriptar datos vacíos");
                return false;
            }

            // Intentar encriptar los datos
            string datosEncriptados = EncriptarDatos(jsonData);

            if (string.IsNullOrEmpty(datosEncriptados))
            {
                Debug.LogError("La encriptación produjo datos vacíos");
                return false;
            }

            // Asegurarnos que el directorio existe
            string directorio = Path.GetDirectoryName(rutaArchivo);
            if (!Directory.Exists(directorio))
            {
                Directory.CreateDirectory(directorio);
            }

            // Guardar los datos encriptados
            File.WriteAllText(rutaArchivo, datosEncriptados);
            Debug.Log($"Datos encriptados guardados en: {rutaArchivo}");

            // Si está habilitado, guardar también una copia sin encriptar (para desarrollo)
            if (guardarCopiaPlaintext)
            {
                string plainPath = Path.Combine(
                    Path.GetDirectoryName(rutaArchivo),
                    "plain_" + Path.GetFileName(rutaArchivo));

                File.WriteAllText(plainPath, jsonData);
                Debug.Log($"Copia sin encriptar guardada en: {plainPath}");
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al guardar JSON encriptado: {e.Message}\n{e.StackTrace}");

            // En caso de error, intentar guardar sin encriptar como plan de contingencia
            try
            {
                string plainPath = Path.Combine(
                    Path.GetDirectoryName(rutaArchivo),
                    "error_plain_" + Path.GetFileName(rutaArchivo));

                File.WriteAllText(plainPath, jsonData);
                Debug.LogWarning($"Ocurrió un error al encriptar, se guardó sin encriptar en: {plainPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"También falló el guardado sin encriptar: {ex.Message}");
            }

            return false;
        }
    }

    /// <summary>
    /// Implementación segura de encriptación AES
    /// </summary>
    private string EncriptarDatos(string textoPlano)
    {
        try
        {
            // Generar un IV aleatorio (más seguro)
            byte[] iv = new byte[16];
            using (var rng = new System.Security.Cryptography.RNGCryptoServiceProvider())
            {
                rng.GetBytes(iv);
            }

            // Convertir texto a bytes
            byte[] plainBytes = Encoding.UTF8.GetBytes(textoPlano);

            // Crear objeto para encriptar
            using (Aes aes = Aes.Create())
            {
                // Ajustar longitud de la clave (asegurar que sea 16, 24 o 32 bytes)
                byte[] keyBytes = Encoding.UTF8.GetBytes(encryptionKey);

                // Ajustar la longitud de la clave a los valores permitidos por AES
                if (keyBytes.Length > 32)
                {
                    keyBytes = keyBytes.Take(32).ToArray();
                }
                else if (keyBytes.Length < 16)
                {
                    keyBytes = keyBytes.Concat(Enumerable.Repeat((byte)0, 16 - keyBytes.Length)).ToArray();
                }
                else if (keyBytes.Length > 16 && keyBytes.Length < 24)
                {
                    keyBytes = keyBytes.Concat(Enumerable.Repeat((byte)0, 24 - keyBytes.Length)).ToArray();
                }
                else if (keyBytes.Length > 24 && keyBytes.Length < 32)
                {
                    keyBytes = keyBytes.Concat(Enumerable.Repeat((byte)0, 32 - keyBytes.Length)).ToArray();
                }

                // Configurar el objeto AES
                aes.Key = keyBytes;
                aes.IV = iv;
                aes.Padding = PaddingMode.PKCS7;
                aes.Mode = CipherMode.CBC;

                // Usar MemoryStream para guardar los resultados
                using (MemoryStream ms = new MemoryStream())
                {
                    // Primero escribir el IV (para poder desencriptar después)
                    ms.Write(iv, 0, iv.Length);

                    // Luego encriptar y escribir los datos
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(plainBytes, 0, plainBytes.Length);
                        cs.FlushFinalBlock();
                    }

                    // Convertir todo a Base64 para almacenar como texto
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error durante la encriptación: {e.Message}\n{e.StackTrace}");

            // Para permitir depuración, imprimir datos sobre la clave que usamos
            try
            {
                byte[] keyBytes = Encoding.UTF8.GetBytes(encryptionKey);
                Debug.LogError($"Detalles de la clave: Longitud={keyBytes.Length} bytes, Primeros 4 bytes={BitConverter.ToString(keyBytes.Take(4).ToArray())}");
            }
            catch { }

            return null;
        }
    }

    /// <summary>
    /// Desencripta datos previamente encriptados con el método EncriptarDatos
    /// </summary>
    public string DesencriptarDatos(string textoCifrado)
    {
        try
        {
            // Convertir Base64 a bytes
            byte[] cipherBytes = Convert.FromBase64String(textoCifrado);

            // Verificar longitud mínima (IV + al menos un bloque)
            if (cipherBytes.Length < 32)
            {
                Debug.LogError("Los datos cifrados son demasiado cortos para contener un IV válido");
                return null;
            }

            // Extraer el IV (primeros 16 bytes)
            byte[] iv = new byte[16];
            Buffer.BlockCopy(cipherBytes, 0, iv, 0, 16);

            // Extraer los datos cifrados (después del IV)
            byte[] encryptedData = new byte[cipherBytes.Length - 16];
            Buffer.BlockCopy(cipherBytes, 16, encryptedData, 0, encryptedData.Length);

            // Ajustar la longitud de la clave
            byte[] keyBytes = Encoding.UTF8.GetBytes(encryptionKey);
            if (keyBytes.Length > 32)
            {
                keyBytes = keyBytes.Take(32).ToArray();
            }
            else if (keyBytes.Length < 16)
            {
                keyBytes = keyBytes.Concat(Enumerable.Repeat((byte)0, 16 - keyBytes.Length)).ToArray();
            }
            else if (keyBytes.Length > 16 && keyBytes.Length < 24)
            {
                keyBytes = keyBytes.Concat(Enumerable.Repeat((byte)0, 24 - keyBytes.Length)).ToArray();
            }
            else if (keyBytes.Length > 24 && keyBytes.Length < 32)
            {
                keyBytes = keyBytes.Concat(Enumerable.Repeat((byte)0, 32 - keyBytes.Length)).ToArray();
            }

            // Desencriptar los datos
            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.IV = iv;
                aes.Padding = PaddingMode.PKCS7;
                aes.Mode = CipherMode.CBC;

                // Crear el objeto para desencriptar
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream ms = new MemoryStream(encryptedData))
                {
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader reader = new StreamReader(cs, Encoding.UTF8))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error durante la desencriptación: {e.Message}\n{e.StackTrace}");
            return null;
        }
    }

    /// <summary>
    /// Lee y desencripta un archivo previamente encriptado
    /// </summary>
    public string LeerArchivoEncriptado(string rutaArchivo)
    {
        try
        {
            if (!File.Exists(rutaArchivo))
            {
                Debug.LogError($"El archivo no existe: {rutaArchivo}");
                return null;
            }

            // Leer los datos encriptados
            string textoCifrado = File.ReadAllText(rutaArchivo);

            // Desencriptar y devolver
            return DesencriptarDatos(textoCifrado);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al leer archivo encriptado: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Método de prueba para verificar si la encriptación/desencriptación funciona correctamente
    /// </summary>
    public bool ProbarEncriptacion()
    {
        try
        {
            // Texto de prueba
            string textoOriginal = "{ \"prueba\": true, \"mensaje\": \"Esto es una prueba\" }";
            Debug.Log($"Texto original: {textoOriginal}");

            // Encriptar
            string encriptado = EncriptarDatos(textoOriginal);
            Debug.Log($"Texto encriptado: {encriptado}");

            // Desencriptar
            string desencriptado = DesencriptarDatos(encriptado);
            Debug.Log($"Texto desencriptado: {desencriptado}");

            // Verificar
            bool exito = textoOriginal == desencriptado;
            Debug.Log($"Prueba de encriptación: {(exito ? "EXITOSA" : "FALLIDA")}");

            return exito;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error en prueba de encriptación: {e.Message}");
            return false;
        }
    }
}