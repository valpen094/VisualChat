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
    public class ChromaController(RAGService ragService) : ControllerBase
    {
        private readonly RAGService _ragService = ragService;
　
        /// <summary>
        /// Query the ChromaDB.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("query")]
        public async Task<IActionResult> QueryAsync([FromBody] DataRequest request)
        {
            if(request == null)
            {
                return BadRequest("Invalid request.");
            }

            string message = string.Empty;
            string className = this.GetType().Name;
            string methodName = MethodBase.GetCurrentMethod().Name;

            Debug.WriteLine($"{DateTime.Now} {className}.{methodName}");

            try
            {
                Debug.WriteLine($"{DateTime.Now} Start query.");

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

                Debug.WriteLine($"{DateTime.Now} End query.");

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
                Debug.WriteLine($"{DateTime.Now} Error: {message}");
                return BadRequest(new { Result = "Error", Content = message });
            }

            Debug.WriteLine($"{DateTime.Now} Sending completion message.");
            return Ok(new { result = "Success", content = message });
        }
    }
}