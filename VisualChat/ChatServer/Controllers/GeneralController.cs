using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ChatServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GeneralController(RAGService ragService) : ControllerBase
    {
        private readonly RAGService _ragService = ragService;

        /// <summary>
        /// Connect to the service.
        /// </summary>
        /// <returns></returns>
        [HttpGet("connect/{userId}")]
        public async Task ConnectAsync()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await _ragService.HandleWebSocketAsync(webSocket);
            }
            else
            {
                HttpContext.Response.StatusCode = 400;
            }
        }

        /// <summary>
        /// Check if the service is alive.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("alive/{userId}")]
        public IActionResult AliveAsync(string userId)
        {
            _ = Task.Run(async () =>
            {
                string message = string.Empty;

                try
                {
                    Debug.WriteLine($"{DateTime.Now} Alive.");
                }
                catch (Exception e)
                {
                    message = $"Error: {e.Message}";
                }
                finally
                {
                    try
                    {
                        // await _ragService.Clients.All.SendAsync("ReceiveResult", new { name = "rag/alive", errorcode = 200, status = "Completed", content = message });
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
    }
}