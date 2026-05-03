const mountedApps = new WeakMap();
let cssLoaded = false;

function ensureCssLoaded() {
    if (cssLoaded) return;

    const href = '/_content/VideoAnonymizer.Web.Modules/vue/video-editor/index.css';
    const existing = document.querySelector(`link[href="${href}"]`);
    if (existing) {
        cssLoaded = true;
        return;
    }

    const link = document.createElement('link');
    link.rel = 'stylesheet';
    link.href = href;
    document.head.appendChild(link);
    cssLoaded = true;
}

async function ensureAssetsLoaded() {
    ensureCssLoaded();

    if (!window.__videoEditorVueLoader) {
        window.__videoEditorVueLoader = import('/_content/VideoAnonymizer.Web.Modules/vue/video-editor/index.js');
    }

    await window.__videoEditorVueLoader;

    if (!window.mountVideoEditorVueApp) {
        throw new Error('Vue VideoEditor bundle did not expose mount function.');
    }
}

export async function mountVideoEditor(element, props, dotNetRef) {
    await ensureAssetsLoaded();

    const callbacks = {
        onDetectedObjectAdded: (videoId, analyzedFrameId, dto) =>
            dotNetRef.invokeMethodAsync('OnDetectedObjectAdded', videoId, analyzedFrameId, dto),
        onDetectedObjectUpdated: (videoId, analyzedFrameId, dto) =>
            dotNetRef.invokeMethodAsync('OnDetectedObjectUpdated', videoId, analyzedFrameId, dto),
        onDetectedObjectsBulkUpdated: (videoId, dtos) =>
            dotNetRef.invokeMethodAsync('OnDetectedObjectsBulkUpdated', videoId, dtos),
    };

    const appHandle = window.mountVideoEditorVueApp(element, { ...props, ...callbacks });
    mountedApps.set(element, appHandle);
}

export async function updateVideoEditor(element, props) {
    const appHandle = mountedApps.get(element);
    if (!appHandle || typeof appHandle.update !== 'function') return;
    appHandle.update(props);
}

export async function updateVideoEditorSettings(element, settings) {
    const appHandle = mountedApps.get(element);
    if (!appHandle || typeof appHandle.updateSettings !== 'function') return;
    appHandle.updateSettings(settings);
}

export async function unmountVideoEditor(element) {
    const appHandle = mountedApps.get(element);
    if (appHandle && typeof appHandle.unmount === 'function') {
        appHandle.unmount();
    }

    mountedApps.delete(element);
}

export async function getFrames(element) {
    const appHandle = mountedApps.get(element);

    if (!appHandle || typeof appHandle.getFrames !== 'function') {
        return null;
    }

    return appHandle.getFrames();
}
