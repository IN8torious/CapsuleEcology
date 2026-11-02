# Cross-Examination — Sparkle 2.25.0 vs Capsule (2026-11-02)

Nathan's order: "cross examine it with the sparkle os package and see
where we can improve." Source: sparkle-2.25.0-win.zip (142 MB, MD5
verified against Drive), app.asar fully mined (134 app-own files;
renderer bundle extracted and read). Nothing executed.

## What Sparkle has that we adopted (preview v13, same day)

- **Cleaner page** — theirs wipes files by category; ours is
  law-filtered: files only, never settings / services / logs
  ("event logs are evidence"), sizes first, plan-then-seal. Their
  categories (temp, prefetch, recycle bin, thumbnail cache)
  matched ours exactly once mined.
- **DNS Changer** — their provider table mined verbatim
  (Cloudflare 1.1.1.1/1.0.0.1, Google 8.8.8.8/8.8.4.4, Quad9
  9.9.9.9/149.112.112.112, OpenDNS, AdGuard); ours lists Automatic
  (DHCP) FIRST, framed as the way back, and seals the choice.
- **Utilities face** — their seven quick tools mapped to our eight
  tiles (incl. Capsule Repair). Their Restore page confirmed our
  design: restore points are a first-class surface, not a setting.
- **Live stats** — theirs (systeminformation tiles) vs ours: an
  honest vitals card reading real browser signals, labeled as
  such. A preview that invented numbers would be a painting;
  the native build reads the full sensor set.
- **Two-depth explanations** — their tweaks carry a plain + a deep
  description. Ours computes the deep read from the catalog
  itself (roots written, services touched, revert coverage %,
  scope) — explanations that cannot drift from the actions.

## What Sparkle ships that we refuse (exhibits, verbatim)

- `disable-defender-rtp`: `Set-MpPreference -DisableRealtimeMonitoring $true`
  (their own meta calls it "risky"; it ships anyway)
- `disable-bitlocker`: `Disable-BitLocker -MountPoint $Env:SystemDrive`
- `disable-core-isolation`: HVCI `Enabled = 0`
- remove-edge, remove-onedrive, a 36 KB debloat-windows AppX
  script, set-services-to-manual, wpbt

Each violates at least one of Nathan's Three Laws. These are the
category's credibility problem in one bundle — and the reason the
receipt + restore-point + mirror discipline is the product.

## Deferred, with reasons

- **Apps Installer** — Sparkle's winget/Chocolatey gallery is a
  bare download dump. Deferred until it could be specified as
  curated environments; specified 2026-11-02 as roadmap R10
  (Environment Capsules) on Nathan's order.
- **Updates page** — information only for us; cadence controls
  that delay security patching stay refused under Law 1.

## Our moats Sparkle does not have

Receipts that verify (hash-chained, tamper-refusing) · revert
parity proven by battery, not promised · a restore point DEMANDED
before apply · lineage on every artifact · the Bible · the world.
Sparkle is a well-made settings panel. Capsule is the same panel
with a memory, a conscience, and a paper trail.
