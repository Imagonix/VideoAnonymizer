from fastapi import FastAPI
from pydantic import BaseModel
import base64
import cv2
import numpy as np
import onnxruntime as ort
from typing import List
from models import DetectRequest, DetectionResult      
from object_tracker_manager import ObjectTrackerManager
from debugging import enable_debugpy_if_dev

enable_debugpy_if_dev()

app = FastAPI(title="VideoAnonymizer Object Detection API")

@app.get("/health")
def health():
    return {
        "status": "running",        
        "cuda_available": "CUDAExecutionProvider" in ort.get_available_providers(),
        }

tracker_manager = ObjectTrackerManager()

MODEL_PATH = r".\models\FaceDetector.onnx"
INPUT_SIZE = (640, 640)
CONF_THRESHOLD = 0.7
NMS_THRESHOLD = 0.4


session = ort.InferenceSession(
    MODEL_PATH,
    providers=["CUDAExecutionProvider", "CPUExecutionProvider"]
)

input_name = session.get_inputs()[0].name

def decode_base64_image(image_base64: str) -> np.ndarray:
    image_bytes = base64.b64decode(image_base64)
    image_array = np.frombuffer(image_bytes, dtype=np.uint8)
    image = cv2.imdecode(image_array, cv2.IMREAD_COLOR)
    if image is None:
        raise ValueError("Bild konnte nicht decodiert werden.")
    return image


def preprocess(image: np.ndarray) -> tuple[np.ndarray, float, float]:
    original_h, original_w = image.shape[:2]
    resized = cv2.resize(image, INPUT_SIZE)
    rgb = cv2.cvtColor(resized, cv2.COLOR_BGR2RGB)
    blob = rgb.astype(np.float32)

    # viele RetinaFace-ONNX-Modelle erwarten NCHW
    blob = np.transpose(blob, (2, 0, 1))
    blob = np.expand_dims(blob, axis=0)

    scale_x = original_w / INPUT_SIZE[0]
    scale_y = original_h / INPUT_SIZE[1]

    return blob, scale_x, scale_y


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

    priors = np.array(priors, dtype=np.float32)

    if clip:
        priors = np.clip(priors, 0.0, 1.0)

    return priors


def decode_boxes(loc: np.ndarray, priors: np.ndarray, variances=(0.1, 0.2)) -> np.ndarray:
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


def postprocess(outputs, scale_x: float, scale_y: float) -> List[DetectionResult]:
    """
    Erwartet den typischen RetinaFace-Output:
    outputs[0] = loc
    outputs[1] = conf
    outputs[2] = landms
    """

    loc = outputs[0][0]
    conf = outputs[1][0]
    # landms = outputs[2][0]  # aktuell nicht genutzt

    priors = generate_priors((INPUT_SIZE[1], INPUT_SIZE[0]))
    boxes = decode_boxes(loc, priors)

    # auf Pixel der Modellgröße skalieren
    boxes[:, 0] *= INPUT_SIZE[0]
    boxes[:, 1] *= INPUT_SIZE[1]
    boxes[:, 2] *= INPUT_SIZE[0]
    boxes[:, 3] *= INPUT_SIZE[1]

    scores = conf[:, 1]  # Klasse 1 = face

    mask = scores > CONF_THRESHOLD
    boxes = boxes[mask]
    scores = scores[mask]

    if len(boxes) == 0:
        return []

    # auf Originalbildgröße skalieren
    boxes[:, 0] *= scale_x
    boxes[:, 1] *= scale_y
    boxes[:, 2] *= scale_x
    boxes[:, 3] *= scale_y

    # OpenCV NMS erwartet [x, y, w, h]
    nms_boxes = []
    for b in boxes:
        x1, y1, x2, y2 = b
        nms_boxes.append([
            int(x1),
            int(y1),
            int(x2 - x1),
            int(y2 - y1)
        ])

    indices = cv2.dnn.NMSBoxes(
        bboxes=nms_boxes,
        scores=scores.tolist(),
        score_threshold=CONF_THRESHOLD,
        nms_threshold=NMS_THRESHOLD
    )

    results: List[DetectionResult] = []

    if len(indices) == 0:
        return results

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
                height=h
            )
        )

    return results


@app.post("/detectObjects", response_model=List[DetectionResult])
def detectObjects(request: DetectRequest):
    image = decode_base64_image(request.imageBase64)
    input_tensor, scale_x, scale_y = preprocess(image)

    outputs = session.run(None, {input_name: input_tensor})

    detections = postprocess(outputs, scale_x, scale_y)

    tracked_results = tracker_manager.track_detections(
        detections_list=detections,
        session_id=request.sessionId,
        fps=request.fps,
        class_name="face"
    )

    return tracked_results

@app.post("/resetTracker")
def reset_tracker_endpoint(sessionId: str):
    tracker_manager.reset_tracker(sessionId)
    return {"status": "ok", "message": f"Tracker für Session {sessionId} zurückgesetzt"}


@app.post("/cleanupTracker")
def cleanup_tracker_endpoint(sessionId: str):
    tracker_manager.cleanup_tracker(sessionId)
    return {"status": "ok", "message": f"Tracker für Session {sessionId} entfernt"}