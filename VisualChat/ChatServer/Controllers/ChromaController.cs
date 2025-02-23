﻿using ChromaDB.Client;
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
                return BadRequest(new { result = "Error", content = message });
            }

            return Ok(new { result = "Success", content = message });
        }

#endif

    }
}