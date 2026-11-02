# Capsule Ecology — instructions for Copilot

You are helping finalize a native Windows application. Read
`TELEMETRY.json` first — it is the machine-readable ground truth of
what is verified, what is not, and what remains. Regenerate it after
any meaningful change: `python tools/refresh-telemetry.py`.
`FINALIZING.md` is the work list, with pass criteria per item.

## What this is

One native Windows app (C# / .NET 8 + WinUI 3, unpackaged full-trust):
law-bound system tweaks with receipts, app packs via winget +
Chocolatey, repair + restore tooling, a files-only cleaner, an
on-device Bible, and AI models with verifiable lineage
(download → hash-verify → adopt → fork). It is the native variant of
a Tauri app; the Tauri build is a mine, not a base — do not port it.

## The laws — do not violate these

**The Capsule Order:** (1) Windows is the foundation, not the enemy.
(2) Observe before you modify — no change without a baseline.
(3) Receipt, or it didn't happen. (4) No rollback, no ship.
(5) Compatibility outranks speed.
**Above all, D7:** humans are sources of authority; systems never are.
**The Three Laws:** security features are NEVER disabled (no Defender/
BitLocker/HVCI tweaks, ever); full Windows compatibility; never lock
the user out of settings — everything stays undoable from inside Windows.

## Hard rules for changes

- Every state change seals a receipt via `ReceiptLog` (SI chain,
  fnv1a). The ledger format is byte-compatible with si-kernel.js and
  is FROZEN — do not alter hashing, field order, or genesis.
- The tweak engine demands a restore point before the first apply;
  if Windows refuses it, nothing applies. Keep that refusal.
- Tweaks come from `contracts/catalog/tweaks.catalog.json` (parity
  with the shipped CapsuleOS 0.2.2 playbook is exact and tested:
  `python contracts/catalog/plan.py` must stay ALL PASS).
- Downloads are user-initiated only, from the original upstream
  source, disclosed up front (what / from where / how big). Nothing
  downloads silently; nothing is bundled that can be fetched.
- Models verify with the source's own semantics: SHA-256 for LFS
  weights, git-blob SHA-1 for small files. A model that has not
  verified does not run and cannot be adopted — refusal is absence.
- Package installs disclose their plan before running, prefer winget,
  fall back to Chocolatey, and seal per-app receipts with the version
  actually resolved ("unknown" if unresolvable — never invent one).
- Catalogs are validated: `contracts/packs/validate_packs.py`,
  `contracts/models/validate_models.py` must stay clean.
- The service layer (`app/Services/*.cs`) must compile at 0 errors.
  Pages stay thin; logic lives in services.
- Do not rename the product or the packages. Naming is the owner's.
- Own creations credit the brand **N8toriousAi**, never a personal
  name. Real third-party creators keep their names. Illustrative
  people in examples are marked "(example)".

## Build

Open `app/CapsuleEcology.csproj` in Visual Studio 2022 (Windows App
SDK 1.6), restore, build x64. The XAML compile step has never run —
it is Windows-only. First-run checks are listed in FINALIZING.md §2.

## Current frontier (see TELEMETRY.json for the live list)

OnnxRuntime inference, Bible bookmarks/highlights, the v17 theme
pass in App.xaml (tokens are in `preview/` — the design target), and
the four stub pages (Lineage, Agents, Capsules & Worlds, All Pages).
