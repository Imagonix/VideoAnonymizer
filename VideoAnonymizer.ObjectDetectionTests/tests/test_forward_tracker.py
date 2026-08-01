import json

import numpy as np
import pytest

import services.forward_tracker as forward_tracker
from models import BoundingBox, DetectionResult, TrackForwardRequest
from services.forward_tracker import Box, _init_tracker


class RecordingTracker:
    def __init__(self):
        self.bounding_box = None

    def init(self, frame, bounding_box):
        self.bounding_box = bounding_box


def test_tracker_init_uses_integer_bounding_box():
    tracker = RecordingTracker()
    frame = np.zeros((20, 30, 3), dtype=np.uint8)

    _init_tracker(tracker, frame, Box(1.2, 2.6, 10.1, 8.8))

    assert tracker.bounding_box == (1, 3, 10, 9)
    assert all(isinstance(value, int) for value in tracker.bounding_box)


@pytest.mark.asyncio
async def test_tracker_tolerates_temporary_detector_miss(monkeypatch):
    frames = [
        np.zeros((120, 160, 3), dtype=np.uint8),
        np.zeros((120, 160, 3), dtype=np.uint8),
        np.zeros((120, 160, 3), dtype=np.uint8),
    ]
    fake_capture = FakeVideoCapture(frames, fps=10.0)
    fake_tracker = DriftingTracker((11, 20, 30, 40))
    detector_results = iter([
        [],
        [create_detection(x=12)],
    ])

    monkeypatch.setattr(forward_tracker.cv2, "VideoCapture", lambda _: fake_capture)
    monkeypatch.setattr(forward_tracker, "_create_tracker", lambda _: fake_tracker)
    monkeypatch.setattr(forward_tracker, "detect_image", lambda _: next(detector_results))

    events = await collect_events(create_request())
    detections = [event for event in events if event["type"] == "detection"]

    assert [event["frameIndex"] for event in detections] == [1, 2]
    assert [event["x"] for event in detections] == [11, 12]
    assert not any(event["type"] == "gap" for event in events)
    assert events[-1]["stoppedReason"] == "end_of_video"


@pytest.mark.asyncio
async def test_tracker_continues_while_tracker_matches_without_detector_confirmation(monkeypatch):
    frames = [np.zeros((120, 160, 3), dtype=np.uint8) for _ in range(11)]
    fake_capture = FakeVideoCapture(frames, fps=10.0)
    fake_tracker = DriftingTracker((11, 20, 30, 40))

    monkeypatch.setattr(forward_tracker.cv2, "VideoCapture", lambda _: fake_capture)
    monkeypatch.setattr(forward_tracker, "_create_tracker", lambda _: fake_tracker)
    monkeypatch.setattr(forward_tracker, "detect_image", lambda _: [])

    events = await collect_events(create_request())
    detections = [event for event in events if event["type"] == "detection"]

    assert [event["frameIndex"] for event in detections] == list(range(1, 11))
    assert not any(event["type"] == "gap" for event in events)
    assert events[-1]["stoppedReason"] == "end_of_video"


@pytest.mark.asyncio
async def test_tracker_stops_instead_of_reattaching_after_object_reaches_frame_edge(monkeypatch):
    frames = [np.zeros((120, 160, 3), dtype=np.uint8) for _ in range(3)]
    fake_capture = FakeVideoCapture(frames, fps=10.0)
    fake_tracker = SequenceBoxTracker([
        (135, 20, 30, 40),
        (80, 20, 30, 40),
    ])

    monkeypatch.setattr(forward_tracker.cv2, "VideoCapture", lambda _: fake_capture)
    monkeypatch.setattr(forward_tracker, "_create_tracker", lambda _: fake_tracker)
    monkeypatch.setattr(forward_tracker, "detect_image", lambda _: [])

    events = await collect_events(create_request(
        boundingBox=BoundingBox(x=120, y=20, width=30, height=40),
    ))
    detections = [event for event in events if event["type"] == "detection"]

    assert [event["frameIndex"] for event in detections] == [1]
    assert detections[0]["x"] == 135
    assert detections[0]["width"] == 25
    assert not any(event["type"] == "gap" for event in events)
    assert events[-1]["stoppedReason"] == "left_frame"


