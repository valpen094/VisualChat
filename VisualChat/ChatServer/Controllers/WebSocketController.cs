using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace ChatServer.Controllers
{
    /// <summary>
    /// WebSocket controller.
    /// </summary>
    /// <param name="httpClientFactory"></param>
    [Route("/ws")]
    public class WebSocketController(RAGService _ragService, IHttpClientFactory httpClientFactory) : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

        private const string PrivateKey = "B6E8359A-F2A4-4189-8718-D2D07F29AABB"; // Set the private key
        private const string Issuer = "your-issuer";
        private const string Audience = "your-audience";

        /// <summary>
        /// Receive a message from the client.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> ReceiveMessage()
        {
            using Log log = new(GetType().Name, "ReceiveMessage");
            string message = string.Empty;

            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                log.WriteLine($"Invalid WebSocket request.");
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
                log.WriteLine($"Internal Server Error: {ex.Message}");
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
            using Log log = new(GetType().Name, "HandleWebSocketCommunication");
            var buffer = new byte[1024 * 4];

            // Received the first message from the client (authentication information)
            WebSocketReceiveResult result1 = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            string receivedMessage1 = Encoding.UTF8.GetString(buffer, 0, result1.Count);

            // Determine if the message is authentication information
            var loginRequest = JsonSerializer.Deserialize<LoginRequest>(receivedMessage1);
            if (loginRequest != null)
            {
                // Authentication process
                var token = await AuthenticateUserAsync(loginRequest.UserId, loginRequest.Password);

                // Send the token to the client
                byte[] responseBuffer = Encoding.UTF8.GetBytes(token);
                await webSocket.SendAsync(new ArraySegment<byte>(responseBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            else
            {
                // Send an error message when authentication fails
                byte[] errorBuffer = Encoding.UTF8.GetBytes("Error: Invalid credentials.");
                await webSocket.SendAsync(new ArraySegment<byte>(errorBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }

            while (webSocket.State == WebSocketState.Open)
            {
                try
                {
                    WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    log.WriteLine($"Received Data: {receivedMessage}");

                    // Process received messages
                    var request = JsonSerializer.Deserialize<WebSocketData>(receivedMessage);
                    if (request != null && ValidateWebSocketData(request))
                    {
                        string responseMessage = await CallApiAsync(request.Action, request.Data);
                        byte[] responseBuffer = Encoding.UTF8.GetBytes($"Result: {responseMessage}");
                        log.WriteLine($"Result: {responseMessage}");
                        await webSocket.SendAsync(new ArraySegment<byte>(responseBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    else
                    {
                        byte[] errorBuffer = Encoding.UTF8.GetBytes($"Error: Invalid data format");
                        log.WriteLine($"Error: Invalid data format");
                        await webSocket.SendAsync(new ArraySegment<byte>(errorBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
                catch (Exception ex)
                {
                    // If an error occurs, send an error message.
                    byte[] errorBuffer = Encoding.UTF8.GetBytes($"Error: {ex.Message}");
                    log.WriteLine($"Error: {ex.Message}");
                    await webSocket.SendAsync(new ArraySegment<byte>(errorBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }

        // ユーザーの認証を行い、JWTトークンを返す
        private async Task<string> AuthenticateUserAsync(string userId, string password)
        {
            // 認証処理（ユーザーIDとパスワードが正しいかをチェック）
            if (userId == "validUser" && password == "validPassword")
            {
                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, userId),
                    new Claim(ClaimTypes.Role, "User")
                };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(PrivateKey));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: Issuer,
                    audience: Audience,
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(30),
                    signingCredentials: creds
                );

                return new JwtSecurityTokenHandler().WriteToken(token);
            }

            return string.Empty; // 認証失敗
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
        /// Validate the token.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="SecurityTokenMalformedException"></exception>
        private bool ValidateToken(string token)
        {
            using Log log = new(GetType().Name, "ValidateToken");

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(PrivateKey);

                // Check if the token is 3-segment
                if (token.Split('.').Length != 3)
                {
#if DEBUG
                    log.WriteLine("JWT must have three segments (JWS).");
#endif
                    throw new SecurityTokenMalformedException("JWT must have three segments (JWS).");
                }

                var parameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidIssuer = "your-issuer",  // Issuer
                    ValidAudience = "your-audience",  // Audience
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };

                // Varidate
                handler.ValidateToken(token, parameters, out var validatedToken);
                return validatedToken != null;
            }
            catch (SecurityTokenException ex)
            {
#if DEBUG
                log.WriteLine($"Token validation failed: {ex.Message}");
                return false;
#endif
            }
            catch (Exception e)
            {
#if DEBUG
                log.WriteLine($"Error: {e.Message}");
#endif
                return false;
            }
        }
    }

    public class LoginRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// WebSocket data model.
    /// </summary>
    public class WebSocketData
    {
        public string Action { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
    }
}