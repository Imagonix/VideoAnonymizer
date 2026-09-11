import glob
import os
import subprocess
import sys
from pathlib import Path

import onnxruntime as ort
from core.config import MODEL_PROVIDERS
from services.detectors import DetectorRegistry

registry = DetectorRegistry(MODEL_PROVIDERS)

AVAILABLE_PROVIDERS = ort.get_available_providers()

print("Available providers:", AVAILABLE_PROVIDERS)
print(
    "Active providers:",
    sorted(
        {
            provider
            for detector in registry.detectors
            for provider in detector.session.get_providers()
        }
    ),
)

def cuda_available() -> bool:
    return "CUDAExecutionProvider" in AVAILABLE_PROVIDERS

def cuda_in_use() -> bool:
    return any(
        "CUDAExecutionProvider" in detector.session.get_providers()
        for detector in registry.detectors
    )

def runtime_status() -> dict:
    nvidia_gpu_detected, gpu_infos, driver_version = _detect_nvidia_gpus()
    gpu_names = [gpu["name"] for gpu in gpu_infos]
    primary_gpu_memory_total_mb = gpu_infos[0].get("memory_total_mb") if gpu_infos else None
    primary_gpu_memory_free_mb = gpu_infos[0].get("memory_free_mb") if gpu_infos else None
    missing_dependencies = _missing_cuda_dependencies()
    cuda_provider_available = cuda_available()
    cuda_execution_provider_active = cuda_in_use()
    models = registry.status()
    active_providers = sorted(
        {
            provider
            for detector in registry.detectors
            for provider in detector.session.get_providers()
        }
    )
    initialization_errors = [
        model["initialization_error"]
        for model in models
        if model.get("initialization_error")
    ]

    if cuda_execution_provider_active:
        severity = "success"
        summary = "CUDA is active. Object detection is using the GPU execution provider."
    elif initialization_errors:
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

    status = {
        "severity": severity,
        "summary": summary,
        "nvidia_gpu_detected": nvidia_gpu_detected,
        "gpus": gpu_infos,
        "gpu_names": gpu_names,
        "gpu_memory_total_mb": primary_gpu_memory_total_mb,
        "gpu_memory_free_mb": primary_gpu_memory_free_mb,
        "nvidia_driver_version": driver_version,
        "onnxruntime_version": ort.__version__,
        "available_providers": AVAILABLE_PROVIDERS,
        "active_providers": active_providers,
        "cuda_provider_available": cuda_provider_available,
        "cuda_execution_provider_active": cuda_execution_provider_active,
        "initialization_error": "\n".join(initialization_errors) if initialization_errors else None,
        "models": models,
        "missing_dependencies": missing_dependencies,
    }

    if not cuda_execution_provider_active:
        status["recommendation"] = _recommendation(cuda_execution_provider_active)
        status["installation_hint"] = [
            "Install CUDA Toolkit 12.x.",
            "Install cuDNN 9.x for CUDA 12.",
            "Copy or extract cuDNN files into the matching CUDA toolkit folders.",
            r"Add C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v12.x\bin to PATH.",
            "Restart the terminal, IDE, or standalone app.",
            "Verify with: where.exe cublasLt64_12.dll",
            "Verify with: where.exe cudnn*.dll",
        ]

    return status

def _recommendation(cuda_execution_provider_active: bool) -> str:
    if cuda_execution_provider_active:
        return "GPU acceleration is ready."

    return (
        "CPU mode can make video analysis slow. "
        "For this package, install CUDA Toolkit 12.x and cuDNN 9.x for CUDA 12."
    )

def _detect_nvidia_gpus() -> tuple[bool, list[dict], str | None]:
    candidates = ["nvidia-smi"]

    if os.name == "nt":
        windir = os.environ.get("WINDIR", r"C:\Windows")
        candidates.append(str(Path(windir) / "System32" / "nvidia-smi.exe"))

    for candidate in candidates:
        try:
            result = subprocess.run(
                [
                    candidate,
                    "--query-gpu=name,driver_version,memory.total,memory.free",
                    "--format=csv,noheader,nounits",
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

        gpus: list[dict] = []
        driver_version: str | None = None

        for line in result.stdout.splitlines():
            parts = [part.strip() for part in line.split(",")]
            if not parts or not parts[0]:
                continue

            gpu = {
                "name": parts[0],
                "memory_total_mb": _parse_int(parts[2]) if len(parts) > 2 else None,
                "memory_free_mb": _parse_int(parts[3]) if len(parts) > 3 else None,
            }
            gpus.append(gpu)

            if len(parts) > 1 and parts[1]:
                driver_version = parts[1]

        if gpus:
            return True, gpus, driver_version

    return False, [], None

def _parse_int(value: str) -> int | None:
    try:
        return int(value)
    except ValueError:
        return None

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
