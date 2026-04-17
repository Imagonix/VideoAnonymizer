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

export async function mountVideoEditor(element, props) {
    await ensureAssetsLoaded();
    const appHandle = window.mountVideoEditorVueApp(element, props);
    mountedApps.set(element, appHandle);
}

export async function updateVideoEditor(element, props) {
    const appHandle = mountedApps.get(element);
    if (!appHandle || typeof appHandle.update !== 'function') return;
    appHandle.update(props);
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
    console.log('getFrames called', { element, appHandle, keys: appHandle ? Object.keys(appHandle) : null });

    if (!appHandle || typeof appHandle.getFrames !== 'function') {
        console.log('missing getFrames on appHandle');
        return null;
    }

    const result = appHandle.getFrames();
    console.log('frames returned', result);
    return result;
}