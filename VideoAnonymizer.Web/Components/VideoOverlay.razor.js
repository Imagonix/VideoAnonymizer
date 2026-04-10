export function getCurrentTime(videoElement) {
    return videoElement?.currentTime ?? 0;
}

export function setCurrentTime(videoElement, timeSeconds) {
    if (!videoElement) {
        return;
    }

    videoElement.currentTime = timeSeconds ?? 0;
}

export function getVideoSize(videoElement) {
    if (!videoElement) {
        return {
            videoWidth: 0,
            videoHeight: 0,
            clientWidth: 0,
            clientHeight: 0
        };
    }

    return {
        videoWidth: videoElement.videoWidth ?? 0,
        videoHeight: videoElement.videoHeight ?? 0,
        clientWidth: videoElement.clientWidth ?? 0,
        clientHeight: videoElement.clientHeight ?? 0
    };
}