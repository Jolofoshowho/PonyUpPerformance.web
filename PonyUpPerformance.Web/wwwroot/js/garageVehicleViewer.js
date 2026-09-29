import * as THREE from 'https://esm.sh/three@0.160.0';
import { GLTFLoader } from 'https://esm.sh/three@0.160.0/examples/jsm/loaders/GLTFLoader.js';
import {
    findVehicleAsset,
    applyAssetTransform,
    applyVehiclePaint,
    normalizeModelToPodium
} from './vehicleModelRegistry.js';

const mount = document.getElementById('garage3dViewer');
const qualityBadge = document.getElementById('garageModelQuality');

function setQuality(text, state) {
    if (!qualityBadge) {
        return;
    }

    qualityBadge.textContent = text;
    qualityBadge.classList.remove(
        'exact',
        'fallback');

    if (state) {
        qualityBadge.classList.add(state);
    }
}

function showUnavailable(message) {
    if (!mount) {
        return;
    }

    mount.innerHTML = '';

    const wrap = document.createElement('div');
    wrap.className = 'garage-model-unavailable';

    const mark = document.createElement('div');
    mark.className = 'garage-model-unavailable-mark';
    mark.textContent = '3D';

    const title = document.createElement('strong');
    title.textContent = 'REALISTIC MODEL NOT YET AVAILABLE';

    const copy = document.createElement('span');
    copy.textContent =
        message ||
        'PonyUp will not substitute a blocky vehicle for the wrong car.';

    wrap.appendChild(mark);
    wrap.appendChild(title);
    wrap.appendChild(copy);
    mount.appendChild(wrap);

    setQuality(
        'MODEL NEEDED',
        'fallback');
}

function loadCarImagesScript(apiKey) {
    return new Promise((resolve, reject) => {
        const existing =
            document.querySelector(
                'script[data-ponyup-carimages]');

        if (existing) {
            if (existing.dataset.loaded === 'true') {
                resolve();
                return;
            }

            existing.addEventListener(
                'load',
                () => resolve(),
                { once: true });

            existing.addEventListener(
                'error',
                reject,
                { once: true });

            return;
        }

        const script =
            document.createElement('script');

        script.src =
            'https://carimagesapi.com/assets/js/carimages.js';

        script.dataset.apiKey =
            apiKey;

        script.dataset.ponyupCarimages =
            'true';

        script.addEventListener(
            'load',
            () => {
                script.dataset.loaded = 'true';
                resolve();
            },
            { once: true });

        script.addEventListener(
            'error',
            reject,
            { once: true });

        document.head.appendChild(script);
    });
}

async function showCatalogFallback(identity, apiKey) {
    if (!mount || !apiKey) {
        showUnavailable(
            'PonyUp is building this vehicle for the first-party catalog.');
        return;
    }

    mount.innerHTML = '';

    const viewer =
        document.createElement(
            'model-viewer');

    viewer.id =
        'garageCatalogViewer';

    viewer.className =
        'garage-catalog-viewer';

    viewer.setAttribute(
        'data-ci-type',
        'car');

    viewer.setAttribute(
        'data-ci-make',
        identity.make);

    viewer.setAttribute(
        'data-ci-model',
        identity.model);

    viewer.setAttribute(
        'data-ci-year',
        identity.year);

    viewer.setAttribute(
        'data-ci-view',
        'front34');

    viewer.setAttribute(
        'data-ci-speed',
        '12');

    viewer.setAttribute(
        'data-ci-light',
        '7');

    viewer.setAttribute(
        'data-ci-backdrop',
        'radial-gradient(120% 120% at 50% 28%, #22272b, #090c0e)');

    viewer.setAttribute(
        'camera-controls',
        '');

    viewer.setAttribute(
        'auto-rotate',
        '');

    viewer.setAttribute(
        'shadow-intensity',
        '1');

    viewer.setAttribute(
        'exposure',
        '1.05');

    mount.appendChild(viewer);

    setQuality(
        'CATALOG 3D',
        'fallback');

    try {
        await loadCarImagesScript(
            apiKey);
    }
    catch (error) {
        console.warn(
            'PonyUp catalog 3D fallback failed to initialize.',
            error);

        showUnavailable(
            'The external catalog could not load this vehicle. PonyUp will not show a block substitute.');
    }
}

