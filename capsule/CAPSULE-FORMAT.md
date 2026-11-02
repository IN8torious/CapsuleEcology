# The .capsule Format — v1 (draft, R8 pulled forward)

A `.capsule` is how ecosystem artifacts travel: a sealed scene,
a model, an agent — passed hand to hand, marketplace to
marketplace — **with its ancestry inside**, verifiable offline
by anyone, no server, no account, no trust-me-bro.

## Container

A ZIP archive with exactly these members:

| Member | Contents |
|---|---|
| `manifest.json` | `{format: "capsule/1", id, kind, created, payload: [{path, size, sha256}]}` |
| `card.json` | The artifact's Capsule Lineage card (schema `capsule-lineage/1`) |
| `chain.jsonl` | The artifact's own receipt chain, one SI-ledger entry per line |
| `payload/…` | The substance: scene files, mods, weights references, media |

## Verification (offline, in order)

1. **Payload integrity** — every payload file's size and
   SHA-256 match the manifest.
2. **Chain integrity** — the chain recomputes under the SI
   recipe: `hash = fnv1a(prev | agent | event)`, genesis prev
   `genesis0`, each entry's `prev` equal to the prior hash.
3. **Card–chain binding** — `card.chain.head` equals the
   chain's final hash and `card.chain.length` its length.
   A card that claims a different history than its chain
   carries is refused.
4. **Ancestry walk** — each parent on the card is reported:
   resolvable (its own capsule present) or external (upstream
   origin recorded; the record is the claim, labeled as such).

Failure at any step refuses the capsule **by absence**: it does
not open with a warning; it does not open.

## Lineage law

- `creator` names the original maker and is never rewritten.
- `foundation` records the root mark ("Created from N8
  Foundation" / "Adopted into N8 Foundation").
- Mutations are chain events; the card's mutation list cites
  their sequence numbers. Edit the card without the chain and
  step 3 catches it. Edit the chain and step 2 catches it.
  Erase the family tree and there is nothing left to verify —
  which is the point (invariant 4: state cannot rewrite
  receipt).

## Reference implementation

`capsule.py` (this folder): `pack` and `verify`, standard
library only. The native app implements the same four steps.
