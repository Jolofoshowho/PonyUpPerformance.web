import * as THREE from 'https://esm.sh/three@0.160.0';
import { GLTFLoader } from 'https://esm.sh/three@0.160.0/examples/jsm/loaders/GLTFLoader.js';
import {
    findVehicleAsset,
    applyAssetTransform,
    applyVehiclePaint,
    applyInteriorColor,
    applyWheelFinish,
    applyWheelVariant,
    getCustomizationOptions,
    normalizeModelToPodium
} from './vehicleModelRegistry.js';

const mount = document.getElementById('garage3dViewer');
const qualityBadge = document.getElementById('garageModelQuality');

const interiorOptionsHost =
    document.querySelector(
        '[data-customizer-options="interior"]');

const wheelFinishHost =
    document.querySelector(
        '[data-customizer-options="wheel-finish"]');

const wheelStyleHost =
    document.querySelector(
        '[data-customizer-options="wheel-style"]');

const interiorStatus =
    document.querySelector(
        '[data-customizer-status="interior"]');

const wheelStatus =
    document.querySelector(
        '[data-customizer-status="wheels"]');

function appearanceStorageKey() {
    const vehicleId =
        mount?.dataset.vehicleId || 'unsaved';

    return `ponyup.garage.appearance.${vehicleId}`;
}

function readAppearance() {
    try {
        const accountValue =
            mount?.dataset.appearance;

        if (accountValue &&
            accountValue.trim() &&
            accountValue.trim() !== '{}') {

            return JSON.parse(
                accountValue);
        }

        const localValue =
            localStorage.getItem(
                appearanceStorageKey());

        return localValue
            ? JSON.parse(localValue)
            : {};
    }
    catch {
        return {};
    }
}

function saveAppearance(appearance) {
    const serialized =
        JSON.stringify(appearance);

    if (mount) {
        mount.dataset.appearance =
            serialized;
    }

    try {
        localStorage.setItem(
            appearanceStorageKey(),
            serialized);
    }
    catch {
        // Server persistence below remains the primary account copy.
    }

    const form =
        document.getElementById(
            'garageAppearancePersistence');

    if (!form) {
        return;
    }

    const data =
        new FormData(form);

    data.set(
        'appearanceJson',
        serialized);

    fetch(
        form.action ||
        window.location.href,
        {
            method: 'POST',
            body: data,
            credentials: 'same-origin',
            headers: {
                'X-Requested-With':
                    'XMLHttpRequest'
            }
        })
        .then(response => {
            if (!response.ok) {
                throw new Error(
                    `Appearance save failed: ${response.status}`);
            }

            return response.json();
        })
        .catch(error => {
            console.warn(
                'PonyUp Garage appearance could not be saved to the account.',
                error);
        });
}

function setCustomizerUnavailable(message) {
    if (interiorOptionsHost) {
        interiorOptionsHost.innerHTML = '';
    }

    if (wheelFinishHost) {
        wheelFinishHost.innerHTML = '';
    }

    if (wheelStyleHost) {
        wheelStyleHost.innerHTML = '';
    }

    if (interiorStatus) {
        interiorStatus.textContent =
            message ||
            'Interior customization requires a PonyUp 3D model.';
    }

    if (wheelStatus) {
        wheelStatus.textContent =
            message ||
            'Wheel customization requires a PonyUp 3D model.';
    }
}

function renderColorOptions(
    host,
    options,
    selectedId,
    onSelect) {

    if (!host) {
        return;
    }

    host.innerHTML = '';

    options.forEach(option => {
        const button =
            document.createElement('button');

        button.type = 'button';
        button.className =
            'garage-option-swatch';

        if (option.id === selectedId) {
            button.classList.add('selected');
        }

        button.title =
            option.label || option.id;

        button.setAttribute(
            'aria-label',
            option.label || option.id);

        const color =
            document.createElement('span');

        color.style.background =
            option.hex || '#888888';

        const label =
            document.createElement('small');

        label.textContent =
            option.label || option.id;

        button.appendChild(color);
        button.appendChild(label);

        button.addEventListener(
            'click',
            () => onSelect(
                option,
                button));

        host.appendChild(button);
    });
}

