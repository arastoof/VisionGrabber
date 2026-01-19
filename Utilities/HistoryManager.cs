using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using VisionGrabber.Models;

namespace VisionGrabber.Utilities
{
    /// <summary>
    /// Provides static methods for managing the application's processing history.
    /// </summary>
    public static class HistoryManager
    {
        private static readonly string HistoryFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VisionGrabber");
        private static readonly string HistoryFile = Path.Combine(HistoryFolder, "history.json");

        /// <summary>
        /// Gets the collection of history items.
        /// </summary>
        public static ObservableCollection<HistoryItem> Items { get; private set; } = new ObservableCollection<HistoryItem>();

        /// <summary>
        /// Loads the history from the persistent storage file.
        /// </summary>
        public static void Load()
        {
            if (File.Exists(HistoryFile))
            {
                try
                {
                    string json = File.ReadAllText(HistoryFile);
                    var items = JsonConvert.DeserializeObject<ObservableCollection<HistoryItem>>(json);
                    if (items != null)
                    {
                        // Order history by most recent first
                        Items = new ObservableCollection<HistoryItem>(items.OrderByDescending(i => i.Timestamp));
                    }
                }
                catch
                {
                    Items = new ObservableCollection<HistoryItem>();
                }
            }
            else
            {
                Items = new ObservableCollection<HistoryItem>();
            }
        }

        /// <summary>
        /// Saves the current history collection to persistent storage.
        /// </summary>
        public static void Save()
        {
            if (!Directory.Exists(HistoryFolder))
            {
                Directory.CreateDirectory(HistoryFolder);
            }

            try
            {
                string json = JsonConvert.SerializeObject(Items, Formatting.Indented);
                File.WriteAllText(HistoryFile, json);
            }
            catch
            {
                // Silently handle save failures to avoid interrupting the user flow
            }
        }

        /// <summary>
        /// Adds a new entry to the history.
        /// </summary>
        /// <param name="content">The generated content result.</param>
        /// <param name="prompt">The prompt used for generation.</param>
        /// <param name="modelName">The name of the model used.</param>
        public static void AddEntry(string content, string prompt, string modelName)
        {
            var item = new HistoryItem
            {
                Content = content,
                Prompt = prompt,
                ModelName = modelName,
                Timestamp = DateTime.Now
            };

            // Ensure UI update on the main thread
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                Items.Insert(0, item);
            });
            
            Save();
        }

        /// <summary>
        /// Deletes a history entry by its unique identifier.
        /// </summary>
        /// <param name="id">The ID of the entry to delete.</param>
        public static void DeleteEntry(string id)
        {
            var item = Items.FirstOrDefault(i => i.Id == id);
            if (item != null)
            {
                Items.Remove(item);
                Save();
            }
        }

        /// <summary>
        /// Updates an existing history entry.
        /// </summary>
        /// <param name="updatedItem">The updated history item.</param>
        public static void UpdateEntry(HistoryItem updatedItem)
        {
            var index = -1;
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i].Id == updatedItem.Id)
                {
                    index = i;
                    break;
                }
            }

            if (index != -1)
            {
                Items[index] = updatedItem;
                Save();
            }
        }
    }
}
