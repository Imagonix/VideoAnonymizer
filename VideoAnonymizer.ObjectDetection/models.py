# models.py
from pydantic import BaseModel, Field
from typing import Optional

class DetectRequest(BaseModel):
    imageBase64: str
    sessionId: str
    fps: float = 25.0


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