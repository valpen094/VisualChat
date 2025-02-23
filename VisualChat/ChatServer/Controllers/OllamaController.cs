using System.Text.RegularExpressions;
using ChromaDB.Client;
using Microsoft.AspNetCore.Mvc;
using OllamaSharp;

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
            string model = request.Data;

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
        public async Task<IActionResult> GenerateAsync([FromBody] DataRequest request)
        {
            using Log log = new(GetType().Name, "GenerateAsync");
            string message = string.Empty;

            string prompt = request.Data;

            var cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            string response = string.Empty;

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

            try
            {
                bool isOptimize = true;
                if (isOptimize)
                {
                    OptimizePrompt(ref prompt);
                }

                log.WriteLine($"{prompt}");

                await foreach (var answerToken in new Chat(_ragService.OllamaClient).SendAsync(prompt))
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    message += answerToken;
                }

                log.WriteLine($"{message}");
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
        public async Task<IActionResult> EmbedAsync([FromBody] DataRequest request)
        {
            using Log log = new(GetType().Name, "EmbedAsync");
            string message = string.Empty;
            string prompt = request.Data;

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

            try
            {
                var result = await _ragService.OllamaClient.EmbedAsync(prompt);
                if (result.Embeddings != null)
                {
                    if (result.Embeddings.Count != 0)
                    {
                        _ragService.QueryEmbedding = result.Embeddings[0];
                        message = _ragService.QueryEmbedding.ToString();
                    }
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
            string model = request.Data;

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

            List<float[]>? embeddings = null;

            string prompt = request.Data;

            var cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

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

            try
            {
                // Embed a prompt.
                log.WriteLine($"Start embeding.");

                var result = await _ragService.OllamaClient.EmbedAsync(prompt);
                embeddings = result.Embeddings;

                if (result.Embeddings != null)
                {
                    if (result.Embeddings.Count != 0)
                    {
                        _ragService.QueryEmbedding = result.Embeddings[0];
                    }
                }

                log.WriteLine($"End embeding.");

                // Query the database
                log.WriteLine($"Start query.");

                ChromaWhereOperator whereCondition = null; // Create where condition
                ChromaWhereDocumentOperator whereDocumentCondition = ChromaWhereDocumentOperator.Contains("doc"); // Create whereDocument condition

                var queryData = await _ragService.ChromaCollectionClient.Query(
                    queryEmbeddings: [new(_ragService.QueryEmbedding)],
                    nResults: 10,
                    whereCondition
                // where: new ("key", "$in", "values")
                );

                log.WriteLine($"End query.");

                foreach (var item in queryData)
                {
                    foreach (var entry in item)
                    {
                        message += $"{entry.Document}\r\n";
                    }
                }

                // Optimize the prompt.
                log.WriteLine($"Start optimize.");

                bool isOptimize = true;
                if (isOptimize)
                {
                    OptimizePrompt(ref prompt);
                }

                log.WriteLine($"End optimize.");

                // Generate a response to a prompt.
                log.WriteLine($"Start generate a text.");

                await foreach (var answerToken in new Chat(_ragService.OllamaClient).SendAsync(prompt))
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    message += answerToken;
                }

                log.WriteLine($"End generate a text.");

                // Get the JSON string from the message.
                log.WriteLine($"Start deserialization.");

                var jsonMatch = Regex.Match(message, @"\{.*?\}", RegexOptions.Singleline);
                if (jsonMatch.Success)
                {
                    string json = jsonMatch.Value;
                    log.WriteLine(json);

                    // Deserialize
                    var jsonObject = System.Text.Json.JsonSerializer.Deserialize<IntelligenceData>(json);
                    log.WriteLine("Deserialization successful.");
                }
                else
                {
                    log.WriteLine("No JSON found in the input string.");
                }

                log.WriteLine($"End deserialization.");
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
        public string text { get; set; } = string.Empty;
    }
}
