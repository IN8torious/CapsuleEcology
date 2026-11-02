using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CapsuleEcology.Services
{
    public enum ModelState { NotDownloaded, Partial, DownloadedUnverified, Verified, Adopted }

    /// <summary>
    /// Models (R4): organisms, not products. Download is user-initiated
    /// only, from the pinned upstream revision on Hugging Face — Capsule
    /// bundles nothing and fetches nothing silently. Verification uses
    /// the source's own semantics (proven against ground truth):
    /// LFS files verify by SHA-256; small files by git-blob SHA-1
    /// (sha1 of "blob {len}\0" + content). A model can be adopted only
    /// from the Verified state — anything less refuses by absence.
    /// Adopt / fork / mutate are lineage events and seal receipts;
    /// forks are local cards (creator: N8toriousAi for our own line)
    /// in %LocalAppData%\CapsuleEcology\lineage\.
    /// </summary>
    public sealed class ModelService
    {
        private readonly ReceiptLog _log;
        public ModelService(ReceiptLog log) { _log = log; }

        public static string ModelsRoot => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CapsuleEcology", "models");
        public static string LineageRoot => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CapsuleEcology", "lineage");
        public string ModelDir(ModelEntry m) => Path.Combine(ModelsRoot, m.Id);
        private string StatePath(ModelEntry m) => Path.Combine(ModelDir(m), "capsule-state.json");

        public (bool Present, bool SizeOk) FileState(ModelEntry m, ModelFileEntry f)
        {
            var p = Path.Combine(ModelDir(m), f.Path);
            if (!File.Exists(p)) return (false, false);
            return (true, new FileInfo(p).Length == f.Size);
        }

        public ModelState StateOf(ModelEntry m)
        {
            int present = 0;
            foreach (var f in m.Files) { var s = FileState(m, f); if (s.Present && s.SizeOk) present++; }
            if (present == 0) return ModelState.NotDownloaded;
            if (present < m.Files.Count) return ModelState.Partial;
            try
            {
                if (File.Exists(StatePath(m)))
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(StatePath(m)));
                    var r = doc.RootElement;
                    if (r.TryGetProperty("adopted", out var a) && a.GetBoolean()) return ModelState.Adopted;
                    if (r.TryGetProperty("verified", out var v) && v.GetBoolean()) return ModelState.Verified;
                }
            }
            catch { }
            return ModelState.DownloadedUnverified;
        }

        public async Task<(bool Ok, string Note)> DownloadAsync(ModelEntry m, Action<string>? progress = null)
        {
            Directory.CreateDirectory(ModelDir(m));
            long total = 0; foreach (var f in m.Files) total += f.Size;
            long done = 0; int fetched = 0, skipped = 0;
            using var http = new HttpClient();
            foreach (var f in m.Files)
            {
                var st = FileState(m, f);
                if (st.Present && st.SizeOk) { skipped++; done += f.Size; continue; }
                var url = $"https://huggingface.co/{m.Repository}/resolve/{m.Revision}/{f.Path}";
                var dest = Path.Combine(ModelDir(m), f.Path);
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                progress?.Invoke($"Downloading {f.Path} — {done / 1_000_000} / {total / 1_000_000} MB");
                try
                {
                    using var resp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                    resp.EnsureSuccessStatusCode();
                    await using var src = await resp.Content.ReadAsStreamAsync();
                    await using var dst = File.Create(dest);
                    var buf = new byte[1 << 20];
                    int n;
                    while ((n = await src.ReadAsync(buf)) > 0)
                    {
                        await dst.WriteAsync(buf.AsMemory(0, n));
                        done += n;
                        if ((done & 0x3FFFFFF) < n) progress?.Invoke($"Downloading {f.Path} — {done / 1_000_000} / {total / 1_000_000} MB");
                    }
                    fetched++;
                }
                catch (Exception ex)
                {
                    return (false, $"Download stopped at {f.Path}: {ex.Message}. Partial files stay on disk; run Download again to resume by size.");
                }
            }
            WriteState(m, verified: false, adopted: false);
            _log.Seal("models", "model-download", $"model download {m.Id}: {fetched} fetched, {skipped} already present, {total} bytes from {m.Repository} @ {m.Revision[..10]}");
            return (true, $"Download complete: {fetched} files fetched, {skipped} already present. Now Verify — adoption unlocks only from the verified state.");
        }

        public (bool Ok, List<string> Lines) Verify(ModelEntry m)
        {
            var lines = new List<string>();
            bool all = true;
            foreach (var f in m.Files)
            {
                var p = Path.Combine(ModelDir(m), f.Path);
                if (!File.Exists(p)) { lines.Add($"MISSING  {f.Path}"); all = false; continue; }
                if (new FileInfo(p).Length != f.Size) { lines.Add($"SIZE     {f.Path}"); all = false; continue; }
                var actual = f.Kind == "lfs" ? Sha256Hex(p) : GitBlobSha1(p);
                var expect = f.Kind == "lfs" ? f.Sha256 : f.BlobSha1;
                bool ok = string.Equals(actual, expect, StringComparison.OrdinalIgnoreCase);
                lines.Add($"{(ok ? "OK      " : "HASH ✗  ")} {f.Path}");
                if (!ok) all = false;
            }
            var st = StateOf(m);
            WriteState(m, verified: all, adopted: all && st == ModelState.Adopted);
            _log.Seal("models", all ? "model-verify" : "model-verify-failed",
                $"model verify {m.Id}: {(all ? "all " + m.Files.Count + " files match the pinned source" : "FAILED — see file list")}");
            return (all, lines);
        }

        public (bool Ok, string Note) Adopt(ModelEntry m)
        {
            var st = StateOf(m);
            if (st != ModelState.Verified && st != ModelState.Adopted)
                return (false, $"Adoption refused: this model is {st}, not Verified. Download, then Verify — adoption is absence until the hashes prove the arrival.");
            WriteState(m, verified: true, adopted: true);
            _log.Seal("models", "model-adopt", $"model adopted {m.Id} ({m.Name}) from {m.Repository} @ {m.Revision[..10]} — lineage root: upstream creator, adopted by N8toriousAi");
            return (true, $"{m.Name} adopted. The adoption is sealed — its card now carries this lineage: created upstream, adopted here, every later fork descends from this receipt.");
        }

        public (bool Ok, string Note) Fork(ModelEntry m, string forkName)
        {
            if (StateOf(m) != ModelState.Adopted)
                return (false, "Fork refused: only an adopted model can be forked — the family tree grows from adoptions.");
            Directory.CreateDirectory(LineageRoot);
            var cards = Data.LoadCards();
            var parent = cards.Find(c => c.Id == m.Id);
            int gen = (parent?.Generation ?? 0) + 1;
            var id = m.Id + "-fork-" + DateTime.Now.ToString("yyyyMMddHHmmss");
            var card = new JsonObject2
            {
                ["id"] = id, ["name"] = forkName, ["parent"] = m.Id,
                ["generation"] = gen, ["creator"] = "N8toriousAi",
                ["created"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                ["mutations"] = "[]",
            };
            File.WriteAllText(Path.Combine(LineageRoot, id + ".json"), card.ToString());
            _log.Seal("models", "model-fork", $"model fork {id} ({forkName}) — generation {gen}, parent {m.Id}, creator N8toriousAi");
            return (true, $"Fork created: {forkName} (generation {gen}). Its card lives in the local lineage store; the fork receipt names its parent forever.");
        }

        public (bool Ok, string Note) Mutate(string forkId, string mutation)
        {
            var path = Path.Combine(LineageRoot, forkId + ".json");
            if (!File.Exists(path)) return (false, "No such local fork.");
            var text = File.ReadAllText(path);
            text = text.Replace("\"mutations\": \"[]\"", $"\"mutations\": \"{mutation}\"");
            if (!text.Contains(mutation)) text = text.Replace("\"mutations\": \"", "\"mutations\": \"" + mutation + "; ");
            File.WriteAllText(path, text);
            _log.Seal("models", "model-mutate", $"model mutate {forkId}: {mutation}");
            return (true, $"Mutation recorded on {forkId} and sealed: {mutation}");
        }

        public List<string> ListForks()
        {
            var list = new List<string>();
            try
            {
                if (Directory.Exists(LineageRoot))
                    foreach (var f in Directory.GetFiles(LineageRoot, "*.json")) list.Add(Path.GetFileNameWithoutExtension(f));
            }
            catch { }
            return list;
        }

        private void WriteState(ModelEntry m, bool verified, bool adopted)
        {
            Directory.CreateDirectory(ModelDir(m));
            File.WriteAllText(StatePath(m),
                $"{{\"verified\": {(verified ? "true" : "false")}, \"adopted\": {(adopted ? "true" : "false")}, \"when\": \"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\"}}");
        }

        private static string Sha256Hex(string path)
        {
            using var sha = SHA256.Create();
            using var fs = File.OpenRead(path);
            return Convert.ToHexString(sha.ComputeHash(fs)).ToLowerInvariant();
        }

        private static string GitBlobSha1(string path)
        {
            var len = new FileInfo(path).Length;
            var header = Encoding.ASCII.GetBytes($"blob {len}\0");
            using var sha = SHA1.Create();
            sha.TransformBlock(header, 0, header.Length, null, 0);
            using var fs = File.OpenRead(path);
            var buf = new byte[1 << 20];
            int n;
            while ((n = fs.Read(buf, 0, buf.Length)) > 0) sha.TransformBlock(buf, 0, n, null, 0);
            sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return Convert.ToHexString(sha.Hash!).ToLowerInvariant();
        }

        // Minimal ordered JSON writer for fork cards (keeps this service dependency-free).
        private sealed class JsonObject2 : Dictionary<string, object>
        {
            public override string ToString()
            {
                var sb = new StringBuilder("{");
                bool first = true;
                foreach (var kv in this)
                {
                    if (!first) sb.Append(", ");
                    first = false;
                    sb.Append($"\"{kv.Key}\": ");
                    sb.Append(kv.Value is int i ? i.ToString() : $"\"{kv.Value}\"");
                }
                return sb.Append('}').ToString();
            }
        }
    }
}
