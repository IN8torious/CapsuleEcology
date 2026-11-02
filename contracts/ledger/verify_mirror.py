#!/usr/bin/env python3
"""Mirror verification of the C# ledger port (Ledger.cs).

No .NET toolchain lives in this VM, so the port's algorithm is
verified here with a line-for-line Python mirror of the C# code,
run against ground-truth vectors produced by si-kernel.js itself.
The C# compilation check happens on Windows; the ALGORITHM —
the part that must be byte-compatible — is proven here, including
a tamper case that must FAIL verification (a test that cannot
fail is a painting)."""
import json, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))

def fnv1a(s):
    h = 0x811c9dc5
    # UTF-16 code units, exactly like JS charCodeAt / C# char
    b = s.encode('utf-16-le', 'surrogatepass')
    for i in range(0, len(b), 2):
        h ^= b[i] | (b[i + 1] << 8)
        h = (h * 0x01000193) & 0xFFFFFFFF
    return format(h, '08x')

def cls_of(kind, msg):
    if kind == 'sentinel' or (kind == 'operator' and msg.startswith('DENIED')):
        return 'veto'
    if kind in ('tool', 'archivist', 'operator'):
        return 'ok'
    return ''

class Ledger:
    def __init__(self): self.entries = []
    def seal(self, actor, kind, msg):
        prev = self.entries[-1]['hash'] if self.entries else 'genesis0'
        e = {'seq': len(self.entries) + 1, 'agent': actor, 'kind': kind,
             'event': msg, 'cls': cls_of(kind, msg), 'prev': prev, 'hash': ''}
        e['hash'] = fnv1a(prev + '|' + actor + '|' + msg)
        self.entries.append(e)
        return e
    def verify(self):
        prev = 'genesis0'
        for e in self.entries:
            if e['prev'] != prev: return False
            if e['hash'] != fnv1a(prev + '|' + e['agent'] + '|' + e['event']): return False
            prev = e['hash']
        return True

def main():
    v = json.load(open(os.path.join(HERE, 'vectors.json')))
    led = Ledger()
    for step in v['sequence']:
        led.seal(step['agent'], step['kind'], step['event'])
    ok = True
    for got, want in zip(led.entries, v['expected']):
        match = all(got[k] == want[k] for k in want)
        ok &= match
        print(('PASS' if match else 'FAIL'), 'seq', want['seq'], 'hash', got['hash'],
              '' if match else f"(want {want['hash']})")
    print('chain verify():', led.verify(), '(want True)')
    ok &= led.verify() is True
    # tamper case: rewrite history, verification must fail
    led.entries[2]['event'] = 'Frost-X: reasoning+99'
    tampered = led.verify()
    print('tampered chain verify():', tampered, '(want False)')
    ok &= tampered is False
    print('LEDGER MIRROR:', 'ALL PASS' if ok else 'FAILURES PRESENT')
    sys.exit(0 if ok else 1)

if __name__ == '__main__':
    main()
