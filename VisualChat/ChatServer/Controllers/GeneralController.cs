using ChromaDB.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OllamaSharp;
using System.Diagnostics;
using System.Reflection;

namespace ChatServer.Controllers
{
    /// <summary>
    /// General controller.
    /// </summary>
    /// <param name="ragService"></param>
    [ApiController]
    [Route("api/[controller]")]
    public class GeneralController(RAGService ragService) : CustomController(ragService)
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

            List<Process[]> psList =
            [
                Process.GetProcessesByName(RAGService.OllamaProcessName),
                Process.GetProcessesByName(RAGService.ChromaProcessName),
                Process.GetProcessesByName(RAGService.WhisperProcessName),
            ];
            List<ProcessStartInfo> psiList =
            [
                new()
                {
                    FileName = "cmd.exe",
                    Arguments = "/c ollama serve",
                    CreateNoWindow = false, // true: not display window, false: display window
                    UseShellExecute = false
                },
                new()
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c chroma.exe run --path \"..\\chromadb\" --host {_ragService.ChromaUri.Item1} --port {_ragService.ChromaUri.Item2}",
                    CreateNoWindow = false, // true: not display window, false: display window
                    UseShellExecute = false
                },
                new()
                {
                    FileName = "",
                    Arguments = "",
                    CreateNoWindow = false, // true: not display window, false: display window
                    UseShellExecute = false
                },
            ];
            List<string> psNameList = [RAGService.OllamaProcessName, RAGService.ChromaProcessName, RAGService.WhisperProcessName];

            if (request == null)
            {
                return BadRequest("Invalid request.");
            }

            var processes = psList.SelectMany(p => p).Where(p => p != null).ToArray();

            for (int i = 0; i < psList.Count; i++)
            {
                // Check if the process is running
                bool isRunning = psList[i].Length != 0;

                try
                {
                    string processName = psNameList[i];

                    if (!isRunning)
                    {
                        log.WriteLine($"There is not {processName} process.");

                        // Start the process
                        try
                        {
                            Process.Start(psiList[i]);
                            log.WriteLine($"Start the {processName} server.");
                        }
                        catch (Exception ex)
                        {
                            log.WriteLine($"Error: {ex.Message}");
                        }
                    }

                    string url = string.Empty;

                    switch (processName)
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
                    return BadRequest(new { Result = "Error", Content = message });
                }
            }

            return Ok(new { Result = "Success", Content = message });
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
                return BadRequest("Invalid request.");
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
                return BadRequest(new { Result = "Error", Content = message });
            }

            return Ok(new { Result = "Success", Content = message });
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
                return BadRequest("Invalid request.");
            }

            return Ok(new { Result = "Success", Content = message });
        }
    }
}