from typing import Dict, List
import supervision as sv
import numpy as np

from models import DetectionResult 


class ObjectTrackerManager:
    def __init__(self):
        self.trackers: Dict[str, sv.ByteTrack] = {}

    def get_or_create_tracker(self, session_id: str, fps: float = 25.0) -> sv.ByteTrack:
        if session_id not in self.trackers:
            self.trackers[session_id] = sv.ByteTrack(
                track_activation_threshold=0.25,  
                lost_track_buffer=30,             
                minimum_matching_threshold=0.8,   
                frame_rate=int(fps)               
            )
        return self.trackers[session_id]

    def reset_tracker(self, session_id: str):
        if session_id in self.trackers:
            self.trackers[session_id].reset()

    def cleanup_tracker(self, session_id: str):
        self.trackers.pop(session_id, None)

    def cleanup_all(self):
        self.trackers.clear()

    def track_detections(
        self,
        detections_list: List[DetectionResult],
        session_id: str,
        fps: float = 25.0,
        class_name: str = "face"
    ) -> List[DetectionResult]:
        if not detections_list:
            return []

        tracker = self.get_or_create_tracker(session_id, fps)

        xyxy = []
        confidence = []
        class_id = []

        for det in detections_list:
            x1 = det.x
            y1 = det.y
            x2 = det.x + det.width
            y2 = det.y + det.height

            xyxy.append([x1, y1, x2, y2])
            confidence.append(det.confidence)
            class_id.append(0)

        sv_detections = sv.Detections(
            xyxy=np.array(xyxy, dtype=np.float32),
            confidence=np.array(confidence, dtype=np.float32),
            class_id=np.array(class_id, dtype=int)
        )

        tracked_detections = tracker.update_with_detections(sv_detections)

        results: List[DetectionResult] = []
        for i in range(len(tracked_detections.xyxy)):
            x1, y1, x2, y2 = tracked_detections.xyxy[i]
            conf = float(tracked_detections.confidence[i])

            track_id = None
            if tracked_detections.tracker_id is not None:
                track_id = int(tracked_detections.tracker_id[i])

            results.append(
                DetectionResult(
                    className=class_name,
                    confidence=conf,
                    x=int(max(0, x1)),
                    y=int(max(0, y1)),
                    width=int(x2 - x1),
                    height=int(y2 - y1),
                    trackId=track_id
                )
            )

        return results