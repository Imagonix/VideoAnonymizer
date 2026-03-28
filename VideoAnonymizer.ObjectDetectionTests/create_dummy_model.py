import onnx
from onnx import helper, numpy_helper
import numpy as np
import os

os.makedirs("models", exist_ok=True)

input_info = helper.make_tensor_value_info("input", onnx.TensorProto.FLOAT, [1, 3, 640, 640])

loc_info = helper.make_tensor_value_info("loc", onnx.TensorProto.FLOAT, [1, 16800, 4])
conf_info = helper.make_tensor_value_info("conf", onnx.TensorProto.FLOAT, [1, 16800, 2])
landms_info = helper.make_tensor_value_info("landms", onnx.TensorProto.FLOAT, [1, 16800, 10])

loc_data = np.random.randn(1, 16800, 4).astype(np.float32) * 0.1
conf_data = np.random.rand(1, 16800, 2).astype(np.float32) * 0.4
landms_data = np.zeros((1, 16800, 10), dtype=np.float32)

loc_node = helper.make_node("Constant", inputs=[], outputs=["loc"], value=numpy_helper.from_array(loc_data))
conf_node = helper.make_node("Constant", inputs=[], outputs=["conf"], value=numpy_helper.from_array(conf_data))
landms_node = helper.make_node("Constant", inputs=[], outputs=["landms"], value=numpy_helper.from_array(landms_data))

graph = helper.make_graph(
    nodes=[loc_node, conf_node, landms_node],
    name="DummyRetinaFace",
    inputs=[input_info],
    outputs=[loc_info, conf_info, landms_info]
)

model = helper.make_model(graph, producer_name="DummyFaceDetector")
model = helper.make_model(graph, producer_name="DummyFaceDetector", opset_imports=[helper.make_opsetid("", 17)])

onnx.save(model, "models/FaceDetector.onnx")

print("✅ Dummy FaceDetector.onnx created successfully (Opset 17)")
print("Path: models/FaceDetector.onnx")