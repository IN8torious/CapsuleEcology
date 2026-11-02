using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace CapsuleEcology.Services
{
    public sealed class ModelCard
    {
        public string Id = "", Name = "", KernelType = "", Creator = "", License = "", Repository = "", Dtype = "";
        public string RevisionShort = "";
        public long WeightsBytes;
        public int Generation, ForkCount, FileCount;
        public List<string> Strengths = new(), Weaknesses = new();
        public string WeightsText => $"{WeightsBytes / 1_000_000_000.0:0.00} GB · {Dtype}";
    }

    public sealed class TweakOption
    {
        public string Id = "", Text = "";
        public int Actions;
        public bool DefaultChecked;
        public string DefaultText => DefaultChecked ? "Default: on" : "Default: off";
        public string ActionsText => $"{Actions} actions";
    }

    public sealed class CatalogAction
    {
        public string Kind = "", Path = "", Value = "", Data = "", Type = "", Operation = "", Name = "", Startup = "", Script = "";
    }

    public sealed class FullOption
    {
        public string Id = "", Text = "";
        public bool DefaultChecked;
        public List<CatalogAction> Apply = new(), Revert = new();
    }

    public sealed class PackApp
    {
        public string Name = "", Winget = "", Choco = "", Note = "";
        public bool Optional;
        public string SourceText =>
            (Winget.Length > 0 ? $"winget: {Winget}" : "") +
            (Winget.Length > 0 && Choco.Length > 0 ? " · " : "") +
            (Choco.Length > 0 ? $"chocolatey: {Choco}" : "");
        public string OptionalText => Optional ? "Optional" : "Core";
    }

    public sealed class Pack
    {
        public string Id = "", Name = "", Role = "";
        public List<PackApp> Apps = new();
        public List<string> Docs = new();
        public string AppsText => $"{Apps.Count} apps";
    }

    public sealed class BibleChapter
    {
        public int Number;
        public List<string> Verses = new();
    }

    public sealed class BibleBook
    {
        public string Name = "";
        public List<BibleChapter> Chapters = new();
    }

    public sealed class BibleHit
    {
        public int BookIndex, ChapterIndex, VerseNumber;
        public string Ref = "", Text = "";
    }

    public static class BibleData
    {
        private static List<BibleBook>? _books;

        public static List<BibleBook> Load()
        {
            if (_books is not null) return _books;
            var books = new List<BibleBook>();
            foreach (var file in new[] { "toolbox-bible-old-testament.json", "toolbox-bible-new-testament.json" })
            {
                var parts = new List<string> { AppContext.BaseDirectory, "Assets", "bible", file };
                using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(parts.ToArray())));
                foreach (var b in doc.RootElement.GetProperty("books").EnumerateArray())
                {
                    var book = new BibleBook { Name = b.GetProperty("book").GetString() ?? "" };
                    foreach (var c in b.GetProperty("chapters").EnumerateArray())
                    {
                        var chEl = c.GetProperty("chapter");
                        var chapter = new BibleChapter
                        {
                            Number = chEl.ValueKind == JsonValueKind.Number ? chEl.GetInt32() : int.Parse(chEl.GetString() ?? "0"),
                        };
                        foreach (var v in c.GetProperty("verses").EnumerateArray())
                            chapter.Verses.Add(v.GetProperty("text").GetString() ?? "");
                        book.Chapters.Add(chapter);
                    }
                    books.Add(book);
                }
            }
            _books = books;
            return books;
        }

        public static (int Books, int Chapters, int Verses) Totals()
        {
            var books = Load();
            int ch = 0, vs = 0;
            foreach (var b in books) { ch += b.Chapters.Count; foreach (var c in b.Chapters) vs += c.Verses.Count; }
            return (books.Count, ch, vs);
        }

        public static List<BibleHit> Search(string query, int cap = 200)
        {
            var hits = new List<BibleHit>();
            if (string.IsNullOrWhiteSpace(query)) return hits;
            var books = Load();
            for (int bi = 0; bi < books.Count && hits.Count < cap; bi++)
            {
                var b = books[bi];
                for (int ci = 0; ci < b.Chapters.Count && hits.Count < cap; ci++)
                {
                    var c = b.Chapters[ci];
                    for (int vi = 0; vi < c.Verses.Count && hits.Count < cap; vi++)
                    {
                        if (c.Verses[vi].Contains(query, StringComparison.OrdinalIgnoreCase))
                            hits.Add(new BibleHit
                            {
                                BookIndex = bi, ChapterIndex = ci, VerseNumber = vi + 1,
                                Ref = $"{b.Name} {c.Number}:{vi + 1}", Text = c.Verses[vi],
                            });
                    }
                }
            }
            return hits;
        }
    }

    public sealed class ModelFileEntry
    {
        public string Path = "", Kind = "", Sha256 = "", BlobSha1 = "";
        public long Size;
        public string HashText => Kind == "lfs" ? "SHA-256 " + Sha256[..12] + "…" : "blob " + BlobSha1[..12] + "…";
        public string SizeText => Size >= 1_000_000_000 ? $"{Size / 1_000_000_000.0:0.00} GB" : $"{Size / 1_000_000.0:0.0} MB";
    }

    public sealed class ModelEntry
    {
        public string Id = "", Name = "", Repository = "", Revision = "", License = "", Dtype = "", Onnx = "";
        public long WeightsBytes;
        public List<ModelFileEntry> Files = new();
        public string SourceText => $"{Repository} @ {Revision[..10]}…";
        public string WeightsText => $"{WeightsBytes / 1_000_000_000.0:0.00} GB · {Dtype} · {License}";
    }

    public static class Data
    {
        private static string AssetPath(params string[] parts)
        {
            var all = new List<string> { AppContext.BaseDirectory, "Assets" };
            all.AddRange(parts);
            return Path.Combine(all.ToArray());
        }

        public static List<ModelCard> LoadCards()
        {
            var cards = new List<ModelCard>();
            foreach (var file in Directory.GetFiles(AssetPath("cards"), "*.json"))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(file));
                var r = doc.RootElement;
                var src = r.GetProperty("source");
                var card = new ModelCard
                {
                    Id = r.GetProperty("id").GetString() ?? "",
                    Name = r.GetProperty("name").GetString() ?? "",
                    KernelType = r.GetProperty("kernelType").GetString() ?? "",
                    Creator = r.GetProperty("creator").GetString() ?? "",
                    Generation = r.GetProperty("generation").GetInt32(),
                    ForkCount = r.GetProperty("forkCount").GetInt32(),
                    License = src.GetProperty("license").GetString() ?? "",
                    Repository = src.GetProperty("repository").GetString() ?? "",
                    Dtype = src.GetProperty("dtype").GetString() ?? "",
                    WeightsBytes = src.GetProperty("weightsBytes").GetInt64(),
                };
                var rev = src.GetProperty("revision").GetString() ?? "";
                card.RevisionShort = rev.Length > 10 ? rev[..10] + "…" : rev;
                card.FileCount = src.GetProperty("files").GetArrayLength();
                foreach (var t in r.GetProperty("traits").GetProperty("strengths").EnumerateArray())
                    card.Strengths.Add(t.GetString() ?? "");
                foreach (var t in r.GetProperty("traits").GetProperty("weaknesses").EnumerateArray())
                    card.Weaknesses.Add(t.GetString() ?? "");
                cards.Add(card);
            }
            return cards;
        }

        public static List<Pack> LoadPacks()
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(AssetPath("packs", "packs.catalog.json")));
            var list = new List<Pack>();
            foreach (var p in doc.RootElement.GetProperty("packs").EnumerateArray())
            {
                var pack = new Pack
                {
                    Id = p.GetProperty("id").GetString() ?? "",
                    Name = p.GetProperty("name").GetString() ?? "",
                    Role = p.GetProperty("role").GetString() ?? "",
                };
                foreach (var a in p.GetProperty("apps").EnumerateArray())
                {
                    pack.Apps.Add(new PackApp
                    {
                        Name = a.GetProperty("name").GetString() ?? "",
                        Winget = a.TryGetProperty("winget", out var w) ? w.GetString() ?? "" : "",
                        Choco = a.TryGetProperty("choco", out var c) ? c.GetString() ?? "" : "",
                        Note = a.TryGetProperty("note", out var n) ? n.GetString() ?? "" : "",
                        Optional = a.TryGetProperty("optional", out var o) && o.GetBoolean(),
                    });
                }
                if (p.TryGetProperty("docs", out var docs))
                    foreach (var d in docs.EnumerateArray()) pack.Docs.Add(d.GetString() ?? "");
                list.Add(pack);
            }
            return list;
        }

        public static List<ModelEntry> LoadModelCatalog()
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(AssetPath("models", "models.catalog.json")));
            var list = new List<ModelEntry>();
            foreach (var m in doc.RootElement.GetProperty("models").EnumerateArray())
            {
                var e = new ModelEntry
                {
                    Id = m.GetProperty("id").GetString() ?? "",
                    Name = m.GetProperty("name").GetString() ?? "",
                    Repository = m.GetProperty("repository").GetString() ?? "",
                    Revision = m.GetProperty("revision").GetString() ?? "",
                    License = m.GetProperty("license").GetString() ?? "",
                    Dtype = m.GetProperty("dtype").GetString() ?? "",
                    Onnx = m.GetProperty("onnx").GetString() ?? "",
                    WeightsBytes = m.GetProperty("weightsBytes").GetInt64(),
                };
                foreach (var f in m.GetProperty("files").EnumerateArray())
                {
                    e.Files.Add(new ModelFileEntry
                    {
                        Path = f.GetProperty("path").GetString() ?? "",
                        Kind = f.GetProperty("kind").GetString() ?? "",
                        Size = f.GetProperty("size").GetInt64(),
                        Sha256 = f.TryGetProperty("sha256", out var s) ? s.GetString() ?? "" : "",
                        BlobSha1 = f.TryGetProperty("blobSha1", out var b) ? b.GetString() ?? "" : "",
                    });
                }
                list.Add(e);
            }
            return list;
        }

        public static List<FullOption> LoadCatalogFull()
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(AssetPath("catalog", "tweaks.catalog.json")));
            var list = new List<FullOption>();
            foreach (var o in doc.RootElement.GetProperty("options").EnumerateArray())
            {
                var fo = new FullOption
                {
                    Id = o.GetProperty("id").GetString() ?? "",
                    Text = o.GetProperty("text").GetString() ?? "",
                    DefaultChecked = o.GetProperty("defaultChecked").GetBoolean(),
                };
                foreach (var a in o.GetProperty("apply").EnumerateArray()) fo.Apply.Add(ParseAction(a));
                foreach (var a in o.GetProperty("revert").EnumerateArray()) fo.Revert.Add(ParseAction(a));
                list.Add(fo);
            }
            return list;
        }

        private static CatalogAction ParseAction(JsonElement a)
        {
            string G(string k) => a.TryGetProperty(k, out var v) ? v.GetString() ?? "" : "";
            return new CatalogAction
            {
                Kind = G("kind"), Path = G("path"), Value = G("value"), Data = G("data"),
                Type = G("type"), Operation = G("operation"), Name = G("name"),
                Startup = G("startup"), Script = G("script"),
            };
        }

        public static (int Options, int Apply, int Revert, List<TweakOption> List) LoadCatalog()
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(AssetPath("catalog", "tweaks.catalog.json")));
            var r = doc.RootElement;
            var list = new List<TweakOption>();
            foreach (var o in r.GetProperty("options").EnumerateArray())
            {
                list.Add(new TweakOption
                {
                    Id = o.GetProperty("id").GetString() ?? "",
                    Text = o.GetProperty("text").GetString() ?? "",
                    DefaultChecked = o.GetProperty("defaultChecked").GetBoolean(),
                    Actions = o.GetProperty("apply").GetArrayLength(),
                });
            }
            return (r.GetProperty("optionCount").GetInt32(),
                    r.GetProperty("applyActions").GetInt32(),
                    r.GetProperty("revertActions").GetInt32(), list);
        }
    }
}
