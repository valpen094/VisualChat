using Microsoft.AspNetCore.Mvc;

namespace ChatServer.Controllers
{
    public class CustomController(RAGService ragService) : ControllerBase
    {
        protected readonly RAGService _ragService = ragService;
    }
}