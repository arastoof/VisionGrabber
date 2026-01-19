using System.Threading.Tasks;

namespace VisionGrabber.Backends
{
    /// <summary>
    /// Defines the interface for image processing backends.
    /// </summary>
    public interface IBackend
    {
        /// <summary>
        /// Sends an image processing request to the backend.
        /// </summary>
        /// <param name="base64Image">The image encoded as a base64 string.</param>
        /// <param name="userPrompt">The prompt to send with the image.</param>
        /// <returns>The generated text result.</returns>
        Task<string> SendImageRequest(string base64Image, string userPrompt);
    }
}
