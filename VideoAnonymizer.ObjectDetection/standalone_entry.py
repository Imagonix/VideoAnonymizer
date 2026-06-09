import argparse
import os

import uvicorn
from core.config import MODELS_PATH


def main():
    parser = argparse.ArgumentParser(description="VideoAnonymizer Object Detection standalone server")
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=8765)
    parser.add_argument("--models-path", default=None)
    args = parser.parse_args()

    if args.models_path:
        os.environ[MODELS_PATH] = args.models_path

    from main import app

    uvicorn.run(app, host=args.host, port=args.port, reload=False)


if __name__ == "__main__":
    main()
