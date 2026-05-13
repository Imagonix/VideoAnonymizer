from contextlib import asynccontextmanager

from fastapi import FastAPI
from core.debugging import enable_debugpy_if_dev
from api.routes import router
from core.startup import log_startup

enable_debugpy_if_dev()


@asynccontextmanager
async def lifespan(app: FastAPI):
    log_startup()
    yield


app = FastAPI(title="VideoAnonymizer Object Detection API", lifespan=lifespan)
app.include_router(router)
