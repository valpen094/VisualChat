using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;
using ChromaDB.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OllamaSharp;
using static ChatServer.Controllers.WhisperController;
using OllamaSharp.Models.Chat;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ChatServer.Controllers
{
    /// <summary>
    /// Ollama controller.
    /// </summary>
    /// <param name="_ragService"></param>
    [ApiController]
    [Route("api/[controller]")]
    public class OllamaController(RAGService _ragService) : ControllerBase
    {

#if DEBUG

        /// <summary>
        /// Load a model.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("pull")]
        public async Task<IActionResult> PullAsync([FromBody] DataRequest request)
        {
            using Log log = new(GetType().Name, "PullAsync");
            string message = string.Empty;

            if (request == null)
            {
                message = "Invalid request.";
                return BadRequest(new { result = "Error", content = message });
            }

            if (_ragService.OllamaClient == null)
            {
                message = "Ollama is not available.";
                log.WriteLine($"Error: " + message);
                return BadRequest(new { result = "Error", content = message });
            }

            string model = request.Data;

            try
            {
                await foreach (var status in _ragService.OllamaClient.PullModelAsync(model))
                {
                    message += $"{status.Percent}% {status.Status}\r\n";
                    log.WriteLine($"{status.Percent}% {status.Status}");
                }
            }
            catch (Exception e)
            {
                message = e.Message;
                log.WriteLine($"Error: " + message);
                return BadRequest(new { result = "Error", content = message });
            }

            return Ok(new { result = "Success", content = message });
        }

        /// <summary>
        /// Chat with a user.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("generate")]
        public IActionResult Generate([FromBody] DataRequest request)
        {
            using Log log = new(GetType().Name, "Generate");
            string message = string.Empty;

            if (request == null)
            {
                message = "Invalid request.";
                return BadRequest(new { result = "Error", content = message });
            }

            if (_ragService.OllamaClient == null)
            {
                message = "Ollama is not available.";
                log.WriteLine($"Error: " + message);
                return BadRequest(new { result = "Error", content = message });
            }

            string prompt = request.Data;

            try
            {
                bool isOptimize = true;
                if (isOptimize)
                {
                    OptimizePrompt(ref prompt);
                }

                log.WriteLine($"{prompt}");

                message = _ragService.GenerateText(prompt).Result;
            }
            catch (Exception e)
            {
                message = e.Message;
                log.WriteLine($"Error: " + message);
                return BadRequest(new { result = "Error", content = message });
            }

            return Ok(new { result = "Success", Content = message });
        }

        /// <summary>
        /// Embed a prompt.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("embed")]
        public IActionResult Embed([FromBody] DataRequest request)
        {
            using Log log = new(GetType().Name, "Embed");
            string message = string.Empty;
            List<float[]> responseData = [];

            if (request == null)
            {
                message = "Invalid request.";
                return BadRequest(new { result = "Error", content = message });
            }

            if (_ragService.OllamaClient == null)
            {
                message = "Ollama is not available.";
                log.WriteLine($"Error: " + message);
                return BadRequest(new { result = "Error", content = message });
            }

            string prompt = request.Data;

            try
            {
                responseData = _ragService.Embed(prompt).Result;
                foreach (var item in responseData)
                {
                    message += $"{item}\r\n";
                }
            }
            catch (Exception e)
            {
                // If an error occurs when embedding the prompt.
                message = e.Message;
                log.WriteLine($"Error: " + message);
                return BadRequest(new { result = "Error", content = message });
            }

            return Ok(new { result = "Success", content = message });
        }

        /// <summary>
        /// Chat with a user.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("select")]
        public IActionResult SelectModelSync([FromBody] DataRequest request)
        {
            using Log log = new(GetType().Name, "SelectModelSync");
            string message = string.Empty;

            if (request == null)
            {
                message = "Invalid request.";
                return BadRequest(new { result = "Error", content = message });
            }

            if (_ragService.OllamaClient == null)
            {
                message = "Ollama is not available.";
                log.WriteLine($"Error: " + message);
                return BadRequest(new { result = "Error", content = message });
            }

            string model = request.Data;

            _ragService.OllamaClient.SelectedModel = model;
            return Ok(new { result = "Success", content = model });
        }

#endif

        /// <summary>
        /// Chat with a user.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("chat")]
        public async Task<IActionResult> ChatAsync([FromBody] DataRequest request)
        {
            using Log log = new(GetType().Name, "ChatAsync");
            string message = string.Empty;

            if (request == null)
            {
                message = "Invalid request.";
                return BadRequest(new { result = "Error", content = message });
            }

            if (_ragService.OllamaClient == null)
            {
                message = "Ollama is not available.";
                log.WriteLine($"Error: " + message);
                return BadRequest(new { result = "Error", content = message });
            }

            string prompt = string.Empty;

            try
            {
                // Record the voice.
                var recordData = await _ragService.Record();
                if (recordData.Item3 != 200)
                {
                    message = recordData.Item1;
                    log.WriteLine($"Error: " + message);
                    return BadRequest(new { result = "Error", content = message });
                }

                // Transcribe the voice.
                var transcribeData = _ragService.Transcribe().Result;
                if (transcribeData.Item3 != 200)
                {
                    message = transcribeData.Item1;
                    log.WriteLine($"Error: " + message);
                    return BadRequest(new { result = "Error", content = message });
                }

                prompt = transcribeData.Item1;

                // Embed a prompt.
                var embedData = _ragService.Embed(prompt).Result[0];

                // Query the database
                var queryData = _ragService.Query(embedData).Result;

                // Optimize the prompt.
                bool isOptimize = true;
                if (isOptimize)
                {
                    OptimizePrompt(ref prompt);
                }

                log.WriteLine($"{prompt}");

                // Generate up to 5 times.
                for (int i = 0; i < 5; i++)
                {
                    log.WriteLine($"Number of tries: {i + 1}");
                    
                    try
                    {
                        // Generate a response to a prompt.
                        message = _ragService.GenerateText(prompt).Result;

                        // Get the JSON string from the message.
                        var jsonMatch = Regex.Match(message, @"\{.*?\}", RegexOptions.Singleline);
                        if (jsonMatch.Success)
                        {
                            string json = jsonMatch.Value;
                            log.WriteLine(json);

                            // Deserialize
                            var jsonObject = JsonSerializer.Deserialize<IntelligenceData>(json);
                            log.WriteLine("Deserialization successful.");
                        }
                        else
                        {
                            log.WriteLine("No JSON found in the input string.");
                        }

                        break; // Exit if deserialization is successful.
                    }
                    catch (JsonException)
                    {
                        continue;
                    }
                }
            }
            catch (Exception e)
            {
                message = e.Message;
                log.WriteLine($"Error: " + message);
                return BadRequest(new { result = "Error", content = message });
            }

            return Ok(new { result = "Success", content = message });
        }

        /// <summary>
        /// Optimize the prompt.
        /// </summary>
        /// <param name="prompt"></param>
        private void OptimizePrompt(ref string prompt)
        {
            using Log log = new(GetType().Name, "OptimizePrompt");

            string optimizedPrompt =
                "\r\nCondition: \r\n" +
                "1. From now on, you will become a machine that answers product names. No opinions or subjectivity are necessary.\r\n" +
                "2. It must be in JSON format. Any format other than JSON is not allowed.\r\n" +
                "3. The parameters of the JSON statement are as follows: accuracy, text(array: 5 elements).\r\n" +
                "4. Meaning of \"accuracy\": The accuracy of your answer as a percentage, Meaning of \"text\": Actual answer result (excluding JSON statements)\r\n" +
                "5. Be sure to display each parameter and output the JSON statement in the following format: { \"accuracy\": \"50%\", \"text\": [ \"This is a sample text_1.\", \"This is a sample text_2.\", \"This is a sample text_3.\", \"This is a sample text_4.\", \"This is a sample text_5.\" ] }" + "\r\n" +
                "6. If the information is insufficient, set { \"accuracy\": \"0%\" and \"text\": [ \"unknown\" ] } \r\n" +
                "7. You can only put a maximum of 50 characters in text.\r\n" +
                "8. Do not output ```json & ```\r\n\r\n" +
                $"Question: {prompt}";

            // Optimaize the prompt.
            prompt = optimizedPrompt;
        }
    }

    public class IntelligenceData
    {
        public string accuracy { get; set; } = string.Empty;
        public List<string> text { get; set; }
    }
}
