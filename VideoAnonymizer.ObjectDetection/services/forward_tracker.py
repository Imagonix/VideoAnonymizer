import logging
import math
from dataclasses import dataclass

import cv2

from models import (
    DetectionResult,
    TrackForwardDetectionResult,
    TrackForwardGap,
    TrackForwardRequest,
    TrackForwardResponse,
)
from services.detection_service import detect_image

logger = logging.getLogger(__name__)


@dataclass(frozen=True)
class Box:
    x: float
    y: float
    width: float
    height: float

    @property
    def center_x(self) -> float:
        return self.x + self.width / 2

    @property
    def center_y(self) -> float:
        return self.y + self.height / 2

    @property
    def area(self) -> float:
        return max(0.0, self.width) * max(0.0, self.height)

    @property
    def diagonal(self) -> float:
        return math.hypot(self.width, self.height)


def track_forward(request: TrackForwardRequest) -> TrackForwardResponse:
    capture = cv2.VideoCapture(request.videoPath)
    if not capture.isOpened():
        raise ValueError(f"Could not open video: {request.videoPath}")

    try:
        fps = capture.get(cv2.CAP_PROP_FPS)
        if fps <= 0:
            fps = 25.0

        seed_frame_index = _resolve_seed_frame_index(request, fps)
        capture.set(cv2.CAP_PROP_POS_FRAMES, seed_frame_index)

        success, frame = capture.read()
        if not success or frame is None:
            raise ValueError(f"Could not read seed frame {seed_frame_index}.")

        frame_height, frame_width = frame.shape[:2]
        seed_box = _clip_box(
            Box(
                float(request.boundingBox.x),
                float(request.boundingBox.y),
                float(request.boundingBox.width),
                float(request.boundingBox.height),
            ),
            frame_width,
            frame_height,
        )
        if seed_box is None:
            raise ValueError("Seed bounding box does not intersect the video frame.")

        tracker = _create_tracker(request.trackerType)
        _init_tracker(tracker, frame, seed_box)

        persist_frame_indexes = {index for index in request.persistFrameIndexes if index > seed_frame_index}
        next_persist_time_ms = request.seedTimeMs + max(1, request.persistEveryMs)
        max_track_time_ms = request.seedTimeMs + max(1, request.maxTrackDurationMs)

        detections: list[TrackForwardDetectionResult] = []
        gaps: list[TrackForwardGap] = []
        reacquired_count = 0
        stopped_reason = "end_of_video"
        last_box = seed_box
        frame_index = seed_frame_index
        lost_started_ms: int | None = None
        last_detector_run_ms: int | None = None

        while True:
            success, frame = capture.read()
            if not success or frame is None:
                stopped_reason = "end_of_video"
                break

            frame_index += 1
            current_time_ms = _frame_time_ms(frame_index, fps)
            if current_time_ms > max_track_time_ms:
                stopped_reason = "max_track_duration"
                break

            frame_height, frame_width = frame.shape[:2]
            should_persist = _should_persist(
                frame_index,
                current_time_ms,
                persist_frame_indexes,
                next_persist_time_ms,
            )

            if lost_started_ms is None:
                ok, tracked_box_raw = tracker.update(frame)
                tracked_box = _clip_box(Box(*tracked_box_raw), frame_width, frame_height) if ok else None
                if tracked_box is not None and _is_sane_box(tracked_box, last_box, frame_width, frame_height):
                    last_box = tracked_box
                    if should_persist:
                        detections.append(_to_detection(request, frame_index, current_time_ms, last_box, False))
                    if not persist_frame_indexes:
                        next_persist_time_ms = _advance_next_persist_time(
                            next_persist_time_ms,
                            request.persistEveryMs,
                            current_time_ms,
                        )
                    continue

                lost_started_ms = current_time_ms
                last_detector_run_ms = None
                logger.debug("Tracker lost object at frame %s.", frame_index)
                continue

            if current_time_ms - lost_started_ms > request.maxLostDurationMs:
                gaps.append(TrackForwardGap(startTimeMs=lost_started_ms, endTimeMs=current_time_ms))
                stopped_reason = "lost_timeout"
                break

            detector_due = (
                last_detector_run_ms is None
                or current_time_ms - last_detector_run_ms >= request.recoveryDetectorIntervalMs
            )
            if not detector_due:
                continue

            last_detector_run_ms = current_time_ms
            reacquired = _find_recovery_detection(
                frame,
                last_box,
                request.objectClass,
                request.searchAreaExpansion,
            )
            if reacquired is None:
                continue

            reacquired_box = _clip_box(
                Box(float(reacquired.x), float(reacquired.y), float(reacquired.width), float(reacquired.height)),
                frame_width,
                frame_height,
            )
            if reacquired_box is None:
                continue

            gaps.append(TrackForwardGap(startTimeMs=lost_started_ms, endTimeMs=current_time_ms))
            lost_started_ms = None
            last_detector_run_ms = None
            last_box = reacquired_box
            reacquired_count += 1
            tracker = _create_tracker(request.trackerType)
            _init_tracker(tracker, frame, last_box)

            if should_persist:
                detections.append(_to_detection(request, frame_index, current_time_ms, last_box, True))
            if not persist_frame_indexes:
                next_persist_time_ms = _advance_next_persist_time(
                    next_persist_time_ms,
                    request.persistEveryMs,
                    current_time_ms,
                )

        return TrackForwardResponse(
            trackId=request.trackId,
            detections=detections,
            reacquiredCount=reacquired_count,
            gaps=gaps,
            stoppedReason=stopped_reason,
        )
    finally:
        capture.release()