function buildPodium(scene) {
    const podiumGroup =
        new THREE.Group();

    scene.add(podiumGroup);

    const top =
        new THREE.Mesh(
            new THREE.CylinderGeometry(
                4.0,
                4.0,
                0.14,
                160),
            new THREE.MeshPhysicalMaterial({
                color: 0x171a1c,
                metalness: 0.9,
                roughness: 0.22,
                clearcoat: 0.45
            }));

    top.position.y = -0.94;
    top.receiveShadow = true;
    podiumGroup.add(top);

    const ring =
        new THREE.Mesh(
            new THREE.TorusGeometry(
                3.76,
                0.03,
                18,
                220),
            new THREE.MeshStandardMaterial({
                color: 0xff3131,
                emissive: 0xff1818,
                emissiveIntensity: 2.1,
                metalness: 0.4,
                roughness: 0.14
            }));

    ring.rotation.x = Math.PI / 2;
    ring.position.y = -0.86;
    podiumGroup.add(ring);

    const base =
        new THREE.Mesh(
            new THREE.CylinderGeometry(
                4.12,
                4.18,
                0.38,
                160),
            new THREE.MeshStandardMaterial({
                color: 0x080a0b,
                metalness: 0.8,
                roughness: 0.28
            }));

    base.position.y = -1.12;
    base.receiveShadow = true;
    scene.add(base);

    const floor =
        new THREE.Mesh(
            new THREE.CircleGeometry(
                10,
                160),
            new THREE.MeshStandardMaterial({
                color: 0x090b0c,
                metalness: 0.42,
                roughness: 0.42
            }));

    floor.rotation.x = -Math.PI / 2;
    floor.position.y = -1.31;
    floor.receiveShadow = true;
    scene.add(floor);

    return podiumGroup;
}

function addStudioLights(scene) {
    const ambient =
        new THREE.HemisphereLight(
            0xf5f8ff,
            0x08090a,
            1.55);

    scene.add(ambient);

    const key =
        new THREE.DirectionalLight(
            0xffffff,
            4.1);

    key.position.set(
        5.3,
        7.6,
        6.2);

    key.castShadow = true;
    key.shadow.mapSize.width = 2048;
    key.shadow.mapSize.height = 2048;
    scene.add(key);

    const redLeft =
        new THREE.PointLight(
            0xff3131,
            22,
            15,
            2);

    redLeft.position.set(
        -4.8,
        1.3,
        4.0);

    scene.add(redLeft);

    const redRight =
        new THREE.PointLight(
            0xff3131,
            16,
            14,
            2);

    redRight.position.set(
        4.4,
        1.0,
        -3.2);

    scene.add(redRight);

    const fill =
        new THREE.PointLight(
            0xffffff,
            14,
            18,
            2);

    fill.position.set(
        3.8,
        4.1,
        6.2);

    scene.add(fill);
}

