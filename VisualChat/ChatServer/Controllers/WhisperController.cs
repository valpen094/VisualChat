using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace ChatServer.Controllers
{
    /// <summary>
    /// Whisper controller.
    /// </summary>
    /// <param name="ragService"></param>
    [ApiController]
    [Route("api/[controller]")]
    public class WhisperController(RAGService ragService) : ControllerBase
    {
        private readonly RAGService _ragService = ragService;

        /// <summary>
        /// Record the voice and send the result to the user.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("record/{userId}")]
        public IActionResult RecordAsync(string userId)
        {
            string message = string.Empty;

            // fire-and-forget
            _ = Task.Run(async () =>
            {
                string statusCode = string.Empty;
                int statusCodeValue = 0;

                try
                {
                    const string url = "faster-whisper/api/record";

                    string filePath = $"{Directory.GetCurrentDirectory()}\\voice.wav";
                    var jsonData = new { filePath };
                    string jsonString = JsonSerializer.Serialize(jsonData);
                    var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                    // Send a message to the server.
                    HttpResponseMessage response = await _ragService.WhisperClient.PostAsync(url, content);

                    statusCode = response.StatusCode.ToString();
                    statusCodeValue = (int)response.StatusCode;

                    // Receive the response from the server.
                    message = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"{DateTime.Now} POST Response: {message}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error: " + ex.Message);
                }
                finally
                {
                    // await _ragService.Clients.All.SendAsync("ReceiveResult", new { name = "whisper/record", errorcode = statusCodeValue, status = statusCode, content = message });
                    Debug.WriteLine($"{DateTime.Now} Sending completion message.");
                }

            }).ConfigureAwait(false);

            return Ok(new { result = "Accept", content = string.Empty });
        }

        /// <summary>
        /// Transcribe the voice and send the result to the user.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("transcribe/{userId}")]
        public IActionResult TranscribeAsync(string userId)
        {
            string message = string.Empty;

            // fire-and-forget
            _ = Task.Run(async () =>
            {
                string statusCode = string.Empty;
                int statusCodeValue = 0;

                try
                {
                    const string url = "faster-whisper/api/transcribe";

                    string filePath = $"{Directory.GetCurrentDirectory()}\\voice.wav";
                    var jsonData = new { filePath };
                    string jsonString = JsonSerializer.Serialize(jsonData);
                    var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                    // Send a message to the server.
                    HttpResponseMessage response = await _ragService.WhisperClient.PostAsync(url, content);

                    statusCode = response.StatusCode.ToString();
                    statusCodeValue = (int)response.StatusCode;

                    // Receive the response from the server.
                    message = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"{DateTime.Now} POST Response: {message}");

                    if (response.IsSuccessStatusCode)
                    {
                        var result = JsonSerializer.Deserialize<TranscriptionResult>(message);
                        if (result != null)
                        {
                            foreach (var segment in result.segments)
                            {
                                Debug.WriteLine($"{DateTime.Now} [{segment.start}s - {segment.end}s] {segment.text}");
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"{DateTime.Now} Error: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error: " + ex.Message);
                }
                finally
                {
                    // await _ragService.Clients.All.SendAsync("ReceiveResult", new { name = "whisper/transcribe", errorcode = statusCodeValue, status = statusCode, content = message });
                    Debug.WriteLine($"{DateTime.Now} Sending completion message.");
                }
                
            }).ConfigureAwait(false);

            return Ok(new { result = "Accept", content = string.Empty });
        }

        /// <summary>
        /// Record and transcribe the voice and send the result to the user.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("whisper/{userId}")]
        public IActionResult WhisperAsync(string userId)
        {
            string message = string.Empty;

            // fire-and-forget
            _ = Task.Run(async () =>
            {
                string statusCode = string.Empty;
                int statusCodeValue = 0;

                try
                {
                    const string url = "faster-whisper/api/whisper";

                    string filePath = $"{Directory.GetCurrentDirectory()}\\voice.wav";
                    var jsonData = new { filePath };
                    string jsonString = JsonSerializer.Serialize(jsonData);
                    var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                    // Send a message to the server.
                    HttpResponseMessage response = await _ragService.WhisperClient.PostAsync(url, content);

                    statusCode = response.StatusCode.ToString();
                    statusCodeValue = (int)response.StatusCode;

                    // Receive the response from the server.
                    message = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"{DateTime.Now} POST Response: {message}");

                    if (response.IsSuccessStatusCode)
                    {
                        var result = JsonSerializer.Deserialize<TranscriptionResult>(message);
                        if (result != null)
                        {
                            foreach (var segment in result.segments)
                            {
                                Debug.WriteLine($"{DateTime.Now} [{segment.start}s - {segment.end}s] {segment.text}");
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"{DateTime.Now} Error: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error: " + ex.Message);
                }
                finally
                {
                    // await _ragService.Clients.All.SendAsync("ReceiveResult", new { name = "whisper/whisper", errorcode = statusCodeValue, status = statusCode, content = message });
                    Debug.WriteLine($"{DateTime.Now} Sending completion message.");
                }

            }).ConfigureAwait(false);

            return Ok(new { result = "Accept", content = string.Empty });
        }

        public class TranscriptionResult
        {
            public List<Segment>? segments { get; set; }
        }

        public class Segment
        {
            public float start { get; set; }
            public float end { get; set; }
            public string? text { get; set; }
        }
    }
}
