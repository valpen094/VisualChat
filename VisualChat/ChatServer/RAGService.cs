using OllamaSharp;
using ChromaDB.Client;

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

        private ILogger<RAGService> Logger { get; set; }
        public OllamaApiClient? OllamaClient { get; set; }
        public ChromaClient? ChromaClient { get; set; }
        public HttpClient? ChromaHttpClient { get; set; }
        public ChromaCollectionClient? ChromaCollectionClient { get; set; }
        public HttpClient? WhisperClient { get; set; }

        /// <summary>
        /// Numeric Vector Data
        /// </summary>
        public float[]? QueryEmbedding { get; set; } = [0, 0f, 1.1f, 2.2f, 3.3f, 4.4f, 5.5f, 6.6f, 7.7f, 8.8f, 9.9f];

        public RAGService(ILogger<RAGService> _logger)
        {
            Logger = _logger;
        }
    }

    /// <summary>
    /// Data model for HTTP requests.
    /// </summary>
    public class DataRequest
    {
        public string Data { get; set; } = string.Empty;
    }
}