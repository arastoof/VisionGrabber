using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using Microsoft.Win32;
using VisionGrabber.Utilities;
using VisionGrabber.Models;

namespace VisionGrabber
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml. Handles application configuration,
    /// shortcut management, and backend settings.
    /// </summary>
    public partial class SettingsWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsWindow"/> class.
        /// </summary>
        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            var settings = SettingsManager.Current;

            // General
            if (settings.DefaultBackend == "Local") DefaultBackendCombo.SelectedIndex = 0;
            else if (settings.DefaultBackend == "Gemini" || settings.DefaultBackend == "Cloud") DefaultBackendCombo.SelectedIndex = 1;
            else if (settings.DefaultBackend == "Remote") DefaultBackendCombo.SelectedIndex = 2;
            else DefaultBackendCombo.SelectedIndex = 3;
            
            KeyChar.Text = settings.ShortcutKey;
            KeyCtrl.IsChecked = settings.ShortcutCtrl;
            KeyShift.IsChecked = settings.ShortcutShift;
            KeyAlt.IsChecked = settings.ShortcutAlt;
            KeyWin.IsChecked = settings.ShortcutWin;

            PromptBox.Text = settings.CustomPrompt;

            // Local
            LlamaPathBox.Text = settings.LocalLlamaPath;
            ModelPathBox.Text = settings.LocalModelPath;
            MmprojPathBox.Text = settings.LocalMmprojPath;
            LlamaPortBox.Text = settings.LocalLlamaPort;
            LlamaContextSizeBox.Text = settings.LocalContextSize;
            StartupCheck.IsChecked = settings.StartLlamaOnStartup;

            // Cloud
            GeminiModelBox.Text = settings.CloudModelId;
            GeminiKeyBox.Password = settings.CloudApiKey;

            // Remote
            RemoteAddressBox.Text = settings.RemoteLlamaAddress;

            // Relay
            RelayServerEnabledCheck.IsChecked = settings.RelayServerEnabled;
            RelayServerPortBox.Text = settings.RelayServerPort;
            DisplayRelayResultsCheck.IsChecked = settings.DisplayRelayResults;
            RelayClientAddressBox.Text = settings.RelayClientAddress;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var settings = SettingsManager.Current;

            // General
            if (DefaultBackendCombo.SelectedIndex == 0) settings.DefaultBackend = "Local";
            else if (DefaultBackendCombo.SelectedIndex == 1) settings.DefaultBackend = "Gemini";
            else if (DefaultBackendCombo.SelectedIndex == 2) settings.DefaultBackend = "Remote";
            else settings.DefaultBackend = "Relay";

            settings.ShortcutKey = KeyChar.Text.ToUpper();
            settings.ShortcutCtrl = KeyCtrl.IsChecked ?? false;
            settings.ShortcutShift = KeyShift.IsChecked ?? false;
            settings.ShortcutAlt = KeyAlt.IsChecked ?? false;
            settings.ShortcutWin = KeyWin.IsChecked ?? false;
            settings.CustomPrompt = PromptBox.Text;

            // Local
            settings.LocalLlamaPath = LlamaPathBox.Text;
            settings.LocalModelPath = ModelPathBox.Text;
            settings.LocalMmprojPath = MmprojPathBox.Text;
            settings.LocalLlamaPort = LlamaPortBox.Text;
            settings.LocalContextSize = LlamaContextSizeBox.Text;
            settings.StartLlamaOnStartup = StartupCheck.IsChecked ?? false;

            // Cloud
            settings.CloudModelId = GeminiModelBox.Text;
            settings.CloudApiKey = GeminiKeyBox.Password;

            // Remote
            settings.RemoteLlamaAddress = RemoteAddressBox.Text;

            // Relay
            settings.RelayServerEnabled = RelayServerEnabledCheck.IsChecked ?? false;
            settings.RelayServerPort = RelayServerPortBox.Text;
            settings.DisplayRelayResults = DisplayRelayResultsCheck.IsChecked ?? false;
            settings.RelayClientAddress = RelayClientAddressBox.Text;
            
            // Mark as configured
            settings.IsConfigured = true;

            SettingsManager.Save();
            
            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void BrowseLlama_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Executables (*.exe)|*.exe|All Files (*.*)|*.*",
                Title = "Select Llama Server Executable"
            };
            if (dialog.ShowDialog() == true)
            {
                LlamaPathBox.Text = dialog.FileName;
            }
        }

        private void BrowseModel_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "GGUF Models (*.gguf)|*.gguf|All Files (*.*)|*.*",
                Title = "Select Model File"
            };
            if (dialog.ShowDialog() == true)
            {
                ModelPathBox.Text = dialog.FileName;
            }
        }

        private void BrowseMmproj_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "GGUF Models (*.gguf)|*.gguf|All Files (*.*)|*.*",
                Title = "Select Multimedia Projector File"
            };
            if (dialog.ShowDialog() == true)
            {
                MmprojPathBox.Text = dialog.FileName;
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void CopyIP_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string localIP = "";
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    localIP = endPoint.Address.ToString();
                }

                if (string.IsNullOrEmpty(localIP))
                {
                    localIP = Dns.GetHostEntry(Dns.GetHostName())
                        .AddressList
                        .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?
                        .ToString();
                }

                if (!string.IsNullOrEmpty(localIP))
                {
                    string fullAddress = $"http://{localIP}:{RelayServerPortBox.Text}";
                    System.Windows.Clipboard.SetText(fullAddress);
                    System.Windows.MessageBox.Show($"Copied to clipboard: {fullAddress}", "Relay Server Address", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show("Could not determine local IP address.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error getting local IP: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
