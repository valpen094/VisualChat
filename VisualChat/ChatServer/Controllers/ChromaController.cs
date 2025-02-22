using ChromaDB.Client;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Reflection;

namespace ChatServer.Controllers
{
    /// <summary>
    /// Chroma controller.
    /// </summary>
    /// <param name="ragService"></param>
    [ApiController]
    [Route("api/[controller]")]
    public class ChromaController : ControllerBase
    {
        private readonly RAGService _ragService;

        public ChromaController(RAGService ragService)
        {
            _ragService = ragService;
        }

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
                return BadRequest("Invalid request.");
            }

            if (_ragService.ChromaClient == null)
            {
                message = "ChromaDB is not available.";
                log.WriteLine($"Error: " + message);
                return BadRequest(new { Result = "Error", Content = message });
            }

            try
            {
                log.WriteLine($"Start query.");

                // Create where condition
                ChromaWhereOperator whereCondition = null;

                // Create whereDocument condition
                ChromaWhereDocumentOperator whereDocumentCondition = ChromaWhereDocumentOperator.Contains("example");

                // Query the database
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
            }
            catch (Exception ex)
            {
                message = ex.Message;
                log.WriteLine($"Error: {message}");
                return BadRequest(new { Result = "Error", Content = message });
            }

            return Ok(new { result = "Success", content = message });
        }
    }
}