from typing import List

from models import DetectionResult, TrackRequest
from object_tracker_manager import ObjectTrackerManager

tracker_manager = ObjectTrackerManager()


def track_objects(request: TrackRequest) -> List[DetectionResult]:
    return tracker_manager.track_detections(
        detections_list=request.detections,
        session_id=request.sessionId,
        fps=request.fps,
        class_name="face"
    )


def reset_tracker(session_id: str) -> None:
    tracker_manager.reset_tracker(session_id)


def cleanup_tracker(session_id: str) -> None:
    tracker_manager.cleanup_tracker(session_id)
