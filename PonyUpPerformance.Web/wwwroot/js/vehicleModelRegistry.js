const MANIFEST_URL = "/models/vehicle-manifest.json";

function normalize(value) {
    return (value || "")
        .toString()
        .trim()
        .toLowerCase()
        .replace(/[^a-z0-9]+/g, " ")
        .trim();
}

function normalizeVin(value) {
    return (value || "")
        .toString()
        .trim()
        .toUpperCase();
}

function yearMatches(year, match) {
    const numericYear = Number(year || 0);

    if (match.year && numericYear !== Number(match.year)) {
        return false;
    }

    if (match.yearMin && numericYear < Number(match.yearMin)) {
        return false;
    }

    if (match.yearMax && numericYear > Number(match.yearMax)) {
        return false;
    }

    return true;
}

function textMatches(actual, expected) {
    if (!expected) {
        return true;
    }

    return normalize(actual) === normalize(expected);
}

function trimMatches(actualTrim, match) {
    if (!match.trim) {
        return true;
    }

    const actual = normalize(actualTrim);
    const expected = normalize(match.trim);

    return actual === expected ||
        actual.includes(expected) ||
        expected.includes(actual);
}

function scoreCandidate(vehicle, entry) {
    if (!entry || entry.enabled === false || !entry.asset) {
        return -1;
    }

    const match = entry.match || {};
    const vin = normalizeVin(vehicle.vin);
    const matchVin = normalizeVin(match.vin);

    if (matchVin) {
        return vin === matchVin ? 1000 : -1;
    }

    if (!yearMatches(vehicle.year, match)) {
        return -1;
    }

    if (!textMatches(vehicle.make, match.make) ||
        !textMatches(vehicle.model, match.model) ||
        !trimMatches(vehicle.trim, match)) {
        return -1;
    }

    let score = 0;

    if (match.make) {
        score += 100;
    }

    if (match.model) {
        score += 100;
    }

    if (match.year) {
        score += 80;
    }
    else if (match.yearMin || match.yearMax) {
        score += 45;
    }

    if (match.trim) {
        score += 60;
    }

    return score;
}

export async function findVehicleAsset(vehicle) {
    try {
        const response = await fetch(
            MANIFEST_URL,
            {
                cache: "no-store"
            });

        if (!response.ok) {
            return null;
        }

        const manifest = await response.json();
        const entries = Array.isArray(manifest.vehicles)
            ? manifest.vehicles
            : [];

        let winner = null;
        let winnerScore = -1;

        for (const entry of entries) {
            const score = scoreCandidate(vehicle, entry);

            if (score > winnerScore) {
                winner = entry;
                winnerScore = score;
            }
        }

        return winner;
    }
    catch (error) {
        console.warn(
            "PonyUp exact vehicle manifest could not be loaded.",
            error);

        return null;
    }
}

export function applyAssetTransform(model, asset) {
    const scale = Number(asset?.scale ?? 1);

    model.scale.multiplyScalar(
        Number.isFinite(scale) && scale > 0
            ? scale
            : 1);

    const position = Array.isArray(asset?.position)
        ? asset.position
        : [0, 0, 0];

    model.position.x +=
        Number(position[0] || 0);

    model.position.y +=
        Number(position[1] || 0);

    model.position.z +=
        Number(position[2] || 0);

    const rotationY = Number(asset?.rotationY || 0);

    if (Number.isFinite(rotationY)) {
        model.rotation.y = rotationY;
    }
}

function materialName(material) {
    return normalize(
        material?.name ||
        "");
}

function meshName(mesh) {
    return normalize(
        mesh?.name ||
        "");
}

function includesAny(value, hints) {
    return hints.some(hint =>
        value.includes(
            normalize(hint)));
}

function applyColorToMatches(
    root,
    colorHex,
    materialHints,
    meshHints,
    treatment = {}) {

    if (!root ||
        !colorHex ||
        ((!Array.isArray(materialHints) || materialHints.length === 0) &&
         (!Array.isArray(meshHints) || meshHints.length === 0))) {

        return 0;
    }

    let changed = 0;

    root.traverse(object => {
        if (!object.isMesh || !object.material) {
            return;
        }

        const objectName =
            meshName(object);

        const meshMatch =
            Array.isArray(meshHints) &&
            meshHints.length > 0 &&
            includesAny(
                objectName,
                meshHints);

        const materials =
            Array.isArray(object.material)
                ? [...object.material]
                : [object.material];

        materials.forEach((material, index) => {
            const matName =
                materialName(material);

            const materialMatch =
                Array.isArray(materialHints) &&
                materialHints.length > 0 &&
                includesAny(
                    matName,
                    materialHints);

            if (!materialMatch && !meshMatch) {
                return;
            }

            const clone =
                material.clone();

            if (clone.color) {
                clone.color.set(
                    colorHex);
            }

            if ("metalness" in clone &&
                treatment.metalness !== undefined) {

                clone.metalness =
                    treatment.metalness;
            }

            if ("roughness" in clone &&
                treatment.roughness !== undefined) {

                clone.roughness =
                    treatment.roughness;
            }

            if ("clearcoat" in clone &&
                treatment.clearcoat !== undefined) {

                clone.clearcoat =
                    treatment.clearcoat;
            }

            if (Array.isArray(object.material)) {
                materials[index] =
                    clone;

                object.material =
                    materials;
            }
            else {
                object.material =
                    clone;
            }

            changed += 1;
        });
    });

    return changed;
}

