export function triggerFileDownload(fileName, url) {
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName || '';
    document.body.appendChild(anchor);
    anchor.click();
    setTimeout(() => anchor.remove(), 100);
} 