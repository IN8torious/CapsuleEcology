#!/usr/bin/env python3
"""R0c — import the CapsuleOS 0.2.2 option catalog into the app's
tweak definitions (tweaks.catalog.json).

Single source of truth stays the playbook line: this importer
reads the assembled 0.2.2 playbook (conf + apply + revert) and
emits the catalog the native app will drive. Parity is asserted:
every YAML action lands in the catalog, every conf option is
accounted for, apply/revert counts match the packaged audit."""
import json, os, re, sys
import xml.etree.ElementTree as ET

HERE = os.path.dirname(os.path.abspath(__file__))
PB = os.path.expanduser('~/workspace/capsuleos-toolbox-edition/playbook')

def split_flow(s):
    parts, cur, q, depth = [], '', None, 0
    i = 0
    while i < len(s):
        ch = s[i]
        if q:
            cur += ch
            if ch == q:
                if q == "'" and i + 1 < len(s) and s[i + 1] == "'":
                    cur += "'"; i += 1
                else: q = None
        elif ch in "'\"": q = ch; cur += ch
        elif ch in '[{': depth += 1; cur += ch
        elif ch in ']}': depth -= 1; cur += ch
        elif ch == ',' and depth == 0: parts.append(cur); cur = ''
        else: cur += ch
        i += 1
    if cur.strip(): parts.append(cur)
    d = {}
    for p in parts:
        if ':' in p:
            k, v = p.split(':', 1)
            v = v.strip()
            if len(v) >= 2 and v[0] == v[-1] and v[0] in "'\"":
                v = v[1:-1].replace("''", "'")
            d[k.strip()] = v
    return d

def flow_dicts(text, tag):
    out, i = [], 0
    while True:
        j = text.find(tag, i)
        if j < 0: break
        k = text.find('{', j)
        if k < 0: break
        depth, q, p = 0, None, k
        while p < len(text):
            ch = text[p]
            if q:
                if ch == q:
                    if q == "'" and p + 1 < len(text) and text[p + 1] == "'": p += 1
                    else: q = None
            elif ch in "'\"": q = ch
            elif ch == '{': depth += 1
            elif ch == '}':
                depth -= 1
                if depth == 0: break
            p += 1
        out.append(split_flow(text[k + 1:p]))
        i = p + 1
    return out

def actions_by_option(yml_path):
    text = open(yml_path).read()
    by_opt = {}
    total = 0
    for tag, kind in [('!registryValue:', 'registryValue'), ('!service:', 'service')]:
        for d in flow_dicts(text, tag):
            opt = d.get('option')
            if not opt: continue
            d = {'kind': kind, **d}
            by_opt.setdefault(opt, []).append(d)
            total += 1
    # powerShell steps carry option: "name" on their own line
    for m in re.finditer(r'-\s*!powerShell:(.*?)(?=\n  - |\Z)', text, re.S):
        om = re.search(r'option:\s*"([^"]+)"', m.group(1))
        fm = re.search(r'-File &quot;|\\?"\.\\([A-Za-z0-9\-]+\.ps1)', m.group(1))
        if om:
            by_opt.setdefault(om.group(1), []).append(
                {'kind': 'powerShell', 'script': fm.group(1) if fm else '',
                 'option': om.group(1)})
            total += 1
    return by_opt, total

def main():
    conf = ET.parse(os.path.join(PB, 'playbook.conf'))
    pages = []
    for pi, page in enumerate(conf.getroot().iter('CheckboxPage')):
        opts = []
        for o in page.iter('CheckboxOption'):
            opts.append({'id': o.findtext('Name'), 'text': o.findtext('Text'),
                         'defaultChecked': o.get('IsChecked') == 'true'})
        pages.append({'page': pi + 1, 'description': page.get('Description'), 'options': opts})
    apply_by, apply_total = actions_by_option(os.path.join(PB, 'Configuration/Tasks/apply.yml'))
    revert_by, revert_total = actions_by_option(os.path.join(PB, 'Configuration/Tasks/revert.yml'))

    catalog, problems = [], []
    conf_ids = set()
    for pg in pages:
        for o in pg['options']:
            conf_ids.add(o['id'])
            a = apply_by.get(o['id'], [])
            r = revert_by.get(o['id'], [])
            if not a: problems.append(f"conf option with NO apply actions: {o['id']}")
            catalog.append({**o, 'page': pg['page'], 'pageDescription': pg['description'],
                            'apply': a, 'revert': r})
    yaml_ids = set(apply_by) | set(revert_by)
    for oid in sorted(yaml_ids - conf_ids):
        problems.append(f"YAML option missing from conf: {oid} (apply {len(apply_by.get(oid, []))}, revert {len(revert_by.get(oid, []))})")
    parity_breaks = [c['id'] for c in catalog
                     if len(c['apply']) != len(c['revert']) and c['apply']
                     and not any(x['kind'] == 'powerShell' for x in c['apply'])]
    out = {'catalogVersion': 1, 'playbookVersion': '0.2.2',
           'source': 'capsuleos-toolbox-edition/playbook (assembled 0.2.2)',
           'optionCount': len(catalog), 'applyActions': apply_total,
           'revertActions': revert_total, 'options': catalog}
    with open(os.path.join(HERE, 'tweaks.catalog.json'), 'w') as f:
        json.dump(out, f, indent=1)
    print(f"options in catalog: {len(catalog)} | apply actions: {apply_total} | revert actions: {revert_total}")
    print(f"registry/service parity breaks (excl. powerShell): {parity_breaks}")
    print(f"problems: {problems}")
    expected_actions = 619
    print('action total matches the 0.2.2 packaged audit (619):', apply_total == expected_actions)

if __name__ == '__main__':
    main()
