# Capsule Ecology — native app (R1 source)

WinUI 3 / .NET 8, unpackaged full-trust — the locked stack.

## Build on Windows

1. Visual Studio 2022 with the **.NET desktop development**
   workload and the **Windows App SDK** (single-project
   development) component.
2. Open `CapsuleEcology.csproj`, restore NuGet (WindowsAppSDK
   1.6.250108002), select **x64**, run.

Written and contract-verified on the Linux side; the compile is
the Windows step. If the SDK pin disagrees with your installed
tooling, bump `Microsoft.WindowsAppSDK` to your installed 1.6+
patch — the code uses only template-grade APIs.

## What's wired

- **Shell** — NavigationView in the preview's structure:
  Civilization / Toolbox / Atlas.
- **Your Ecosystem (home)** — live counts from the real data:
  model cards, the 0.2.2 catalog (85 options / 619 actions),
  and the demo lineage chain sealed by the C# ledger port.
- **Models** — both Phi cards from `Assets/cards/`, rendered
  from the verified R0 JSON.
- **Receipts** — the ledger, live: verify + a tamper button
  that edits a *sealed* entry (the only honest kind of tamper)
  so verification fails as it must.
- **Tweaks** — the full 85-option catalog, searchable. Browse
  now; apply arrives in R2 behind a restore point + receipt.
- **Loud stubs** — Lineage, Agents, Capsules, Bible, Repair,
  All Pages: each names its roadmap phase in the page itself.

`Services/Ledger.cs` is the R0 contracts port (byte-compatible
with si-kernel.js), plus a `Clear()` helper for the demo page.

## Compile verification (2026-11-02)

The service layer (Services/*.cs — Ledger, Data, TweakEngine,
PackageService) is compiler-verified: built with the real .NET 8 SDK
(8.0.404) in a Linux check project — **0 errors**. The full WinUI
build was also attempted there; it proceeds through restore and
stops exactly at XamlCompiler.exe (a Windows .NET Framework binary,
exit 126 on Linux) — that step, and only that step, is the Windows
handoff. Build in Visual Studio 2022 on Windows as above; expect
the XAML compile + link to be the first thing that runs.

## Apps (R10)

Assets/packs/packs.catalog.json is the pack catalog (12 packs,
59 apps, winget + Chocolatey ids). Services/PackageService.cs is
the manager: winget primary, Chocolatey optional, restore point
before any install, one receipt per app. Contract source of truth:
contracts/packs/ (with validate_packs.py).

## R3 (2026-11-02)

Repair, Utilities and Bible faces are real pages now (no longer
stubs): RepairPage (toolkit install/uninstall, DISM/SFC repair run
with dated logs, restore-point manager, baseline snapshots),
UtilitiesPage (DNS changer with DHCP revert, files-only Cleaner
with sizes first, Quick Tools), BiblePage (Toolbox Bible reader +
full-canon search; Assets/bible carries both canon JSONs).
New services: RepairService, RestorePointService,
SystemSnapshotService, DnsService, CleanerService, BibleData —
all compile-checked at 0 errors under the .NET 8 SDK; the only
Windows step remains XamlCompiler, as before.

## R4 core (2026-11-02)

ModelsPage is the adoption desk now: download-from-source (pinned
Hugging Face revisions), per-file verify (SHA-256 for LFS weights,
git-blob SHA-1 for small files), adopt (verified state only),
fork/mutate with local lineage cards. Services: ModelService +
Data.LoadModelCatalog (Assets/models/models.catalog.json).
Local inference (OnnxRuntime) is NOT built yet — see FINALIZING.md
at the repo root.
