using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ML.OnnxRuntimeGenAI;

namespace CapsuleEcology.Services
{
    /// <summary>
    /// Local inference (R4, final item): a verified model talks back.
    /// The gate is the law from the Models page — an unverified model
    /// NEVER runs; only Verified or Adopted state may load a session.
    /// Runtime: OnnxRuntime GenAI over the model's own package
    /// (genai_config.json + tokenizer.json ride with the verified
    /// files, so tokenization and KV-cache handling are the source's
    /// own, not a re-implementation). On Windows the DirectML provider
    /// is requested first; any provider failure falls back to CPU —
    /// the fallback is reported, never silent. Prompt shaping is the
    /// Phi-3 chat template, applied to the raw user text here and
    /// nowhere else. Every completed run seals a receipt (agent
    /// 'models'): model id, prompt length, tokens generated — the
    /// conversation text itself is NOT receipted.
    ///
    /// Two package facts, proven by the 2026-11-02 CPU smoke test
    /// (real pinned Phi-3.5 files, hashes verified, text generated):
    /// (1) the web repos' genai_config.json carries a "web" provider
    /// entry the native runtime rejects outright — providers must be
    /// cleared before load; (2) GenAI loads the graph as model.onnx
    /// beside the config, while the graph's external data keeps its
    /// original name (model_q4f16.onnx_data). So a run never loads
    /// the verified folder directly: EnsureRuntimeView stages a
    /// runtime/ view — a sanitized config copy plus hard links (copy
    /// fallback) — and the verified bytes are never modified.
    /// </summary>
    public sealed class InferenceService
    {
        private readonly ModelService _models;
        private readonly ReceiptLog _log;

        public InferenceService(ModelService models, ReceiptLog log)
        {
            _models = models;
            _log = log;
        }

        /// <summary>The gate: only a hash-verified (or adopted) model may run.</summary>
        public bool CanRun(ModelEntry m)
        {
            var s = _models.StateOf(m);
            if (s != ModelState.Verified && s != ModelState.Adopted) return false;
            var dir = _models.ModelDir(m);
            return File.Exists(Path.Combine(dir, "genai_config.json"))
                && File.Exists(Path.Combine(dir, "tokenizer.json"))
                && File.Exists(Path.Combine(dir, m.Onnx))
                && File.Exists(Path.Combine(dir, m.Onnx + "_data"));
        }

        public string RuntimeDir(ModelEntry m) => Path.Combine(_models.ModelDir(m), "runtime");

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

