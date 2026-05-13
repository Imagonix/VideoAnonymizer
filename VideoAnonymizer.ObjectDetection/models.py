# models.py
from pydantic import BaseModel, Field
from typing import Optional

class DetectRequest(BaseModel):
    imageBase64: str


class BatchDetectFrame(BaseModel):
    frameIndex: int
    imageBase64: str


class BatchDetectRequest(BaseModel):
    frames: list[BatchDetectFrame]


class DetectionResult(BaseModel):
    className: str
    confidence: float
    x: int
    y: int
    width: int
    height: int
    trackId: Optional[int] = Field(
        default=None,
        json_schema_extra={"type": "integer", "nullable": True}
    )


class BatchDetectionResult(BaseModel):
    frameIndex: int
    detections: list[DetectionResult]


class TrackRequest(BaseModel):
    detections: list[DetectionResult]
    sessionId: str
    fps: float = 25.0
