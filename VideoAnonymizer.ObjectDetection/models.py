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
    blurShape: str = "ellipse"
    trackId: Optional[int] = Field(
        default=None,
        json_schema_extra={"type": "integer", "nullable": True}
    )


class BatchDetectionResult(BaseModel):
    frameIndex: int
    detections: list[DetectionResult]


class BoundingBox(BaseModel):
    x: int
    y: int
    width: int
    height: int


class TrackForwardRequest(BaseModel):
    videoPath: str
    seedFrameIndex: Optional[int] = None
    seedTimeMs: int = 0
    boundingBox: BoundingBox
    objectClass: str
    trackId: int
    persistEveryMs: int = 100
    persistFrameIndexes: list[int] = Field(default_factory=list)
    maxLostDurationMs: int = 5000
    recoveryDetectorIntervalMs: int = 250
    trackerType: str = "CSRT"
    searchAreaExpansion: float = 3.0
    maxTrackDurationMs: int = 30000


class TrackForwardDetectionResult(BaseModel):
    frameIndex: int
    timeMs: int
    className: str
    confidence: float
    x: int
    y: int
    width: int
    height: int
    blurShape: str = "ellipse"
    trackId: int
    reacquired: bool = False


class TrackForwardGap(BaseModel):
    startTimeMs: int
    endTimeMs: int


class TrackForwardResponse(BaseModel):
    trackId: int
    detections: list[TrackForwardDetectionResult]
    reacquiredCount: int = 0
    gaps: list[TrackForwardGap] = Field(default_factory=list)
    stoppedReason: str = "completed"
