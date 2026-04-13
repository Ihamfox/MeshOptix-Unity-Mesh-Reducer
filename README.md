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

## Key Features

- Quality-first simplification pipeline with staged fallback behavior
- Per-run summary with before/after triangles and vertices
- Caching of unique source meshes to avoid duplicate work
- Optional automatic Read/Write enable for importable model meshes
- Works on active and optionally inactive children
- Undo support for scene object changes

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

### Replacement

- Replace Selected Object
	- Duplicates the selected object, optimizes the duplicate, then swaps naming/state.
- Keep Disabled Backup
	- Keeps original object disabled instead of deleting it.

Use in-place behavior (Replace Selected Object off) when you need maximum scene reference safety.

## Supported Targets

- MeshFilter + MeshRenderer pairs
- SkinnedMeshRenderer

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

## Credits

- Creator: Hamed Khalifa
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
