import glob
import os
import subprocess
import sys
from pathlib import Path

import onnxruntime as ort
from core.config import MODEL_PATH, MODEL_PROVIDERS

SESSION_INITIALIZATION_ERROR: str | None = None

try:
    session = ort.InferenceSession(MODEL_PATH, providers=MODEL_PROVIDERS)
except Exception as ex:
    SESSION_INITIALIZATION_ERROR = str(ex)
    print("CUDA session initialization failed. Falling back to CPUExecutionProvider.")
    print(SESSION_INITIALIZATION_ERROR)
    session = ort.InferenceSession(MODEL_PATH, providers=["CPUExecutionProvider"])

input_name = session.get_inputs()[0].name
AVAILABLE_PROVIDERS = ort.get_available_providers()
ACTIVE_PROVIDERS = session.get_providers()

print("Available providers:", AVAILABLE_PROVIDERS)
print("Active providers:", ACTIVE_PROVIDERS)

def cuda_available() -> bool:
    return "CUDAExecutionProvider" in AVAILABLE_PROVIDERS

def cuda_in_use() -> bool:
    return "CUDAExecutionProvider" in ACTIVE_PROVIDERS

def runtime_status() -> dict:
    nvidia_gpu_detected, gpu_names, driver_version = _detect_nvidia_gpus()
    missing_dependencies = _missing_cuda_dependencies()
    cuda_provider_available = cuda_available()
    cuda_execution_provider_active = cuda_in_use()

    if cuda_execution_provider_active:
        severity = "success"
        summary = "CUDA is active. Object detection is using the GPU execution provider."
    elif SESSION_INITIALIZATION_ERROR:
        severity = "warning"
        summary = "CUDA initialization failed. Object detection is running on CPU."
    elif cuda_provider_available:
        severity = "warning"
        summary = "CUDA is visible to ONNX Runtime, but the model session is running on CPU."
    elif nvidia_gpu_detected:
        severity = "warning"
        summary = "An NVIDIA GPU was detected, but ONNX Runtime does not expose CUDA."
    else:
        severity = "warning"
        summary = "No active CUDA execution provider was detected. Object detection will run on CPU."

    return {
        "severity": severity,
        "summary": summary,
        "nvidia_gpu_detected": nvidia_gpu_detected,
        "gpu_names": gpu_names,
        "nvidia_driver_version": driver_version,
        "onnxruntime_version": ort.__version__,
        "available_providers": AVAILABLE_PROVIDERS,
        "active_providers": ACTIVE_PROVIDERS,
        "cuda_provider_available": cuda_provider_available,
        "cuda_execution_provider_active": cuda_execution_provider_active,
        "runs_on_cpu": "CPUExecutionProvider" in ACTIVE_PROVIDERS,
        "initialization_error": SESSION_INITIALIZATION_ERROR,
        "missing_dependencies": missing_dependencies,
        "recommendation": _recommendation(cuda_execution_provider_active),
        "installation_hint": [
            "Install CUDA Toolkit 12.x.",
            "Install cuDNN 9.x for CUDA 12.",
            "Copy or extract cuDNN files into the matching CUDA toolkit folders.",
            r"Add C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v12.x\bin to PATH.",
            "Restart the terminal, IDE, or standalone app.",
            "Verify with: where.exe cublasLt64_12.dll",
            "Verify with: where.exe cudnn*.dll",
        ],
    }

def _recommendation(cuda_execution_provider_active: bool) -> str:
    if cuda_execution_provider_active:
        return "GPU acceleration is ready."

    return (
        "CPU mode can make video analysis slow. "
        "For this package, install CUDA Toolkit 12.x and cuDNN 9.x for CUDA 12."
    )

def _detect_nvidia_gpus() -> tuple[bool, list[str], str | None]:
    candidates = ["nvidia-smi"]

    if os.name == "nt":
        windir = os.environ.get("WINDIR", r"C:\Windows")
        candidates.append(str(Path(windir) / "System32" / "nvidia-smi.exe"))

    for candidate in candidates:
        try:
            result = subprocess.run(
                [
                    candidate,
                    "--query-gpu=name,driver_version",
                    "--format=csv,noheader",
                ],
                capture_output=True,
                check=False,
                text=True,
                timeout=2,
            )
        except Exception:
            continue

        if result.returncode != 0:
            continue

        gpu_names: list[str] = []
        driver_version: str | None = None

        for line in result.stdout.splitlines():
            parts = [part.strip() for part in line.split(",", maxsplit=1)]
            if not parts or not parts[0]:
                continue

            gpu_names.append(parts[0])
            if len(parts) > 1 and parts[1]:
                driver_version = parts[1]

        if gpu_names:
            return True, gpu_names, driver_version

    return False, [], None

def _missing_cuda_dependencies() -> list[str]:
    if os.name != "nt":
        return []

    missing: list[str] = []
    if not _file_exists_on_loader_path("cublasLt64_12.dll"):
        missing.append("cublasLt64_12.dll")

    if not _file_exists_on_loader_path("cudnn64_9.dll") and not _file_exists_on_loader_path("cudnn*.dll"):
        missing.append("cudnn64_9.dll")

    return missing

def _file_exists_on_loader_path(pattern: str) -> bool:
    search_dirs = [Path.cwd()]
    bundle_dir = getattr(sys, "_MEIPASS", None)
    if bundle_dir:
        search_dirs.append(Path(bundle_dir))

    search_dirs.extend(
        Path(path_entry)
        for path_entry in os.environ.get("PATH", "").split(os.pathsep)
        if path_entry
    )

    for directory in search_dirs:
        try:
            if glob.glob(str(directory / pattern)):
                return True
        except OSError:
            continue

    return False
