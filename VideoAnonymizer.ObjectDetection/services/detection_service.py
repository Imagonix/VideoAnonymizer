from typing import List
from models import DetectRequest, DetectionResult
from object_tracker_manager import ObjectTrackerManager
from services.image_decoder import decode_base64_image
from services.preprocessing import preprocess
from services.postprocessing import postprocess
from services.model_session import session, input_name

tracker_manager = ObjectTrackerManager()

def detect_objects(request: DetectRequest) -> List[DetectionResult]:
    image = decode_base64_image(request.imageBase64)
    input_tensor, scale_x, scale_y = preprocess(image)
    outputs = session.run(None, {input_name: input_tensor})
    detections = postprocess(outputs, scale_x, scale_y)

    return tracker_manager.track_detections(
        detections_list=detections,
        session_id=request.sessionId,
        fps=request.fps,
        class_name="face"
    )

def reset_tracker(session_id: str) -> None:
    tracker_manager.reset_tracker(session_id)

def cleanup_tracker(session_id: str) -> None:
    tracker_manager.cleanup_tracker(session_id)