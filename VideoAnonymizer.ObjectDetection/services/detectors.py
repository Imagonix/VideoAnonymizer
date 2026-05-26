import logging
import math
from concurrent.futures import ThreadPoolExecutor
from dataclasses import dataclass
from typing import Iterable

import cv2
import numpy as np
import onnxruntime as ort

from models import DetectionResult
from services.model_config import DetectorConfig, YoloOutputConfig, load_detector_configs

logger = logging.getLogger(__name__)


@dataclass(frozen=True)
class DetectionFrame:
    frame_index: int
    image: np.ndarray


@dataclass(frozen=True)
class PreparedFrame:
    frame_index: int
    input_tensor: np.ndarray
    scale_x: float
    scale_y: float
    resize_ratio: float
    original_width: int
    original_height: int


@dataclass(frozen=True)
class FrameDetections:
    frame_index: int
    detections: list[DetectionResult]


class DetectorRegistry:
    def __init__(self, providers: list[str]):
        self.detectors = [
            ObjectDetector(config, providers)
            for config in load_detector_configs()
        ]
        self._executor = ThreadPoolExecutor(max_workers=max(1, len(self.detectors)))

        for detector in self.detectors:
            detector.warm_up()

        logger.info(
            "Loaded %s object detectors: %s",
            len(self.detectors),
            ", ".join(detector.config.name for detector in self.detectors),
        )

    def detect(self, image: np.ndarray) -> list[DetectionResult]:
        result = self.detect_batch([DetectionFrame(0, image)])
        return result[0].detections if result else []

    def detect_batch(self, frames: list[DetectionFrame]) -> list[FrameDetections]:
        if not frames:
            return []

        detector_results = list(
            self._executor.map(
                lambda detector: detector.detect_batch(frames),
                self.detectors,
            )
        )

        results_by_frame_index: dict[int, list[DetectionResult]] = {
            frame.frame_index: []
            for frame in frames
        }

        for result_set in detector_results:
            for result in result_set:
                results_by_frame_index.setdefault(result.frame_index, []).extend(result.detections)

        return [
            FrameDetections(
                frame_index=frame.frame_index,
                detections=results_by_frame_index.get(frame.frame_index, []),
            )
            for frame in frames
        ]

    def status(self) -> list[dict]:
        return [detector.status() for detector in self.detectors]


