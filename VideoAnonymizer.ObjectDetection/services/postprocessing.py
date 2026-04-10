import cv2
import numpy as np
from typing import List

from core.config import INPUT_SIZE, CONF_THRESHOLD, NMS_THRESHOLD
from models import DetectionResult

def generate_priors(
    image_size: tuple[int, int],
    min_sizes: list[list[int]] = [[16, 32], [64, 128], [256, 512]],
    steps: list[int] = [8, 16, 32],
    clip: bool = False
) -> np.ndarray:
    priors = []
    image_h, image_w = image_size

    for k, step in enumerate(steps):
        feature_map_h = int(np.ceil(image_h / step))
        feature_map_w = int(np.ceil(image_w / step))

        for i in range(feature_map_h):
            for j in range(feature_map_w):
                for min_size in min_sizes[k]:
                    s_kx = min_size / image_w
                    s_ky = min_size / image_h
                    cx = (j + 0.5) * step / image_w
                    cy = (i + 0.5) * step / image_h
                    priors.append([cx, cy, s_kx, s_ky])

    priors_array = np.array(priors, dtype=np.float32)

    if clip:
        priors_array = np.clip(priors_array, 0.0, 1.0)

    return priors_array


def decode_boxes(
    loc: np.ndarray,
    priors: np.ndarray,
    variances: tuple[float, float] = (0.1, 0.2)
) -> np.ndarray:
    boxes = np.concatenate(
        (
            priors[:, :2] + loc[:, :2] * variances[0] * priors[:, 2:],
            priors[:, 2:] * np.exp(loc[:, 2:] * variances[1]),
        ),
        axis=1,
    )

    # cx, cy, w, h -> x1, y1, x2, y2
    boxes[:, :2] -= boxes[:, 2:] / 2
    boxes[:, 2:] += boxes[:, :2]

    return boxes


def postprocess(
    outputs: list[np.ndarray],
    scale_x: float,
    scale_y: float
) -> List[DetectionResult]:
    """
    Expects typical RetinaFace-like output:
    outputs[0] = loc
    outputs[1] = conf
    outputs[2] = landms
    """

    loc = outputs[0][0]
    conf = outputs[1][0]
    # landms = outputs[2][0]  # currently unused

    priors = generate_priors((INPUT_SIZE[1], INPUT_SIZE[0]))
    boxes = decode_boxes(loc, priors)

    # Scale to model input size in pixels
    boxes[:, 0] *= INPUT_SIZE[0]
    boxes[:, 1] *= INPUT_SIZE[1]
    boxes[:, 2] *= INPUT_SIZE[0]
    boxes[:, 3] *= INPUT_SIZE[1]

    scores = conf[:, 1]  # class 1 = face

    mask = scores > CONF_THRESHOLD
    boxes = boxes[mask]
    scores = scores[mask]

    if len(boxes) == 0:
        return []

    # Scale to original image size
    boxes[:, 0] *= scale_x
    boxes[:, 1] *= scale_y
    boxes[:, 2] *= scale_x
    boxes[:, 3] *= scale_y

    # OpenCV NMSBoxes expects [x, y, w, h]
    nms_boxes: list[list[int]] = []
    for box in boxes:
        x1, y1, x2, y2 = box
        nms_boxes.append([
            int(x1),
            int(y1),
            int(x2 - x1),
            int(y2 - y1),
        ])

    indices = cv2.dnn.NMSBoxes(
        bboxes=nms_boxes,
        scores=scores.tolist(),
        score_threshold=CONF_THRESHOLD,
        nms_threshold=NMS_THRESHOLD,
    )

    if len(indices) == 0:
        return []

    results: List[DetectionResult] = []

    for idx in indices.flatten():
        x = max(0, nms_boxes[idx][0])
        y = max(0, nms_boxes[idx][1])
        w = max(0, nms_boxes[idx][2])
        h = max(0, nms_boxes[idx][3])

        results.append(
            DetectionResult(
                className="face",
                confidence=float(scores[idx]),
                x=x,
                y=y,
                width=w,
                height=h,
            )
        )

    return results