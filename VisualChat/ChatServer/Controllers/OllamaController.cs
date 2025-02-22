using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using OllamaSharp;

namespace ChatServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OllamaController(RAGService ragService) : CustomController(ragService)
    {
        /// <summary>
        /// Load a model.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("pull")]
        public async Task<IActionResult> PullAsync([FromBody] DataRequest request)
        {
            string message = string.Empty;
            string className = this.GetType().Name;
            string methodName = MethodBase.GetCurrentMethod().Name;

            string model = request.Data;

            Debug.WriteLine($"{DateTime.Now} {className}.{methodName}");

            if (request == null)
            {
                return BadRequest("Invalid request.");
            }

            if (_ragService.OllamaClient == null)
            {
                message = "Ollama is not available.";
                Debug.WriteLine($"{DateTime.Now} Error: " + message);
                return BadRequest(new { Result = "Error", Content = message });
            }

            try
            {
                await foreach (var status in _ragService.OllamaClient.PullModelAsync(model))
                {
                    message += $"{status.Percent}% {status.Status}\r\n";
                    Debug.WriteLine($"{DateTime.Now} {status.Percent}% {status.Status}");
                }
            }
            catch (Exception e)
            {
                message = e.Message;
                Debug.WriteLine($"{DateTime.Now} Error: " + message);
                return BadRequest(new { Result = "Error", Content = message });
            }
            finally
            {
                Debug.WriteLine($"{DateTime.Now} Sending completion message.");
            }

            return Ok(new { Result = "Success", Content = message });
        }

        /// <summary>
        /// Chat with a user.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("chat")]
        public async Task<IActionResult> ChatAsync([FromBody] DataRequest request)
        {
            string message = string.Empty;
            string className = this.GetType().Name;
            string methodName = MethodBase.GetCurrentMethod().Name;

            List<float[]>? embeddings = null;

            string prompt = request.Data;

            var cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            Debug.WriteLine($"{DateTime.Now} {className}.{methodName}");

            if (request == null)
            {
                return BadRequest("Invalid request.");
            }

            if (_ragService.OllamaClient == null)
            {
                message = "Ollama is not available.";
                Debug.WriteLine($"{DateTime.Now} Error: " + message);
                return BadRequest(new { Result = "Error", Content = message });
            }

            try
            {
                // Embed a prompt.
                var result = await _ragService.OllamaClient.EmbedAsync(prompt);
                embeddings = result.Embeddings;

                // ChromaDBへクエリを投げる
                // 

                // Optimize the prompt.
                bool isOptimize = true;
                if (isOptimize)
                {
                    OptimizePrompt(ref prompt);
                }

                // Generate a response to a prompt.
                await foreach (var answerToken in new Chat(_ragService.OllamaClient).SendAsync(prompt))
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    message += answerToken;
                }
            }
            catch (Exception e)
            {
                message = e.Message;
                Debug.WriteLine($"{DateTime.Now} Error: " + message);
                return BadRequest(new { Result = "Error", Content = message });
            }
            finally
            {
                Debug.WriteLine($"{DateTime.Now} Sending completion message.");
            }

            return Ok(new { Result = "Success", Content = message });
        }

        /// <summary>
        /// Chat with a user.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateAsync([FromBody] DataRequest request)
        {
            string message = string.Empty;
            string className = this.GetType().Name;
            string methodName = MethodBase.GetCurrentMethod().Name;

            string prompt = request.Data;

            var cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            string response = string.Empty;

            Debug.WriteLine($"{DateTime.Now} {className}.{methodName}");

            if (request == null)
            {
                return BadRequest("Invalid request.");
            }

            if (_ragService.OllamaClient == null)
            {
                message = "Ollama is not available.";
                Debug.WriteLine($"{DateTime.Now} Error: " + message);
                return BadRequest(new { Result = "Error", Content = message });
            }

            try
            {
                bool isOptimize = true;
                if (isOptimize)
                {
                    OptimizePrompt(ref prompt);
                }

                Debug.WriteLine($"{DateTime.Now} {prompt}");

                await foreach (var answerToken in new Chat(_ragService.OllamaClient).SendAsync(prompt))
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    message += answerToken;
                }

                Debug.WriteLine($"{DateTime.Now} {message}");
            }
            catch (Exception e)
            {
                message = e.Message;
                Debug.WriteLine($"{DateTime.Now} Error: " + message);
                return BadRequest(new { Result = "Error", Content = message });
            }
            finally
            {
                Debug.WriteLine($"{DateTime.Now} Sending completion message.");
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
            string message = string.Empty;
            string className = this.GetType().Name;
            string methodName = MethodBase.GetCurrentMethod().Name;

            string prompt = request.Data;

            Debug.WriteLine($"{DateTime.Now} {className}.{methodName}");

            if (request == null)
            {
                return BadRequest("Invalid request.");
            }

            if (_ragService.OllamaClient == null)
            {
                message = "Ollama is not available.";
                Debug.WriteLine($"{DateTime.Now} Error: " + message);
                return BadRequest(new { Result = "Error", Content = message });
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
                Debug.WriteLine($"{DateTime.Now} Error: " + message);
                return BadRequest(new { Result = "Error", Content = message });
            }
            finally
            {
                Debug.WriteLine($"{DateTime.Now} Sending completion message.");
            }

            return Ok(new { Result = "Success", Content = message });
        }

        /// <summary>
        /// Chat with a user.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("select")]
        public IActionResult SelectModel([FromBody] DataRequest request)
        {
            string message = string.Empty;
            string className = this.GetType().Name;
            string methodName = MethodBase.GetCurrentMethod().Name;

            string model = request.Data;

            Debug.WriteLine($"{DateTime.Now} {className}.{methodName}");

            if (request == null)
            {
                return BadRequest("Invalid request.");
            }

            if (_ragService.OllamaClient == null)
            {
                message = "Ollama is not available.";
                Debug.WriteLine($"{DateTime.Now} Error: " + message);
                return BadRequest(new { Result = "Error", Content = message });
            }

            _ragService.OllamaClient.SelectedModel = model;
            return Ok(new { result = "Success", content = model });
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
}
