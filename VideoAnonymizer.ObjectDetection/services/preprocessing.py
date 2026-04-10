import cv2
import numpy as np
from core.config import INPUT_SIZE

def preprocess(image: np.ndarray) -> tuple[np.ndarray, float, float]:
    original_h, original_w = image.shape[:2]
    resized = cv2.resize(image, INPUT_SIZE)
    rgb = cv2.cvtColor(resized, cv2.COLOR_BGR2RGB)
    blob = rgb.astype(np.float32)

    blob = np.transpose(blob, (2, 0, 1))
    blob = np.expand_dims(blob, axis=0)

    scale_x = original_w / INPUT_SIZE[0]
    scale_y = original_h / INPUT_SIZE[1]

    return blob, scale_x, scale_y