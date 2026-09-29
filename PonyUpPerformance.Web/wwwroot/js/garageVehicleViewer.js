const viewer =
    document.getElementById(
        'garageCatalogViewer');

const qualityBadge =
    document.getElementById(
        'garageModelQuality');

if (viewer) {
    const setLoaded = () => {
        viewer.classList.add(
            'garage-catalog-viewer-loaded');

        if (qualityBadge) {
            qualityBadge.textContent =
                'CATALOG 3D';
            qualityBadge.classList.add(
                'exact');
        }
    };

    const setError = () => {
        viewer.classList.add(
            'garage-catalog-viewer-error');

        if (qualityBadge) {
            qualityBadge.textContent =
                'CATALOG FALLBACK';
            qualityBadge.classList.remove(
                'exact');
            qualityBadge.classList.add(
                'fallback');
        }
    };

    viewer.addEventListener(
        'load',
        setLoaded);

    viewer.addEventListener(
        'error',
        setError);

    /*
     * The provider loader can resolve the model before
     * this module executes. In that case model-viewer
     * will already expose a src attribute.
     */
    window.setTimeout(() => {
        if (viewer.getAttribute('src')) {
            setLoaded();
        }
    }, 800);
}
