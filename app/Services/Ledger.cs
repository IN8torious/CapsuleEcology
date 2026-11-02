// Capsule Ecology — SI Ledger, C# port.
//
// Byte-compatible with si-kernel.js (the web kernel), the RingFlow
// receipts, and the Nimbusis constitution service. Canonical entry:
//   { seq, agent, kind, event, cls, prev, hash }
//   hash = Fnv1a(prev + "|" + agent + "|" + event)
//   genesis prev = "genesis0"
//
// FNV-1a here runs over UTF-16 code units — exactly what the JS
// original does with charCodeAt, and exactly what a C# string is.
// Verified against ground-truth vectors produced by si-kernel.js
// itself (vectors.json, same folder; mirror test: verify_mirror.py).

using System;
using System.Collections.Generic;

namespace Capsule.Contracts
{
    public sealed class LedgerEntry
    {
        public int Seq { get; set; }
        public string Agent { get; set; } = "";
        public string Kind { get; set; } = "";
        public string Event { get; set; } = "";
        public string Cls { get; set; } = "";
        public string Prev { get; set; } = "";
        public string Hash { get; set; } = "";
    }

    public static class Fnv
    {
        public static string Fnv1a(string s)
        {
            uint h = 0x811c9dc5;
            foreach (char c in s)
            {
                h ^= c;                    // char is a UTF-16 code unit
                h = unchecked(h * 0x01000193);
            }
            return h.ToString("x8");
        }
    }

    public sealed class Ledger
    {
        public const string Genesis = "genesis0";

        private readonly List<LedgerEntry> _entries = new();
        public IReadOnlyList<LedgerEntry> Entries => _entries;
        public int Length => _entries.Count;

        public void Clear() => _entries.Clear();

        public LedgerEntry Seal(string actor, string kind, string msg)
        {
            string prev = _entries.Count > 0 ? _entries[_entries.Count - 1].Hash : Genesis;
            string cls =
                (kind == "sentinel" || (kind == "operator" && msg.StartsWith("DENIED"))) ? "veto"
                : (kind == "tool" || kind == "archivist" || kind == "operator") ? "ok"
                : "";
            var e = new LedgerEntry
            {
                Seq = _entries.Count + 1,
                Agent = actor,
                Kind = kind,
                Event = msg,
                Cls = cls,
                Prev = prev,
                Hash = ""
            };
            e.Hash = Fnv.Fnv1a(prev + "|" + actor + "|" + msg);
            _entries.Add(e);
            return e;
        }

        public bool Verify()
        {
            string prev = Genesis;
            foreach (var e in _entries)
            {
                if (e.Prev != prev) return false;
                if (e.Hash != Fnv.Fnv1a(prev + "|" + e.Agent + "|" + e.Event)) return false;
                prev = e.Hash;
            }
            return true;
        }
    }
}
