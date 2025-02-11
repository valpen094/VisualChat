using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OllamaSharp;

namespace ChatServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OllamaController(RAGService ragService) : ControllerBase
    {
        private readonly RAGService _ragService = ragService;

        /// <summary>
        /// Load a model.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpGet("pull/{userId}")]
        public IActionResult PullAsync(string userId, string model)
        {
            string message = string.Empty;

            // fire-and-forget
            _ = Task.Run(async () =>
            {     
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
                    message = $"Error: {e.Message}";
                }
                finally
                {
                    var response = new
                    {
                        name = "ollama/pull",
                        errorcode = 200,
                        status = "",
                        content = "",
                    };

                    try
                    {
                        //await _ragService.Clients.Client(userId).SendAsync("ReceiveResult", response);
                        // await _ragService.Clients.All.SendAsync("ReceiveResult", new { name = "ollama/pull", errorcode = 200, status = "Completed", content = message });
                        Debug.WriteLine($"{DateTime.Now} Sending completion message.");
                    }
                    catch (Exception ex)
                    {
                        // If an error occurs when sending to the Hub.
                        Debug.WriteLine($"{DateTime.Now} Error sending to client: {ex.Message}");
                    }
                }

            }).ConfigureAwait(false);

            return Ok(new { result = "Accept", content = string.Empty });
        }

        /// <summary>
        /// Chat with a user.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="prompt"></param>
        /// <returns></returns>
        [HttpGet("chat/{userId}")]
        public IActionResult ChatAsync(string userId, string prompt)
        {
            List<float[]>? embeddings = null;

            string message = string.Empty;
            string request = prompt;

            var cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            // fire-and-forget
            _ = Task.Run(async () =>
            {
                try
                {
                    // Embed a prompt.
                    var result = await _ragService.OllamaClient.EmbedAsync(prompt);
                    embeddings = result.Embeddings;

                    // ChromaDBへクエリを投げる
                    // 
                    // Generate a response to a prompt.
                    await foreach (var answerToken in new Chat(_ragService.OllamaClient).SendAsync(request))
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
                    message = $"{DateTime.Now} Error: {e.Message}";
                }
                finally
                {
                    try
                    {
                        // await _ragService.Clients.All.SendAsync("ReceiveResult", new { name = "ollama/chat", errorcode = 200, status = "Completed", content = message });
                        Debug.WriteLine($"{DateTime.Now} Sending completion message.");
                    }
                    catch (Exception ex)
                    {
                        // If an error occurs when sending to the Hub.
                        Debug.WriteLine($"{DateTime.Now} Error sending to client: {ex.Message}");
                    }
                }

            }, token).ConfigureAwait(false);

            return Ok(new { result = "Accept", content = string.Empty });
        }

        /// <summary>
        /// Generate a response to a prompt.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="prompt"></param>
        /// <returns></returns>
        [HttpGet("generate/{userId}")]
        public IActionResult GenerateAsync(string userId, string prompt)
        {
            string message = string.Empty;
            string request = prompt;

            var cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            // fire-and-forget
            _ = Task.Run(async () =>
            {
                string response = string.Empty;

                try
                {
                    await foreach (var answerToken in new Chat(_ragService.OllamaClient).SendAsync(request))
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
                    message = $"{DateTime.Now} Error: {e.Message}";
                }
                finally
                {
                    try
                    {
                        //  await _ragService.Clients.All.SendAsync("ReceiveResult", new { name = "ollama/generate", errorcode = 200, status = "Completed", content = message });
                        Debug.WriteLine($"{DateTime.Now} Sending completion message.");
                    }
                    catch (Exception ex)
                    {
                        // If an error occurs when sending to the Hub.
                        Debug.WriteLine($"{DateTime.Now} Error sending to client: {ex.Message}");
                    }
                }

            }, token).ConfigureAwait(false);

            return Ok(new { result = "Accept", content = string.Empty });
        }

        /// <summary>
        /// Embed a prompt.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="prompt"></param>
        /// <returns></returns>
        [HttpGet("embed/{userId}")]
        public IActionResult EmbedAsync(string userId, string prompt)
        {
            object? message = null;

            var cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            // fire-and-forget
            _ = Task.Run(async () =>
            {
                try
                {
                    var result = await _ragService.OllamaClient.EmbedAsync(prompt);
                    message = result.Embeddings;
                }
                catch (Exception e)
                {
                    // If an error occurs when embedding the prompt.
                    message = $"Error: {e.Message}";
                }
                finally
                {
                    try
                    {
                        if (message == null)
                        {
                            message = new List<float[]>();
                        }

                        // await _ragService.Clients.All.SendAsync("ReceiveResult", new { name = "ollama/embed", errorcode = 200, status = "Completed", content = message });
                        Debug.WriteLine($"{DateTime.Now} Sending completion message.");
                    }
                    catch (Exception ex)
                    {
                        // If an error occurs when sending to the Hub.
                        Debug.WriteLine($"{DateTime.Now} Error sending to client: {ex.Message}");
                    }
                }

            }, token).ConfigureAwait(false);

            return Ok(new { result = "Accept", content = string.Empty });
        }

        /// <summary>
        /// Select a model.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpGet("select/{userId}")]
        public IActionResult SelectModel(string userId, string model)
        {
            _ragService.OllamaClient.SelectedModel = model;
            return Ok(new { result = "Accept", content = _ragService.OllamaClient.SelectedModel });
        }
    }
}
