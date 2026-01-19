using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Drawing.Imaging;

namespace VisionGrabber
{
    /// <summary>
    /// Interaction logic for SnippingWindow.xaml. Provides a full-screen overlay for
    /// selecting a screen area and capturing it as a bitmap.
    /// </summary>
    public partial class SnippingWindow : Window
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();
        private System.Windows.Point _startPoint;
        private Bitmap _screenBitmap;

        /// <summary>
        /// Gets the captured image as a <see cref="BitmapSource"/>.
        /// </summary>
        public BitmapSource ResultImage { get; private set; }

        /// <summary>
        /// Gets the captured image as a base64 encoded string.
        /// </summary>
        public string Base64Result { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SnippingWindow"/> class.
        /// </summary>
        public SnippingWindow()
        {
            // Force Windows to tell us the truth about pixels
            // SetProcessDPIAware(); 
            
            InitializeComponent();
            this.KeyUp += SnippingWindow_KeyUp;
            this.Loaded += (s, e) => 
            {
                this.Activate();
                this.Focus();
                CaptureFullScreens();
            };
        }

        private void SnippingWindow_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
                this.Close();
            }
        }

        private void CaptureFullScreens()
        {
            // Use SystemParameters.VirtualScreen to cover ALL monitors
            double virtualLeft = SystemParameters.VirtualScreenLeft;
            double virtualTop = SystemParameters.VirtualScreenTop;
            double virtualWidth = SystemParameters.VirtualScreenWidth;
            double virtualHeight = SystemParameters.VirtualScreenHeight;

            // Positioning the window to cover the entire virtual screen
            this.Left = virtualLeft;
            this.Top = virtualTop;
            this.Width = virtualWidth;
            this.Height = virtualHeight;

            // Convert logical units to physical pixels for the screen blast
            // We use System.Drawing.Forms.Screen to get the actual physical bounds
            int pixelLeft = (int)System.Windows.Forms.Screen.AllScreens.Min(s => s.Bounds.X);
            int pixelTop = (int)System.Windows.Forms.Screen.AllScreens.Min(s => s.Bounds.Y);
            int pixelRight = (int)System.Windows.Forms.Screen.AllScreens.Max(s => s.Bounds.Right);
            int pixelBottom = (int)System.Windows.Forms.Screen.AllScreens.Max(s => s.Bounds.Bottom);
            int pixelWidth = pixelRight - pixelLeft;
            int pixelHeight = pixelBottom - pixelTop;

            _screenBitmap = new Bitmap(pixelWidth, pixelHeight, PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(_screenBitmap))
            {
                // Copy the entire virtual desktop
                g.CopyFromScreen(pixelLeft, pixelTop, 0, 0, new System.Drawing.Size(pixelWidth, pixelHeight));
            }

            BackgroundScreen.Source = BitmapToImageSource(_screenBitmap);

            // Set the full overlay size
            FullOverlayRect.Rect = new Rect(0, 0, virtualWidth, virtualHeight);
            SelectionHoleRect.Rect = new Rect(0, 0, 0, 0); 
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(DrawingCanvas);
            SelectionRect.Visibility = Visibility.Visible;
            Canvas.SetLeft(SelectionRect, _startPoint.X);
            Canvas.SetTop(SelectionRect, _startPoint.Y);
            SelectionRect.Width = 0;
            SelectionRect.Height = 0;
            SelectionHoleRect.Rect = new Rect(_startPoint.X, _startPoint.Y, 0, 0);
        }

        private void Canvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released) return;

            var pos = e.GetPosition(DrawingCanvas);
            var x = Math.Min(pos.X, _startPoint.X);
            var y = Math.Min(pos.Y, _startPoint.Y);
            var w = Math.Abs(pos.X - _startPoint.X);
            var h = Math.Abs(pos.Y - _startPoint.Y);

            Canvas.SetLeft(SelectionRect, x);
            Canvas.SetTop(SelectionRect, y);
            SelectionRect.Width = w;
            SelectionRect.Height = h;
            SelectionHoleRect.Rect = new Rect(x, y, w, h);
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            double visualX = Canvas.GetLeft(SelectionRect);
            double visualY = Canvas.GetTop(SelectionRect);
            double visualWidth = SelectionRect.Width;
            double visualHeight = SelectionRect.Height;

            if (visualWidth <= 1 || visualHeight <= 1)
            {
                this.Close();
                return;
            }

            // Calculate ratios for mapping logical WPF units to physical pixels
            // Note: This works even if monitors have different DPIs because _screenBitmap 
            // covers the entire physical virtual screen, and DrawingCanvas covers the entire logical virtual screen.
            double scaleX = _screenBitmap.Width / DrawingCanvas.ActualWidth;
            double scaleY = _screenBitmap.Height / DrawingCanvas.ActualHeight;

            int physicalX = (int)(visualX * scaleX);
            int physicalY = (int)(visualY * scaleY);
            int physicalWidth = (int)(visualWidth * scaleX);
            int physicalHeight = (int)(visualHeight * scaleY);

            // Constraint check
            physicalX = Math.Max(0, Math.Min(physicalX, _screenBitmap.Width - 1));
            physicalY = Math.Max(0, Math.Min(physicalY, _screenBitmap.Height - 1));
            physicalWidth = Math.Max(1, Math.Min(physicalWidth, _screenBitmap.Width - physicalX));
            physicalHeight = Math.Max(1, Math.Min(physicalHeight, _screenBitmap.Height - physicalY));

            if (physicalWidth > 0 && physicalHeight > 0)
            {
                using (var target = new Bitmap(physicalWidth, physicalHeight))
                {
                    using (Graphics g = Graphics.FromImage(target))
                    {
                        g.DrawImage(_screenBitmap, 
                            new Rectangle(0, 0, physicalWidth, physicalHeight), 
                            new Rectangle(physicalX, physicalY, physicalWidth, physicalHeight), 
                            GraphicsUnit.Pixel);
                    }

                    using (MemoryStream ms = new MemoryStream())
                    {
                        target.Save(ms, ImageFormat.Png);
                        Base64Result = Convert.ToBase64String(ms.ToArray());
                    }
                }
                this.DialogResult = true;
            }
            
            this.Close();
        }

        // Helper to convert System.Drawing.Bitmap to WPF ImageSource
        private BitmapSource BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                // Save to PNG in memory (keeps quality and fixes colors)
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze(); // Important for threading

                return bitmapImage;
            }
        }
    }
}
