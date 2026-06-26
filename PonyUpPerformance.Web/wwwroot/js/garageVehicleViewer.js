import * as THREE from 'https://unpkg.com/three@0.160.0/build/three.module.js';

const mount = document.getElementById('garage3dViewer');

if (mount) {
    const paint = mount.dataset.paint || '#b8b8b8';
    const bodyStyle = (mount.dataset.body || 'Sedan').toLowerCase();

    const scene = new THREE.Scene();

    const camera = new THREE.PerspectiveCamera(
        30,
        mount.clientWidth / mount.clientHeight,
        0.1,
        1000
    );

    camera.position.set(4.8, 2.15, 6.9);
    camera.lookAt(0, 0.32, 0);

    const renderer = new THREE.WebGLRenderer({
        antialias: true,
        alpha: true
    });

    renderer.setPixelRatio(window.devicePixelRatio || 1);
    renderer.setSize(mount.clientWidth, mount.clientHeight);
    renderer.shadowMap.enabled = true;
    renderer.shadowMap.type = THREE.PCFSoftShadowMap;
    mount.appendChild(renderer.domElement);

    const vehicle = new THREE.Group();
    vehicle.rotation.y = -0.55;
    scene.add(vehicle);

    const bodyMaterial = new THREE.MeshStandardMaterial({
        color: paint,
        metalness: 0.72,
        roughness: 0.21
    });

    const glassMaterial = new THREE.MeshStandardMaterial({
        color: 0x101b25,
        metalness: 0.35,
        roughness: 0.06,
        transparent: true,
        opacity: 0.86
    });

    const tireMaterial = new THREE.MeshStandardMaterial({
        color: 0x030303,
        metalness: 0.12,
        roughness: 0.76
    });

    const wheelMaterial = new THREE.MeshStandardMaterial({
        color: 0xc9ced2,
        metalness: 0.88,
        roughness: 0.16
    });

    const trimMaterial = new THREE.MeshStandardMaterial({
        color: 0x070707,
        metalness: 0.55,
        roughness: 0.28
    });

    const headlightMaterial = new THREE.MeshStandardMaterial({
        color: 0xfff3c0,
        emissive: 0x4a3a12,
        metalness: 0.1,
        roughness: 0.18
    });

    const taillightMaterial = new THREE.MeshStandardMaterial({
        color: 0xff3131,
        emissive: 0x5d0808,
        metalness: 0.1,
        roughness: 0.2
    });

    function addLightSet() {
        const ambient = new THREE.AmbientLight(0xffffff, 1.2);
        scene.add(ambient);

        const key = new THREE.DirectionalLight(0xffffff, 2.8);
        key.position.set(5.5, 7.2, 5.8);
        key.castShadow = true;
        scene.add(key);

        const leftRed = new THREE.PointLight(0xff3131, 2.8, 11);
        leftRed.position.set(-3.8, 1.1, 3.4);
        scene.add(leftRed);

        const floorGlow = new THREE.PointLight(0xff3b2f, 2.1, 7);
        floorGlow.position.set(0, -0.55, 1.8);
        scene.add(floorGlow);
    }

    function box(width, height, depth, x, y, z, material) {
        const geometry = new THREE.BoxGeometry(width, height, depth, 3, 3, 3);
        const mesh = new THREE.Mesh(geometry, material);
        mesh.position.set(x, y, z);
        mesh.castShadow = true;
        mesh.receiveShadow = true;
        vehicle.add(mesh);
        return mesh;
    }

    function roundedBody(width, height, depth, x, y, z, material) {
        const geometry = new THREE.BoxGeometry(width, height, depth, 6, 3, 3);
        const mesh = new THREE.Mesh(geometry, material);
        mesh.position.set(x, y, z);
        mesh.scale.y = 0.92;
        mesh.castShadow = true;
        mesh.receiveShadow = true;
        vehicle.add(mesh);
        return mesh;
    }

    function wheel(x, z, scale) {
        const tire = new THREE.Mesh(
            new THREE.CylinderGeometry(0.42 * scale, 0.42 * scale, 0.34 * scale, 56),
            tireMaterial
        );

        tire.rotation.z = Math.PI / 2;
        tire.position.set(x, -0.52, z);
        tire.castShadow = true;
        vehicle.add(tire);

        const rim = new THREE.Mesh(
            new THREE.CylinderGeometry(0.23 * scale, 0.23 * scale, 0.38 * scale, 56),
            wheelMaterial
        );

        rim.rotation.z = Math.PI / 2;
        rim.position.set(x, -0.52, z);
        rim.castShadow = true;
        vehicle.add(rim);
    }

    function headlight(x, z) {
        box(0.13, 0.15, 0.46, x, -0.02, z, headlightMaterial);
    }

    function taillight(x, z) {
        box(0.13, 0.15, 0.38, x, -0.02, z, taillightMaterial);
    }

    function buildSedan() {
        roundedBody(5.35, 0.56, 1.76, 0, -0.05, 0, bodyMaterial);

        box(1.35, 0.20, 1.67, -2.12, 0.02, 0, bodyMaterial);
        box(1.18, 0.17, 1.62, 2.14, 0.00, 0, bodyMaterial);

        box(2.15, 0.46, 1.48, -0.14, 0.47, 0, bodyMaterial);
        box(1.76, 0.32, 1.38, -0.14, 0.60, 0, glassMaterial);

        box(0.04, 0.40, 1.52, -0.88, 0.36, 0, trimMaterial);
        box(0.04, 0.34, 1.52, 0.58, 0.32, 0, trimMaterial);

        box(0.08, 0.08, 1.58, -2.58, -0.11, 0, trimMaterial);
        box(0.08, 0.08, 1.56, 2.56, -0.11, 0, trimMaterial);

        headlight(2.73, 0.53);
        headlight(2.73, -0.53);
        taillight(-2.73, 0.50);
        taillight(-2.73, -0.50);

        wheel(-1.76, 0.96, 1);
        wheel(1.76, 0.96, 1);
        wheel(-1.76, -0.96, 1);
        wheel(1.76, -0.96, 1);
    }

    function buildCoupe() {
        roundedBody(5.15, 0.52, 1.70, 0, -0.07, 0, bodyMaterial);

        box(1.66, 0.17, 1.58, -2.14, -0.02, 0, bodyMaterial);
        box(1.25, 0.14, 1.54, 2.05, -0.04, 0, bodyMaterial);

        box(1.58, 0.38, 1.38, -0.08, 0.38, 0, bodyMaterial);
        box(1.15, 0.26, 1.28, -0.08, 0.49, 0, glassMaterial);

        headlight(2.62, 0.51);
        headlight(2.62, -0.51);
        taillight(-2.62, 0.48);
        taillight(-2.62, -0.48);

        wheel(-1.70, 0.93, 0.98);
        wheel(1.70, 0.93, 0.98);
        wheel(-1.70, -0.93, 0.98);
        wheel(1.70, -0.93, 0.98);
    }

    function buildTruck() {
        roundedBody(5.78, 0.72, 2.02, 0, -0.03, 0, bodyMaterial);

        box(1.58, 0.78, 1.70, -1.08, 0.64, 0, bodyMaterial);
        box(1.16, 0.46, 1.58, -1.08, 0.81, 0, glassMaterial);

        box(2.02, 0.30, 1.90, 1.43, 0.28, 0, bodyMaterial);
        box(0.06, 0.36, 1.94, 0.42, 0.23, 0, trimMaterial);

        headlight(2.92, 0.66);
        headlight(2.92, -0.66);
        taillight(-2.92, 0.64);
        taillight(-2.92, -0.64);

        wheel(-1.98, 1.08, 1.08);
        wheel(1.98, 1.08, 1.08);
        wheel(-1.98, -1.08, 1.08);
        wheel(1.98, -1.08, 1.08);
    }

    function buildSuv() {
        roundedBody(5.38, 0.74, 1.94, 0, -0.03, 0, bodyMaterial);

        box(2.95, 0.88, 1.70, -0.12, 0.66, 0, bodyMaterial);
        box(2.45, 0.50, 1.58, -0.12, 0.84, 0, glassMaterial);

        box(0.05, 0.52, 1.68, -1.08, 0.56, 0, trimMaterial);
        box(0.05, 0.46, 1.68, 0.44, 0.54, 0, trimMaterial);

        headlight(2.75, 0.63);
        headlight(2.75, -0.63);
        taillight(-2.75, 0.60);
        taillight(-2.75, -0.60);

        wheel(-1.82, 1.03, 1.04);
        wheel(1.82, 1.03, 1.04);
        wheel(-1.82, -1.03, 1.04);
        wheel(1.82, -1.03, 1.04);
    }

    function buildVan() {
        roundedBody(5.45, 0.86, 1.96, 0, -0.02, 0, bodyMaterial);

        box(3.45, 0.95, 1.72, -0.35, 0.70, 0, bodyMaterial);
        box(3.05, 0.52, 1.58, -0.35, 0.88, 0, glassMaterial);

        headlight(2.78, 0.62);
        headlight(2.78, -0.62);
        taillight(-2.78, 0.60);
        taillight(-2.78, -0.60);

        wheel(-1.78, 1.02, 1.03);
        wheel(1.78, 1.02, 1.03);
        wheel(-1.78, -1.02, 1.03);
        wheel(1.78, -1.02, 1.03);
    }

    addLightSet();

    if (bodyStyle.includes('truck')) {
        buildTruck();
    } else if (bodyStyle.includes('suv')) {
        buildSuv();
    } else if (bodyStyle.includes('coupe')) {
        buildCoupe();
    } else if (bodyStyle.includes('van')) {
        buildVan();
    } else {
        buildSedan();
    }

    const floor = new THREE.Mesh(
        new THREE.CylinderGeometry(3.82, 3.82, 0.08, 160),
        new THREE.MeshStandardMaterial({
            color: 0x090909,
            metalness: 0.55,
            roughness: 0.36
        })
    );

    floor.position.y = -0.94;
    floor.receiveShadow = true;
    scene.add(floor);

    const ring = new THREE.Mesh(
        new THREE.TorusGeometry(3.62, 0.026, 16, 220),
        new THREE.MeshStandardMaterial({
            color: 0xff3131,
            emissive: 0x661010,
            metalness: 0.45,
            roughness: 0.16
        })
    );

    ring.rotation.x = Math.PI / 2;
    ring.position.y = -0.875;
    scene.add(ring);

    let dragging = false;
    let lastX = 0;

    mount.addEventListener('pointerdown', e => {
        dragging = true;
        lastX = e.clientX;
        mount.setPointerCapture(e.pointerId);
    });

    mount.addEventListener('pointermove', e => {
        if (!dragging) return;

        const delta = e.clientX - lastX;
        vehicle.rotation.y += delta * 0.008;
        lastX = e.clientX;
    });

    mount.addEventListener('pointerup', () => {
        dragging = false;
    });

    mount.addEventListener('pointercancel', () => {
        dragging = false;
    });

    mount.addEventListener('wheel', e => {
        e.preventDefault();

        camera.position.z += e.deltaY * 0.004;
        camera.position.z = Math.max(5.2, Math.min(9.2, camera.position.z));
        camera.lookAt(0, 0.32, 0);
    }, { passive: false });

    function resize() {
        if (!mount.clientWidth || !mount.clientHeight) {
            return;
        }

        camera.aspect = mount.clientWidth / mount.clientHeight;
        camera.updateProjectionMatrix();
        renderer.setSize(mount.clientWidth, mount.clientHeight);
    }

    window.addEventListener('resize', resize);

    function animate() {
        requestAnimationFrame(animate);
        renderer.render(scene, camera);
    }

    resize();
    animate();
}
