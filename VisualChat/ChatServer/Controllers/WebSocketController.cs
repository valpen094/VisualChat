using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace ChatServer.Controllers
{
    /// <summary>
    /// WebSocket controller.
    /// </summary>
    /// <param name="httpClientFactory"></param>
    [Route("/ws")]
    public class WebSocketController(IHttpClientFactory httpClientFactory) : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

        /// <summary>
        /// Receive a message from the client.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> ReceiveMessage()
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                Debug.WriteLine($"{DateTime.Now} Invalid WebSocket request.");
                return BadRequest("Invalid WebSocket request.");
            }

            try
            {
                using WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await HandleWebSocketCommunication(webSocket);
                return Ok("WebSocket connection established.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{DateTime.Now} Internal Server Error: {ex.Message}");
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle WebSocket communication.
        /// </summary>
        /// <param name="webSocket"></param>
        /// <returns></returns>
        private async Task HandleWebSocketCommunication(WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];

            while (webSocket.State == WebSocketState.Open)
            {
                try
                {
                    WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Debug.WriteLine($"{DateTime.Now} Received Data: {receivedMessage}");

                    // Process received messages
                    var request = JsonSerializer.Deserialize<WebSocketData>(receivedMessage);
                    if (request != null && ValidateWebSocketData(request))
                    {
                        string responseMessage = await CallApiAsync(request.Action, request.Data);
                        byte[] responseBuffer = Encoding.UTF8.GetBytes($"Result: {responseMessage}");
                        Debug.WriteLine($"{DateTime.Now} Result: {responseMessage}");
                        await webSocket.SendAsync(new ArraySegment<byte>(responseBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    else
                    {
                        byte[] errorBuffer = Encoding.UTF8.GetBytes($"Error: Invalid data format");
                        Debug.WriteLine($"{DateTime.Now} Error: Invalid data format");
                        await webSocket.SendAsync(new ArraySegment<byte>(errorBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
                catch (Exception ex)
                {
                    // If an error occurs, send an error message.
                    byte[] errorBuffer = Encoding.UTF8.GetBytes($"Error: {ex.Message}");
                    Debug.WriteLine($"{DateTime.Now} Error: {ex.Message}");
                    await webSocket.SendAsync(new ArraySegment<byte>(errorBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }

        /// <summary>
        /// Validate the WebSocket data.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool ValidateWebSocketData(WebSocketData data)
        {
            /*
            if (string.IsNullOrEmpty(data.UserName) || string.IsNullOrEmpty(data.UserId) || string.IsNullOrEmpty(data.Action))
            {
                return false;
            }
            */
            return true;

        }
        /// <summary>
        /// Call the API.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private async Task<string> CallApiAsync(string action, string data)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var payload = JsonSerializer.Serialize(new { Data = data });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await httpClient.PostAsync($"http://localhost:5293/api/{action}", content);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                return $"API Error: {response.StatusCode}";
            }
        }
    }

    /// <summary>
    /// WebSocket data model.
    /// </summary>
    public class WebSocketData
    {
        public string UserName { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
    }
}