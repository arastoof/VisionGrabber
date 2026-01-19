using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using VisionGrabber.Models;
using VisionGrabber.Utilities;

namespace VisionGrabber
{
    public partial class HistoryWindow : Window
    {
        private HistoryItem _currentItem;
        private bool _isDirty;

        public HistoryWindow()
        {
            InitializeComponent();
            HistoryManager.Load(); // Ensure loaded
            HistoryList.ItemsSource = HistoryManager.Items;
            
            // Auto Select first if available
            if (HistoryManager.Items.Count > 0)
            {
                HistoryList.SelectedIndex = 0;
            }
        }

        private void HistoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isDirty && _currentItem != null)
            {
                // Logic for unsaved changes if needed
            }

            if (HistoryList.SelectedItem is HistoryItem item)
            {
                _currentItem = item;
                DetailDate.Text = item.Timestamp.ToString("f");
                DetailModel.Text = item.ModelName; 
                DetailContent.Text = item.Content;
                
                DetailContent.IsEnabled = true;
                BtnSave.IsEnabled = true;
                BtnCopy.IsEnabled = true;
                BtnDelete.IsEnabled = true;
                
                // Update Preview
                UpdatePreview(item.Content);

                _isDirty = false;
                StatusText.Text = "";
            }
            else
            {
                _currentItem = null;
                DetailDate.Text = "Select an item";
                DetailModel.Text = "";
                DetailContent.Text = "";
                
                DetailContent.IsEnabled = false;
                BtnSave.IsEnabled = false;
                BtnCopy.IsEnabled = false;
                BtnDelete.IsEnabled = false;
                
                if (DetailPreview != null) DetailPreview.Document = PreviewRenderer.RenderDocument("");
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_currentItem != null)
            {
                _currentItem.Content = DetailContent.Text;
                HistoryManager.UpdateEntry(_currentItem);
                HistoryList.Items.Refresh(); // Refresh list to update snippet
                StatusText.Text = "Saved changes.";
                _isDirty = false;
            }
        }

        private void BtnCopy_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(DetailContent.Text))
            {
                System.Windows.Clipboard.SetText(DetailContent.Text);
                StatusText.Text = "Copied to clipboard.";
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_currentItem != null)
            {
                // Use fully qualified System.Windows.MessageBox to avoid ambiguity
                if (System.Windows.MessageBox.Show("Are you sure you want to delete this entry?", "Confirm Delete", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    HistoryManager.DeleteEntry(_currentItem.Id);
                    StatusText.Text = "Entry deleted.";
                }
            }
        }

        private void DetailContent_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_currentItem != null)
            {
                _isDirty = true;
                UpdatePreview(DetailContent.Text);
            }
        }

        private void UpdatePreview(string markdown)
        {
            if (DetailPreview != null)
            {
                DetailPreview.Document = PreviewRenderer.RenderDocument(markdown);
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
    }
}
