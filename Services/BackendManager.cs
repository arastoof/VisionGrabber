using VisionGrabber.Backends;
using VisionGrabber.Utilities;
using VisionGrabber.Models;

namespace VisionGrabber.Services
{
    /// <summary>
    /// Manages the lifecycle and selection of different image processing backends.
    /// </summary>
    public class BackendManager
    {
        public LlamaBackend Llama { get; }
        public GeminiBackend Gemini { get; }
        public RemoteLlamaBackend RemoteLlama { get; }
        public RelayBackend Relay { get; }

        /// <summary>
        /// Initializes a new instance of the BackendManager and its constituent backends.
        /// </summary>
        public BackendManager()
        {
            Llama = new LlamaBackend();
            Gemini = new GeminiBackend();
            RemoteLlama = new RemoteLlamaBackend();
            Relay = new RelayBackend();
        }

        /// <summary>
        /// Retrieves a backend instance by its index in the selection UI.
        /// </summary>
        /// <param name="index">The UI index of the backend.</param>
        /// <returns>The corresponding IBackend instance.</returns>
        public IBackend GetBackendByIndex(int index)
        {
            if (index == 0) return Llama;
            if (index == 1) return Gemini;
            if (index == 2) return RemoteLlama;
            return Relay;
        }

        /// <summary>
        /// Retrieves the currently active backend based on application settings.
        /// </summary>
        /// <returns>The active IBackend instance.</returns>
        public IBackend GetActiveBackend()
        {
            var settings = SettingsManager.Current;
            if (settings.DefaultBackend == "Local") return Llama;
            if (settings.DefaultBackend == "Remote") return RemoteLlama;
            if (settings.DefaultBackend == "Relay") return Relay;
            return Gemini;
        }

        /// <summary>
        /// Returns the backend used for relay server processing.
        /// The relay server always uses the local Llama backend.
        /// </summary>
        public IBackend GetRelayServerBackend()
        {
            return Llama;
        }

        /// <summary>
        /// Starts the llama-server for relay server mode.
        /// Called when the relay server is enabled.
        /// </summary>
        public void StartRelayServerBackend()
        {
            Task.Run(() => Llama.StartServer());
        }

        /// <summary>
        /// Starts background services that should be running on application startup.
        /// </summary>
        public void StartDefaultServices()
        {
            if (SettingsManager.Current.StartLlamaOnStartup)
            {
                Task.Run(() => Llama.StartServer());
            }
        }

        /// <summary>
        /// Stops all running backends and clean up resources.
        /// </summary>
        public void StopAll()
        {
            Llama.StopServer();
        }
    }
}
