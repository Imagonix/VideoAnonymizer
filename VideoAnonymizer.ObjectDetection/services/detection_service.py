import logging
import threading
from typing import List

from models import BatchDetectRequest, BatchDetectionResult, DetectRequest, DetectionResult
from services.image_decoder import decode_base64_image
from services.detectors import DetectionFrame
from services.model_session import registry

logger = logging.getLogger(__name__)
_inference_semaphore = threading.Semaphore(1)

def detect_objects(request: DetectRequest) -> List[DetectionResult]:
    image = decode_base64_image(request.imageBase64)
    with _inference_semaphore:
        return registry.detect(image)


def detect_objects_batch(request: BatchDetectRequest) -> List[BatchDetectionResult]:
    prepared_frames = [
        DetectionFrame(
            frame_index=frame.frameIndex,
            image=decode_base64_image(frame.imageBase64),
        )
        for frame in request.frames
    ]

    if not prepared_frames:
        return []

    try:
        with _inference_semaphore:
            results = registry.detect_batch(prepared_frames)

        return [
            BatchDetectionResult(
                frameIndex=result.frame_index,
                detections=result.detections,
            )
            for result in results
        ]
    except Exception:
        logger.warning(
            "Batched object detection failed for %s frames. Falling back to per-frame inference.",
            len(prepared_frames),
            exc_info=True,
        )
        return [
            _detect_prepared_frame(frame)
            for frame in prepared_frames
        ]


def _detect_prepared_frame(frame: DetectionFrame) -> BatchDetectionResult:
    with _inference_semaphore:
        detections = registry.detect(frame.image)

    return BatchDetectionResult(
        frameIndex=frame.frame_index,
        detections=detections,
    )
