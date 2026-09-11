#!/bin/bash
set -e

DATA_DIR="${DATA_DIR:-/data}"

mkdir -p "$DATA_DIR/App_Data/Uploads" "$DATA_DIR/models"

if [ -d /opt/object-detection/models ]; then
    find /opt/object-detection/models -maxdepth 1 -type f \( -name '*.onnx' -o -name '*.detector.json' \) \
        -exec sh -c 'target="$1/$(basename "$2")"; if [ ! -s "$target" ]; then cp "$2" "$target"; fi' sh "$DATA_DIR/models" {} \;
fi

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

wait $DOTNET_PID
