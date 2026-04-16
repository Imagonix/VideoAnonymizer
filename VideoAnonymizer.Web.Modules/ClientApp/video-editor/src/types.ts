export type DetectedObjectDto = {
  id: string;
  confidence: number;
  className: string | null;
  selected: boolean;
  trackId: number | null;
  x: number;
  y: number;
  width: number;
  height: number;
  analyzedFrameId: string;
};

export type AnalyzedFrameDto = {
  id: string;
  timeSeconds: number;
  videoId: string;
  detectedObjects: DetectedObjectDto[];
};

export type VideoEditorProps = {
  videoId: string;
  videoSourceUrl: string;
  frames: AnalyzedFrameDto[];
};

export type TimelineObjectBase = {
};

export type SingleTimelineObject = TimelineObjectBase & {
  detectedObj: DetectedObjectDto;
  type: 'single';
  timeSeconds: number;
};

export type TrackedTimelineObject = TimelineObjectBase & {
  type: 'tracked';
  occurences: [number, DetectedObjectDto][];
};

export type TimelineObject = SingleTimelineObject | TrackedTimelineObject;