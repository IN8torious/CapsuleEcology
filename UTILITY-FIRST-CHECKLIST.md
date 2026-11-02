# Utility-First Roadmap — Checklist Against Our Real State

Source: Nathan's three converging messages, 2026-11-02 13:36–13:54 EST
(Ecology Arc → three-pillar framing → "Strengthening Windows, Not
Replacing It," Phases 1–10 + Five Laws). Checklisted by Ogechi against
shipped artifacts, 2026-11-02. Statuses are evidence-based: BUILT means
a file exists and was verified; PARTIAL names what is missing.

## The verdict in one line

Yes — this is our roadmap with the order corrected. Almost everything
in Phases 1–4 and 10 already exists in some verified form; Phases 5,
6 (tooling) and 8 are the real gaps; the ecology content (Phase 9) is
unchanged from the adopted 11:24 vision, only moved to where it is
earned. Nothing here contradicts the Three Laws — the "Five Laws" in
the paste are the Three Laws plus two gates we were already missing
names for (posture, observe-first).

## Phase checklist

- [~] **P1 System Truth** — PARTIAL. Preview Home carries a live
  vitals card (cores, memory, network, display, storage, battery —
  real browser signals, labeled as such). Native app declares
  System.Management. NOT built: full inventory (drivers / services /
  startup / scheduled tasks), health score, baseline snapshot.
  ⚠ MEDUSA caution: a single Health Score that cannot show its
  formula is a painting — ship subscores or don't ship the number.
- [~] **P2 Recovery First** — STRONG PARTIAL. Restore point is
  DEMANDED before any apply in TweakEngine.cs (refusal by absence —
  if Windows refuses the restore point, nothing applies). Repair
  toolkit shipped in CapsuleOS 0.2.2 (DISM CheckHealth→ScanHealth→
  RestoreHealth→SFC, dated logs, revert mirror). Revert parity exact
  (618 mirrors / 619 applies). NOT built: restore-point manager
  (list/pick/roll back), driver rollback, one-click repair buttons.
- [x] **P3 Optimization With Receipts** — BUILT (one honest caveat).
  85 options / 619 actions as data; every option inspectable down to
  the single registry value; plan assembles before apply; applying
  seals a receipt into the self-verifying chain; scope chips
  (user/machine) and computed deep-reads on every option. Caveat:
  "Performance Gain +12 / Security Delta 0" style numbers are NOT
  measured — ours are guaranteed by construction (Three Laws scan)
  and proven by the parity battery, not benchmarked. Measuring real
  deltas is future work; inventing them would be a painting.
- [~] **P4 Compatibility Layer** — PARTIAL. Build-side: 18 law
  patterns scan at zero, revert parity exact, repair-path integrity
  asserted on the packaged 0.2.2. NOT built: per-option impact
  warnings at apply time ("this touches Memory Integrity — continue?")
  — the inspector has the data to compute these; the gate UI doesn't.
- [ ] **P5 Performance Center** — NOT STARTED beyond vitals. No
  startup analyzer, no service tiering (Recommended / Experimental /
  Unsafe — a good frame; our catalog's default-on set is a crude
  version of "Recommended"), no live tracking.
- [~] **P6 Security Strengthening** — POSTURE BUILT, TOOLING NOT.
  We are already the opposite of the questionable playbooks by audit:
  no Defender/Firewall/SmartScreen/Update targets in any shipped
  build (0.1.0 audit + 0.2.x law scans). NOT built: security scanner,
  hardening profiles, security receipts.
- [~] **P7 Daily Workspace** — PARTIAL. Bible reader + full-canon
  search built; World built (now real 3D); quick-tools tiles built
  (Cleaner, DNS Changer, Restore face — preview seals plans; native
  faces are R3). NOT built: universal search (Bible-only today),
  notes.
- [ ] **P8 Automation & Intelligence** — NOT STARTED. No schedules.
  D7 note: scheduled maintenance is lawful only as user-armed rules
  (delegated capability), never self-originating change. The
  inspector's deep-reads are the seed of the AI Technician's
  explanations ("what / why / risk") — the shape already exists.
- [~] **P9 Windows Ecology** — IN PROGRESS, bridge unbuilt. The world
  is now real 3D geometry with real texture maps (WebGL2 instanced
  voxels, 2026-11-02). What is NOT built: binding terrain to the real
  machine's history (mountains = stability, storms = detected
  problems). The preview's chain is a demonstration ledger; the
  binding needs the native app's real receipts store. This phase is
  the Ecology Arc's Phases 1–5 wearing work clothes — same content,
  earned order.
- [~] **P10 Capsule Preservation** — PARTIAL. `.capsule` format built
  and verified (manifest + lineage card + receipt chain + payload;
  one-byte tamper refused, proven) with model payloads. NOT built:
  the environment payload kind (preferences + layouts + receipts +
  recovery references) that makes "rebuild a new PC in minutes" real.

## The Five Laws vs our Three Laws

Paste's Five: (1) Windows First (2) Observe Before Modify
(3) Every Change Has A Receipt (4) Every Change Has A Return Path
(5) Compatibility Is A Feature.

Ours (Nathan, 2026-11-02 10:45): never disable security · full
compatibility · never lock the user out. Mapping: his Law 1 lives
inside paste-Law 5 + Phase 6 posture; his Law 2 IS paste-Law 5; his
Law 3 is paste-Law 4 read strictly (a return path that ends at a
locked Settings page is no return path). Paste-Laws 2 and 3 name
gates we practice but never wrote down: baseline-before-change
(Phase 1 — currently only the restore point honors it) and the
receipt as the unit of trust (the ledger, R0).

## Design principle — RATIFIED (Nathan, 2026-11-02 14:26 EST)

**THE CAPSULE ORDER**
1. Windows is the foundation, not the enemy.
2. Observe before you modify — no change without a baseline.
3. Receipt, or it didn't happen.
4. No rollback, no ship.
5. Compatibility outranks speed.

Working order, in one breath: *utility earns the install, receipts
earn the trust, the ecology earns the memory — and nothing ships
ahead of its layer.* D7 stands above all five: the system proposes,
the human disposes; even in Phase 9 the world only ever SHOWS
history — it never authors it.

## What changes in the engineering roadmap (ratified — applied to ROADMAP.md 2026-11-02)

- R3 grows: Restore face + health/baseline snapshot join Utilities +
  Bible (P1/P2 debt is utility-layer, so it outranks model work).
- R4 Models stays, but the ecology dashboard/genetics (Arc P1/P2)
  move behind R7's real receipts — identity reads real history or
  waits.
- New backlog items: P5 Performance Center, P6 scanner + profiles,
  P8 user-armed automation. None displace the Three Laws scans.
- Sequencing revision to record: the 11:24 civilization vision is
  NOT repealed — it is re-ordered. Civilization becomes the flavor;
  the toolbox remains the reason it exists (Nathan, 13:51).
