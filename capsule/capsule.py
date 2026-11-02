#!/usr/bin/env python3
"""Reference implementation of the .capsule format v1:
pack + offline verify. Standard library only."""
import hashlib, json, os, sys, zipfile

def fnv1a(s):
    h = 0x811c9dc5
    b = s.encode('utf-16-le', 'surrogatepass')
    for i in range(0, len(b), 2):
        h ^= b[i] | (b[i + 1] << 8)
        h = (h * 0x01000193) & 0xFFFFFFFF
    return format(h, '08x')

class Chain:
    def __init__(self): self.entries = []
    def seal(self, agent, kind, event):
        prev = self.entries[-1]['hash'] if self.entries else 'genesis0'
        e = {'seq': len(self.entries) + 1, 'agent': agent, 'kind': kind,
             'event': event, 'cls': '', 'prev': prev, 'hash': ''}
        e['hash'] = fnv1a(prev + '|' + agent + '|' + event)
        self.entries.append(e)
        return e

def verify_chain(entries):
    prev = 'genesis0'
    for e in entries:
        if e['prev'] != prev: return False
        if e['hash'] != fnv1a(prev + '|' + e['agent'] + '|' + e['event']): return False
        prev = e['hash']
    return True

def pack(card_path, payload_files, out_path, seal_events):
    card = json.load(open(card_path))
    chain = Chain()
    for (agent, kind, event) in seal_events:
        chain.seal(agent, kind, event)
    card['chain'] = {'head': chain.entries[-1]['hash'] if chain.entries else None,
                     'length': len(chain.entries)}
    manifest = {'format': 'capsule/1', 'id': card['id'], 'kind': card['kind'],
                'created': '2026-11-02', 'payload': []}
    blobs = {}
    for name, path in payload_files:
        data = open(path, 'rb').read()
        blobs['payload/' + name] = data
        manifest['payload'].append({'path': 'payload/' + name, 'size': len(data),
                                    'sha256': hashlib.sha256(data).hexdigest()})
    with zipfile.ZipFile(out_path, 'w', zipfile.ZIP_DEFLATED) as z:
        z.writestr('manifest.json', json.dumps(manifest, indent=1))
        z.writestr('card.json', json.dumps(card, indent=1))
        z.writestr('chain.jsonl', '\n'.join(json.dumps(e) for e in chain.entries))
        for name, data in blobs.items():
            z.writestr(name, data)
    print(f'packed {out_path}: chain head {card["chain"]["head"]}, {len(blobs)} payload files')

def verify(path):
    ok = True
    with zipfile.ZipFile(path) as z:
        names = set(z.namelist())
        for req in ('manifest.json', 'card.json', 'chain.jsonl'):
            if req not in names:
                print('FAIL missing member:', req); return False
        manifest = json.loads(z.read('manifest.json'))
        card = json.loads(z.read('card.json'))
        entries = [json.loads(l) for l in z.read('chain.jsonl').decode().strip().split('\n') if l.strip()]
        for p in manifest['payload']:
            data = z.read(p['path'])
            good = len(data) == p['size'] and hashlib.sha256(data).hexdigest() == p['sha256']
            ok &= good
            print(('PASS' if good else 'FAIL'), 'payload', p['path'], p['size'], 'bytes')
    chain_ok = verify_chain(entries)
    print(('PASS' if chain_ok else 'FAIL'), 'chain integrity:', len(entries), 'receipts')
    ok &= chain_ok
    head = entries[-1]['hash'] if entries else None
    bind = card['chain']['head'] == head and card['chain']['length'] == len(entries)
    print(('PASS' if bind else 'FAIL'), 'card-chain binding: head', head)
    ok &= bind
    for parent in card['lineage']['parents']:
        print('INFO ancestry:', parent['relation'], '->', parent['id'],
              '(external origin: recorded, not chain-verifiable)' if not parent.get('chainRef') else '')
    print('CAPSULE:', 'VERIFIED' if ok else 'REFUSED')
    return ok

if __name__ == '__main__':
    if sys.argv[1] == 'verify':
        sys.exit(0 if verify(sys.argv[2]) else 1)
    print(__doc__)
