# ROADMAP — Capsule Ecology, the native application

Founded 2026-11-02. Nathan's decisions, in his words, govern this
document. Checkbox = verified, never aspirational. Nothing here
is built yet; this roadmap exists before code, per standing law.

## Locked decisions

- **"Go fully native — one real Windows application."**
  (Nathan, 2026-11-02, tapped.) No Tauri shell, no Node sidecar,
  no WSL dependency for core features. One application.
- **The product is the Capsule Ecosystem/Ecology** — "people
  don't install a playbook, they adopt a lineage." Homepage =
  **Your Ecosystem**, not CPU/RAM/tweaks. Full text:
  `../capsuleos-toolbox-edition/VISION-CAPSULE-ECOSYSTEM.md`.
- **The three laws** bind every feature: security never disabled;
  full Windows compatibility; never lock the user out — nothing
  a user can't undo from inside Windows.
- **Receipts over claims. "Inspect it," not "Trust Nate."**
- **Models are organisms**: adopted, raised, forked, mutated,
  shared — never bundled. Models arrive only as user-chosen
  downloads from the original source, hash-verified, with
  **Capsule Lineage** on every artifact (parent, grandparent,
  constitution version, kernel generation, mutation list,
  creator — ancestry always visible, chain-verifiable).
- **Size bar ~600 MB installed; polish bar: "make these people
  salivate."** Harakiri aesthetic (obsidian/crimson, PainTr).
- The law-bound settings core already exists (CapsuleOS 0.2.2:
  619 gated, mirrored actions). It enters the app as **data** —
  one option catalog driving both the app and the .apbx.

## Stack (locked 2026-11-02)

Nathan, on the recommendation below: "I wanna go with what you
consider to be the best route so far." Locked accordingly.

