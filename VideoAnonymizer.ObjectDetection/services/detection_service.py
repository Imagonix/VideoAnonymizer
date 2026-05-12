import logging
from typing import List

import numpy as np

from models import BatchDetectRequest, BatchDetectionResult, DetectRequest, DetectionResult
from services.image_decoder import decode_base64_image
from services.preprocessing import preprocess
from services.postprocessing import postprocess
from services.model_session import session, input_name

logger = logging.getLogger(__name__)

def detect_objects(request: DetectRequest) -> List[DetectionResult]:
    image = decode_base64_image(request.imageBase64)
    input_tensor, scale_x, scale_y = preprocess(image)
    outputs = session.run(None, {input_name: input_tensor})
    return postprocess(outputs, scale_x, scale_y)


def detect_objects_batch(request: BatchDetectRequest) -> List[BatchDetectionResult]:
    prepared_frames = [
        _prepare_frame(frame.frameIndex, frame.imageBase64)
        for frame in request.frames
    ]

    if not prepared_frames:
        return []

    if len(prepared_frames) == 1:
        return [_detect_prepared_frame(prepared_frames[0])]

    input_batch = np.concatenate(
        [frame.input_tensor for frame in prepared_frames],
        axis=0,
    )

    try:
        outputs = session.run(None, {input_name: input_batch})
        return [
            BatchDetectionResult(
                frameIndex=frame.frame_index,
                detections=postprocess(outputs, frame.scale_x, frame.scale_y, batch_index),
            )
            for batch_index, frame in enumerate(prepared_frames)
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


def _prepare_frame(frame_index: int, image_base64: str):
    image = decode_base64_image(image_base64)
    input_tensor, scale_x, scale_y = preprocess(image)
    return PreparedFrame(frame_index, input_tensor, scale_x, scale_y)


def _detect_prepared_frame(frame) -> BatchDetectionResult:
    outputs = session.run(None, {input_name: frame.input_tensor})
    return BatchDetectionResult(
        frameIndex=frame.frame_index,
        detections=postprocess(outputs, frame.scale_x, frame.scale_y),
    )


class PreparedFrame:
    def __init__(
        self,
        frame_index: int,
        input_tensor: np.ndarray,
        scale_x: float,
        scale_y: float,
    ):
        self.frame_index = frame_index
        self.input_tensor = input_tensor
        self.scale_x = scale_x
        self.scale_y = scale_y
