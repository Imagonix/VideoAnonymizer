import logging
import os

logger = logging.getLogger("uvicorn.error")

def log_startup() -> None:
    port = os.getenv("PORT")

    if port:
        logger.info(f"FastAPI Server started on http://0.0.0.0:{port}")
        logger.info(f"   Health: http://0.0.0.0:{port}/health")
        logger.info(f"   Detect: http://0.0.0.0:{port}/detectObjects")
    else:
        logger.warning("PORT environment variable is not set!")