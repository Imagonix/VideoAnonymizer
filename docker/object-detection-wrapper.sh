#!/bin/bash
set -e

if curl -s http://127.0.0.1:8765/health > /dev/null 2>&1; then
    exit 0
fi

cd /opt/object-detection
export FACE_DETECTOR_MODEL_PATH="${6:-/data/models/FaceDetector.onnx}"
exec /opt/venv/bin/python -m uvicorn main:app --host "${2:-127.0.0.1}" --port "${4:-8765}"
