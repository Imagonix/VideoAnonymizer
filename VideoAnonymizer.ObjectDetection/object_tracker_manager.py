from dataclasses import dataclass, field
from typing import Dict, List
import supervision as sv
import numpy as np

from models import DetectionResult


@dataclass
class TrackingSession:
    trackers: Dict[str, sv.ByteTrack] = field(default_factory=dict)
    global_track_ids: Dict[tuple[str, int], int] = field(default_factory=dict)
    next_track_id: int = 1


class ObjectTrackerManager:
    def __init__(self):
        self.sessions: Dict[str, TrackingSession] = {}

    def get_or_create_session(self, session_id: str) -> TrackingSession:
        if session_id not in self.sessions:
            self.sessions[session_id] = TrackingSession()
        return self.sessions[session_id]

    def get_or_create_tracker(
        self,
        session: TrackingSession,
        class_name: str,
        fps: float = 25.0
    ) -> sv.ByteTrack:
        if class_name not in session.trackers:
            session.trackers[class_name] = sv.ByteTrack(
                track_activation_threshold=0.35,
                lost_track_buffer=45,
                minimum_matching_threshold=0.55,
                frame_rate=int(fps),
                minimum_consecutive_frames=3
            )
        return session.trackers[class_name]

    def reset_tracker(self, session_id: str):
        self.sessions.pop(session_id, None)

    def cleanup_tracker(self, session_id: str):
        self.sessions.pop(session_id, None)

    def cleanup_all(self):
        self.sessions.clear()

    def track_detections(
        self,
        detections_list: List[DetectionResult],
        session_id: str,
        fps: float = 25.0
    ) -> List[DetectionResult]:
        if not detections_list:
            return []

        session = self.get_or_create_session(session_id)
        results: List[DetectionResult] = []

        grouped_detections = _group_detections_by_class(detections_list)
        for class_name, class_detections in grouped_detections.items():
            results.extend(
                self._track_class_detections(
                    session,
                    class_name,
                    class_detections,
                    fps
                )
            )

        return results

    def _track_class_detections(
        self,
        session: TrackingSession,
        class_name: str,
        detections_list: List[DetectionResult],
        fps: float
    ) -> List[DetectionResult]:
        tracker = self.get_or_create_tracker(session, class_name, fps)

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
                local_track_id = int(tracked_detections.tracker_id[i])
                track_id = self._get_or_create_global_track_id(
                    session,
                    class_name,
                    local_track_id
                )

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

    def _get_or_create_global_track_id(
        self,
        session: TrackingSession,
        class_name: str,
        local_track_id: int
    ) -> int:
        key = (class_name, local_track_id)
        if key not in session.global_track_ids:
            session.global_track_ids[key] = session.next_track_id
            session.next_track_id += 1

        return session.global_track_ids[key]


def _group_detections_by_class(
    detections_list: List[DetectionResult]
) -> Dict[str, List[DetectionResult]]:
    grouped: Dict[str, List[DetectionResult]] = {}
    for detection in detections_list:
        grouped.setdefault(detection.className, []).append(detection)

    return grouped
