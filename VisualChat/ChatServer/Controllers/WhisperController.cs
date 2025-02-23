using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Reflection;

using System.Text;
using System.Text.Json;

namespace ChatServer.Controllers
{
    /// <summary>
    /// Whisper controller.
    /// </summary>
    /// <param name="_ragService"></param>
    [ApiController]
    [Route("api/[controller]")]
    public class WhisperController(RAGService _ragService) : ControllerBase
    {

#if DEBUG

        /// <summary>
        /// Record the voice and send the result to the user.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("record")]
        public IActionResult Record([FromBody] DataRequest request)
        {
            using Log log = new(GetType().Name, "Record");
            string message = string.Empty;

            if (request == null)
            {
                message = "Invalid request.";
                return BadRequest(new { result = "Error", content = message });
            }

            if (_ragService.WhisperClient == null)
            {
                message = "Whisper client is not available.";
                log.WriteLine($"Error: " + message);
                return BadRequest(new { result = "Error", content = message });
            }

            try
            {                
                // Record the voice.
                message = _ragService.Record().Result.Item1;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                log.WriteLine($"Error: " + message);
                return BadRequest(new { result = "Error", content = message });
            }

            return Ok(new { result = "Success", content = message });
        }

        /// <summary>
        /// Transcribe the voice and send the result to the user.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("transcribe")]
        public IActionResult Transcribe([FromBody] DataRequest request)
        {
            using Log log = new(GetType().Name, "Transcribe");
            string message = string.Empty;

            if (request == null)
            {
                message = "Invalid request.";
                return BadRequest(new { result = "Error", content = message });
            }

            if (_ragService.WhisperClient == null)
            {
                message = "Whisper client is not available.";
                log.WriteLine($"Error: " + message);
                return BadRequest(new { result = "Error", content = message });
            }

            try
            {
                // Transcribe the voice.
                message = _ragService.Transcribe().Result.Item1;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                log.WriteLine($"Error: " + message);
                return BadRequest(new { result = "Error", content = message });
            }

            return Ok(new { result = "Success", content = message });
        }

        /// <summary>
        /// Record and transcribe the voice and send the result to the user.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("whisper")]
        public IActionResult Whisper([FromBody] DataRequest request)
        {
            using Log log = new(GetType().Name, "Whisper");
            string message = string.Empty;
            Tuple<string, string, int> responseData = new(string.Empty, string.Empty, 0);

            if (request == null)
            {
                message = "Invalid request.";
                return BadRequest(new { result = "Error", content = message });
            }

            if (_ragService.WhisperClient == null)
            {
                message = "Whisper client is not available.";
                log.WriteLine($"Error: " + message);
                return BadRequest(new { result = "Error", content = message });
            }

            try
            {
                // Record the voice, and transcribe it.
                responseData = _ragService.Whisper().Result;
                if (responseData.Item3 != 200)
                {
                    message = responseData.Item1;
                    log.WriteLine($"Error: " + message);
                    return BadRequest(new { result = "Error", content = message });
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
                log.WriteLine($"Error: " + message);
                return BadRequest(new { result = "Error", content = message });
            }

            return Ok(new { result = "Success", content = message });
        }

#endif

    }
}