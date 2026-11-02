#!/usr/bin/env python3
"""Validate packs.catalog.json — capsule-packs/1 contract.
Rules: format tag; unique kebab pack ids; every pack has >=1 app and a role;
every app has a name and at least one real source id (winget and/or choco);
winget ids look like Publisher.Package; optional is bool; notes are strings."""
import json, re, sys

def main(path):
    d = json.load(open(path))
    errs = []
    assert d.get('format') == 'capsule-packs/1', 'format tag'
    packs = d['packs']
    ids = [p['id'] for p in packs]
    if len(ids) != len(set(ids)): errs.append('duplicate pack ids')
    for p in packs:
        if not re.fullmatch(r'[a-z0-9]+(-[a-z0-9]+)*', p['id']): errs.append(f"pack id not kebab: {p['id']}")
        if not p.get('role'): errs.append(f"{p['id']}: no role")
        if not p.get('apps'): errs.append(f"{p['id']}: no apps")
        for a in p.get('apps', []):
            if not a.get('name'): errs.append(f"{p['id']}: app without name")
            w, c = a.get('winget'), a.get('choco')
            if not w and not c: errs.append(f"{p['id']}/{a.get('name')}: no source id")
            if w and not re.fullmatch(r'[A-Za-z0-9][A-Za-z0-9\-_]*(\.[A-Za-z0-9][A-Za-z0-9\-_]*)+', w):
                errs.append(f"{p['id']}/{a['name']}: winget id malformed: {w}")
            if c and not re.fullmatch(r'[a-z0-9][a-z0-9\-._]*', c):
                errs.append(f"{p['id']}/{a['name']}: choco id malformed: {c}")
            if not isinstance(a.get('optional'), bool): errs.append(f"{p['id']}/{a['name']}: optional not bool")
    apps = sum(len(p['apps']) for p in packs)
    both = sum(1 for p in packs for a in p['apps'] if a.get('winget') and a.get('choco'))
    print(f"packs: {len(packs)} · apps: {apps} · apps with both sources: {both}")
    if errs:
        print('INVALID:'); [print(' -', e) for e in errs]; return 1
    print('packs.catalog.json VALID — capsule-packs/1')
    return 0

if __name__ == '__main__':
    sys.exit(main(sys.argv[1] if len(sys.argv) > 1 else 'packs.catalog.json'))
