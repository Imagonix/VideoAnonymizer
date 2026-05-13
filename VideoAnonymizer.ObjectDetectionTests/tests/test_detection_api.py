import pytest
from PIL import Image
import io
import base64
import time
from types import SimpleNamespace

import numpy as np

from models import DetectionResult
from object_tracker_manager import ObjectTrackerManager

def test_detectObjects_multiple_fresh_sessions(client, valid_base64_image):
    for i in range(3):
        session_id = f"csharp-session-{i}-{int(time.time() * 1000)}"
        
        request = {
            "imageBase64": valid_base64_image,
            "sessionId": session_id,
            "fps": 25.0
        }

        response = client.post("/detectObjects", json=request)
        assert response.status_code == 200, f"Session {session_id} failed"

def test_health_endpoint(client):
    response = client.get("/health")
    assert response.status_code == 200
    data = response.json()
    assert data["status"] == "running"
    assert "cuda_available" in data


def test_detectObjects_valid_image(client, detect_request):
    response = client.post("/detectObjects", json=detect_request)
    
    assert response.status_code == 200
    data = response.json()
    
    assert isinstance(data, list)
    
    for detection in data:
        assert detection["className"] == "face"
        assert "confidence" in detection
        assert "x" in detection
        assert "y" in detection
        assert "width" in detection
        assert "height" in detection
        assert "trackId" in detection

def test_trackObjects_empty_detections(client):
    request = {
        "detections": [],
        "sessionId": "track-test-empty",
        "fps": 25.0
    }

    response = client.post("/trackObjects", json=request)

    assert response.status_code == 200
    assert response.json() == []


def test_tracker_reuses_track_id_for_same_class_and_local_track():
    manager = ObjectTrackerManager()
    _use_fake_tracker_with_local_track_id_one(manager)

    first_result = manager.track_detections(
        [
            DetectionResult(className="face", confidence=0.9, x=10, y=10, width=20, height=20),
        ],
        session_id="same-class"
    )[0]

    second_result = manager.track_detections(
        [
            DetectionResult(className="face", confidence=0.9, x=10, y=10, width=20, height=20),
        ],
        session_id="same-class"
    )[0]

    assert first_result.trackId == second_result.trackId


def test_tracker_assigns_distinct_track_ids_for_different_classes_with_same_local_track():
    manager = ObjectTrackerManager()
    _use_fake_tracker_with_local_track_id_one(manager)

    results = manager.track_detections(
        [
            DetectionResult(className="face", confidence=0.9, x=10, y=10, width=20, height=20),
            DetectionResult(className="license-plate", confidence=0.8, x=10, y=10, width=20, height=20),
        ],
        session_id="same-position-different-classes"
    )

    assert results[0].trackId != results[1].trackId


def _use_fake_tracker_with_local_track_id_one(manager):
    trackers = {}

    class FakeTracker:
        def update_with_detections(self, detections):
            return SimpleNamespace(
                xyxy=detections.xyxy,
                confidence=detections.confidence,
                tracker_id=np.ones(len(detections.xyxy), dtype=int)
            )

    def fake_get_or_create_tracker(session, class_name, fps=25.0):
        if class_name not in trackers:
            trackers[class_name] = FakeTracker()

        return trackers[class_name]

    manager.get_or_create_tracker = fake_get_or_create_tracker

def test_resetTracker(client):
    response = client.post("/resetTracker?sessionId=test-session-456")
    assert response.status_code == 200
    data = response.json()
    assert data["status"] == "ok"
    assert "test-session-456" in data["message"]


def test_cleanupTracker(client):
    response = client.post("/cleanupTracker?sessionId=test-session-789")
    assert response.status_code == 200
    data = response.json()
    assert data["status"] == "ok"
    assert "test-session-789" in data["message"]


def test_detectObjects_empty_image(client):
    img = Image.new("RGB", (1, 1), color="black")
    buf = io.BytesIO()
    img.save(buf, format="JPEG")
    buf.seek(0)
    tiny_base64 = base64.b64encode(buf.getvalue()).decode("utf-8")

    request = {
        "imageBase64": tiny_base64,
        "sessionId": "tiny-test",
        "fps": 30.0
    }
    response = client.post("/detectObjects", json=request)
    assert response.status_code == 200
