using System;

namespace VisionGrabber.Models
{
    /// <summary>
    /// Represents the application's configuration settings.
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Gets or sets the default backend to use. 
        /// Options include: "Local", "Gemini", "Remote", or "Relay".
        /// </summary>
        public string DefaultBackend { get; set; } = "Gemini";

        #region Local Llama Settings
        public string LocalLlamaPath { get; set; } = "";
        public string LocalModelPath { get; set; } = "";
        public string LocalMmprojPath { get; set; } = "";
        public string LocalLlamaPort { get; set; } = "8081";
        public string LocalContextSize { get; set; } = "2048";
        public bool StartLlamaOnStartup { get; set; } = false;
        #endregion
        
        #region Cloud Settings
        public string CloudModelId { get; set; } = "gemini-2.0-flash-lite";

        /// <summary>
        /// Gets or sets the cloud API key. Stored encrypted in storage.
        /// </summary>
        public string CloudApiKey { get; set; } = "";
        #endregion
        
        #region Remote Llama Settings
        public string RemoteLlamaAddress { get; set; } = "http://127.0.0.1:8081";
        #endregion

        #region Relay Settings
        public bool RelayServerEnabled { get; set; } = false;
        public string RelayServerPort { get; set; } = "8082";
        public bool DisplayRelayResults { get; set; } = true;
        public string RelayClientAddress { get; set; } = "http://127.0.0.1:8082";
        #endregion

        /// <summary>
        /// Gets or sets whether the initial configuration has been completed.
        /// </summary>
        public bool IsConfigured { get; set; } = false;

        #region Hotkey Settings
        public string ShortcutKey { get; set; } = "T";
        public bool ShortcutAlt { get; set; } = true;
        public bool ShortcutShift { get; set; } = true;
        public bool ShortcutCtrl { get; set; } = false;
        public bool ShortcutWin { get; set; } = false;
        #endregion

        /// <summary>
        /// Gets or sets the custom induction prompt used for OCR processing.
        /// </summary>
        public string CustomPrompt { get; set; } = "OCR this image. Format inline equations with single $ signs and display equations with double $$ signs. Use HTML for tables. Return only the content.";
    }
}
