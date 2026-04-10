import os
from pathlib import Path

MODEL_PATH = Path(
    os.getenv(
        "FACE_DETECTOR_MODEL_PATH",
        Path(__file__).resolve().parent.parent / "models" / "FaceDetector.onnx"
    )
)

INPUT_SIZE = (640, 640)
CONF_THRESHOLD = 0.70
NMS_THRESHOLD = 0.4
MODEL_PROVIDERS = ["CUDAExecutionProvider", "CPUExecutionProvider"]