using OllamaSharp;
using ChromaDB.Client;
using OllamaSharp.Models.Chat;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Text;
using static ChatServer.Controllers.WhisperController;

namespace ChatServer
{
    public class RAGService
    {
        public Tuple<string, string> OllamaUri { get; set; } = new("localhost", "11434");
        public Tuple<string, string> ChromaUri { get; set; } = new("localhost", "8000");
        public Tuple<string, string> WhisperUri { get; set; } = new("localhost", "5023");

        public const string OllamaProcessName = "ollama";
        public const string ChromaProcessName = "chroma";
        public const string WhisperProcessName = "faster-whisper";

        public const string ChromaCollectionName = "docs";

        public OllamaApiClient? OllamaClient { get; set; }
        public ChromaClient? ChromaClient { get; set; }
        public HttpClient? ChromaHttpClient { get; set; }
        public ChromaCollectionClient? ChromaCollectionClient { get; set; }
        public HttpClient? WhisperClient { get; set; }

        /// <summary>
        /// Numeric Vector Data
        /// </summary>
        public float[]? QueryEmbedding { get; set; } = [0, 0f, 1.1f, 2.2f, 3.3f, 4.4f, 5.5f, 6.6f, 7.7f, 8.8f, 9.9f];

        public async Task<string> Query()
        {
            using Log log = new(GetType().Name, "Process: Query");
            string resultData = string.Empty;

            // Create where condition
            ChromaWhereOperator whereCondition = null;

            // Create whereDocument condition
            ChromaWhereDocumentOperator whereDocumentCondition = ChromaWhereDocumentOperator.Contains("example");

            // Query the database
            var queryData = await ChromaCollectionClient.Query(
                queryEmbeddings: [new(QueryEmbedding)],
                nResults: 10,
                whereCondition
                // where: new ("key", "$in", "values")
            );

            foreach (var item in queryData)
            {
                foreach (var entry in item)
                {
                    resultData += $"{entry.Document}\r\n";
                }
            }

            return resultData;
        }

        public async Task<Tuple<string, string, int>> Record()
        {
            using Log log = new(GetType().Name, "Process: Record");
            Tuple<string, string, int> resultData = new(string.Empty, string.Empty, 0);

            string responseData = string.Empty;
            string statusCode = string.Empty;
            int statusCodeValue = 0;

            const string url = "faster-whisper/api/record";

            string filePath = $"{Directory.GetCurrentDirectory()}\\voice.wav";
            var jsonData = new { filePath };
            string jsonString = JsonSerializer.Serialize(jsonData);
            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

            // Send a message to the server.
            HttpResponseMessage response = await WhisperClient.PostAsync(url, content);

            statusCode = response.StatusCode.ToString();
            statusCodeValue = (int)response.StatusCode;

            // Receive the response from the server.
            responseData = await response.Content.ReadAsStringAsync();
            log.WriteLine($"POST Response: {responseData}");

            if (!response.IsSuccessStatusCode)
            {
                responseData = statusCode;
            }

            resultData = new Tuple<string, string, int>($"{statusCodeValue}, {responseData}", statusCode, statusCodeValue);
            return resultData;
        }

        public async Task<Tuple<string, string, int>> Transcribe()
        {
            using Log log = new(GetType().Name, "Process: Transcribe");
            Tuple<string, string, int> resultData = new(string.Empty, string.Empty, 0);

            string responseData = string.Empty;
            string statusCode = string.Empty;
            int statusCodeValue = 0;

            const string url = "faster-whisper/api/transcribe";

            string filePath = $"{Directory.GetCurrentDirectory()}\\voice.wav";
            var jsonData = new { filePath };
            string jsonString = JsonSerializer.Serialize(jsonData);
            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

            // Send a message to the server.
            HttpResponseMessage response = await WhisperClient.PostAsync(url, content);

            statusCode = response.StatusCode.ToString();
            statusCodeValue = (int)response.StatusCode;

            // Receive the response from the server.
            responseData = await response.Content.ReadAsStringAsync();
            log.WriteLine($"POST Response: {resultData}");

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<TranscriptionResult>(responseData);
                if (result != null)
                {
                    responseData = string.Empty;
                    foreach (var segment in result.segments)
                    {
                        log.WriteLine($"[{segment.start}s - {segment.end}s] {segment.text}");
                        responseData += segment.text;
                    }
                }
            }
            else
            {
                responseData = statusCode;
            }

            resultData = new Tuple<string, string, int>($"{statusCodeValue}, {responseData}", statusCode, statusCodeValue);
            return resultData;
        }
    }

    /// <summary>
    /// Data model for HTTP requests.
    /// </summary>
    public class DataRequest
    {
        public string Data { get; set; } = string.Empty;
    }

    /// <summary>
    /// Data model for HTTP responses.
    /// </summary>
    public class DataResponse
    {
        public string Result { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// Transcription result model.
    /// </summary>
    public class TranscriptionResult
    {
        public List<Segment>? segments { get; set; }
    }

    /// <summary>
    /// Segment model.
    /// </summary>
    public class Segment
    {
        public float start { get; set; }
        public float end { get; set; }
        public string? text { get; set; }
    }
}