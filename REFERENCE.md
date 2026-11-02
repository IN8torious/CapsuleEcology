# REFERENCE — the deconstructed apbx is the mine

Standing rule (Nathan, 2026-11-02): pull from the deconstructed
CapsuleOS refresh before building from scratch. Everything
below is local and verified to the byte where noted.

## The mine

`~/workspace/drive-intake/CapsuleOS-refresh/`
(MD5 b21d3ad4… == Drive's record; SHA-256 51f46636…)

- `CapsuleOS.apbx` — the 4.71 GB original. Do not re-download.
- `CENSUS-REVIEW.md` — the full census + page-inventory addendum.
- `LINK-DOOR-PLAN.md` — distribution notes (superseded by the
  native unification; kept for history).
- `census/` — the small extracted files: playbook core (v0.1.0),
  `Install-CapsuleToolbox.ps1`, `manifest.json`,
  `design-audit.json` (the 224-case page audit),
  `linux-runtime/` (WSL payload manifest),
  `models/` (the staged model catalog).
- `app/` — the NSIS-extracted installed app (616 MB):
  `capsule_toolbox.exe` (Tauri shell — icons live in its
  resources), `runtime/` (the Node sidecar: assistant modules,
  model catalog, Toolbox Bible JSONs, three.js/rhino3dm).

## Already pulled into the Ecology build

| Asset | From | Into |
|---|---|---|
| App icons (16–256 + playbook.png) | exe .rsrc/ICON + apbx | `assets/icons/` → preview brand/favicon, app `ApplicationIcon` |
| Model catalog (both Phi models, pinned) | `app/runtime/assistant/model-catalog.json` | R0 lineage cards; R4 hash semantics proven live |
| Toolbox Bible JSONs | `app/runtime/assistant/` (cross-checked vs `~/workspace/toolbox-bible/`) | preview reader; R3 data |
| Page map (20 pages + 11 Bible views) | `design-audit.json` case list | preview Atlas; R1 navigation |
| Proposal/audit pattern | `runtime/assistant/kernel-proposals.cjs`, `auditor-proposals.cjs` | R7 design reference (contracts, not code) |

## Still in the mine (pull when the phase comes)

- Mod forge behavior (`mod-package.cjs`, `mod-report.cjs`) → R5.
- Scene editing behavior (`runtime/app/scene-html-edit.js`) → R5.
- WSL provision flow (`wsl-provision.cjs`) → the open WSL call.
- Controller route map (`controller.cjs`) → R7/API reference.
