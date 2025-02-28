from fastapi import FastAPI
from api.audio import router as audio_router

app = FastAPI()

app.include_router(audio_router, prefix="/audio", tags=["audio"])

if __name__ == "__main__":
    import uvicorn
    print("\033[32mFastAPI - Swagger UI\033[0m: http://localhost:5023/docs")
    uvicorn.run(app, host = "localhost", port = 5023)