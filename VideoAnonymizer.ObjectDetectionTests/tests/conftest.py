import pytest
from fastapi.testclient import TestClient
from PIL import Image
import io
import base64
import sys
from pathlib import Path

root_dir = Path(__file__).resolve().parent.parent.parent
object_detection_path = root_dir / "VideoAnonymizer.ObjectDetection"

if str(object_detection_path) not in sys.path:
    sys.path.insert(0, str(object_detection_path))

from main import app

@pytest.fixture(scope="module")
def client():
    with TestClient(app) as test_client:
        yield test_client


@pytest.fixture
def valid_base64_image():
    img = Image.new("RGB", (640, 480), color="red")
    buf = io.BytesIO()
    img.save(buf, format="JPEG")
    buf.seek(0)
    return base64.b64encode(buf.getvalue()).decode("utf-8")


@pytest.fixture
def detect_request(valid_base64_image):
    return {
        "imageBase64": valid_base64_image,
        "sessionId": "test-session-123",
        "fps": 25.0
    }