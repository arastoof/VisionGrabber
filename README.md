# VisionGrabber

VisionGrabber is a desktop application that lets you capture any part of your screen and process the visual data using LLMs. It is especially useful for processing screenshots of tables (able to give outputs in markdown or HTML), math formulae (able to give outputs in LaTeX), and other formatted text. VisionGrabber allows you to do both local models for privacy and flexibility, and cloud processing for performance.

## Features

- **Flexible Backends**: Use local models via [llama.cpp](https://github.com/ggml-org/llama.cpp) or Google's Gemini API.
- **Relay System**: Run one instance as a server to allow other computers on your network to process screenshots.
- **Snipping Tool**: An interface for selecting screen regions that works on multiple monitors.
- **Preview Generation**: See an editable preview of the model's output, useful for seeing tables and other formatted text.
- **Custom Prompt**: Use your own prompt to instruct the model on how to format the output (e.g., HTML or markdown)

---

## Getting Started
- Download and install the [.NET runtime]([https://dotnet.microsoft.com/en-us/download](https://dotnet.microsoft.com/en-us/download/dotnet/10.0/runtime)) for Desktop Apps if you don't have it already. You'll need at least version 8. 

### Prerequisites
#### For Local Processing:
- **[llama-server.exe](https://github.com/ggml-org/llama.cpp/releases)**: Essential for local LLMs.
  - *Nvidia GPUs*: Use the **CUDA** version (requires [CUDA toolkit](https://developer.nvidia.com/cuda/toolkit)).
  - *Non-Nvidia GPUs*: Use the **Vulkan** version.
  - *CPU Only*: Use the **x64 (CPU)** version.
- **Vision Model**: A llama.cpp-compatible model with a vision encoder (e.g., [Qwen 3 VL 2B](https://huggingface.co/Qwen/Qwen3-VL-2B-Instruct-GGUF)).

#### For Cloud Processing:
- **[Gemini API Key](https://aistudio.google.com/api-keys)**: Required for the Gemini backend.

---

### Installation

1. **Pre-Built**: Download the latest release from the [GitHub Releases](https://github.com/arastoof/VisionGrabber/releases) page.
2. **From Source**: You'll need the **[.NET SDK](https://dotnet.microsoft.com/download/dotnet)** (8.0 or later). Then, just run
   ```bash
   git clone https://github.com/your-username/VisionGrabber.git
   cd VisionGrabber
   dotnet build
   ```

---

## Configuration & Usage

1. **Launch**: Open `VisionGrabber.exe`.
2. **Set Up Backends**: Open the **Settings** window and choose your tab:
   - **Local**: Point to your `llama-server.exe`, model, and `mmproj` files.
   - **Cloud**: Paste your Gemini API key.
   - **Remote**: Enter the IP and port of a networked `llama-server`.
   - **Relay**: Enable "Run Relay Server" on a host machine to process images for other clients.

3. **Capture**: Select your backend from the dropdown, click capture, and wait for the results.
