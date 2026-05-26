import json
import logging
import os
from dataclasses import dataclass
from pathlib import Path
from typing import Any

logger = logging.getLogger(__name__)

DEFAULT_MODELS_DIR = Path(__file__).resolve().parent.parent / "models"


@dataclass(frozen=True)
class PreprocessingConfig:
    color_format: str = "rgb"
    scale: float = 1.0
    resize_mode: str = "stretch"
    pad_value: int = 114
    mean: tuple[float, float, float] | None = None
    std: tuple[float, float, float] | None = None


@dataclass(frozen=True)
class YoloOutputConfig:
    layout: str = "rows"
    box_format: str = "cxcywh"
    score_index: int = 4
    class_index: int | None = None
    class_scores_start_index: int | None = None
    coordinate_scale: str = "auto"
    score_activation: str = "none"
    strides: tuple[int, ...] = (8, 16, 32)


@dataclass(frozen=True)
class DetectorConfig:
    name: str
    model_path: Path
    detector_type: str
    classes: dict[int, str]
    enabled: bool
    input_size: tuple[int, int]
    confidence_threshold: float
    nms_threshold: float
    max_batch_size: int | None
    preprocessing: PreprocessingConfig
    yolo_output: YoloOutputConfig


def load_detector_configs() -> list[DetectorConfig]:
    model_paths = _find_model_paths()
    configs: list[DetectorConfig] = []

    for model_path in model_paths:
        config_path = _find_config_path(model_path)
        if config_path is None:
            logger.warning(
                "Skipping model %s because no detector config was found.",
                model_path,
            )
            continue

        config = _read_detector_config(model_path, config_path)
        if not config.enabled:
            logger.info("Skipping disabled detector %s from %s.", config.name, config_path)
            continue

        configs.append(config)

    if not configs:
        models_dir = get_models_directory()
        raise RuntimeError(
            "No enabled object detector models were found. "
            f"Models directory: {models_dir}"
        )

    return configs


def get_models_directory() -> Path:
    face_model_path = os.getenv("FACE_DETECTOR_MODEL_PATH")
    if face_model_path:
        return Path(face_model_path).expanduser().resolve().parent

    return DEFAULT_MODELS_DIR.resolve()


def _find_model_paths() -> list[Path]:
    models_dir = get_models_directory()
    model_paths = set()

    if models_dir.exists():
        model_paths.update(path.resolve() for path in models_dir.glob("*.onnx"))

    return sorted(model_paths, key=lambda path: path.name.lower())


def _find_config_path(model_path: Path) -> Path | None:
    candidate = model_path.with_suffix(".detector.json")
    return candidate if candidate.exists() else None


def _read_detector_config(model_path: Path, config_path: Path) -> DetectorConfig:
    with config_path.open("r", encoding="utf-8") as file:
        data = json.load(file)

    detector_type = str(data.get("detectorType") or data.get("type") or "").strip().lower()
    if detector_type not in {"retinaface", "yolo", "yolox"}:
        raise ValueError(
            f"Detector config {config_path} has unsupported detectorType '{detector_type}'."
        )

    name = str(data.get("name") or model_path.stem).strip()
    if not name:
        raise ValueError(f"Detector config {config_path} must define a non-empty name.")

    classes = _parse_classes(data.get("classes") or data.get("classNames"), config_path)
    input_size = _parse_input_size(data.get("inputSize"), config_path)

    return DetectorConfig(
        name=name,
        model_path=model_path,
        detector_type=detector_type,
        classes=classes,
        enabled=bool(data.get("enabled", True)),
        input_size=input_size,
        confidence_threshold=float(data.get("confidenceThreshold", 0.5)),
        nms_threshold=float(data.get("nmsThreshold", 0.45)),
        max_batch_size=_parse_optional_positive_int(data.get("maxBatchSize"), config_path),
        preprocessing=_parse_preprocessing(data.get("preprocessing") or {}),
        yolo_output=_parse_yolo_output(data.get("output") or {}),
    )


def _parse_classes(value: Any, config_path: Path) -> dict[int, str]:
    if isinstance(value, list):
        classes = {
            index: str(class_name).strip()
            for index, class_name in enumerate(value)
            if str(class_name).strip()
        }
    elif isinstance(value, dict):
        classes = {
            int(index): str(class_name).strip()
            for index, class_name in value.items()
            if str(class_name).strip()
        }
    else:
        raise ValueError(f"Detector config {config_path} must define classes.")

    if not classes:
        raise ValueError(f"Detector config {config_path} must define at least one class.")

    return classes


def _parse_input_size(value: Any, config_path: Path) -> tuple[int, int]:
    if value is None:
        return (640, 640)

    if not isinstance(value, list) or len(value) != 2:
        raise ValueError(f"Detector config {config_path} inputSize must be [width, height].")

    width = int(value[0])
    height = int(value[1])
    if width <= 0 or height <= 0:
        raise ValueError(f"Detector config {config_path} inputSize values must be positive.")

    return (width, height)


def _parse_optional_positive_int(value: Any, config_path: Path) -> int | None:
    if value is None:
        return None

    parsed = int(value)
    if parsed <= 0:
        raise ValueError(f"Detector config {config_path} maxBatchSize must be positive.")

    return parsed


def _parse_preprocessing(value: dict[str, Any]) -> PreprocessingConfig:
    mean = _parse_optional_float_triplet(value.get("mean"))
    std = _parse_optional_float_triplet(value.get("std"))
    return PreprocessingConfig(
        color_format=str(value.get("colorFormat", "rgb")).strip().lower(),
        scale=float(value.get("scale", 1.0)),
        resize_mode=str(value.get("resizeMode", "stretch")).strip().lower(),
        pad_value=int(value.get("padValue", 114)),
        mean=mean,
        std=std,
    )


def _parse_yolo_output(value: dict[str, Any]) -> YoloOutputConfig:
    return YoloOutputConfig(
        layout=str(value.get("layout", "rows")).strip().lower(),
        box_format=str(value.get("boxFormat", "cxcywh")).strip().lower(),
        score_index=int(value.get("scoreIndex", 4)),
        class_index=_parse_nullable_int(value.get("classIndex")),
        class_scores_start_index=_parse_nullable_int(value.get("classScoresStartIndex")),
        coordinate_scale=str(value.get("coordinateScale", "auto")).strip().lower(),
        score_activation=str(value.get("scoreActivation", "none")).strip().lower(),
        strides=_parse_strides(value.get("strides")),
    )


def _parse_nullable_int(value: Any) -> int | None:
    return None if value is None else int(value)


def _parse_optional_float_triplet(value: Any) -> tuple[float, float, float] | None:
    if value is None:
        return None

    if not isinstance(value, list) or len(value) != 3:
        raise ValueError("Expected a three-value numeric array.")

    return (float(value[0]), float(value[1]), float(value[2]))


def _parse_strides(value: Any) -> tuple[int, ...]:
    if value is None:
        return (8, 16, 32)

    if not isinstance(value, list) or not value:
        raise ValueError("Expected strides to be a non-empty numeric array.")

    strides = tuple(int(stride) for stride in value)
    if any(stride <= 0 for stride in strides):
        raise ValueError("Expected strides to contain positive values.")

    return strides
