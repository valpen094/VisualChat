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
                message = await _ragService.Query();
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