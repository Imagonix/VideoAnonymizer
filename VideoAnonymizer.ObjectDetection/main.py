from fastapi import FastAPI

app = FastAPI(title="VideoAnonymizer Object Detection API")

@app.get("/health")
def health():
    return {"status": "running"}