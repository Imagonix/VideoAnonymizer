import pytest
from PIL import Image
import io
import base64
import time

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