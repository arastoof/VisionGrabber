using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Forms;
using System.Media;
using System.Windows.Documents;
using System.Text.RegularExpressions;
using VisionGrabber.Services;
using VisionGrabber.Models;
using VisionGrabber.Utilities;
using VisionGrabber.Backends;


namespace VisionGrabber
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml. Manages the main application lifecycle,
    /// backend orchestration, and UI updates.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly BackendManager _backendManager;
        private readonly GlobalHotkeyService _hotkeyService;
        private readonly TrayIconService _trayService;
        private RelayServer _relayServer;
        private string _lastRelayStatus = "";
        
        private bool _isExplicitExit = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class and sets up core services.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            InitializePreview();

            // Load settings
            SettingsManager.Load();

            // First run check
            if (!SettingsManager.Current.IsConfigured)
            {
                var settingsWindow = new SettingsWindow();
                settingsWindow.ShowDialog();
            }

            _backendManager = new BackendManager();
            _hotkeyService = new GlobalHotkeyService();
            _trayService = new TrayIconService();

            _hotkeyService.Initialize(this, () => 
            {
                SystemSounds.Exclamation.Play();
                TriggerCapture();
            });

            _trayService.Initialize(
                showWindow: () => ShowMainWindow(),
                captureArea: () => TriggerCapture(),
                openSettings: () => OpenSettings(),
                exitApp: () => ExitApplication()
            );
            
            _isInitialized = true;

            // Set Default Backend
            string def = SettingsManager.Current.DefaultBackend;
            if (def == "Local") BackendCombo.SelectedIndex = 0;
            else if (def == "Remote") BackendCombo.SelectedIndex = 2;
            else if (def == "Relay") BackendCombo.SelectedIndex = 3;
            else BackendCombo.SelectedIndex = 1;

            _backendManager.StartDefaultServices();
            
            InitializeRelayServer();
            UpdateStatusDisplay();
        }

        private void InitializeRelayServer()
        {
            _relayServer = new RelayServer(_backendManager, HandleRelayResult, HandleRelayStatus);
            if (SettingsManager.Current.RelayServerEnabled)
            {
                // Start llama-server for relay processing (will cleanup any existing instances)
                _backendManager.StartRelayServerBackend();
                _relayServer.Start(SettingsManager.Current.RelayServerPort);
            }
        }

        private void HandleRelayStatus(string status)
        {
            _lastRelayStatus = status;
            UpdateStatusDisplay();
        }

        private void UpdateStatusDisplay()
        {
            Dispatcher.Invoke(() =>
            {
                string baseStatus = "Ready";
                if (BackendCombo != null)
                {
                    if (BackendCombo.SelectedIndex == 0) baseStatus = "Local";
                    else if (BackendCombo.SelectedIndex == 1) baseStatus = "Cloud (Google Gemini)";
                    else if (BackendCombo.SelectedIndex == 2) baseStatus = "Networked llama-server";
                    else baseStatus = "Networked VisionGrabber Server";
                }

                string finalStatus = baseStatus;
                if (!string.IsNullOrEmpty(_lastRelayStatus))
                {
                    finalStatus = $"{baseStatus} | {_lastRelayStatus}";
                }
                
                StatusText.Text = finalStatus;
                StatusText.ToolTip = finalStatus;
            });
        }

        private void HandleRelayResult(string result, string prompt)
        {
            Dispatcher.Invoke(() =>
            {
                ShowMainWindow();
                OutputBox.Text = result;
                StatusText.Text = "Received from Relay Client";
                UpdatePreview(result);
                HistoryManager.AddEntry(result, prompt, "Relay Server");
                StatusText.ToolTip = StatusText.Text;
            });
        }

        private bool _isInitialized = false;

        private void InitializePreview()
        {
            UpdatePreview("Ready...");
        }



        private void OpenSettings()
        {
             var settingsWindow = new SettingsWindow();
             if (settingsWindow.ShowDialog() == true)
             {
                 // Re-register hotkey in case it changed
                 _hotkeyService.Register();

                 // Update Relay Server state
                 _relayServer?.Stop();
                 if (SettingsManager.Current.RelayServerEnabled)
                 {
                     // Start llama-server for relay processing (will cleanup any existing instances)
                     _backendManager.StartRelayServerBackend();
                     _relayServer.Start(SettingsManager.Current.RelayServerPort);
                 }
             }
        }

        private void BackendCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ApiKeyBox == null || _backendManager == null || !_isInitialized) return;

            bool isLocal = BackendCombo.SelectedIndex == 0;
            bool isCloud = BackendCombo.SelectedIndex == 1;
            bool isRemote = BackendCombo.SelectedIndex == 2;
            bool isRelay = BackendCombo.SelectedIndex == 3;

            if (isLocal)
            {
                ApiKeyLabel.Visibility = Visibility.Collapsed;
                ApiKeyBox.Visibility = Visibility.Collapsed;

                StatusText.Text = "Starting Local Model (Please wait)...";
                
                Task.Run(() =>
                {
                    _backendManager.Llama.StartServer();
                    UpdateStatusDisplay();
                });
            }
            else if (isCloud)
            {
                ApiKeyLabel.Visibility = Visibility.Visible;
                ApiKeyBox.Visibility = Visibility.Visible;
                
                // Pre-fill API Key from settings if available and box is empty
                if (string.IsNullOrEmpty(ApiKeyBox.Password) && !string.IsNullOrEmpty(SettingsManager.Current?.CloudApiKey))
                {
                    ApiKeyBox.Password = SettingsManager.Current.CloudApiKey;
                }

                _backendManager.Llama.StopServer();
                UpdateStatusDisplay();
            }
            else if (isRemote)
            {
                ApiKeyLabel.Visibility = Visibility.Collapsed;
                ApiKeyBox.Visibility = Visibility.Collapsed;

                _backendManager.Llama.StopServer();
                UpdateStatusDisplay();
            }
            else if (isRelay)
            {
                ApiKeyLabel.Visibility = Visibility.Collapsed;
                ApiKeyBox.Visibility = Visibility.Collapsed;

                _backendManager.Llama.StopServer();
                UpdateStatusDisplay();
            }
        }

        private async void TriggerCapture()
        {
            if (this.Visibility == Visibility.Visible)
            {
                this.WindowState = WindowState.Minimized;
                await Task.Delay(200);
            }

            var snipper = new SnippingWindow();
            snipper.Topmost = true;
            snipper.Activate();
            
            if (snipper.ShowDialog() == true)
            {
                ShowMainWindow();
                
                ThinkingBar.Visibility = Visibility.Visible; 
                StatusText.Text = "Thinking...";
                StatusText.ToolTip = "The AI is currently processing the screenshot...";
                OutputBox.IsReadOnly = true;

                string base64 = snipper.Base64Result;
                string prompt = SettingsManager.Current.CustomPrompt;

                if (string.IsNullOrWhiteSpace(prompt))
                {
                    prompt = "OCR this image. Format inline equations with single $ signs and display equations with double $$ signs. Use HTML tables. Return only the content.";
                }

                try 
                {
                    IBackend activeBackend;
                    if (BackendCombo.SelectedIndex == 0)
                    {
                        activeBackend = _backendManager.Llama;
                    }
                    else if (BackendCombo.SelectedIndex == 1)
                    {
                        if (!string.IsNullOrWhiteSpace(ApiKeyBox.Password))
                        {
                            SettingsManager.Current.CloudApiKey = ApiKeyBox.Password;
                            SettingsManager.Save(); 
                        }

                        activeBackend = _backendManager.Gemini;
                    }
                    else if (BackendCombo.SelectedIndex == 2)
                    {
                        activeBackend = _backendManager.RemoteLlama;
                    }
                    else
                    {
                        activeBackend = _backendManager.Relay;
                    }

                    string result = await activeBackend.SendImageRequest(base64, prompt);
                    OutputBox.Text = result;
                    StatusText.Text = "Done!";
                    StatusText.ToolTip = "Processing complete.";

                    // Add to History
                    string backendName = "Unknown";
                    if (activeBackend is GeminiBackend) backendName = "Gemini";
                    else if (activeBackend is LlamaBackend) backendName = "Llama";
                    else if (activeBackend is RemoteLlamaBackend) backendName = "Remote";
                    else if (activeBackend is RelayBackend) backendName = "Relay";
                    
                    HistoryManager.AddEntry(result, prompt, backendName);
                }
                catch (Exception ex)
                {
                    OutputBox.Text = "Error: " + ex.Message;
                    StatusText.Text = "Failed";
                }
                finally
                {
                    ThinkingBar.Visibility = Visibility.Hidden;
                    OutputBox.IsReadOnly = false;
                }
            }
        }

        private void History_Click(object sender, RoutedEventArgs e)
        {
            var historyWindow = new HistoryWindow();
            historyWindow.Show();
        }

        private void OutputBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdatePreview(OutputBox.Text);
        }

        private void UpdatePreview(string markdown)
        {
            if (PreviewViewer == null) return;
            PreviewViewer.Document = PreviewRenderer.RenderDocument(markdown);
        }

        private void ShowMainWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
            this.Topmost = true; 
            this.Topmost = false;
            this.Focus();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            OpenSettings();
        }

        private void Capture_Click(object sender, RoutedEventArgs e) => TriggerCapture();
        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(OutputBox.Text))
            {
                System.Windows.Clipboard.SetText(OutputBox.Text);
                StatusText.Text = "Copied to Clipboard";
                Task.Delay(2000).ContinueWith(_ => Dispatcher.Invoke(() => StatusText.Text = "Ready"));
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ExitApplication()
        {
            _isExplicitExit = true;
            this.Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!_isExplicitExit)
            {
                e.Cancel = true;
                this.Hide();
            }
            else
            {
                _hotkeyService.Dispose();
                _trayService.Dispose();
                _backendManager.StopAll();
                _relayServer?.Stop();
                base.OnClosing(e);
            }
        }

    }
}
