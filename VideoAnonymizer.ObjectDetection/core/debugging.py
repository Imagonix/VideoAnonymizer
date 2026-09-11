import os
import logging

logger = logging.getLogger("uvicorn.error")

_debugpy_listen_started = False


def enable_debugpy_if_dev():
    global _debugpy_listen_started

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

    if _debugpy_listen_started:
        logger.debug("debugpy listener already initialized in this process")
        return

    host = os.getenv("DEBUGPY_HOST", "0.0.0.0")
    port = int(os.getenv("DEBUGPY_PORT", "5678"))

    logger.info("Starting debugpy listener on %s:%s", host, port)
    try:
        debugpy.listen((host, port))
    except RuntimeError as ex:
        message = str(ex)
        if _is_address_in_use_error(message) or "already been called" in message:
            logger.warning(
                "debugpy listener on %s:%s is already active; continuing without starting another listener",
                host,
                port,
            )
            _debugpy_listen_started = True
            return

        raise

    _debugpy_listen_started = True

    if os.getenv("DEBUGPY_WAIT", "1") == "1":
        logger.info("Waiting for debugger attach...")
        debugpy.wait_for_client()
        logger.info("Debugger attached")


def _is_address_in_use_error(message):
    address_in_use_markers = (
        "WinError 10048",
        "Address already in use",
        "address already in use",
        "EADDRINUSE",
        "errno 98",
    )

    return any(marker in message for marker in address_in_use_markers)
