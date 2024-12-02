using UnityEngine;
using System;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public class SecurityManager : MonoBehaviour
{
    // Encryption key management
    private static readonly string KeyPath = Path.Combine(Application.persistentDataPath, ".keystore");
    private static readonly int KeySize = 256;
    private static readonly int IVSize = 16;
    private static byte[] MasterKey;

    // Store restricted keys that should never be used in PlayerPrefs
    private static readonly HashSet<string> RestrictedKeys = new HashSet<string>
    {
        "password",
        "token",
        "secret",
        "key",
        "credential",
        "auth"
    };

    // Initialize security manager
    private void Awake()
    {
        InitializeMasterKey();
    }

    // Initialize or load master encryption key
    private static void InitializeMasterKey()
    {
        if (MasterKey == null)
        {
            if (File.Exists(KeyPath))
            {
                try
                {
                    MasterKey = File.ReadAllBytes(KeyPath);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error loading master key: {e.Message}");
                    GenerateNewMasterKey();
                }
            }
            else
            {
                GenerateNewMasterKey();
            }
        }
    }

    // Generate new master key for encryption
    private static void GenerateNewMasterKey()
    {
        try
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                MasterKey = new byte[KeySize / 8];
                rng.GetBytes(MasterKey);
                File.WriteAllBytes(KeyPath, MasterKey);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error generating master key: {e.Message}");
            throw;
        }
    }

    // Store credential securely
    public static bool StoreCredential(string key, string value)
    {
        try
        {
            // Check if this is a restricted key
            if (IsRestrictedKey(key))
            {
                Debug.LogWarning($"Attempted to store restricted key '{key}' in PlayerPrefs");
                return false;
            }

            // Encrypt the value
            byte[] encryptedData = EncryptString(value);

            // Store in secure location using the secure storage API
            string secureKey = GenerateSecureKey(key);
            string encryptedString = Convert.ToBase64String(encryptedData);

            // Store in secure preferences
            SecurePlayerPrefs.SetString(secureKey, encryptedString);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error storing credential: {e.Message}");
            return false;
        }
    }

    // Retrieve and verify credential
    public static bool VerifyCredential(string key, string expectedValue)
    {
        try
        {
            string secureKey = GenerateSecureKey(key);
            string encryptedString = SecurePlayerPrefs.GetString(secureKey);

            if (string.IsNullOrEmpty(encryptedString))
                return false;

            byte[] encryptedData = Convert.FromBase64String(encryptedString);
            string decryptedValue = DecryptString(encryptedData);

            return decryptedValue == expectedValue;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error verifying credential: {e.Message}");
            return false;
        }
    }

    // Check if key is restricted
    private static bool IsRestrictedKey(string key)
    {
        return RestrictedKeys.Any(restricted =>
            key.ToLower().Contains(restricted.ToLower()));
    }

    // Generate secure key for storage
    private static string GenerateSecureKey(string key)
    {
        using (var sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
            return Convert.ToBase64String(hashBytes);
        }
    }

    // Encrypt string data
    private static byte[] EncryptString(string plainText)
    {
        InitializeMasterKey();

        using (Aes aes = Aes.Create())
        {
            aes.KeySize = KeySize;
            aes.Key = MasterKey;
            aes.GenerateIV();

            ICryptoTransform encryptor = aes.CreateEncryptor();

            using (MemoryStream msEncrypt = new MemoryStream())
            {
                // Write the IV first
                msEncrypt.Write(aes.IV, 0, aes.IV.Length);

                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                {
                    swEncrypt.Write(plainText);
                }

                return msEncrypt.ToArray();
            }
        }
    }

    // Decrypt string data
    private static string DecryptString(byte[] cipherText)
    {
        InitializeMasterKey();

        using (Aes aes = Aes.Create())
        {
            aes.KeySize = KeySize;
            aes.Key = MasterKey;

            // Get the IV from the cipher text
            byte[] iv = new byte[IVSize];
            Array.Copy(cipherText, 0, iv, 0, iv.Length);
            aes.IV = iv;

            ICryptoTransform decryptor = aes.CreateDecryptor();

            using (MemoryStream msDecrypt = new MemoryStream(cipherText, iv.Length, cipherText.Length - iv.Length))
            using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
            {
                return srDecrypt.ReadToEnd();
            }
        }
    }
}

// Secure wrapper for PlayerPrefs
public static class SecurePlayerPrefs
{
    private static readonly string SecurePrefix = "SEC_";

    public static void SetString(string key, string value)
    {
        string secureKey = SecurePrefix + key;
        PlayerPrefs.SetString(secureKey, value);
        PlayerPrefs.Save();
    }

    public static string GetString(string key)
    {
        string secureKey = SecurePrefix + key;
        return PlayerPrefs.GetString(secureKey, null);
    }

    public static bool HasKey(string key)
    {
        string secureKey = SecurePrefix + key;
        return PlayerPrefs.HasKey(secureKey);
    }

    public static void DeleteKey(string key)
    {
        string secureKey = SecurePrefix + key;
        PlayerPrefs.DeleteKey(secureKey);
        PlayerPrefs.Save();
    }
}