using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using VisionGrabber.Services;
using VisionGrabber.Utilities;

namespace VisionGrabber
{
    /// <summary>
    /// Implements an HTTP server that listens for and processes image requests from relay clients.
    /// </summary>
    public class RelayServer
    {
        private HttpListener _listener;
        private bool _isRunning;
        private readonly BackendManager _backendManager;
        private readonly Action<string, string> _onResultReceived;
        private readonly Action<string> _onStatusUpdate;

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayServer"/> class.
        /// </summary>
        /// <param name="backendManager">The backend manager instance.</param>
        /// <param name="onResultReceived">Callback for when a result is received.</param>
        /// <param name="onStatusUpdate">Callback for status updates.</param>
        public RelayServer(BackendManager backendManager, Action<string, string> onResultReceived, Action<string> onStatusUpdate = null)
        {
            _backendManager = backendManager;
            _onResultReceived = onResultReceived;
            _onStatusUpdate = onStatusUpdate;
        }

        public void Start(string port)
        {
            if (_isRunning) return;

            try
            {
                // Attempt to add firewall rule for the port (requires admin)
                EnsureFirewallRule(port);
                
                _listener = new HttpListener();
                
                // Use + to listen on all interfaces (requires Admin)
                _listener.Prefixes.Add($"http://+:{port}/");
                
                _listener.Start();
                _isRunning = true;
                _onStatusUpdate?.Invoke($"Relay Server: Running on Port {port}");
                
                Task.Run(() => ListenLoop());
            }
            catch (Exception ex)
            {
                string message = $"Relay Server failed to start: {ex.Message}";
                if (ex.Message.Contains("Access is denied"))
                {
                    message += "\n\nTry running the app as Administrator. This is required to listen on all network interfaces.";
                }
                
                System.Windows.MessageBox.Show(message, "Relay Server Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                _isRunning = false;
                Stop();
            }
        }

        public void Stop()
        {
            _isRunning = false;
            try
            {
                if (_listener != null && _listener.IsListening)
                {
                    _listener.Stop();
                }
                _listener?.Close();
                _onStatusUpdate?.Invoke("Relay Server: Stopped");
            }
            catch (Exception) { }
            finally
            {
                _listener = null;
            }
        }

        /// <summary>
        /// Ensures a Windows Firewall rule exists for the relay server port.
        /// Creates a TCP inbound rule if it doesn't exist. Requires admin privileges.
        /// </summary>
        private void EnsureFirewallRule(string port)
        {
            // Validate port is a valid integer to prevent command injection
            if (!int.TryParse(port, out int portNumber) || portNumber < 1 || portNumber > 65535)
            {
                return;
            }

            try
            {
                string ruleName = "VisionGrabber Relay Server";
                
                // Check if rule already exists
                var checkProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = $"advfirewall firewall show rule name=\"{ruleName}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };
                
                checkProcess.Start();
                string output = checkProcess.StandardOutput.ReadToEnd();
                checkProcess.WaitForExit();
                
                // If rule exists and has the right port, we're done
                if (output.Contains(ruleName) && output.Contains(portNumber.ToString()))
                {
                    return;
                }

                // Delete old rule if it exists (port might have changed)
                if (output.Contains(ruleName))
                {
                    var deleteProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "netsh",
                            Arguments = $"advfirewall firewall delete rule name=\"{ruleName}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    deleteProcess.Start();
                    deleteProcess.WaitForExit();
                }

                // Create new inbound TCP rule for specified port only
                var addProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = $"advfirewall firewall add rule name=\"{ruleName}\" dir=in action=allow protocol=TCP localport={portNumber}",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                
                addProcess.Start();
                addProcess.WaitForExit();
            }
            catch (Exception)
            {
                // Firewall rule creation failed - will be handled by HttpListener error
                // Most likely: not running as administrator
            }
        }

        private async Task ListenLoop()
        {
            while (_isRunning)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    // Process each request in its own task to avoid blocking the loop
                    _ = Task.Run(() => ProcessRequest(context));
                }
                catch (Exception)
                {
                    if (!_isRunning) break;
                }
            }
        }

        private async void ProcessRequest(HttpListenerContext context)
        {
            using (var response = context.Response)
            {
                try
                {
                    var request = context.Request;
                    
                    // Allow simple GET requests to test if server is active from a browser
                    if (request.HttpMethod == "GET")
                    {
                        byte[] pingBuffer = Encoding.UTF8.GetBytes("Relay Server is Active");
                        response.ContentType = "text/plain";
                        response.ContentLength64 = pingBuffer.Length;
                        await response.OutputStream.WriteAsync(pingBuffer, 0, pingBuffer.Length);
                        return;
                    }

                    if (request.HttpMethod != "POST" || request.Url.AbsolutePath != "/process")
                    {
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                        return;
                    }

                    _onStatusUpdate?.Invoke($"Relay Server: Processing request from {request.RemoteEndPoint}");

                    string body;
                    using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        body = await reader.ReadToEndAsync();
                    }

                    if (string.IsNullOrWhiteSpace(body))
                    {
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return;
                    }

                    dynamic data = JsonConvert.DeserializeObject(body);
                    if (data == null || data.image == null)
                    {
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return;
                    }

                    string image = data.image;
                    string prompt = data.prompt ?? "";

                    // Use the local Llama backend for relay processing
                    var backend = _backendManager.GetRelayServerBackend();
                    string result = await backend.SendImageRequest(image, prompt);

                    // If enabled, display on this screen
                    if (SettingsManager.Current.DisplayRelayResults)
                    {
                        _onResultReceived?.Invoke(result, prompt);
                    }

                    byte[] buffer = Encoding.UTF8.GetBytes(result);
                    response.ContentLength64 = buffer.Length;
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                }
                catch (Exception ex)
                {
                    try
                    {
                        response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        byte[] buffer = Encoding.UTF8.GetBytes("Server Error: " + ex.Message);
                        response.ContentLength64 = buffer.Length;
                        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    }
                    catch { }
                }
            }
        }
    }
}
