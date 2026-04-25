import argparse
import os

import uvicorn


def main():
    parser = argparse.ArgumentParser(description="VideoAnonymizer Object Detection standalone server")
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=8765)
    parser.add_argument("--model-path", default=None)
    args = parser.parse_args()

    if args.model_path:
        os.environ["FACE_DETECTOR_MODEL_PATH"] = args.model_path

    from main import app

    uvicorn.run(app, host=args.host, port=args.port, reload=False)


if __name__ == "__main__":
    main()
