using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Reflection;
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
        private readonly RAGService _ragService = ragService ?? throw new ArgumentNullException(nameof(ragService));

        /// <summary>
        /// Record the voice and send the result to the user.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("record")]
        public async Task<IActionResult> RecordAsync([FromBody] DataRequest request)
        {
            string message = string.Empty;
            string className = this.GetType().Name;
            string methodName = MethodBase.GetCurrentMethod().Name;

            string statusCode = string.Empty;
            int statusCodeValue = 0;

            Debug.WriteLine($"{DateTime.Now} {className}.{methodName}");

            if (request == null)
            {
                return BadRequest("Invalid request.");
            }

            try
            {
                if (_ragService.WhisperClient == null)
                {
                    message = "Whisper client is not available.";
                    Debug.WriteLine($"{DateTime.Now} Error: " + message);
                    return BadRequest(new { Result = "Error", Content = message });
                }

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
                message = ex.Message;
                Debug.WriteLine($"{DateTime.Now} Error: " + message);
                return BadRequest(new { Result = "Error", Content = message });
            }
            finally
            {
                // await _ragService.Clients.All.SendAsync("ReceiveResult", new { name = "whisper/record", errorcode = statusCodeValue, status = statusCode, content = message });
                Debug.WriteLine($"{DateTime.Now} Sending completion message.");
            }

            return Ok(new { Result = "Success", Content = message });
        }

        /// <summary>
        /// Transcribe the voice and send the result to the user.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("transcribe")]
        public async Task<IActionResult> TranscribeAsync([FromBody] DataRequest request)
        {
            string message = string.Empty;
            string className = this.GetType().Name;
            string methodName = MethodBase.GetCurrentMethod().Name;

            string statusCode = string.Empty;
            int statusCodeValue = 0;

            Debug.WriteLine($"{DateTime.Now} {className}.{methodName}");

            if (request == null)
            {
                return BadRequest("Invalid request.");
            }

            try
            {
                if (_ragService.WhisperClient == null)
                {
                    message = "Whisper client is not available.";
                    return BadRequest(new { Result = "Error", Content = message });
                }

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
                    message = statusCode;
                    Debug.WriteLine($"{DateTime.Now} Error: {statusCodeValue}, {message}");
                    return BadRequest(new { Result = "Error", Content = message });
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
                Debug.WriteLine("Error: " + message);
                return BadRequest(new { Result = "Error", Content = message });
            }
            finally
            {
                // await _ragService.Clients.All.SendAsync("ReceiveResult", new { name = "whisper/transcribe", errorcode = statusCodeValue, status = statusCode, content = message });
                Debug.WriteLine($"{DateTime.Now} Sending completion message.");
            }

            return Ok(new { Result = "Success", Content = message });
        }

        /// <summary>
        /// Record and transcribe the voice and send the result to the user.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("whisper")]
        public async Task<IActionResult> WhisperAsync([FromBody] DataRequest request)
        {
            string message = string.Empty;
            string className = this.GetType().Name;
            string methodName = MethodBase.GetCurrentMethod().Name;

            string statusCode = string.Empty;
            int statusCodeValue = 0;

            Debug.WriteLine($"{DateTime.Now} {className}.{methodName}");

            if (request == null)
            {
                return BadRequest("Invalid request.");
            }

            try
            {
                if (_ragService.WhisperClient == null)
                {
                    message = "Whisper client is not available.";
                    Debug.WriteLine($"{DateTime.Now} Error: " + message);
                    return BadRequest(new { Result = "Error", Content = message });
                }

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
                    message = statusCode;
                    Debug.WriteLine($"{DateTime.Now} Error: {statusCodeValue}, {message}");
                    return BadRequest(new { Result = "Error", Content = message });
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
                Debug.WriteLine($"{DateTime.Now} Error: " + message);
                return BadRequest(new { Result = "Error", Content = message });
            }
            finally
            {
                // await _ragService.Clients.All.SendAsync("ReceiveResult", new { name = "whisper/whisper", errorcode = statusCodeValue, status = statusCode, content = message });
                Debug.WriteLine($"{DateTime.Now} Sending completion message.");
            }

            return Ok(new { Result = "Success", Content = message });
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
