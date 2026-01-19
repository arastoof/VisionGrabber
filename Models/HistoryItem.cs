using System;

namespace VisionGrabber.Models
{
    /// <summary>
    /// Represents a single OCR processing result stored in history.
    /// </summary>
    public class HistoryItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Prompt { get; set; }
        public string Content { get; set; }
        public string ModelName { get; set; }
    }
}
