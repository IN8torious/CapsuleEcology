# FINALIZING — Capsule Ecology v0.1.0 pre-release → final

For Nathan + Copilot. Everything below is the honest remainder between
this pre-release and a final release. Nothing here is hidden debt —
each item names what proves it done.

## 1. Build it (Windows, Visual Studio 2022)

1. Open `app/CapsuleEcology.csproj` in VS2022 (Windows App SDK 1.6,
   .NET 8). Restore NuGet, build **x64 Release**.
2. Expected state: the entire service layer (Services/*.cs) already
   compiles at **0 errors** under the .NET 8 SDK (verified in CI-less
   Linux check). The step that has never run is the XAML compiler —
   it is Windows-only by platform design. First build either passes
   it or names the exact XAML that needs attention.
3. Run it. Every page should open: Home, Tweaks, Apps, Utilities,
   Repair, Bible, Models, Receipts. Stubs remain only for Lineage,
   Agents & Crews, Capsules & Worlds, All Pages.

## 2. First-run checks (in order)

- **Receipts page**: chain verifies against the si-kernel ground truth.
- **Tweaks**: select one option, read the plan, apply, revert. A restore
  point is demanded before the first apply — if Windows refuses it,
  nothing applies. That refusal is correct behavior.
- **Apps**: install one small pack first (Windows Recovery — Rufus is
  ~1.5 MB). Expect: restore point, per-app receipts with resolved
  versions, uninstall mirror works.
- **Repair**: install the toolkit; run Quick SFC as administrator.
  Capture a baseline snapshot; confirm the JSON lands in
  %LocalAppData%\CapsuleEcology\baselines\.
- **Models**: Download Phi-3.5 (2.32 GB from the pinned source) →
  Verify (all 16 files) → Adopt → Fork it. Each step seals a receipt.

## 3. Remaining build items

- **Local inference (the last R4 item).** Download + verify + lineage
  are built; the chat surface is not. To finish: add
  Microsoft.ML.OnnxRuntime (DirectML) to the app, load
  `onnx/model_q4f16.onnx` (external data file rides alongside), BPE
  tokenizer from the downloaded `tokenizer.json` (32,000 tokens,
  61,249 merges), Phi-3 chat template, KV-cache generation loop.
  Gate it exactly as the Models page states: unverified models never
  run. A CPU smoke test was attempted in the Linux build VM; treat
  inference as **unproven until it generates text on Windows**.
- **Bible bookmarks + highlights** (R3 remainder).
- **Native theme pass**: adopt the preview v17 token set (warm
  plum-charcoal, accent #d9777f, Slate + Paper presets, per-user
  accent) in App.xaml. The preview HTML in `preview/` is the target.
- **Lineage / Agents / Worlds pages**: replace the SoonPage stubs.
  The World's WebGL build in the preview is the design target.

## 4. Ship shape

- **Installer**: signed NSIS, unpackaged full-trust (decided at stack
  lock). Signing certificate = Nathan's open call — the trust flip
  argues for it ("Inspect it" needs a known installer).
- **Product name**: "Capsule Ecology" is the working candidate.
  Naming is Nathan's; rename before the final release, not after.
- **AME coexistence**: a CapsuleOS .apbx 0.3 that installs this app
  closes the loop with the playbook line; the v0.1.0 AME Wizard load
  test is still owed on the playbook side.

## 5. The laws (do not finalize past these)

1. Windows is the foundation, not the enemy.
2. Observe before you modify — no change without a baseline.
3. Receipt, or it didn't happen.
4. No rollback, no ship.
5. Compatibility outranks speed.
Above all five: D7 — humans are sources of authority; systems never are.
Plus the Three Laws: security features are never disabled; full Windows
compatibility; the user is never locked out of settings.