class ObjectDetector:
    def __init__(self, config: DetectorConfig, providers: list[str]):
        self.config = config
        self.session_initialization_error: str | None = None

        try:
            self.session = ort.InferenceSession(config.model_path, providers=providers)
        except Exception as ex:
            self.session_initialization_error = str(ex)
            logger.warning(
                "CUDA session initialization failed for %s. Falling back to CPUExecutionProvider.",
                config.name,
                exc_info=True,
            )
            self.session = ort.InferenceSession(config.model_path, providers=["CPUExecutionProvider"])

        self.input = self.session.get_inputs()[0]
        self.input_name = self.input.name
        self.output_metadata = self.session.get_outputs()
        self._priors: np.ndarray | None = None
        self._max_batch_size = self._resolve_max_batch_size()

    def detect_batch(self, frames: list[DetectionFrame]) -> list[FrameDetections]:
        results: list[FrameDetections] = []
        batch_size = self._max_batch_size or len(frames)

        for chunk in _chunks(frames, batch_size):
            prepared_frames = [self._prepare_frame(frame) for frame in chunk]
            input_batch = np.concatenate(
                [frame.input_tensor for frame in prepared_frames],
                axis=0,
            )
            outputs = self.session.run(None, {self.input_name: input_batch})

            for batch_index, prepared_frame in enumerate(prepared_frames):
                detections = self._postprocess(outputs, prepared_frame, batch_index)
                results.append(FrameDetections(prepared_frame.frame_index, detections))

        return results

    def warm_up(self) -> None:
        width, height = self.config.input_size
        dummy = np.zeros((1, 3, height, width), dtype=np.float32)
        self.session.run(None, {self.input_name: dummy})

    def status(self) -> dict:
        return {
            "name": self.config.name,
            "type": self.config.detector_type,
            "classes": self.config.classes,
            "input": {
                "name": self.input.name,
                "shape": self.input.shape,
                "type": self.input.type,
            },
            "outputs": [
                {
                    "name": output.name,
                    "shape": output.shape,
                    "type": output.type,
                }
                for output in self.output_metadata
            ],
            "active_providers": self.session.get_providers(),
            "initialization_error": self.session_initialization_error,
            "max_batch_size": self._max_batch_size,
        }

    def _resolve_max_batch_size(self) -> int | None:
        if self.config.max_batch_size is not None:
            return self.config.max_batch_size

        first_dimension = self.input.shape[0] if self.input.shape else None
        if isinstance(first_dimension, int) and first_dimension > 0:
            return first_dimension

        return None

    def _prepare_frame(self, frame: DetectionFrame) -> PreparedFrame:
        image = frame.image
        original_height, original_width = image.shape[:2]
        input_width, input_height = self.config.input_size
        resize_ratio = min(input_width / original_width, input_height / original_height)

        if self.config.preprocessing.resize_mode == "letterbox":
            resized_width = int(original_width * resize_ratio)
            resized_height = int(original_height * resize_ratio)
            resized_image = cv2.resize(image, (resized_width, resized_height))
            resized = np.full(
                (input_height, input_width, image.shape[2]),
                self.config.preprocessing.pad_value,
                dtype=image.dtype,
            )
            resized[:resized_height, :resized_width] = resized_image
        elif self.config.preprocessing.resize_mode == "stretch":
            resized = cv2.resize(image, (input_width, input_height))
        else:
            raise ValueError(
                f"Unsupported resizeMode '{self.config.preprocessing.resize_mode}' "
                f"for detector {self.config.name}."
            )

        if self.config.preprocessing.color_format == "rgb":
            resized = cv2.cvtColor(resized, cv2.COLOR_BGR2RGB)
        elif self.config.preprocessing.color_format != "bgr":
            raise ValueError(
                f"Unsupported colorFormat '{self.config.preprocessing.color_format}' "
                f"for detector {self.config.name}."
            )

        blob = resized.astype(np.float32)
        blob *= self.config.preprocessing.scale

        if self.config.preprocessing.mean is not None:
            blob -= np.array(self.config.preprocessing.mean, dtype=np.float32)

        if self.config.preprocessing.std is not None:
            std = np.array(self.config.preprocessing.std, dtype=np.float32)
            blob /= np.where(std == 0, 1, std)

        blob = np.transpose(blob, (2, 0, 1))
        blob = np.expand_dims(blob, axis=0)

        return PreparedFrame(
            frame_index=frame.frame_index,
            input_tensor=blob,
            scale_x=original_width / input_width,
            scale_y=original_height / input_height,
            resize_ratio=resize_ratio,
            original_width=original_width,
            original_height=original_height,
        )

    def _postprocess(
        self,
        outputs: list[np.ndarray],
        frame: PreparedFrame,
        batch_index: int,
    ) -> list[DetectionResult]:
        if self.config.detector_type == "retinaface":
            return self._postprocess_retinaface(outputs, frame, batch_index)

        if self.config.detector_type == "yolo":
            return self._postprocess_yolo(outputs, frame, batch_index)

        if self.config.detector_type == "yolox":
            return self._postprocess_yolox(outputs, frame, batch_index)

        raise ValueError(f"Unsupported detector type '{self.config.detector_type}'.")

    def _postprocess_retinaface(
        self,
        outputs: list[np.ndarray],
        frame: PreparedFrame,
        batch_index: int,
    ) -> list[DetectionResult]:
        loc = outputs[0][batch_index]
        conf = outputs[1][batch_index]

        priors = self._get_priors()
        boxes = _decode_retinaface_boxes(loc, priors)
        input_width, input_height = self.config.input_size

        boxes[:, 0] *= input_width
        boxes[:, 1] *= input_height
        boxes[:, 2] *= input_width
        boxes[:, 3] *= input_height

        scores = conf[:, 1]
        mask = scores > self.config.confidence_threshold
        boxes = boxes[mask]
        scores = scores[mask]

        if len(boxes) == 0:
            return []

        boxes[:, 0] *= frame.scale_x
        boxes[:, 1] *= frame.scale_y
        boxes[:, 2] *= frame.scale_x
        boxes[:, 3] *= frame.scale_y

        class_name = self.config.classes[min(self.config.classes.keys())]
        return _nms_results(
            boxes,
            scores,
            np.zeros(len(scores), dtype=np.int32),
            {0: class_name},
            self.config.confidence_threshold,
            self.config.nms_threshold,
            frame.original_width,
            frame.original_height,
        )

    def _postprocess_yolo(
        self,
        outputs: list[np.ndarray],
        frame: PreparedFrame,
        batch_index: int,
    ) -> list[DetectionResult]:
        predictions = outputs[0][batch_index]
        output_config = self.config.yolo_output

        if output_config.layout == "columns":
            predictions = predictions.transpose()
        elif output_config.layout != "rows":
            raise ValueError(
                f"Unsupported YOLO output layout '{output_config.layout}' "
                f"for detector {self.config.name}."
            )

        if predictions.ndim != 2 or predictions.shape[1] < 5:
            logger.warning(
                "Detector %s returned unsupported YOLO output shape %s.",
                self.config.name,
                predictions.shape,
            )
            return []

        boxes = _convert_yolo_boxes(
            predictions[:, :4],
            output_config,
            frame,
        )
        scores, class_ids = _extract_yolo_scores(
            predictions,
            output_config,
            self.config.classes,
        )

        mask = scores > self.config.confidence_threshold
        if not np.any(mask):
            return []

        return _nms_results(
            boxes[mask],
            scores[mask],
            class_ids[mask],
            self.config.classes,
            self.config.confidence_threshold,
            self.config.nms_threshold,
            frame.original_width,
            frame.original_height,
        )

    def _postprocess_yolox(
        self,
        outputs: list[np.ndarray],
        frame: PreparedFrame,
        batch_index: int,
    ) -> list[DetectionResult]:
        predictions = outputs[0][batch_index]
        output_config = self.config.yolo_output

        if output_config.layout == "columns":
            predictions = predictions.transpose()
        elif output_config.layout != "rows":
            raise ValueError(
                f"Unsupported YOLOX output layout '{output_config.layout}' "
                f"for detector {self.config.name}."
            )

        if predictions.ndim != 2 or predictions.shape[1] < 5:
            logger.warning(
                "Detector %s returned unsupported YOLOX output shape %s.",
                self.config.name,
                predictions.shape,
            )
            return []

        grids, expanded_strides = _make_yolox_grids_strides(
            self.config.input_size,
            output_config.strides,
        )
        if grids.shape[0] != predictions.shape[0]:
            raise ValueError(
                f"Grid count does not match prediction count for detector {self.config.name}. "
                f"grids={grids.shape[0]}, predictions={predictions.shape[0]}."
            )

        raw_boxes = predictions[:, :4].astype(np.float32)
        decoded_boxes = np.empty_like(raw_boxes)
        decoded_boxes[:, 0:2] = (raw_boxes[:, 0:2] + grids) * expanded_strides
        decoded_boxes[:, 2:4] = np.exp(raw_boxes[:, 2:4]) * expanded_strides

        scores, class_ids = _extract_yolox_scores(
            predictions,
            output_config,
            self.config.classes,
        )
        mask = scores >= self.config.confidence_threshold
        if not np.any(mask):
            return []

        decoded_boxes = decoded_boxes[mask]
        scores = scores[mask]
        class_ids = class_ids[mask]

        boxes = np.zeros_like(decoded_boxes)
        boxes[:, 0] = decoded_boxes[:, 0] - decoded_boxes[:, 2] / 2
        boxes[:, 1] = decoded_boxes[:, 1] - decoded_boxes[:, 3] / 2
        boxes[:, 2] = decoded_boxes[:, 0] + decoded_boxes[:, 2] / 2
        boxes[:, 3] = decoded_boxes[:, 1] + decoded_boxes[:, 3] / 2
        boxes /= frame.resize_ratio

        return _nms_results(
            boxes,
            scores,
            class_ids,
            self.config.classes,
            self.config.confidence_threshold,
            self.config.nms_threshold,
            frame.original_width,
            frame.original_height,
        )

    def _get_priors(self) -> np.ndarray:
        if self._priors is None:
            input_width, input_height = self.config.input_size
            self._priors = _generate_retinaface_priors((input_height, input_width))

        return self._priors


