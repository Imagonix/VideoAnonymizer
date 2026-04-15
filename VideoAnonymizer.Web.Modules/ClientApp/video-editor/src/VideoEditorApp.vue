<Timeline
  :duration="videoDuration"
  :currentTime="currentTime"
  @seek="seekTo"
>
  <TimelineRow
    v-for="obj in timelineObjects"
    :key="obj.key"
    :objectKey="obj.key"
    :color="obj.color"
    :duration="videoDuration"
    :segments="buildSegments(obj.key)"
  />
</Timeline>