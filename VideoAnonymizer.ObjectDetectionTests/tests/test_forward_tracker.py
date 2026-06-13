import numpy as np

from services.forward_tracker import Box, _init_tracker


class RecordingTracker:
    def __init__(self):
        self.bounding_box = None

    def init(self, frame, bounding_box):
        self.bounding_box = bounding_box


def test_tracker_init_uses_integer_bounding_box():
    tracker = RecordingTracker()
    frame = np.zeros((20, 30, 3), dtype=np.uint8)

    _init_tracker(tracker, frame, Box(1.2, 2.6, 10.1, 8.8))

    assert tracker.bounding_box == (1, 3, 10, 9)
    assert all(isinstance(value, int) for value in tracker.bounding_box)