def _chunks(values: list[DetectionFrame], size: int) -> Iterable[list[DetectionFrame]]:
    for index in range(0, len(values), size):
        yield values[index:index + size]


def _generate_retinaface_priors(
    image_size: tuple[int, int],
    min_sizes: list[list[int]] | None = None,
    steps: list[int] | None = None,
    clip: bool = False,
) -> np.ndarray:
    min_sizes = min_sizes or [[16, 32], [64, 128], [256, 512]]
    steps = steps or [8, 16, 32]
    priors = []
    image_h, image_w = image_size

    for index, step in enumerate(steps):
        feature_map_h = int(np.ceil(image_h / step))
        feature_map_w = int(np.ceil(image_w / step))

        for i in range(feature_map_h):
            for j in range(feature_map_w):
                for min_size in min_sizes[index]:
                    s_kx = min_size / image_w
                    s_ky = min_size / image_h
                    cx = (j + 0.5) * step / image_w
                    cy = (i + 0.5) * step / image_h
                    priors.append([cx, cy, s_kx, s_ky])

    priors_array = np.array(priors, dtype=np.float32)
    return np.clip(priors_array, 0.0, 1.0) if clip else priors_array


def _decode_retinaface_boxes(
    loc: np.ndarray,
    priors: np.ndarray,
    variances: tuple[float, float] = (0.1, 0.2),
) -> np.ndarray:
    boxes = np.concatenate(
        (
            priors[:, :2] + loc[:, :2] * variances[0] * priors[:, 2:],
            priors[:, 2:] * np.exp(loc[:, 2:] * variances[1]),
        ),
        axis=1,
    )

    boxes[:, :2] -= boxes[:, 2:] / 2
    boxes[:, 2:] += boxes[:, :2]

    return boxes