- **Shell: C# / .NET 8 + WinUI 3, unpackaged, full-trust.**
  The current native Windows UI stack; XAML for the salivation
  layer; unpackaged (not MSIX-sandboxed) because a settings app
  needs real registry/service access — same trust posture as
  Sparkle, shipped via a signed NSIS installer (the "known
  installer" in the trust flip). Build happens on Windows
  (Nathan's laptop); contracts/data layers test anywhere.
- **Models in-process: Microsoft.ML.OnnxRuntime with DirectML.**
  Official .NET ONNX runtime — the Node sidecar's job, absorbed.
- **3D (voxel editor / modeler): Direct3D 11 via WinUI
  SwapChainPanel** (Vortice bindings). The three.js scene logic
  ports as *design*, not code — native rendering, math-only orbit
  camera law applies unchanged.
- **Agents in-process:** the assistant's proposal → audit flow
  (kernel-proposals → auditor-proposals in the rough build)
  ports to C# modules behind the same contracts. Mira's Gate
  pattern; D7 stands — systems propose, humans hold authority.
- **Ledger: C# port of the SI receipt chain**, byte-compatible
  (fnv1a, genesis `genesis0`) — lineage chains verify across the
  web kernel, Nimbusis, and the app.
- Alternative considered: Rust shell (windows-rs). Rejected as
  the *primary* recommendation on UI-toolkit maturity vs the
  polish bar and timeline — Nathan's Rust/Zig stays sovereign
  in the kernel line. If he overrules, R0–R1 estimates change,
  contracts don't.

## Material standard (Nathan, 2026-11-02)

Frosted glass + 3D bezel pops — Temple's frosted glass and
the launcher bezel law, unified: cards and model cards are
RAISED, rim-lit, glossed glass over an ambient crimson
nebula; controls (inputs, buttons at rest) are CARVED
(inset). In WinUI: AcrylicBrush + ThemeShadow at
Z-translation ( in App.xaml, 
on the nav pane). The preview (v3) is the reference render.

## Color direction (Nathan, 2026-11-02 — standing)

The entire UI palette is cleaned up to be MORE INVITING:
warm charcoal instead of near-black, a friendlier rose
accent instead of hard crimson, softer contrast throughout.
Chrome only — content (the world, the paintings, scripture)
keeps its natural colors in every theme, per the theme law.
The native app's theme pass inherits this palette, not the
old obsidian severity. Applied to the preview first (v17).

## Design principle — THE CAPSULE ORDER (RATIFIED, Nathan 2026-11-02)

1. Windows is the foundation, not the enemy.
2. Observe before you modify — no change without a baseline.
3. Receipt, or it didn't happen.
4. No rollback, no ship.
5. Compatibility outranks speed.

Working order: utility earns the install, receipts earn the
trust, the ecology earns the memory — nothing ships ahead of
its layer. D7 stands above all five. Full checklist:
UTILITY-FIRST-CHECKLIST.md.

## What carries over unchanged (data, not rewrites)

- CapsuleOS 0.2.2 option catalog (generator output = the app's
  tweak definitions; the .apbx stays the install-time surface).
- Toolbox Bible JSONs (31,098 verses, verified) + reader design.
- Both model catalog entries (repo, revision, per-file hashes)
  → the first two Model Cards, lineage fields added.
- Repair toolkit scripts → the engine under the Utilities face.
- The rough build (Tauri + Node + WSL) is a *mine*, not a base:
  port behavior and data, carry no code across — "start new"
  discipline for the shell.

## Phases

- [x] **R0 — Contracts & schemas.** ✅ 2026-11-02, all verified
  in `contracts/`: ledger C# port cross-checked against
  si-kernel.js ground-truth vectors (5/5 hashes, chain verifies,
  tamper case fails); Capsule Lineage card schema v1 + the two
  Phi cards (weights reconcile to the byte); option-catalog
  importer (85 options, 619 apply actions — exact parity with
  the packaged 0.2.2 audit, zero unexplained breaks).
- [~] **R1 — Shell + Your Ecosystem home.** Source written
  2026-11-02 (`app/`): NavigationView shell in the preview's
  structure; Home / Models / Receipts / Tweaks wired to the
  real R0 data; remaining sections are loud stubs naming
  their phases. Box checks after the Windows-side compile
  (VS2022 + Windows App SDK).
- [~] **R2 — Tweaks, natively.** Engine source written
  2026-11-02 (`app/Services/TweakEngine.cs`): restore point
  demanded before apply (no point, no apply — refusal by
  absence), per-option receipts into a self-verifying JSONL
  ledger, HKU mapped to Registry.Users. Parity battery
  PASSED over the catalog (`contracts/catalog/plan.py`):
  mirror parity exact for all 85 options, Three Laws scan
  clean, dry-run plans exact. Box checks when it runs on
  Windows alongside the 0.2.2 machine-state comparison.
- [~] **R3 — Utilities + Bible.** SOURCE COMPLETE 2026-11-02
  (Nathan's "Go"): RepairService (toolkit install/uninstall via
  the shipped scripts — now the full 6-file Executables set in
  Assets/scripts; DISM CheckHealth→ScanHealth→RestoreHealth→
  SFC→confirm SFC, dated logs, plain refusal when not elevated),
  RepairPage (toolkit + repair run + restore points +
  baselines); RestorePointService (list/create) +
  SystemSnapshotService (capsule-baseline/1 JSON: OS, hardware,
  disk, DNS, restore points, catalog fingerprints, chain state —
  the committed P1/P2 debt); DnsService (WMI read, elevated
  write, verify-by-reread, DHCP-first) + CleanerService (files
  only, sizes first, thumbcache/temp/prefetch/recycle, skips
  counted) + UtilitiesPage (DNS, Cleaner, Quick Tools incl.
  power plans); BibleData + BiblePage (reader, A−/A+,
  full-canon search capped at 200, opens at Psalm 23; assets
  verified 66/1,189/31,098). All services compile-checked
  0 errors under .NET 8; XAML well-formed; full build still
  stops at the single XamlCompiler.exe Windows step. Remaining
  in scope: Bible bookmarks/highlights. Original scope text:
  Repair toolkit face (DISM/SFC
  with live log view); Bible reader (search, bookmarks,
  highlights — Temple's lessons applied). Scope confirmed by
  the 2026-11-02 Sparkle cross-examination (all four already
  prototyped in the preview, v13): Cleaner (files only, sizes
  first, dry-run list, never settings/services/logs), DNS
  Changer (current DNS shown, one-tap return to DHCP), Restore
  face (restore-point status + the 618 revert mirrors +
  receipts as third witness), Quick Tools (cleanmgr, msinfo32,
  Storage Sense, Fast Startup, driver/explorer restarts, power
  plans). Utility-first checklist (2026-11-02) — with The
  Capsule Order ratified, the P1/P2 debt is COMMITTED to this
  phase: baseline snapshot (inventory before first change) and
  a restore-point manager (list / pick / roll back).
- [ ] **R4 — Models.** Download-from-source cards (disclosed
  source/size/license/hash, user-initiated only), hash verify,
  local inference (OnnxRuntime/DirectML), Model Cards with
  lineage; adopt/fork/mutate flows writing receipts.
  Hash semantics PROVEN 2026-11-02 against the live source:
  weights verify by SHA-256 (LFS oid); small files by
  **git-blob SHA-1** — naive file SHA-1 would refuse valid
  downloads (caught on generation_config.json, 195 B, blob
  hash matches the catalog exactly).
- [ ] **R5 — Worlds.** Voxel procedural editor on the native 3D
  viewport; capsule scene format (sealed scene + mods, tradable
  — the card-collection forge from the 2026-11-01 design).
  Preview editor (2026-11-02, v10–v11): Blender-pattern dock
  drawer (Sculpt/Paint/Props/Terrain/View), falloff brush engine,
  undo/redo, generators + erosion + sea level — all receipt-
  sealed. Generation follows Nathan's World Gen V2 recipe
  (Voronoi biome tiles; warped Voronoi-edge rivers carved
  descending; biome props). 64×96 globe with moisture and
  polar snow-line fields; zoom resolves subdivision detail,
  tree glyphs, rivers and 8 walking citizens with tracking.
  The preview file itself is now a 15.25 MB deep build: full
  Toolbox Bible embedded with a working reader/search, the
  full 619-action catalog with per-option inspector, both
  models' complete source manifests + embedded documents, the
  real Phi tokenizer (searchable, downloadable), and nine
  painted page heroes.
  Preview v14 (2026-11-02): the world is REAL 3D — WebGL2
  instanced voxel cubes (6,144) displaced by the heightfield,
  a procedural texture-array atlas (per-biome color + bump
  layers: grass blades, rock strata, sand ripples, ice cracks,
  water streaks) with per-pixel lighting, instanced 3D trees /
  rocks / citizens, billboard clouds, fresnel atmosphere, and
  raycast picking through the true camera — no 2D assets.
  (Nathan: "the 3D model needs real 3D textures, not 2D
  assets.") Same day: a theme system — Obsidian / Slate /
  Paper (light) presets + a user-picked accent color driving
  the whole chrome; content viewports keep their natural
  colors in every theme (Nathan: light version + users choose
  their own colors).
- [ ] **R6 — Modeler.** Create/edit models (glTF/OBJ first; 3DM
  import if the native path proves out); every save mints
  lineage (creator, parents, mutation note).
- [ ] **R7 — Agents.** Crew page: agents as citizens (cards,
  traits, state), proposals queue at the Gate for the user's
  approval; nothing executes unapproved (D7).
- [~] **R8 — Exchange.** Format drafted + reference
  implementation VERIFIED 2026-11-02 (`capsule/`):
  CAPSULE-FORMAT.md, capsule.py pack/verify, a real sample
  capsule (Phi-3.5 card + adoption chain, head 30a92edd);
  a one-byte payload tamper is REFUSED, as required.
  Fork/mutate/remix verbs and the native implementation
  still to come.
- [ ] **R9 — Ship shape.** Size audit vs the 600 MB bar;
  installer signing; AME coexistence (.apbx 0.3 installs the
  app); AME load test — the debt from v0.1.0 finally paid here;
  publication per Nathan's call.
- [ ] **R10 — Environment Capsules (Apps).** Added 2026-11-02
  on Nathan's order ("Add these to our roadmap"). App
  packaging is NOT a download dump: a pack is a curated
  environment — apps + settings + recommendations +
  documentation + optional tweaks — installed as a role
  ("install a Developer Capsule, not five installers").
  Sources: **winget primary** (ships with Windows, resolves
  to upstream vendor installers, no added agent); **Chocolatey
  optional** second source for packages winget lacks (its
  catalog breadth is real; its community feed is a third-party
  trust surface, so Chocolatey packages carry the same pin +
  disclose + verify treatment, never silent trust). Every
  install runs the Capsule Order: restore point first →
  manifest disclosed up front (each app: what / from where /
  how big, per the download law) → user-initiated → one
  receipt per app (package id, version, source) → uninstall
  mirrors for the whole pack. Edge is a first-class citizen
  (Privacy-Enhanced Edge stack: tracking prevention + DNS +
  extension recommendations) — strengthen Windows, don't
  amputate it. Starter packs from Nathan's list: Privacy
  Starter, Windows Power User, Developer, Minecraft Modding,
  Hytale Creator, Content Creator, Gamer, Retro Gaming, Home
  Security, Power User Security, Laptop Longevity, Family PC,
  Windows Recovery. Pack definitions are data in the R0
  contract style; a pack is itself a .capsule payload kind
  (environment), so packs inherit lineage + verification.
  PROGRESS 2026-11-02 (Nathan: "use winget and chocolatey
  as package installers"): pack catalog BUILT —
  contracts/packs/packs.catalog.json — now 13 packs /
  105 apps (102 with both a winget and a Chocolatey id),
  validated by contracts/packs/validate_packs.py. Sparkle
  2.25.0's apps gallery was mined and repurposed at
  Nathan's direction; the catalog LEADS with "Add Back
  Edge" — restoration as a headline option, the standing
  answer to every tool that ships an Edge remover. Native
  side BUILT: Services/PackageService.cs (source detection,
  restore-point gate, disclosed plan, per-app receipts with
  resolved versions, uninstall mirrors) + Views/AppsPage
  wired in the shell. The app's whole service layer now
  COMPILE-CHECKS under the real .NET 8 compiler (0 errors,
  verified in the Linux VM); the final XAML compile + link
  remains a Windows/VS step by platform design.

## Open — Nathan's calls, in his time

1. Installer signing certificate (the trust flip argues yes;
   it costs money — his call).
2. The app's final name (Capsule Ecology is the candidate,
   not yet locked — naming is his).
3. Whether WSL survives at all as an optional provision
   (the assistant's Linux muscle) or the native port absorbs it.

## Build notes

- Windows builds run on Nathan's laptop (WinUI requires it);
  this VM verifies contracts, data, and chains.
- One build line. The rough build and the playbook line are
  mines and components — never parallel variants of the app.
