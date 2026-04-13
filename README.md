# MeshOptix: Scene Mesh Optimizer for Unity

MeshOptix is a Unity editor tool focused on practical scene-level mesh reduction with a clean workflow and quality-first defaults.

It helps you reduce triangle and vertex counts on selected scene hierarchies while keeping control over preservation behavior, collider updates, and replacement strategy.

## What This Project Includes

- A polished Unity Editor window: MeshOptix : Scene Mesh Optimizer
- Scene hierarchy optimization for MeshFilter/MeshRenderer and SkinnedMeshRenderer targets
- Mesh asset generation into your Assets folder
- Replace mode and in-place mode workflows
- Collider remapping for matching MeshCollider sources
- Detailed run report and optimization result popup
- A simplified preservation UI with advanced options available when needed

## Key Features

- Quality-first simplification pipeline with staged fallback behavior
- Per-run summary with before/after triangles and vertices
- Caching of unique source meshes to avoid duplicate work
- Optional automatic Read/Write enable for importable model meshes
- Works on active and optionally inactive children
- Undo support for scene object changes

## Requirements

- Unity 6 (tested in this repo with 6000.3.9f1)
- Windows/macOS/Linux Unity Editor environment

## Installation

1. Clone this repository.
2. Open the project in Unity.
3. Let Unity finish package import and script compilation.
4. Open the tool from:

- Tools/Optimization/MeshOptix : Scene Mesh Optimizer
- or GameObject/Optimization/Open MeshOptix : Scene Mesh Optimizer

## Quick Start

1. Select a GameObject in the Scene Hierarchy.
2. Open MeshOptix : Scene Mesh Optimizer.
3. Set Target Triangle Ratio.
4. Choose preservation and processing options.
5. Choose replacement behavior.
6. Set output folder (must be under Assets).
7. Click Optimize Selected.
8. Review the optimization popup and the Last Run panel.

## How the Workflow Operates

1. The tool validates your scene selection.
2. It collects supported renderers in the selected hierarchy.
3. It prepares readable source meshes when possible (if enabled).
4. It simplifies each unique mesh and saves optimized mesh assets.
5. It assigns optimized meshes back to targets.
6. It optionally updates matching MeshCollider components.
7. It finalizes replacement mode behavior if enabled.
8. It reports totals and per-mesh messages.

## Settings Reference

### Core Simplification

- Target Triangle Ratio
	- 1.0 keeps original density.
	- 0.5 targets roughly half triangles.
	- Effective range is clamped between 0.05 and 1.0.
- Include Inactive Children
	- Includes disabled child objects in processing.
- Update Matching MeshColliders
	- Remaps colliders only when they referenced the same original mesh.

### Preservation (Essential)

- Preserve UV Seams (Recommended)
	- Combined control for UV0 and UV1 seam protection.
- Preserve Hard Edges
	- Helps maintain crisp edge boundaries.
- Preserve Skinning Boundaries
	- Helps skinned meshes retain cleaner bone-region behavior.
- Recalculate Tangents
	- Recommended if normal maps are used.

### Preservation (Advanced Foldout)

- Preserve UV0 Seams
- Preserve UV1 Seams
- Recalculate Normals
- Optimize Mesh Buffers
- Auto Enable Read/Write

These are intentionally grouped under advanced controls to keep everyday usage simple while preserving power-user flexibility.

### Replacement

- Replace Selected Object
	- Duplicates the selected object, optimizes the duplicate, then swaps naming/state.
- Keep Disabled Backup
	- Keeps original object disabled instead of deleting it.

Use in-place behavior (Replace Selected Object off) when you need maximum scene reference safety.

### Output

- Output folder must be inside Assets.
- Default folder:
	- Assets/Project/Prefabs/Optimized_Meshes

## Results and Reporting

After each run you get:

- A results popup with:
	- Renderers touched
	- Unique meshes simplified/skipped
	- Triangles before/after
	- Vertices before/after
	- Output folder
	- Detailed per-mesh messages
- A Last Run section in the main window with summary and message log

## Supported Targets

- MeshFilter + MeshRenderer pairs
- SkinnedMeshRenderer
- Optional matching MeshCollider updates

## Important Behavior Notes

- You must select a scene instance, not a prefab asset from the Project window.
- Undo restores scene object changes.
- Generated mesh assets remain in the project (they are asset files).
- If no mesh is meaningfully reduced, the run is aborted and reverted.

## Known Limitations

- Blend shapes are not supported by the custom simplifier path.
- Non-readable meshes may be skipped unless readable import can be enabled.
- Automatic Read/Write toggling works only for importable model assets.
- Exact target ratio is not always guaranteed if quality constraints require stopping early.

## Troubleshooting

- Nothing happens or button disabled:
	- Ensure a valid scene object is selected.
	- Ensure the selected hierarchy contains supported mesh components.
	- Ensure output folder is valid and inside Assets.

- Mesh skipped as not readable:
	- Enable Auto Enable Read/Write.
	- Or manually enable Read/Write on the model import settings.

- Unexpected visual artifacts:
	- Increase Target Triangle Ratio.
	- Enable Preserve Hard Edges and/or seam preservation.
	- Enable Recalculate Normals in advanced options.

- Scene references break after optimization:
	- Prefer in-place mode by disabling Replace Selected Object.

## Repository Structure

- Assets/Scripts/Editor/SceneMeshOptimizerWindow.cs
	- Editor UI, optimization orchestration, reporting, and utility classes
- Assets/Scripts/MeshOptix/MeshSimplifierCore
	- Core simplification implementation and supporting math/data types

## Credits

- Creator: Hamed Khalifa
- Company: Hamfox inc
- LinkedIn: https://www.linkedin.com/in/hamed-khalifa/
- GitHub: https://github.com/Ihamfox

This project includes and adapts ideas and implementations related to quadric-error simplification, including MIT-licensed prior work acknowledged in source headers.

## License

MIT License

See LICENSE for full text.

## Contributing

Issues and pull requests are welcome. If you propose algorithmic changes, include:

- Performance impact notes
- Visual quality comparisons
- Repro steps and test meshes