@pytest.mark.asyncio
async def test_tracker_rejects_abrupt_reattachment_before_object_reaches_frame_edge(monkeypatch):
    frames = [np.zeros((120, 160, 3), dtype=np.uint8) for _ in range(4)]
    fake_capture = FakeVideoCapture(frames, fps=10.0)
    fake_tracker = SequenceBoxTracker([
        (12, 20, 30, 40),
        (80, 20, 30, 40),
    ])

    monkeypatch.setattr(forward_tracker.cv2, "VideoCapture", lambda _: fake_capture)
    monkeypatch.setattr(forward_tracker, "_create_tracker", lambda _: fake_tracker)
    monkeypatch.setattr(forward_tracker, "detect_image", lambda _: [])

    events = await collect_events(create_request(maxLostDurationMs=1))
    detections = [event for event in events if event["type"] == "detection"]

    assert [event["frameIndex"] for event in detections] == [1]
    assert events[-1]["stoppedReason"] == "lost_timeout"


@pytest.mark.asyncio
async def test_tracker_stops_when_outward_edge_motion_reverses_onto_another_object(monkeypatch):
    frames = [np.zeros((120, 160, 3), dtype=np.uint8) for _ in range(4)]
    fake_capture = FakeVideoCapture(frames, fps=10.0)
    fake_tracker = SequenceBoxTracker([
        (115, 20, 30, 40),
        (125, 20, 30, 40),
        (120, 20, 30, 40),
    ])

    monkeypatch.setattr(forward_tracker.cv2, "VideoCapture", lambda _: fake_capture)
    monkeypatch.setattr(forward_tracker, "_create_tracker", lambda _: fake_tracker)
    monkeypatch.setattr(forward_tracker, "detect_image", lambda _: [])

    events = await collect_events(create_request(
        boundingBox=BoundingBox(x=100, y=20, width=30, height=40),
    ))
    detections = [event for event in events if event["type"] == "detection"]

    assert [event["frameIndex"] for event in detections] == [1, 2]
    assert events[-1]["stoppedReason"] == "left_frame"


@pytest.mark.asyncio
async def test_tracker_reacquires_object_after_tracker_stops_matching(monkeypatch):
    frames = [np.zeros((120, 160, 3), dtype=np.uint8) for _ in range(4)]
    fake_capture = FakeVideoCapture(frames, fps=10.0)
    trackers = iter([
        LosingTracker((11, 20, 30, 40)),
        DriftingTracker((12, 20, 30, 40)),
    ])
    detector_results = iter([
        [],
        [create_detection(x=12)],
    ])

    monkeypatch.setattr(forward_tracker.cv2, "VideoCapture", lambda _: fake_capture)
    monkeypatch.setattr(forward_tracker, "_create_tracker", lambda _: next(trackers))
    monkeypatch.setattr(forward_tracker, "detect_image", lambda _: next(detector_results))

    events = await collect_events(create_request())
    detections = [event for event in events if event["type"] == "detection"]
    gaps = [event for event in events if event["type"] == "gap"]

    assert [event["frameIndex"] for event in detections] == [1, 3]
    assert detections[-1]["x"] == 12
    assert detections[-1]["reacquired"] is True
    assert gaps == [{"type": "gap", "startTimeMs": 200, "endTimeMs": 300}]
    assert events[-1]["reacquiredCount"] == 1


