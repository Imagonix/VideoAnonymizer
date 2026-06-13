from fastapi import APIRouter, HTTPException
from typing import List
from models import (
    BatchDetectRequest,
    BatchDetectionResult,
    DetectRequest,
    DetectionResult,
    TrackForwardRequest,
    TrackForwardResponse,
)
from services.detection_service import detect_objects, detect_objects_batch
from services.forward_tracker import track_forward
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


@router.post("/trackForward", response_model=TrackForwardResponse)
def track_forward_endpoint(request: TrackForwardRequest):
    try:
        return track_forward(request)
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc
