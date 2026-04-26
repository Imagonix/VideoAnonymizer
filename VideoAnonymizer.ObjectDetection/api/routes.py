from fastapi import APIRouter
from typing import List
from models import DetectRequest, DetectionResult
from services.detection_service import (
    detect_objects,
    reset_tracker,
    cleanup_tracker
)
from services.model_session import cuda_available
from services.model_session import runtime_status

router = APIRouter()

@router.get("/health")
def health():
    return {
        "status": "running",
        "cuda_available": cuda_available(),
        "cuda": runtime_status(),
    }

@router.post("/detectObjects", response_model=List[DetectionResult])
def detect_objects_endpoint(request: DetectRequest):
    return detect_objects(request)

@router.post("/resetTracker")
def reset_tracker_endpoint(sessionId: str):
    reset_tracker(sessionId)
    return {"status": "ok", "message": f"Tracker for session {sessionId} reset"}

@router.post("/cleanupTracker")
def cleanup_tracker_endpoint(sessionId: str):
    cleanup_tracker(sessionId)
    return {"status": "ok", "message": f"Tracker for session {sessionId} removed"}
