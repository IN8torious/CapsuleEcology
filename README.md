# Capsule Ecology — pre-release v0.1.0

**A Windows strengthening platform.** Capsule helps you understand,
repair, secure, optimize and preserve your PC without sacrificing
compatibility. Not an optimizer that amputates — a maintenance layer
with receipts.

> People don't install a playbook. They adopt a lineage.

## What this is

A single native Windows application (C# / .NET 8 + WinUI 3, unpackaged
full-trust) that unifies the Capsule line:

- **Tweaks** — the CapsuleOS 0.2.2 catalog (85 options, 619 actions),
  every one gated, mirrored, and sealed into a hash-chained receipt
  ledger. A restore point is demanded before the first change;
  if Windows refuses it, nothing applies.
- **Apps** — Environment Capsules: curated packs (13 packs, 105 apps)
  installed through **winget** (primary) and **Chocolatey** (optional),
  orchestrated by the kernel spine: proposal → capability → receipt.
  The catalog leads with **Add Back Edge** — Capsule restores what
  other tools remove.
- **Repair** — Microsoft's own DISM/SFC sequence, the Capsule repair
  toolkit, a restore-point manager, and baseline snapshots.
- **Utilities** — DNS changer (DHCP-first, verified by re-reading),
  a files-only Cleaner that shows sizes before deleting anything,
  Quick Tools.
- **Bible** — the Toolbox Bible: the whole canon on-device, free.
- **Models** — AI models as organisms: adopt from the pinned source,
  verify every file (SHA-256 for weights, git-blob SHA-1 for small
  files), then fork and mutate — every step a receipt, every card a
  lineage. Inference lands in finalizing (see FINALIZING.md).

## The Capsule Order

1. Windows is the foundation, not the enemy.
2. Observe before you modify — no change without a baseline.
3. Receipt, or it didn't happen.
4. No rollback, no ship.
5. Compatibility outranks speed.

Working order beneath: utility earns the install, receipts earn the
trust, the ecology earns the memory. Above all of it, D7: humans are
sources of authority; systems never are.

## Verification state (honest)

- Service layer compiles at **0 errors** under the .NET 8 SDK.
- Ledger port is byte-compatible with the SI kernel (ground-truth
  vectors pass; tampered chains fail).
- Tweak catalog parity vs the shipped 0.2.2 playbook: exact
  (619 apply / 618 revert — the one delta is a known apply-only step).
- Pack + model catalogs machine-validated (`contracts/*/validate_*.py`).
- Model hash semantics proven against live source data.
- **Not yet run**: the WinUI XAML compile and the app itself — both
  are Windows steps. See **FINALIZING.md** for the exact checklist.

## Layout

- `app/` — the WinUI application (build in VS2022)
- `contracts/` — the R0 contracts: ledger, lineage schema, tweak /
  pack / model catalogs + validators
- `capsule/` — the .capsule package format + reference verifier
- `preview/` — the single-file design target (open in any browser)
- `ROADMAP.md` — phases R0–R10 with verified status
- `FINALIZING.md` — what remains to final, and what proves it

Created by **N8toriousAi**. Third-party models keep their creators'
names (Microsoft / onnx-community / Xenova) — lineage is honest or
it is nothing.
