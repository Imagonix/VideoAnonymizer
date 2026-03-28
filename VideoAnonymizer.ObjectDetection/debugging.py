import os
import logging

logger = logging.getLogger("uvicorn.error")

def enable_debugpy_if_dev():
    env = (
        os.getenv("PYTHON_ENV")
        or os.getenv("ASPNETCORE_ENVIRONMENT")
        or "Production"
    )

    if env.lower() != "development":
        logger.debug("debugpy disabled (not development)")
        return

    if os.getenv("DEBUGPY", "1") != "1":
        logger.info("debugpy disabled via DEBUGPY env var")
        return

    import debugpy

    host = os.getenv("DEBUGPY_HOST", "0.0.0.0")
    port = int(os.getenv("DEBUGPY_PORT", "5678"))

    logger.info("Starting debugpy listener on %s:%s", host, port)
    debugpy.listen((host, port))

    if os.getenv("DEBUGPY_WAIT", "1") == "1":
        logger.info("Waiting for debugger attach...")
        debugpy.wait_for_client()
        logger.info("Debugger attached")