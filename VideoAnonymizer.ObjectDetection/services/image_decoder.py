import base64
import cv2
import numpy as np

def decode_base64_image(image_base64: str) -> np.ndarray:
    image_bytes = base64.b64decode(image_base64)
    image_array = np.frombuffer(image_bytes, dtype=np.uint8)
    image = cv2.imdecode(image_array, cv2.IMREAD_COLOR)

    if image is None:
        raise ValueError("Image could not be decoded.")

    return image
