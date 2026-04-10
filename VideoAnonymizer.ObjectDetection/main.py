from fastapi import FastAPI
from core.debugging import enable_debugpy_if_dev
from api.routes import router
from core.startup import log_startup

enable_debugpy_if_dev()

app = FastAPI(title="VideoAnonymizer Object Detection API")
app.include_router(router)

@app.on_event("startup")
async def startup_event():
    log_startup()