function renderWheelStyles(
    host,
    styles,
    selectedId,
    onSelect) {

    if (!host) {
        return;
    }

    host.innerHTML = '';

    if (!Array.isArray(styles) ||
        styles.length <= 1) {
        return;
    }

    const label =
        document.createElement('p');

    label.className =
        'garage-customizer-note';

    label.textContent =
        'Wheel style';

    host.appendChild(label);

    const row =
        document.createElement('div');

    row.className =
        'garage-wheel-style-row';

    styles.forEach(style => {
        const button =
            document.createElement('button');

        button.type = 'button';
        button.className =
            'garage-wheel-style-button';

        if (style.id === selectedId) {
            button.classList.add('selected');
        }

        button.textContent =
            style.label || style.id;

        button.addEventListener(
            'click',
            () => onSelect(
                style,
                button));

        row.appendChild(button);
    });

    host.appendChild(row);
}

function markSelected(
    host,
    selectedButton) {

    if (!host) {
        return;
    }

    host.querySelectorAll(
        'button.selected')
        .forEach(button =>
            button.classList.remove(
                'selected'));

    selectedButton.classList.add(
        'selected');
}

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

    setCustomizerUnavailable();
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

    setCustomizerUnavailable(
        'Customization is available when a PonyUp-owned 3D model is loaded.');

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

    const customization =
        getCustomizationOptions(
            asset);

    const appearance =
        readAppearance();

    const selectedInterior =
        customization.interiorColors.find(
            option =>
                option.id ===
                appearance.interiorId) ||
        customization.interiorColors[0] ||
        null;

    const selectedWheelFinish =
        customization.wheelFinishes.find(
            option =>
                option.id ===
                appearance.wheelFinishId) ||
        customization.wheelFinishes[0] ||
        null;

    const selectedWheelStyle =
        customization.wheelStyles.find(
            option =>
                option.id ===
                appearance.wheelStyleId) ||
        customization.wheelStyles[0] ||
        null;

    let interiorMatches = 0;
    let wheelMatches = 0;

    if (selectedInterior) {
        interiorMatches =
            applyInteriorColor(
                model,
                selectedInterior.hex,
                asset);
    }

    if (selectedWheelFinish) {
        wheelMatches =
            applyWheelFinish(
                model,
                selectedWheelFinish.hex,
                asset);
    }

    if (selectedWheelStyle) {
        applyWheelVariant(
            model,
            selectedWheelStyle.id,
            asset);
    }

    if (interiorMatches > 0 &&
        customization.interiorColors.length > 0) {

        if (interiorStatus) {
            interiorStatus.textContent =
                selectedInterior?.label ||
                'Interior';
        }

        renderColorOptions(
            interiorOptionsHost,
            customization.interiorColors,
            selectedInterior?.id,
            (option, button) => {
                const changed =
                    applyInteriorColor(
                        model,
                        option.hex,
                        asset);

                if (changed <= 0) {
                    return;
                }

                markSelected(
                    interiorOptionsHost,
                    button);

                appearance.interiorId =
                    option.id;

                saveAppearance(
                    appearance);

                if (interiorStatus) {
                    interiorStatus.textContent =
                        option.label;
                }
            });
    }
    else if (interiorStatus) {
        interiorStatus.textContent =
            'Interior material mapping is still needed for this model.';
    }

    if (wheelMatches > 0 &&
        customization.wheelFinishes.length > 0) {

        if (wheelStatus) {
            wheelStatus.textContent =
                selectedWheelStyle?.label
                    ? `${selectedWheelStyle.label} · ${selectedWheelFinish?.label || ''}`
                    : selectedWheelFinish?.label || 'Wheels';
        }

        renderColorOptions(
            wheelFinishHost,
            customization.wheelFinishes,
            selectedWheelFinish?.id,
            (option, button) => {
                const changed =
                    applyWheelFinish(
                        model,
                        option.hex,
                        asset);

                if (changed <= 0) {
                    return;
                }

                markSelected(
                    wheelFinishHost,
                    button);

                appearance.wheelFinishId =
                    option.id;

                saveAppearance(
                    appearance);

                if (wheelStatus) {
                    wheelStatus.textContent =
                        option.label;
                }
            });

        renderWheelStyles(
            wheelStyleHost,
            customization.wheelStyles,
            selectedWheelStyle?.id,
            (style, button) => {
                if (!applyWheelVariant(
                        model,
                        style.id,
                        asset)) {
                    return;
                }

                const row =
                    button.parentElement;

                markSelected(
                    row,
                    button);

                appearance.wheelStyleId =
                    style.id;

                saveAppearance(
                    appearance);

                if (wheelStatus) {
                    wheelStatus.textContent =
                        style.label;
                }
            });
    }
    else if (wheelStatus) {
        wheelStatus.textContent =
            'Wheel material mapping is still needed for this model.';
    }

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