async function showPonyUpModel(identity, asset, paint) {
    if (!mount) {
        return;
    }

    mount.innerHTML = '';

    const scene =
        new THREE.Scene();

    const camera =
        new THREE.PerspectiveCamera(
            30,
            Math.max(mount.clientWidth, 1) /
            Math.max(mount.clientHeight, 1),
            0.1,
            100);

    camera.position.set(
        7.0,
        3.1,
        8.9);

    camera.lookAt(
        0,
        0.35,
        0);

    const renderer =
        new THREE.WebGLRenderer({
            antialias: true,
            alpha: true,
            powerPreference: 'high-performance'
        });

    renderer.setPixelRatio(
        Math.min(
            window.devicePixelRatio || 1,
            2));

    renderer.setSize(
        Math.max(mount.clientWidth, 1),
        Math.max(mount.clientHeight, 1));

    renderer.shadowMap.enabled = true;
    renderer.shadowMap.type =
        THREE.PCFSoftShadowMap;

    renderer.outputColorSpace =
        THREE.SRGBColorSpace;

    renderer.toneMapping =
        THREE.ACESFilmicToneMapping;

    renderer.toneMappingExposure =
        1.05;

    mount.appendChild(
        renderer.domElement);

    const turntable =
        new THREE.Group();

    turntable.rotation.y = -0.55;
    scene.add(turntable);

    buildPodium(scene);
    addStudioLights(scene);

    const loader =
        new GLTFLoader();

    setQuality(
        'LOADING PONYUP 3D',
        'exact');

    const gltf =
        await new Promise(
            (resolve, reject) => {
                loader.load(
                    asset.asset,
                    resolve,
                    undefined,
                    reject);
            });

    const model =
        gltf.scene ||
        gltf.scenes?.[0];

    if (!model) {
        throw new Error(
            'The GLB did not contain a renderable scene.');
    }

    model.traverse(object => {
        if (!object.isMesh) {
            return;
        }

        object.castShadow = true;
        object.receiveShadow = true;
    });

    normalizeModelToPodium(
        THREE,
        model,
        Number(
            asset.targetLength ||
            5.7));

    applyAssetTransform(
        model,
        asset);

    applyVehiclePaint(
        model,
        paint,
        asset);

    turntable.add(model);

    setQuality(
        'PONYUP 3D',
        'exact');

    let dragging = false;
    let lastX = 0;
    let lastInteraction =
        performance.now();

    mount.addEventListener(
        'pointerdown',
        event => {
            dragging = true;
            lastX = event.clientX;
            lastInteraction =
                performance.now();

            mount.setPointerCapture(
                event.pointerId);
        });

    mount.addEventListener(
        'pointermove',
        event => {
            if (!dragging) {
                return;
            }

            const delta =
                event.clientX - lastX;

            turntable.rotation.y +=
                delta * 0.008;

            lastX =
                event.clientX;

            lastInteraction =
                performance.now();
        });

    function stopDragging(event) {
        dragging = false;
        lastInteraction =
            performance.now();

        if (event?.pointerId !== undefined &&
            mount.hasPointerCapture?.(
                event.pointerId)) {

            mount.releasePointerCapture(
                event.pointerId);
        }
    }

    mount.addEventListener(
        'pointerup',
        stopDragging);

    mount.addEventListener(
        'pointercancel',
        stopDragging);

    mount.addEventListener(
        'wheel',
        event => {
            event.preventDefault();

            lastInteraction =
                performance.now();

            camera.position.z +=
                event.deltaY * 0.006;

            camera.position.z =
                Math.max(
                    6.5,
                    Math.min(
                        11.4,
                        camera.position.z));

            camera.lookAt(
                0,
                0.35,
                0);
        },
        {
            passive: false
        });

    function resize() {
        const width =
            Math.max(
                mount.clientWidth,
                1);

        const height =
            Math.max(
                mount.clientHeight,
                1);

        camera.aspect =
            width / height;

        camera.updateProjectionMatrix();

        renderer.setSize(
            width,
            height);
    }

    window.addEventListener(
        'resize',
        resize);

    const observer =
        new ResizeObserver(
            resize);

    observer.observe(
        mount);

    const clock =
        new THREE.Clock();

    function animate() {
        requestAnimationFrame(
            animate);

        const delta =
            Math.min(
                clock.getDelta(),
                0.04);

        const idleFor =
            performance.now() -
            lastInteraction;

        if (!dragging &&
            idleFor > 1800) {

            turntable.rotation.y +=
                delta * 0.16;
        }

        renderer.render(
            scene,
            camera);
    }

    resize();
    animate();
}

async function initializeGarageVehicle() {
    if (!mount) {
        return;
    }

    const identity = {
        vin:
            mount.dataset.vin || '',
        year:
            mount.dataset.year || '',
        make:
            mount.dataset.make || '',
        model:
            mount.dataset.model || '',
        trim:
            mount.dataset.trim || ''
    };

    const paint =
        mount.dataset.paint ||
        '#b8b8b8';

    const apiKey =
        mount.dataset.carImagesKey ||
        '';

    if (!identity.make ||
        !identity.model ||
        !identity.year) {

        showUnavailable(
            'Add Year, Make, and Model so PonyUp can identify the correct vehicle.');
        return;
    }

    setQuality(
        'CHECKING PONYUP CATALOG',
        '');

    const asset =
        await findVehicleAsset(
            identity);

    if (asset) {
        try {
            await showPonyUpModel(
                identity,
                asset,
                paint);

            return;
        }
        catch (error) {
            console.warn(
                'PonyUp first-party 3D model failed to load.',
                asset.asset,
                error);
        }
    }

    await showCatalogFallback(
        identity,
        apiKey);
}

void initializeGarageVehicle();