export function applyVehiclePaint(
    root,
    paintHex,
    asset) {

    const customization =
        asset?.customization || {};

    const materialHints =
        Array.isArray(
            customization.paintMaterialHints) &&
        customization.paintMaterialHints.length > 0
            ? customization.paintMaterialHints
            : Array.isArray(asset?.paintMaterialHints) &&
              asset.paintMaterialHints.length > 0
                ? asset.paintMaterialHints
                : [
                    "body",
                    "paint",
                    "carpaint",
                    "exterior",
                    "bodywork",
                    "shell"
                ];

    const meshHints =
        Array.isArray(
            customization.paintMeshHints)
            ? customization.paintMeshHints
            : Array.isArray(asset?.paintMeshHints)
                ? asset.paintMeshHints
                : [];

    return applyColorToMatches(
        root,
        paintHex || "#b8b8b8",
        materialHints,
        meshHints,
        {
            metalness: 0.72,
            roughness: 0.22,
            clearcoat: 0.88
        });
}

export function applyInteriorColor(
    root,
    colorHex,
    asset) {

    const customization =
        asset?.customization || {};

    return applyColorToMatches(
        root,
        colorHex,
        customization.interiorMaterialHints || [],
        customization.interiorMeshHints || [],
        {
            metalness: 0.05,
            roughness: 0.62,
            clearcoat: 0.08
        });
}

export function applyWheelFinish(
    root,
    colorHex,
    asset) {

    const customization =
        asset?.customization || {};

    return applyColorToMatches(
        root,
        colorHex,
        customization.wheelMaterialHints ||
            asset?.rimNames ||
            [],
        customization.wheelMeshHints || [],
        {
            metalness: 0.9,
            roughness: 0.18,
            clearcoat: 0.5
        });
}

export function applyWheelVariant(
    root,
    variantId,
    asset) {

    const styles =
        asset?.customization?.wheelStyles;

    if (!Array.isArray(styles) ||
        styles.length === 0) {

        return false;
    }

    const selected =
        styles.find(style =>
            style.id === variantId) ||
        styles[0];

    const allHints =
        styles
            .flatMap(style =>
                Array.isArray(style.meshHints)
                    ? style.meshHints
                    : []);

    root.traverse(object => {
        if (!object.isMesh) {
            return;
        }

        const name =
            meshName(object);

        const isWheelVariantMesh =
            allHints.length > 0 &&
            includesAny(
                name,
                allHints);

        if (!isWheelVariantMesh) {
            return;
        }

        object.visible =
            includesAny(
                name,
                selected.meshHints || []);
    });

    return true;
}

export function getCustomizationOptions(
    asset) {

    const customization =
        asset?.customization || {};

    return {
        interiorColors:
            Array.isArray(
                customization.interiorColors)
                ? customization.interiorColors
                : [],

        wheelFinishes:
            Array.isArray(
                customization.wheelFinishes)
                ? customization.wheelFinishes
                : [],

        wheelStyles:
            Array.isArray(
                customization.wheelStyles)
                ? customization.wheelStyles
                : []
    };
}

export function normalizeModelToPodium(
    THREE,
    model,
    targetLength = 5.7) {

    const box =
        new THREE.Box3()
            .setFromObject(model);

    const size =
        new THREE.Vector3();

    const center =
        new THREE.Vector3();

    box.getSize(size);
    box.getCenter(center);

    if (!Number.isFinite(size.x) ||
        !Number.isFinite(size.y) ||
        !Number.isFinite(size.z)) {

        return;
    }

    model.position.sub(center);

    const longest =
        Math.max(
            size.x,
            size.z,
            0.001);

    const scale =
        targetLength / longest;

    model.scale.multiplyScalar(scale);

    const normalizedBox =
        new THREE.Box3()
            .setFromObject(model);

    model.position.y -=
        normalizedBox.min.y + 0.87;
}
