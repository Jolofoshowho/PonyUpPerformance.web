# PonyUp First-Party Vehicle 3D Catalog

PonyUp's Garage always prefers a first-party GLB from `vehicle-manifest.json`.
The outside catalog is only a coverage fallback. The old procedural/block
vehicle renderer must never be used as the customer-facing fallback.

## Production standard

A vehicle model is not enabled until it passes all of these checks:

- Correct vehicle generation/body shell for the declared year range.
- Recognizable factory proportions, fascia, greenhouse, lamps, wheel openings,
  roofline, deck/bed, and major trim geometry.
- No baked third-party watermark.
- Source and license recorded in the manifest.
- Commercial use is permitted for PonyUp.
- Web-ready GLB, preferably under 12 MB after optimization.
- Sensible real-time geometry target; generally 40k–150k visible triangles.
- PBR materials where practical.
- Body paint is isolated enough for PonyUp's paint selector to recolor without
  recoloring glass, tires, lights, chrome, or the interior.
- Correct orientation and scale on the PonyUp spinner podium.
- Mobile test passed before `enabled` is changed to `true`.

## File layout

```
wwwroot/models/
  vehicle-manifest.json
  vehicles/
    <vehicle-generation>.glb
```

## Matching priority

The Garage model registry resolves in this order:

1. VIN-specific first-party entry.
2. Exact year/make/model/trim first-party entry.
3. Year-range make/model first-party generation entry.
4. External realistic catalog fallback, when configured.
5. Clean "model needed" state.

There is deliberately no blocky procedural car fallback.

## Licensing

Do not add a GLB by copying a watermarked or access-restricted model.

Every first-party manifest entry should retain provenance metadata:

```json
"source": {
  "type": "ponyup-generated | cc0 | cc-by | licensed",
  "reference": "source URL or internal generation record",
  "license": "license identifier",
  "attribution": "required attribution, if any"
}
```

Free-plan Meshy output is currently CC BY 4.0 and requires attribution. If it
is used, retain the attribution with the asset record and surface the required
credit in PonyUp's credits/license page.

## Build order

Use the private `/ModelCatalog` owner page. It combines saved Garage vehicles
and analysis history, puts missing models first, and ranks repeated
year/make/model demand above one-off vehicles. This keeps first-party catalog
work focused on actual PonyUp traffic.


## 500-model production target

The current production target is 500 realistic vehicle-generation models.

A model does not count toward the 500 until it has:

- Approved realistic vehicle geometry for its declared generation/body style.
- Exterior body-paint material mapping.
- Interior material mapping suitable for customer color/material selection.
- Wheel/rim material mapping for finish changes.
- Wheel-style geometry variants when alternate wheels are supplied for that vehicle.
- Saved appearance compatibility in My Garage.
- Mobile/web performance approval.
- Source, license, and required attribution recorded in the manifest.

The owner `/ModelCatalog` page remains the build queue. User demand determines
which missing vehicles are built first.
