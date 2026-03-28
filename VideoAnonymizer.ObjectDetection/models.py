# models.py
from pydantic import BaseModel
from typing import Optional

class DetectRequest(BaseModel):
    imageBase64: str
    sessionId: str
    fps: float = 25.0


class DetectionResult(BaseModel):
    """Standard Response-Modell für Detektionen mit optionaler Track-ID"""
    className: str
    confidence: float
    x: int
    y: int
    width: int
    height: int
    trackId: Optional[int] = None