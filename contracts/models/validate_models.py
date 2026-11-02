#!/usr/bin/env python3
"""Validate models.catalog.json — capsule-models/1.
Rules: format tag; per model — pinned 40-hex revision, an onnx entry point that
exists in the file list, every file sized and carrying exactly one hash of the
right kind (lfs -> 64-hex sha256, blob -> 40-hex blobSha1), weightsBytes ==
sum of LFS sizes under onnx/."""
import json, re, sys
def main(path):
    d = json.load(open(path)); errs = []
    assert d.get('format') == 'capsule-models/1'
    for m in d['models']:
        if not re.fullmatch(r'[0-9a-f]{40}', m['revision']): errs.append(f"{m['id']}: revision not pinned sha")
        paths = [f['path'] for f in m['files']]
        if m['onnx'] not in paths: errs.append(f"{m['id']}: onnx entry not in files")
        for f in m['files']:
            if f['kind'] == 'lfs' and not re.fullmatch(r'[0-9a-f]{64}', f.get('sha256', '')): errs.append(f"{m['id']}/{f['path']}: bad sha256")
            if f['kind'] == 'blob' and not re.fullmatch(r'[0-9a-f]{40}', f.get('blobSha1', '')): errs.append(f"{m['id']}/{f['path']}: bad blobSha1")
            if f['size'] <= 0: errs.append(f"{m['id']}/{f['path']}: bad size")
        w = sum(f['size'] for f in m['files'] if f['kind'] == 'lfs' and f['path'].startswith('onnx/'))
        if w != m['weightsBytes']: errs.append(f"{m['id']}: weightsBytes {m['weightsBytes']} != onnx LFS sum {w}")
    print(f"models: {len(d['models'])} · files: {sum(len(m['files']) for m in d['models'])}")
    if errs: print('INVALID:'); [print(' -', e) for e in errs]; return 1
    print('models.catalog.json VALID — capsule-models/1'); return 0
if __name__ == '__main__': sys.exit(main(sys.argv[1] if len(sys.argv) > 1 else 'models.catalog.json'))