@pytest.mark.asyncio
async def test_detector_corrects_tracker_that_disagrees_with_last_verified_position(monkeypatch):
    frames = [
        np.zeros((120, 160, 3), dtype=np.uint8),
        np.zeros((120, 160, 3), dtype=np.uint8),
    ]
    fake_capture = FakeVideoCapture(frames, fps=10.0)
    fake_tracker = DriftingTracker((50, 20, 30, 40))

    monkeypatch.setattr(forward_tracker.cv2, "VideoCapture", lambda _: fake_capture)
    monkeypatch.setattr(forward_tracker, "_create_tracker", lambda _: fake_tracker)
    monkeypatch.setattr(forward_tracker, "detect_image", lambda _: [create_detection(x=12)])

    events = await collect_events(create_request())
    detections = [event for event in events if event["type"] == "detection"]

    assert len(detections) == 1
    assert detections[0]["x"] == 12
    assert fake_tracker.initialized_with == (12, 20, 30, 40)


@pytest.mark.asyncio
async def test_tracker_marks_object_lost_when_detector_confirms_only_drift_target(monkeypatch):
    frames = [
        np.zeros((120, 160, 3), dtype=np.uint8),
        np.zeros((120, 160, 3), dtype=np.uint8),
        np.zeros((120, 160, 3), dtype=np.uint8),
    ]
    fake_capture = FakeVideoCapture(frames, fps=10.0)
    fake_tracker = DriftingTracker((100, 20, 30, 40))

    monkeypatch.setattr(forward_tracker.cv2, "VideoCapture", lambda _: fake_capture)
    monkeypatch.setattr(forward_tracker, "_create_tracker", lambda _: fake_tracker)
    monkeypatch.setattr(forward_tracker, "detect_image", lambda _: [
        DetectionResult(
            className="face",
            confidence=0.95,
            x=100,
            y=20,
            width=30,
            height=40,
        )
    ])

    request = create_request(
        maxLostDurationMs=1,
    )

    events = await collect_events(request)

    assert [event for event in events if event["type"] == "detection"] == []
    assert events[0] == {"type": "progress", "frameIndex": 1, "timeMs": 100}
    assert any(event["type"] == "gap" for event in events)
    assert events[-1]["type"] == "complete"
    assert events[-1]["stoppedReason"] == "lost_timeout"


def create_request(**overrides):
    values = {
        "videoPath": "ignored.mp4",
        "seedFrameIndex": 0,
        "seedTimeMs": 0,
        "boundingBox": BoundingBox(x=10, y=20, width=30, height=40),
        "objectClass": "face",
        "trackId": 7,
        "persistEveryMs": 100,
        "recoveryDetectorIntervalMs": 1,
    }
    values.update(overrides)
    return TrackForwardRequest(**values)


def create_detection(x):
    return DetectionResult(
        className="face",
        confidence=0.95,
        x=x,
        y=20,
        width=30,
        height=40,
    )


async def collect_events(request):
    return [
        json.loads(event)
        async for event in forward_tracker.track_forward_stream(request)
    ]


class FakeVideoCapture:
    def __init__(self, frames, fps):
        self.frames = frames
        self.fps = fps
        self.index = 0
        self.released = False

    def isOpened(self):
        return True

    def get(self, _):
        return self.fps

    def set(self, _, value):
        self.index = int(value)

    def read(self):
        if self.index >= len(self.frames):
            return False, None

        frame = self.frames[self.index]
        self.index += 1
        return True, frame

    def release(self):
        self.released = True


class DriftingTracker:
    def __init__(self, bounding_box):
        self.bounding_box = bounding_box
        self.initialized_with = None

    def init(self, frame, bounding_box):
        self.initialized_with = bounding_box

    def update(self, frame):
        return True, self.bounding_box


class LosingTracker(DriftingTracker):
    def __init__(self, bounding_box):
        super().__init__(bounding_box)
        self.update_count = 0

    def update(self, frame):
        self.update_count += 1
        if self.update_count == 1:
            return True, self.bounding_box
        return False, self.bounding_box


class SequenceBoxTracker(DriftingTracker):
    def __init__(self, bounding_boxes):
        super().__init__(bounding_boxes[0])
        self.bounding_boxes = iter(bounding_boxes)

    def update(self, frame):
        return True, next(self.bounding_boxes)
