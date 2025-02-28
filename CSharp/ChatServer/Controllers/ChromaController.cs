using ChromaDB.Client;
using Microsoft.AspNetCore.Mvc;

namespace ChatServer.Controllers
{
    /// <summary>
    /// Chroma controller.
    /// </summary>
    /// <param name="_ragService"></param>
    [ApiController]
    [Route("api/[controller]")]
    public class ChromaController(RAGService _ragService) : ControllerBase
    {

#if DEBUG

        /// <summary>
        /// Query the ChromaDB.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("query")]
        public async Task<IActionResult> QueryAsync([FromBody] DataRequest request)
        {
            using Log log = new(GetType().Name, "QueryAsync");
            string message = string.Empty;

            if (request == null)
            {
                message = "Invalid request.";
                return BadRequest(new { result = "Error", content = message });
            }

            if (_ragService.ChromaClient == null)
            {
                message = "ChromaDB is not available.";
                log.WriteLine($"Error: " + message);
                return BadRequest(new { result = "Error", content = message });
            }

            try
            {
                // Query the ChromaDB.
                float[] queryEmbeddings = [0, 0f, 1.1f, 2.2f, 3.3f, 4.4f, 5.5f, 6.6f, 7.7f, 8.8f, 9.9f];
                message = await _ragService.Query(queryEmbeddings);
            }
            catch (Exception ex)
            {
                message = ex.Message;
                log.WriteLine($"Error: {message}");
                return BadRequest(new { result = "Error", content = message });
            }

            return Ok(new { result = "Success", content = message });
        }

#endif

    }
}