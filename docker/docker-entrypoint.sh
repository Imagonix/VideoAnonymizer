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

cleanup() {
    echo "Shutting down..."
    kill $DOTNET_PID 2>/dev/null || true
    wait
}
trap cleanup SIGTERM SIGINT

echo "Starting VideoAnonymizer..."
cd /app
dotnet VideoAnonymizer.StandaloneHost.dll &
DOTNET_PID=$!

echo "Waiting for model file at $DATA_DIR/models/FaceDetector.onnx..."
for i in $(seq 1 120); do
    if [ -f "$DATA_DIR/models/FaceDetector.onnx" ] && [ -s "$DATA_DIR/models/FaceDetector.onnx" ]; then
        echo "Model file found."
        break
    fi
    if [ $i -eq 120 ]; then
        echo "WARNING: Model file did not appear within timeout."
    fi
    sleep 2
done

wait $DOTNET_PID
