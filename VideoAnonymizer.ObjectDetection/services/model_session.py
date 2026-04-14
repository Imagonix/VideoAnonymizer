import onnxruntime as ort
from core.config import MODEL_PATH, MODEL_PROVIDERS

session = ort.InferenceSession(MODEL_PATH, providers=MODEL_PROVIDERS)
input_name = session.get_inputs()[0].name
print("Active providers:", session.get_providers())

def cuda_available() -> bool:
    return "CUDAExecutionProvider" in ort.get_available_providers()