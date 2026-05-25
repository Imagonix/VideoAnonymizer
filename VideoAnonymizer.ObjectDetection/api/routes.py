from fastapi import APIRouter
from typing import List
from models import BatchDetectRequest, BatchDetectionResult, DetectRequest, DetectionResult
from services.detection_service import detect_objects, detect_objects_batch
from services.model_session import cuda_available
from services.model_session import registry
from services.model_session import runtime_status

router = APIRouter()

@router.get("/health")
def health():
    return {
        "status": "running",
        "cuda_available": cuda_available(),
        "detectors": [detector.config.name for detector in registry.detectors],
        "cuda": runtime_status(),
    }

@router.post("/detectObjects", response_model=List[DetectionResult])
def detect_objects_endpoint(request: DetectRequest):
    return detect_objects(request)

@router.post("/detectObjectsBatch", response_model=List[BatchDetectionResult])
def detect_objects_batch_endpoint(request: BatchDetectRequest):
    return detect_objects_batch(request)
