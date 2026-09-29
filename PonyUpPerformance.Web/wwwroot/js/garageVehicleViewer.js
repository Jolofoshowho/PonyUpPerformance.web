import * as THREE from 'https://unpkg.com/three@0.160.0/build/three.module.js';

const addVehiclePanel = document.getElementById('garageAddVehicle');

document.querySelectorAll('[data-open-add-vehicle]').forEach(button => {
    button.addEventListener('click', () => {
        if (!addVehiclePanel) {
            return;
        }

        addVehiclePanel.open = true;
        addVehiclePanel.scrollIntoView({
            behavior: 'smooth',
            block: 'start'
        });

        window.setTimeout(() => {
            addVehiclePanel.querySelector('input')?.focus();
        }, 350);
    });
});

document.querySelectorAll('[data-focus-mileage]').forEach(button => {
    button.addEventListener('click', () => {
        const mileage = document.getElementById('garageMileage');

        if (!mileage) {
            return;
        }

        mileage.scrollIntoView({
            behavior: 'smooth',
            block: 'center'
        });

        window.setTimeout(() => {
            mileage.focus();
            mileage.select();
        }, 350);
    });
});

const mount = document.getElementById('garage3dViewer');

if (mount) {
    const paint = mount.dataset.paint || '#b8b8b8';
    const bodyStyle = (mount.dataset.body || 'Sedan').toLowerCase();
    const renderType = (mount.dataset.render || bodyStyle).toLowerCase();
    const fallback = mount.querySelector('.garage-render-fallback');

    try {
        const scene = new THREE.Scene();

        const camera = new THREE.PerspectiveCamera(
            30,
            Math.max(mount.clientWidth, 1) / Math.max(mount.clientHeight, 1),
            0.1,
            100
        );

        camera.position.set(6.9, 3.0, 8.8);
        camera.lookAt(0, 0.35, 0);

        const renderer = new THREE.WebGLRenderer({
            antialias: true,
            alpha: true,
            powerPreference: 'high-performance'
        });

        renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
        renderer.setSize(
            Math.max(mount.clientWidth, 1),
            Math.max(mount.clientHeight, 1)
        );
        renderer.shadowMap.enabled = true;
        renderer.shadowMap.type = THREE.PCFSoftShadowMap;
        renderer.outputColorSpace = THREE.SRGBColorSpace;
        renderer.toneMapping = THREE.ACESFilmicToneMapping;
        renderer.toneMappingExposure = 1.05;

        if (fallback) {
            fallback.style.display = 'none';
        }

        mount.appendChild(renderer.domElement);

        const turntable = new THREE.Group();
        turntable.rotation.y = -0.55;
        scene.add(turntable);

        const vehicle = new THREE.Group();
        vehicle.position.y = 0.12;
        turntable.add(vehicle);

        const bodyMaterial = new THREE.MeshPhysicalMaterial({
            color: new THREE.Color(paint),
            metalness: 0.82,
            roughness: 0.2,
            clearcoat: 0.9,
            clearcoatRoughness: 0.12
        });

        const lowerBodyMaterial = new THREE.MeshStandardMaterial({
            color: new THREE.Color(paint).multiplyScalar(0.78),
            metalness: 0.7,
            roughness: 0.25
        });

        const glassMaterial = new THREE.MeshPhysicalMaterial({
            color: 0x14202a,
            metalness: 0.08,
            roughness: 0.08,
            transparent: true,
            opacity: 0.84,
            transmission: 0.12
        });

        const tireMaterial = new THREE.MeshStandardMaterial({
            color: 0x020202,
            metalness: 0.03,
            roughness: 0.88
        });

        const wheelMaterial = new THREE.MeshPhysicalMaterial({
            color: 0x9ea4aa,
            metalness: 0.95,
            roughness: 0.12,
            clearcoat: 0.42
        });

        const darkTrimMaterial = new THREE.MeshStandardMaterial({
            color: 0x050607,
            metalness: 0.62,
            roughness: 0.26
        });

        const grilleMaterial = new THREE.MeshStandardMaterial({
            color: 0x020202,
            metalness: 0.35,
            roughness: 0.38
        });

        const headlightMaterial = new THREE.MeshPhysicalMaterial({
            color: 0xf7fbff,
            emissive: 0x8cc8ff,
            emissiveIntensity: 0.75,
            metalness: 0.08,
            roughness: 0.08
        });

        const taillightMaterial = new THREE.MeshPhysicalMaterial({
            color: 0xff3030,
            emissive: 0xff1111,
            emissiveIntensity: 0.82,
            metalness: 0.06,
            roughness: 0.15
        });

        function meshBox(width, height, depth, x, y, z, material, parent = vehicle) {
            const geometry = new THREE.BoxGeometry(width, height, depth, 4, 2, 2);
            const mesh = new THREE.Mesh(geometry, material);
            mesh.position.set(x, y, z);
            mesh.castShadow = true;
            mesh.receiveShadow = true;
            parent.add(mesh);
            return mesh;
        }

        function meshWedge(width, height, depth, x, y, z, material, tilt = 0) {
            const mesh = meshBox(width, height, depth, x, y, z, material);
            mesh.rotation.z = tilt;
            return mesh;
        }

        function wheel(x, z, scale = 1) {
            const wheelGroup = new THREE.Group();
            wheelGroup.position.set(x, -0.44, z);

            const tire = new THREE.Mesh(
                new THREE.CylinderGeometry(
                    0.48 * scale,
                    0.48 * scale,
                    0.34 * scale,
                    56
                ),
                tireMaterial
            );

            tire.rotation.z = Math.PI / 2;
            tire.castShadow = true;
            wheelGroup.add(tire);

            const rim = new THREE.Mesh(
                new THREE.CylinderGeometry(
                    0.28 * scale,
                    0.28 * scale,
                    0.37 * scale,
                    32
                ),
                wheelMaterial
            );

            rim.rotation.z = Math.PI / 2;
            rim.castShadow = true;
            wheelGroup.add(rim);

            const hub = new THREE.Mesh(
                new THREE.CylinderGeometry(
                    0.08 * scale,
                    0.08 * scale,
                    0.4 * scale,
                    24
                ),
                darkTrimMaterial
            );

            hub.rotation.z = Math.PI / 2;
            wheelGroup.add(hub);

            vehicle.add(wheelGroup);
        }

        function lightPair(x, z, rear = false, spread = 0.58) {
            const material = rear
                ? taillightMaterial
                : headlightMaterial;

            [-spread, spread].forEach(side => {
                const lamp = meshBox(
                    0.10,
                    0.17,
                    0.5,
                    x,
                    -0.01,
                    side * z,
                    material
                );

                lamp.rotation.z = rear ? -0.02 : 0.02;
            });
        }

        function addCommonDetails(length, width, wheelX, wheelScale) {
            meshBox(
                length * 0.91,
                0.10,
                width * 0.98,
                0,
                -0.35,
                0,
                darkTrimMaterial
            );

            meshBox(
                0.08,
                0.34,
                width * 0.6,
                length / 2 + 0.035,
                -0.06,
                0,
                grilleMaterial
            );

            meshBox(
                0.07,
                0.25,
                width * 0.62,
                -(length / 2 + 0.03),
                -0.05,
                0,
                darkTrimMaterial
            );

            lightPair(length / 2 + 0.07, width * 0.37, false);
            lightPair(-(length / 2 + 0.06), width * 0.35, true);

            wheel(-wheelX, width * 0.56, wheelScale);
            wheel(wheelX, width * 0.56, wheelScale);
            wheel(-wheelX, -width * 0.56, wheelScale);
            wheel(wheelX, -width * 0.56, wheelScale);
        }

        function buildSedan(longNose = false) {
            const length = longNose ? 5.8 : 5.45;
            const width = 1.86;
            const wheelX = longNose ? 1.96 : 1.82;

            meshBox(length, 0.58, width, 0, -0.02, 0, bodyMaterial);
            meshWedge(1.38, 0.20, width * 0.94, length * 0.35, 0.18, 0, bodyMaterial, -0.015);
            meshWedge(1.05, 0.17, width * 0.91, -length * 0.39, 0.14, 0, lowerBodyMaterial, 0.01);

            const cabin = meshBox(
                longNose ? 2.25 : 2.42,
                0.72,
                width * 0.83,
                longNose ? -0.24 : -0.06,
                0.54,
                0,
                bodyMaterial
            );
            cabin.scale.y = 0.9;

            const glass = meshBox(
                longNose ? 1.82 : 1.98,
                0.49,
                width * 0.75,
                longNose ? -0.24 : -0.06,
                0.68,
                0,
                glassMaterial
            );
            glass.scale.y = 0.9;

            meshBox(0.05, 0.55, width * 0.87, -0.84, 0.48, 0, darkTrimMaterial);
            meshBox(0.05, 0.48, width * 0.87, 0.68, 0.45, 0, darkTrimMaterial);

            addCommonDetails(length, width, wheelX, 1.02);
        }

        function buildCoupe() {
            const length = 5.35;
            const width = 1.88;

            meshBox(length, 0.54, width, 0, -0.05, 0, bodyMaterial);
            meshWedge(1.65, 0.18, width * 0.94, 1.95, 0.15, 0, bodyMaterial, -0.018);
            meshWedge(1.08, 0.14, width * 0.89, -2.08, 0.09, 0, lowerBodyMaterial, 0.012);

            const cabin = meshBox(
                1.72,
                0.61,
                width * 0.79,
                -0.22,
                0.48,
                0,
                bodyMaterial
            );
            cabin.scale.y = 0.88;

            const glass = meshBox(
                1.38,
                0.41,
                width * 0.71,
                -0.22,
                0.59,
                0,
                glassMaterial
            );
            glass.scale.y = 0.88;

            meshBox(0.9, 0.08, width * 0.72, 2.12, 0.33, 0, darkTrimMaterial);

            addCommonDetails(length, width, 1.78, 1.02);
        }

        function buildTruck() {
            const length = 5.95;
            const width = 2.08;

            meshBox(length, 0.72, width, 0, -0.02, 0, bodyMaterial);
            meshBox(1.72, 0.88, width * 0.84, 1.08, 0.64, 0, bodyMaterial);
            meshBox(1.32, 0.54, width * 0.76, 1.08, 0.8, 0, glassMaterial);

            meshBox(2.05, 0.31, width * 0.93, -1.72, 0.27, 0, lowerBodyMaterial);
            meshBox(0.06, 0.38, width * 0.96, -0.66, 0.24, 0, darkTrimMaterial);

            addCommonDetails(length, width, 2.05, 1.12);
        }

        function buildSuv() {
            const length = 5.55;
            const width = 2.02;

            meshBox(length, 0.72, width, 0, -0.02, 0, bodyMaterial);
            meshBox(3.15, 1.0, width * 0.85, -0.12, 0.67, 0, bodyMaterial);
            meshBox(2.65, 0.62, width * 0.77, -0.08, 0.83, 0, glassMaterial);

            meshBox(0.05, 0.66, width * 0.88, -1.02, 0.62, 0, darkTrimMaterial);
            meshBox(0.05, 0.58, width * 0.88, 0.5, 0.58, 0, darkTrimMaterial);

            addCommonDetails(length, width, 1.88, 1.08);
        }

        function buildVan() {
            const length = 5.65;
            const width = 2.02;

            meshBox(length, 0.78, width, 0, 0.0, 0, bodyMaterial);
            meshBox(3.65, 1.14, width * 0.86, -0.3, 0.75, 0, bodyMaterial);
            meshBox(3.1, 0.65, width * 0.78, -0.28, 0.95, 0, glassMaterial);

            addCommonDetails(length, width, 1.86, 1.08);
        }

        if (renderType.includes('truck') || bodyStyle.includes('truck')) {
            buildTruck();
        } else if (renderType.includes('suv') || bodyStyle.includes('suv')) {
            buildSuv();
        } else if (renderType.includes('coupe') || bodyStyle.includes('coupe')) {
            buildCoupe();
        } else if (renderType.includes('van') || bodyStyle.includes('van')) {
            buildVan();
        } else {
            buildSedan(renderType.includes('long nose'));
        }

        const turntableTop = new THREE.Mesh(
            new THREE.CylinderGeometry(3.95, 3.95, 0.12, 160),
            new THREE.MeshPhysicalMaterial({
                color: 0x171a1c,
                metalness: 0.88,
                roughness: 0.24,
                clearcoat: 0.4
            })
        );

        turntableTop.position.y = -0.93;
        turntableTop.receiveShadow = true;
        turntable.add(turntableTop);

        const turntableInnerRing = new THREE.Mesh(
            new THREE.TorusGeometry(3.72, 0.028, 18, 240),
            new THREE.MeshStandardMaterial({
                color: 0xff3131,
                emissive: 0xff1818,
                emissiveIntensity: 2.0,
                metalness: 0.4,
                roughness: 0.15
            })
        );

        turntableInnerRing.rotation.x = Math.PI / 2;
        turntableInnerRing.position.y = -0.855;
        turntable.add(turntableInnerRing);

        const base = new THREE.Mesh(
            new THREE.CylinderGeometry(4.1, 4.15, 0.34, 160),
            new THREE.MeshStandardMaterial({
                color: 0x080a0b,
                metalness: 0.78,
                roughness: 0.28
            })
        );

        base.position.y = -1.1;
        base.receiveShadow = true;
        scene.add(base);

        const baseLip = new THREE.Mesh(
            new THREE.TorusGeometry(4.06, 0.055, 20, 240),
            new THREE.MeshStandardMaterial({
                color: 0x2a2c2e,
                metalness: 0.95,
                roughness: 0.12
            })
        );

        baseLip.rotation.x = Math.PI / 2;
        baseLip.position.y = -0.95;
        scene.add(baseLip);

        const floor = new THREE.Mesh(
            new THREE.CircleGeometry(9.8, 128),
            new THREE.MeshStandardMaterial({
                color: 0x090b0c,
                metalness: 0.42,
                roughness: 0.42
            })
        );

        floor.rotation.x = -Math.PI / 2;
        floor.position.y = -1.28;
        floor.receiveShadow = true;
        scene.add(floor);

        const floorGlow = new THREE.Mesh(
            new THREE.RingGeometry(4.35, 6.5, 160),
            new THREE.MeshBasicMaterial({
                color: 0x601010,
                transparent: true,
                opacity: 0.16,
                side: THREE.DoubleSide
            })
        );

        floorGlow.rotation.x = -Math.PI / 2;
        floorGlow.position.y = -1.265;
        scene.add(floorGlow);

        const ambient = new THREE.HemisphereLight(
            0xf4f8ff,
            0x08090a,
            1.65
        );
        scene.add(ambient);

        const key = new THREE.DirectionalLight(0xffffff, 4.1);
        key.position.set(4.8, 7.5, 5.8);
        key.castShadow = true;
        key.shadow.mapSize.width = 2048;
        key.shadow.mapSize.height = 2048;
        scene.add(key);

        const rimLeft = new THREE.PointLight(0xff3131, 24, 14, 2);
        rimLeft.position.set(-4.8, 1.1, 3.8);
        scene.add(rimLeft);

        const rimRight = new THREE.PointLight(0xff3131, 18, 14, 2);
        rimRight.position.set(4.4, 1.0, -3.0);
        scene.add(rimRight);

        const softFront = new THREE.PointLight(0xffffff, 16, 18, 2);
        softFront.position.set(3.7, 4.0, 6.0);
        scene.add(softFront);

        const softBack = new THREE.PointLight(0x9cc8ff, 9, 16, 2);
        softBack.position.set(-4.8, 3.0, -4.0);
        scene.add(softBack);

        let dragging = false;
        let lastX = 0;
        let lastInteraction = performance.now();

        mount.addEventListener('pointerdown', event => {
            dragging = true;
            lastX = event.clientX;
            lastInteraction = performance.now();
            mount.setPointerCapture(event.pointerId);
        });

        mount.addEventListener('pointermove', event => {
            if (!dragging) {
                return;
            }

            const delta = event.clientX - lastX;
            turntable.rotation.y += delta * 0.008;
            lastX = event.clientX;
            lastInteraction = performance.now();
        });

        function stopDragging(event) {
            dragging = false;
            lastInteraction = performance.now();

            if (event?.pointerId !== undefined &&
                mount.hasPointerCapture?.(event.pointerId)) {
                mount.releasePointerCapture(event.pointerId);
            }
        }

        mount.addEventListener('pointerup', stopDragging);
        mount.addEventListener('pointercancel', stopDragging);
        mount.addEventListener('pointerleave', event => {
            if (dragging) {
                stopDragging(event);
            }
        });

        mount.addEventListener('wheel', event => {
            event.preventDefault();
            lastInteraction = performance.now();

            camera.position.z += event.deltaY * 0.006;
            camera.position.z = Math.max(
                6.5,
                Math.min(11.4, camera.position.z)
            );
            camera.lookAt(0, 0.35, 0);
        }, { passive: false });

        function resize() {
            const width = Math.max(mount.clientWidth, 1);
            const height = Math.max(mount.clientHeight, 1);

            camera.aspect = width / height;
            camera.updateProjectionMatrix();
            renderer.setSize(width, height);
        }

        window.addEventListener('resize', resize);

        const resizeObserver = new ResizeObserver(resize);
        resizeObserver.observe(mount);

        const clock = new THREE.Clock();

        function animate() {
            requestAnimationFrame(animate);

            const delta = Math.min(clock.getDelta(), 0.04);
            const idleFor = performance.now() - lastInteraction;

            if (!dragging && idleFor > 1800) {
                turntable.rotation.y += delta * 0.16;
            }

            renderer.render(scene, camera);
        }

        resize();
        animate();
    } catch (error) {
        console.error('PonyUp Garage 3D viewer failed to initialize.', error);

        if (fallback) {
            fallback.style.display = 'flex';
        }
    }
}