def _convert_yolo_boxes(
    raw_boxes: np.ndarray,
    output_config: YoloOutputConfig,
    frame: PreparedFrame,
) -> np.ndarray:
    if output_config.box_format == "cxcywh":
        cx = raw_boxes[:, 0]
        cy = raw_boxes[:, 1]
        width = raw_boxes[:, 2]
        height = raw_boxes[:, 3]
        boxes = np.stack(
            (
                cx - width / 2,
                cy - height / 2,
                cx + width / 2,
                cy + height / 2,
            ),
            axis=1,
        )
    elif output_config.box_format == "xywh":
        x = raw_boxes[:, 0]
        y = raw_boxes[:, 1]
        width = raw_boxes[:, 2]
        height = raw_boxes[:, 3]
        boxes = np.stack((x, y, x + width, y + height), axis=1)
    elif output_config.box_format == "xyxy":
        boxes = raw_boxes.copy()
    else:
        raise ValueError(f"Unsupported YOLO boxFormat '{output_config.box_format}'.")

    coordinate_scale = output_config.coordinate_scale
    if coordinate_scale == "auto":
        coordinate_scale = "normalized" if np.nanmax(np.abs(boxes)) <= 2.0 else "input"

    if coordinate_scale == "normalized":
        boxes[:, [0, 2]] *= frame.original_width
        boxes[:, [1, 3]] *= frame.original_height
    elif coordinate_scale == "input":
        boxes[:, [0, 2]] *= frame.scale_x
        boxes[:, [1, 3]] *= frame.scale_y
    else:
        raise ValueError(f"Unsupported YOLO coordinateScale '{output_config.coordinate_scale}'.")

    return boxes


def _extract_yolo_scores(
    predictions: np.ndarray,
    output_config: YoloOutputConfig,
    classes: dict[int, str],
) -> tuple[np.ndarray, np.ndarray]:
    if output_config.class_scores_start_index is not None:
        class_scores = predictions[:, output_config.class_scores_start_index:]
        relative_class_ids = np.argmax(class_scores, axis=1)
        selected_class_scores = class_scores[np.arange(class_scores.shape[0]), relative_class_ids]

        objectness = (
            predictions[:, output_config.score_index]
            if 0 <= output_config.score_index < output_config.class_scores_start_index
            else np.ones_like(selected_class_scores)
        )
        scores = objectness * selected_class_scores
        class_ids = relative_class_ids.astype(np.int32)
    else:
        scores = predictions[:, output_config.score_index]
        if output_config.class_index is None:
            class_ids = np.full(len(scores), min(classes.keys()), dtype=np.int32)
        else:
            class_ids = np.rint(predictions[:, output_config.class_index]).astype(np.int32)

    if output_config.score_activation == "sigmoid":
        scores = 1 / (1 + np.exp(-scores))
    elif output_config.score_activation != "none":
        raise ValueError(f"Unsupported YOLO scoreActivation '{output_config.score_activation}'.")

    return scores.astype(np.float32), class_ids