def _resolve_seed_frame_index(request: TrackForwardRequest, fps: float) -> int:
    if request.seedFrameIndex is not None:
        return max(0, request.seedFrameIndex)

    return max(0, int(round((request.seedTimeMs / 1000.0) * fps)))


def _create_tracker(preferred_tracker_type: str):
    normalized = (preferred_tracker_type or "CSRT").upper()
    candidates = [normalized]
    for fallback in ["CSRT", "KCF", "MIL"]:
        if fallback not in candidates:
            candidates.append(fallback)

    for name in candidates:
        creator = getattr(cv2, f"Tracker{name}_create", None)
        if creator is not None:
            return creator()

        legacy = getattr(cv2, "legacy", None)
        legacy_creator = getattr(legacy, f"Tracker{name}_create", None) if legacy is not None else None
        if legacy_creator is not None:
            return legacy_creator()

    raise ValueError("No supported OpenCV tracker is available. Tried CSRT, KCF and MIL.")


def _init_tracker(tracker, frame, box: Box) -> None:
    tracker.init(frame, _to_cv_rect(box))


def _to_cv_rect(box: Box) -> tuple[int, int, int, int]:
    return (
        int(round(box.x)),
        int(round(box.y)),
        max(1, int(round(box.width))),
        max(1, int(round(box.height))),
    )


def _frame_time_ms(frame_index: int, fps: float) -> int:
    return int(round((frame_index / fps) * 1000))


def _should_persist(
    frame_index: int,
    current_time_ms: int,
    persist_frame_indexes: set[int],
    next_persist_time_ms: int,
) -> bool:
    if persist_frame_indexes:
        return frame_index in persist_frame_indexes

    return current_time_ms >= next_persist_time_ms


def _advance_next_persist_time(next_persist_time_ms: int, persist_every_ms: int, current_time_ms: int) -> int:
    interval = max(1, persist_every_ms)
    while next_persist_time_ms <= current_time_ms:
        next_persist_time_ms += interval

    return next_persist_time_ms


def _is_sane_box(box: Box, previous: Box, frame_width: int, frame_height: int) -> bool:
    if box.width < 4 or box.height < 4:
        return False

    if not _intersects_frame(box, frame_width, frame_height):
        return False

    previous_area = max(previous.area, 1.0)
    scale_ratio = box.area / previous_area
    if scale_ratio < 0.20 or scale_ratio > 5.0:
        return False

    center_jump = math.hypot(box.center_x - previous.center_x, box.center_y - previous.center_y)
    max_jump = max(64.0, previous.diagonal * 3.0)
    return center_jump <= max_jump


def _intersects_frame(box: Box, frame_width: int, frame_height: int) -> bool:
    return (
        box.x < frame_width
        and box.y < frame_height
        and box.x + box.width > 0
        and box.y + box.height > 0
    )


def _clip_box(box: Box, frame_width: int, frame_height: int) -> Box | None:
    x1 = max(0.0, min(float(frame_width), box.x))
    y1 = max(0.0, min(float(frame_height), box.y))
    x2 = max(0.0, min(float(frame_width), box.x + box.width))
    y2 = max(0.0, min(float(frame_height), box.y + box.height))

    width = x2 - x1
    height = y2 - y1
    if width <= 0 or height <= 0:
        return None

    return Box(x1, y1, width, height)


def _find_recovery_detection(
    frame,
    last_box: Box,
    object_class: str,
    search_area_expansion: float,
) -> DetectionResult | None:
    detections = detect_image(frame)
    search_area = _expand_box(last_box, max(1.0, search_area_expansion))
    candidates = [
        detection
        for detection in detections
        if _same_class(detection.className, object_class)
        and _center_inside(Box(detection.x, detection.y, detection.width, detection.height), search_area)
    ]

    if not candidates:
        return None

    return max(
        candidates,
        key=lambda detection: _recovery_score(
            Box(detection.x, detection.y, detection.width, detection.height),
            last_box,
            detection.confidence,
        ),
    )


def _expand_box(box: Box, factor: float) -> Box:
    width = box.width * factor
    height = box.height * factor
    return Box(
        box.center_x - width / 2,
        box.center_y - height / 2,
        width,
        height,
    )


def _center_inside(box: Box, area: Box) -> bool:
    return (
        area.x <= box.center_x <= area.x + area.width
        and area.y <= box.center_y <= area.y + area.height
    )


def _same_class(actual: str | None, expected: str | None) -> bool:
    return (actual or "").strip().lower() == (expected or "").strip().lower()


def _recovery_score(box: Box, last_box: Box, confidence: float) -> float:
    distance = math.hypot(box.center_x - last_box.center_x, box.center_y - last_box.center_y)
    distance_penalty = distance / max(1.0, last_box.diagonal)
    return _iou(box, last_box) + float(confidence) * 0.25 - distance_penalty * 0.1


def _iou(a: Box, b: Box) -> float:
    x1 = max(a.x, b.x)
    y1 = max(a.y, b.y)
    x2 = min(a.x + a.width, b.x + b.width)
    y2 = min(a.y + a.height, b.y + b.height)
    intersection = max(0.0, x2 - x1) * max(0.0, y2 - y1)
    union = a.area + b.area - intersection
    return 0.0 if union <= 0 else intersection / union


def _to_detection(
    request: TrackForwardRequest,
    frame_index: int,
    time_ms: int,
    box: Box,
    reacquired: bool,
) -> TrackForwardDetectionResult:
    return TrackForwardDetectionResult(
        frameIndex=frame_index,
        timeMs=time_ms,
        className=request.objectClass,
        confidence=0.90 if reacquired else 0.80,
        x=int(round(box.x)),
        y=int(round(box.y)),
        width=max(1, int(round(box.width))),
        height=max(1, int(round(box.height))),
        trackId=request.trackId,
        reacquired=reacquired,
    )
