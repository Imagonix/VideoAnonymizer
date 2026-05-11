from typing import List
from models import DetectRequest, DetectionResult
from services.image_decoder import decode_base64_image
from services.preprocessing import preprocess
from services.postprocessing import postprocess
from services.model_session import session, input_name

def detect_objects(request: DetectRequest) -> List[DetectionResult]:
    image = decode_base64_image(request.imageBase64)
    input_tensor, scale_x, scale_y = preprocess(image)
    outputs = session.run(None, {input_name: input_tensor})
    return postprocess(outputs, scale_x, scale_y)