        private static void LinkOrCopy(string dest, string src)
        {
            if (File.Exists(dest)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            if (OperatingSystem.IsWindows() && CreateHardLink(dest, src, IntPtr.Zero)) return;
            File.Copy(src, dest);
        }

        /// <summary>
        /// Stage the runtime view of a verified package (see class
        /// notes). Idempotent; the verified folder is only ever read.
        /// </summary>
        public void EnsureRuntimeView(ModelEntry m)
        {
            var src = _models.ModelDir(m);
            var rt = RuntimeDir(m);
            var cfgPath = Path.Combine(rt, "genai_config.json");
            var graphDst = Path.Combine(rt, "model.onnx");
            var dataName = Path.GetFileName(m.Onnx) + "_data";
            var dataDst = Path.Combine(rt, dataName);
            if (File.Exists(cfgPath) && File.Exists(graphDst) && File.Exists(dataDst)) return;

            Directory.CreateDirectory(rt);
            // Sanitized config: our copy — providers dropped (the web
            // entry breaks native load; LoadModel sets its own), graph
            // renamed to the name GenAI loads.
            var cfg = JsonNode.Parse(File.ReadAllText(Path.Combine(src, "genai_config.json")))!;
            var decoder = cfg["model"]!["decoder"]!;
            decoder["filename"] = "model.onnx";
            decoder["session_options"]?.AsObject().Remove("provider_options");
            File.WriteAllText(cfgPath, cfg.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

            LinkOrCopy(graphDst, Path.Combine(src, m.Onnx));
            LinkOrCopy(dataDst, Path.Combine(src, m.Onnx + "_data"));
            foreach (var small in new[] { "tokenizer.json", "tokenizer_config.json",
                                          "special_tokens_map.json", "added_tokens.json",
                                          "config.json", "generation_config.json" })
            {
                var s = Path.Combine(src, small);
                if (File.Exists(s)) LinkOrCopy(Path.Combine(rt, small), s);
            }
        }

        /// <summary>Why a model cannot run, in plain words ("" when it can).</summary>
        public string GateReason(ModelEntry m)
        {
            var s = _models.StateOf(m);
            if (s != ModelState.Verified && s != ModelState.Adopted)
                return "This model is " + s + ". Only a verified model may run — download it, then Verify.";
            if (!CanRun(m)) return "The verified package is missing its runtime files (genai_config.json / tokenizer.json / model). Re-verify.";
            return "";
        }

        /// <summary>Phi-3 chat template — the one place prompts are shaped.</summary>
        public static string ApplyChatTemplate(string userPrompt)
            => "<|user|>\n" + userPrompt + "<|end|>\n<|assistant|>\n";

        /// <summary>One completed local run: the text, the provider that
        /// actually served it, and the token counts the receipt records.</summary>
        public sealed class InferenceResult
        {
            public string Text = "";
            public string Provider = "";
            public int PromptTokens;
            public int NewTokens;
        }

        /// <summary>
        /// Run one chat turn locally. Throws InvalidOperationException
        /// when the gate refuses — refusal is absence, not a partial run.
        /// </summary>
        public async Task<InferenceResult> ChatAsync(ModelEntry m, string userPrompt,
            int maxNewTokens, CancellationToken ct)
        {
            if (!CanRun(m))
                throw new InvalidOperationException(GateReason(m));
            return await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                EnsureRuntimeView(m);
                var dir = RuntimeDir(m);
                var note = new StringBuilder();
                using var model = LoadModel(dir, note);
                using var tokenizer = new Tokenizer(model);
                var prompt = ApplyChatTemplate(userPrompt);
                using var input = tokenizer.Encode(prompt);
                int promptTokens = input[0UL].Length;

                using var genParams = new GeneratorParams(model);
                genParams.SetSearchOption("max_length", promptTokens + Math.Max(8, maxNewTokens));
                genParams.SetSearchOption("temperature", 0.7);
                genParams.SetSearchOption("top_p", 0.9);

                using var generator = new Generator(model, genParams);
                generator.AppendTokens(input[0UL]);
                while (!generator.IsDone())
                {
                    ct.ThrowIfCancellationRequested();
                    generator.GenerateNextToken();
                }
                var seq = generator.GetSequence(0UL);
                var sb = new StringBuilder();
                using var stream = tokenizer.CreateStream();
                for (int i = promptTokens; i < seq.Length; i++)
                    sb.Append(stream.Decode(seq[i]));

                _log.Seal("models", "inference-run",
                    m.Id + " promptTokens=" + promptTokens + " newTokens=" + (seq.Length - promptTokens));
                return new InferenceResult
                {
                    Text = sb.ToString().Trim(),
                    Provider = note.Length > 0 ? note.ToString() : "CPU",
                    PromptTokens = promptTokens,
                    NewTokens = seq.Length - promptTokens
                };
            }, ct);
        }

        /// <summary>
        /// DirectML first on Windows; CPU fallback is caught, noted, and
        /// used — never thrown at the user as a raw provider error.
        /// </summary>
        private static Model LoadModel(string dir, StringBuilder note)
        {
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    using var config = new Config(dir);
                    config.ClearProviders();
                    config.AppendProvider("dml");
                    var model = new Model(config);
                    note.Append("DirectML");
                    return model;
                }
                catch
                {
                    note.Clear().Append("CPU (DirectML unavailable — fell back)");
                }
            }
            using var cpuConfig = new Config(dir);
            cpuConfig.ClearProviders();
            return new Model(cpuConfig);
        }
    }
}
