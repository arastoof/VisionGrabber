using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using VisionGrabber.Models;

namespace VisionGrabber.Utilities
{
    /// <summary>
    /// Provides static methods for managing application configuration settings, including persistence and encryption of sensitive data.
    /// </summary>
    public static class SettingsManager
    {
        private static readonly string SettingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VisionGrabber");
        private static readonly string SettingsFile = Path.Combine(SettingsFolder, "settings.json");

        /// <summary>
        /// Gets the current application settings.
        /// </summary>
        public static AppSettings Current { get; private set; }

        /// <summary>
        /// Loads settings from persistent storage and decrypts sensitive data.
        /// </summary>
        public static void Load()
        {
            if (File.Exists(SettingsFile))
            {
                try
                {
                    string json = File.ReadAllText(SettingsFile);
                    Current = JsonConvert.DeserializeObject<AppSettings>(json);

                    // Decrypt API Key if present
                    if (!string.IsNullOrEmpty(Current.CloudApiKey))
                    {
                        try 
                        {
                            Current.CloudApiKey = Unprotect(Current.CloudApiKey);
                        }
                        catch
                        {
                            // If decryption fails (e.g. data from another user or machine), reset to empty
                            Current.CloudApiKey = "";
                        }
                    }
                }
                catch
                {
                    Current = new AppSettings();
                }
            }
            else
            {
                Current = new AppSettings();
            }
        }
 

        public static void Save()
        {
            if (!Directory.Exists(SettingsFolder))
            {
                Directory.CreateDirectory(SettingsFolder);
            }

            // Create a depth copy using JSON serialization to avoid encrypting the live in-memory settings object
            string currentJson = JsonConvert.SerializeObject(Current);
            var toSave = JsonConvert.DeserializeObject<AppSettings>(currentJson);

            // Encrypt API Key in the saved copy
            if (!string.IsNullOrEmpty(Current.CloudApiKey))
            {
                toSave.CloudApiKey = Protect(Current.CloudApiKey);
            }

            string json = JsonConvert.SerializeObject(toSave, Formatting.Indented);
            File.WriteAllText(SettingsFile, json);
        }

        /// <summary>
        /// Encrypts clear text using Windows DPAPI.
        /// </summary>
        private static string Protect(string clearText)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(clearText);
            byte[] protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(protectedBytes);
        }

        /// <summary>
        /// Decrypts protected text using Windows DPAPI.
        /// </summary>
        private static string Unprotect(string protectedText)
        {
            byte[] protectedBytes = Convert.FromBase64String(protectedText);
            byte[] bytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
