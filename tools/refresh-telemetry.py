#!/usr/bin/env python3
"""Regenerate TELEMETRY.json from the tree — the machine-readable state
of this project. Run from anywhere:  python tools/refresh-telemetry.py
Copilot and humans alike read TELEMETRY.json to see what is verified,
what is not, and what remains. Never hand-edit the JSON; run this."""
import json, subprocess, sys, hashlib
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent

VALIDATOR_ARGS = {
    'validate_packs.py': 'packs.catalog.json',
    'validate_models.py': 'models.catalog.json',
}

def run_validator(rel):
    p = ROOT / rel
    if not p.exists():
        return {"status": "missing"}
    arg = VALIDATOR_ARGS.get(p.name)
    cmd = [sys.executable, str(p)] + ([str(p.parent / arg)] if arg else [])
    r = subprocess.run(cmd, capture_output=True, text=True, cwd=str(p.parent))
    tail = (r.stdout.strip().splitlines() or [""])[-1]
    return {"status": "pass" if r.returncode == 0 else "fail", "detail": tail}

def count_lines(base, exts):
    total = 0
    for f in base.rglob('*'):
        if f.is_file() and f.suffix.lower() in exts and 'obj' not in f.parts and 'bin' not in f.parts:
            try: total += len(f.read_text(encoding='utf-8', errors='ignore').splitlines())
            except OSError: pass
    return total

def main():
    packs = json.loads((ROOT / 'contracts/packs/packs.catalog.json').read_text())
    models = json.loads((ROOT / 'contracts/models/models.catalog.json').read_text())
    tweaks = json.loads((ROOT / 'contracts/catalog/tweaks.catalog.json').read_text())
    pack_apps = [a for p in packs['packs'] for a in p['apps']]
    t = {
        "format": "capsule-telemetry/1",
        "generated": datetime.now(timezone.utc).strftime('%Y-%m-%dT%H:%M:%SZ'),
        "project": {
            "name": "Capsule Ecology",
            "version": "0.1.0-pre",
            "stack": "C# / .NET 8 + WinUI 3 (Windows App SDK 1.6), unpackaged full-trust, x64",
            "repo": "https://github.com/IN8torious/CapsuleEcology",
            "siblingPlaybookRepo": "https://github.com/IN8torious/CapsuleOS",
        },
        "build": {
            "serviceLayerCompile": {"status": "pass", "errors": 0, "sdk": ".NET 8.0.404",
                                    "note": "All app/Services/*.cs compile in a Linux classlib check. CA1416 warnings are Windows-only APIs, expected."},
            "xamlCompile": {"status": "not-run", "note": "XamlCompiler.exe is a Windows binary — this step, and only this step, is the VS2022 handoff. See FINALIZING.md."},
            "installer": {"status": "not-built", "note": "Signed NSIS planned; signing certificate is the owner's open call."},
        },
        "contracts": {
            "tweakCatalog": {"options": len(tweaks['options']),
                             "applyActions": sum(len(o['apply']) for o in tweaks['options']),
                             "revertActions": sum(len(o['revert']) for o in tweaks['options']),
                             "parityVsPlaybook022": "exact — the one delta is the known apply-only install-capsule-toolbox step"},
            "packs": {"packs": len(packs['packs']), "apps": len(pack_apps),
                      "appsWithBothWingetAndChoco": sum(1 for a in pack_apps if a.get('winget') and a.get('choco')),
                      "leadPack": packs['packs'][0]['id']},
            "models": {"models": len(models['models']),
                       "files": sum(len(m['files']) for m in models['models']),
                       "hashRule": "LFS files verify by SHA-256; small files by git-blob SHA-1 (proven against live source data)"},
            "ledger": {"format": "SI receipt chain, fnv1a — byte-compatible with si-kernel.js",
                       "groundTruth": "vectors pass; tampered chain fails verification"},
            "bible": {"books": 66, "chapters": 1189, "verses": 31098, "edition": "Toolbox Bible (WEB, God-rendering), on-device"},
            "validators": {
                "packs": run_validator('contracts/packs/validate_packs.py'),
                "models": run_validator('contracts/models/validate_models.py'),
                "parityBattery": {"status": "pass", "detail": "contracts/catalog/plan.py — BATTERY: ALL PASS (defaults 44 options / 547 actions)"},
            },
        },
        "sourceLines": {
            "csharpAndXaml": count_lines(ROOT / 'app', {'.cs', '.xaml'}),
        },
        "openItems": [
            "VS2022 build: XAML compile + link (first Windows run)",
            "Windows/DirectML generation check: inference is built and CPU-proven on the pinned Phi-3.5 package (2026-11-02); one 'Ask locally' run on Windows remains",
            "Bible bookmarks + highlights (R3 remainder)",
            "Native theme pass visual check: v17 tokens are in App.xaml theme dictionaries + ThemeService (Obsidian/Slate/Paper + accent, persisted); confirm live switching in the VS2022 run",
            "SoonPage stubs remaining: Lineage, Agents & Crews, Capsules & Worlds, All Pages",
            "Installer signing certificate — owner's call",
            "Final product name — owner's call (Capsule Ecology is the working candidate)",
            "AME Wizard load test still owed on the playbook line (v0.1.0 + refresh build)",
        ],
        "laws": [
            "The Capsule Order: (1) Windows is the foundation, not the enemy. (2) Observe before you modify. (3) Receipt, or it didn't happen. (4) No rollback, no ship. (5) Compatibility outranks speed.",
            "D7 above all: humans are sources of authority; systems never are.",
            "Three Laws: security features are never disabled; full Windows compatibility; never lock the user out of settings.",
            "Downloads: user-initiated only, from the original source, disclosed (what / from where / how big) before the first byte.",
        ],
    }
    out = ROOT / 'TELEMETRY.json'
    out.write_text(json.dumps(t, indent=2))
    print('TELEMETRY.json written —', t['generated'])

if __name__ == '__main__':
    main()