def _make_yolox_grids_strides(
    input_size: tuple[int, int],
    strides: tuple[int, ...],
) -> tuple[np.ndarray, np.ndarray]:
    input_width, input_height = input_size
    grids: list[np.ndarray] = []
    expanded_strides: list[np.ndarray] = []

    for stride in strides:
        grid_height = input_height // stride
        grid_width = input_width // stride
        xv, yv = np.meshgrid(np.arange(grid_width), np.arange(grid_height))
        grid = np.stack((xv, yv), axis=2).reshape(-1, 2)
        grids.append(grid)
        expanded_strides.append(np.full((grid.shape[0], 1), stride))

    return (
        np.concatenate(grids, axis=0).astype(np.float32),
        np.concatenate(expanded_strides, axis=0).astype(np.float32),
    )


def _extract_yolox_scores(
    predictions: np.ndarray,
    output_config: YoloOutputConfig,
    classes: dict[int, str],
) -> tuple[np.ndarray, np.ndarray]:
    objectness = _sigmoid_if_needed(predictions[:, output_config.score_index].astype(np.float32))

    if predictions.shape[1] == 5:
        scores = objectness
        class_ids = np.full(len(scores), min(classes.keys()), dtype=np.int32)
        return scores, class_ids

    class_scores_start = output_config.class_scores_start_index or 5
    class_scores = _sigmoid_if_needed(predictions[:, class_scores_start:].astype(np.float32))
    relative_class_ids = np.argmax(class_scores, axis=1)
    best_class_scores = class_scores[np.arange(class_scores.shape[0]), relative_class_ids]

    scores = objectness * best_class_scores
    return scores.astype(np.float32), relative_class_ids.astype(np.int32)


def _sigmoid_if_needed(values: np.ndarray) -> np.ndarray:
    if values.size == 0:
        return values

    if values.min() < 0 or values.max() > 1:
        return 1 / (1 + np.exp(-values))

    return values


def _nms_results(
    boxes_xyxy: np.ndarray,
    scores: np.ndarray,
    class_ids: np.ndarray,
    classes: dict[int, str],
    confidence_threshold: float,
    nms_threshold: float,
    original_width: int,
    original_height: int,
) -> list[DetectionResult]:
    results: list[DetectionResult] = []

    for class_id in sorted(set(class_ids.tolist())):
        if class_id not in classes:
            continue

        class_mask = class_ids == class_id
        class_boxes = boxes_xyxy[class_mask]
        class_scores = scores[class_mask]
        nms_boxes = [
            _box_to_nms_rect(box, original_width, original_height)
            for box in class_boxes
        ]

        valid_indexes = [
            index
            for index, box in enumerate(nms_boxes)
            if box[2] > 0 and box[3] > 0
        ]
        if not valid_indexes:
            continue

        filtered_boxes = [nms_boxes[index] for index in valid_indexes]
        filtered_scores = [float(class_scores[index]) for index in valid_indexes]
        indices = cv2.dnn.NMSBoxes(
            bboxes=filtered_boxes,
            scores=filtered_scores,
            score_threshold=confidence_threshold,
            nms_threshold=nms_threshold,
        )

        for flattened_index in _flatten_indices(indices):
            original_index = valid_indexes[int(flattened_index)]
            x, y, width, height = nms_boxes[original_index]
            results.append(
                DetectionResult(
                    className=classes[class_id],
                    confidence=float(class_scores[original_index]),
                    x=x,
                    y=y,
                    width=width,
                    height=height,
                )
            )

    return results


def _box_to_nms_rect(
    box: np.ndarray,
    original_width: int,
    original_height: int,
) -> list[int]:
    x1, y1, x2, y2 = box
    x1 = max(0, min(original_width, math.floor(float(x1))))
    y1 = max(0, min(original_height, math.floor(float(y1))))
    x2 = max(0, min(original_width, math.ceil(float(x2))))
    y2 = max(0, min(original_height, math.ceil(float(y2))))

    return [
        x1,
        y1,
        max(0, x2 - x1),
        max(0, y2 - y1),
    ]


def _flatten_indices(indices) -> list[int]:
    if indices is None or len(indices) == 0:
        return []

    return np.array(indices).reshape(-1).astype(int).tolist()
