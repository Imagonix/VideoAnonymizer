#!/bin/bash
set -e

DATA_DIR="${DATA_DIR:-/data}"

mkdir -p "$DATA_DIR/App_Data/Uploads" "$DATA_DIR/models"

if [ ! -L /app/App_Data ]; then
    rm -rf /app/App_Data
    ln -sf "$DATA_DIR/App_Data" /app/App_Data
fi

if [ ! -L /app/data ]; then
    rm -rf /app/data
    ln -sf "$DATA_DIR" /app/data
fi

echo "Starting object detection service..."
cd /opt/object-detection
export FACE_DETECTOR_MODEL_PATH="$DATA_DIR/models/FaceDetector.onnx"
export PORT=8765
/opt/venv/bin/python -m uvicorn main:app \
    --host 127.0.0.1 --port 8765 &
DETECTION_PID=$!

for i in $(seq 1 30); do
    if curl -s http://127.0.0.1:8765/health > /dev/null 2>&1; then
        echo "Object detection service ready."
        break
    fi
    if [ $i -eq 30 ]; then
        echo "WARNING: Object detection service did not start in time."
    fi
    sleep 1
done

cd /app
echo "Starting VideoAnonymizer..."
exec dotnet VideoAnonymizer.StandaloneHost.dll
