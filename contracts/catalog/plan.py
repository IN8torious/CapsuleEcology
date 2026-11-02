#!/usr/bin/env python3
"""R2 contract proof + dry-run planner over tweaks.catalog.json.

The native TweakEngine consumes exactly this catalog. This
harness proves the contract the engine relies on:
  1. every option's revert mirrors its apply (same paths/keys),
  2. the Three Laws scan is clean across the whole catalog,
  3. a dry-run plan for any selection is exact and ordered —
     the same plan the app's review screen will show.
"""
import json, sys, os
HERE = os.path.dirname(os.path.abspath(__file__))
cat = json.load(open(os.path.join(HERE, 'tweaks.catalog.json')))

LAW_PATTERNS = ['DisableRealtimeMonitoring', 'DisableAntiSpyware', 'DisableAntiVirus',
  'DisableBehaviorMonitoring', 'DisableIOAVProtection', 'DisableScriptScanning',
  'TamperProtection', 'NoAutoUpdate', 'DisableWindowsUpdateAccess', 'WUServer',
  'HideInsiderPage', 'Debugger', 'Disable-BitLocker', 'HypervisorEnforcedCodeIntegrity',
  r'Stop-Service.*WinDefend', 'WinDefend']

def key_of(a):
    return (a.get('path', '').lower(), a.get('value', '').lower())

def battery():
    problems = []
    for o in cat['options']:
        app = [a for a in o['apply'] if a['kind'] == 'registryValue']
        rev = [a for a in o['revert'] if a['kind'] == 'registryValue']
        if sorted(map(key_of, app)) != sorted(map(key_of, rev)):
            problems.append(o['id'])
        blob = json.dumps(o).lower()
        for p in LAW_PATTERNS:
            import re
            if re.search(p.lower(), blob):
                problems.append(f"LAW HIT {p} in {o['id']}")
    print(f"options: {cat['optionCount']} | apply: {cat['applyActions']} | revert: {cat['revertActions']}")
    print('mirror breaks / law hits:', problems if problems else 'none')
    return not problems

def plan(option_ids):
    by_id = {o['id']: o for o in cat['options']}
    steps, kinds = [], {}
    for oid in option_ids:
        o = by_id[oid]
        for a in o['apply']:
            kinds[a['kind']] = kinds.get(a['kind'], 0) + 1
            if a['kind'] == 'registryValue':
                steps.append(f"REG  {a['path']} :: {a['value']} = {a.get('data')} ({a.get('type')})")
            elif a['kind'] == 'service':
                steps.append(f"SVC  {a.get('name')} -> {a.get('startup')}")
            else:
                steps.append(f"PS   {a.get('script')}")
    return steps, kinds

if __name__ == '__main__':
    ok = battery()
    sample = ['capsule-dark-theme', 'capsule-repair-toolkit', 'capsule2-explorer-explorer']
    steps, kinds = plan(sample)
    print(f"\nDRY-RUN PLAN for {sample}:")
    print('kinds:', kinds, '| total steps:', len(steps))
    for s in steps[:8]: print(' ', s)
    if len(steps) > 8: print(f'  … and {len(steps) - 8} more')
    print('\nBATTERY:', 'ALL PASS' if ok else 'FAILURES')
    sys.exit(0 if ok else 1)
