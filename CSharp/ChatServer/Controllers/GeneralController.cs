using ChromaDB.Client;
using Microsoft.AspNetCore.Mvc;
using OllamaSharp;
using System.Diagnostics;
using System.Net.Sockets;

namespace ChatServer.Controllers
{
    /// <summary>
    /// General controller.
    /// </summary>
    /// <param name="_ragService"></param>
    [ApiController]
    [Route("api/[controller]")]
    public class GeneralController(RAGService _ragService) : ControllerBase
    {
        /// <summary>
        /// Open the server.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("open")]
        public async Task<IActionResult> OpenAsync([FromBody] DataRequest request)
        {
            using Log log = new(GetType().Name, "OpenAsync");
            string message = string.Empty;

            List<string> serviceNameList = [RAGService.OllamaProcessName, RAGService.ChromaProcessName, RAGService.WhisperProcessName];
            List<Tuple<string, string>> serviceList = [_ragService.OllamaUri, _ragService.ChromaUri, _ragService.WhisperUri];
            List<ProcessStartInfo> psiList =
            [
                new()
                {
                    FileName = "cmd.exe",
                    Arguments = "/c ollama serve",
                    CreateNoWindow = true, // true: not display window, false: display window
                    UseShellExecute = false
                },
                new()
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c chroma.exe run --path \"..\\chromadb\" --host {_ragService.ChromaUri.Item1} --port {_ragService.ChromaUri.Item2}",
                    CreateNoWindow = true, // true: not display window, false: display window
                    UseShellExecute = false
                },
                new()
                {
                    FileName = Path.GetFullPath("..\\TranscriptionServer\\faster-whisper_server.bat"),
                    Arguments = "",
                    CreateNoWindow = false, // true: not display window, false: display window
                    UseShellExecute = false,
                    WorkingDirectory = Path.GetFullPath("..\\TranscriptionServer")
                },
            ];

            if (request == null)
            {
                message = "Invalid request.";
                return BadRequest(new { result = "Error", content = message });
            }

            for (int i = 0; i < serviceList.Count; i++)
            {
                try
                {
                    string serviceName = serviceNameList[i];

                    // Check if the port is open
                    bool isRunning = IsPortOpen(serviceList[i].Item1, int.Parse(serviceList[i].Item2));
                    if (!isRunning)
                    {
                        log.WriteLine($"There is not {serviceName} process.");

                        // Start the process
                        try
                        {
                            Process.Start(psiList[i]);
                            log.WriteLine($"Start the {serviceName} server.");
                        }
                        catch (Exception ex)
                        {
                            log.WriteLine($"Error: {ex.Message}");
                        }
                    }

                    string url = string.Empty;

                    switch (serviceName)
                    {
                        case RAGService.OllamaProcessName:
                            url = $"http://{_ragService.OllamaUri.Item1}:{_ragService.OllamaUri.Item2}";
                            log.WriteLine($"Connecting to {url}...");
                            _ragService.OllamaClient = new OllamaSharp.OllamaApiClient(new Uri(url));
                            log.WriteLine($"Connected to {url}");

                            // Pull and select models.
                            string response = string.Empty;
                            _ragService.OllamaClient.SelectedModel = "phi3";

                            await foreach (var status in _ragService.OllamaClient.PullModelAsync(_ragService.OllamaClient.SelectedModel))
                            {
                                response += $"{status.Percent}% {status.Status}\r\n";
                                log.WriteLine($"{status.Percent}% {status.Status}");
                            }
                            break;
                        case RAGService.ChromaProcessName:
                            url = $"http://{_ragService.ChromaUri.Item1}:{_ragService.ChromaUri.Item2}/api/v1/";
                            var options = new ChromaConfigurationOptions(url);
                            log.WriteLine($"Connecting to {url}...");
                            _ragService.ChromaHttpClient = new HttpClient { BaseAddress = new Uri(url) };
                            _ragService.ChromaClient = new ChromaClient(options, _ragService.ChromaHttpClient);
                            log.WriteLine($"Connected to {url}");

                            // Create or Get a collection
                            var collection = await _ragService.ChromaClient.GetOrCreateCollection(RAGService.ChromaCollectionName);
                            _ragService.ChromaCollectionClient = new ChromaCollectionClient(collection, options, _ragService.ChromaHttpClient);
                            log.WriteLine("Collection obtained.");
                            break;
                        case RAGService.WhisperProcessName:
                            Thread.Sleep(10000); // Wait for the server to start.
                            url = $"http://{_ragService.WhisperUri.Item1}:{_ragService.WhisperUri.Item2}";
                            log.WriteLine($"Connecting to {url}...");
                            _ragService.WhisperClient = new HttpClient { BaseAddress = new Uri(url) };
                            log.WriteLine($"Connected to {url}");
                            break;
                        default:
                            log.WriteLine("dummy");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    message = ex.Message;
                    log.WriteLine($"Error: {message}");
                    return BadRequest(new { result = "Error", content = message });
                }
            }

            return Ok(new { result = "Success", content = message });
        }

        /// <summary>
        /// Close the server.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("close")]
        public IActionResult CloseSync([FromBody] DataRequest request)
        {
            using Log log = new(GetType().Name, "CloseSync");
            string message = string.Empty;

            List<Process[]> psList =
            [
                Process.GetProcessesByName(RAGService.OllamaProcessName),
                Process.GetProcessesByName(RAGService.ChromaProcessName),
                Process.GetProcessesByName(RAGService.WhisperProcessName),
            ];

            if (request == null)
            {
                message = "Invalid request.";
                return BadRequest(new { result = "Error", content = message });
            }

            try
            {
                foreach (var ps in psList)
                {
                    foreach (var p in ps)
                    {
                        log.WriteLine($"{p.ProcessName} is existed.");

                        p.Kill();
                        p.WaitForExit();

                        log.WriteLine($"Process: {p.ProcessName} terminated.");
                    }
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
                log.WriteLine($"Error: {message}");
                return BadRequest(new { result = "Error", content = message });
            }

            return Ok(new { result = "Success", content = message });
        }

        /// <summary>
        /// Check if the server is alive.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("alive")]
        public IActionResult AliveSync([FromBody] DataRequest request)
        {
            using Log log = new(GetType().Name, "AliveSync");
            string message = string.Empty;

            if (request == null)
            {
                message = "Invalid request.";
                return BadRequest(new { result = "Error", content = message });
            }

            return Ok(new { result = "Success", content = message });
        }

        /// <summary>
        /// Check if the port is open.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        private bool IsPortOpen(string host, int port)
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    client.Connect(host, port);
                    return true; // Port is in use and connection is successful.
                }
            }
            catch (SocketException)
            {
                return false; // Failed to connect.
            }
        }
    }
}