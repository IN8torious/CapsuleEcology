using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace CapsuleEcology.Services
{
    public sealed class CleanCategory
    {
        public string Id = "", Name = "", PathText = "", Note = "";
        public long Bytes = -1;   // -1 = not measurable (protected contents)
        public int Files;
        public bool Irreversible;
        public string SizeText => Bytes < 0 ? "size not measurable" : $"{Bytes / 1_000_000.0:0.0} MB · {Files} files";
    }

    /// <summary>
    /// Cleaner (R3 — Sparkle's shape, our law): FILES ONLY, never
    /// settings, services or logs; sizes are measured and shown FIRST;
    /// the user picks the categories; every clean seals a receipt with
    /// what was actually freed. Locked/in-use files are skipped and
    /// counted, never forced. The Recycle Bin is the one irreversible
    /// category and is labeled as such.
    /// </summary>
    public sealed class CleanerService
    {
        private readonly ReceiptLog _log;
        public CleanerService(ReceiptLog log) { _log = log; }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? rootPath, uint flags);
        private const uint SHERB_NOCONFIRMATION = 0x1, SHERB_NOPROGRESSUI = 0x2, SHERB_NOSOUND = 0x4;

        private static string LocalAppData => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        public List<CleanCategory> Categories() => new()
        {
            new CleanCategory { Id = "temp", Name = "Temporary Files",
                PathText = Path.Combine(Path.GetTempPath()) + " + C:\\Windows\\Temp",
                Note = "App and system temp files. In-use files are skipped." },
            new CleanCategory { Id = "prefetch", Name = "Prefetch Files", PathText = @"C:\Windows\Prefetch",
                Note = "Windows rebuilds these as apps launch. Cleaning needs the app run as administrator." },
            new CleanCategory { Id = "thumbnails", Name = "Thumbnail Cache",
                PathText = Path.Combine(LocalAppData, @"Microsoft\Windows\Explorer"),
                Note = "Explorer rebuilds thumbnails on demand." },
            new CleanCategory { Id = "recycle", Name = "Recycle Bin", PathText = @"C:\$Recycle.Bin",
                Note = "Permanently removes everything in the Recycle Bin.", Irreversible = true },
        };

        public CleanCategory Measure(CleanCategory cat)
        {
            switch (cat.Id)
            {
                case "temp":
                    Add(cat, Path.GetTempPath()); Add(cat, @"C:\Windows\Temp", accumulate: true); break;
                case "prefetch": Add(cat, @"C:\Windows\Prefetch"); break;
                case "thumbnails":
                    cat.Bytes = 0;
                    try
                    {
                        if (Directory.Exists(cat.PathText))
                            foreach (var f in Directory.GetFiles(cat.PathText, "thumbcache_*.db"))
                            { try { cat.Bytes += new FileInfo(f).Length; cat.Files++; } catch { } }
                    }
                    catch { }
                    break;
                case "recycle":
                    cat.Bytes = -1; cat.Files = 0;
                    try { Add(cat, @"C:\$Recycle.Bin"); if (cat.Files == 0) cat.Bytes = -1; }
                    catch { cat.Bytes = -1; }
                    break;
            }
            return cat;

            static void Add(CleanCategory c, string dir, bool accumulate = false)
            {
                if (!accumulate) { c.Bytes = 0; c.Files = 0; }
                try
                {
                    if (!Directory.Exists(dir)) return;
                    foreach (var f in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
                    {
                        try { c.Bytes += new FileInfo(f).Length; c.Files++; } catch { }
                    }
                }
                catch { if (!accumulate) c.Bytes = -1; }
            }
        }

        public (long Freed, int Deleted, int Skipped) Clean(CleanCategory cat)
        {
            long freed = 0; int deleted = 0, skipped = 0;
            if (cat.Id == "recycle")
            {
                var rc = SHEmptyRecycleBin(IntPtr.Zero, null, SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND);
                _log.Seal("cleaner", "clean", "cleaner: Recycle Bin emptied" + (rc == 0 ? "" : $" (shell returned {rc})"));
                return (0, rc == 0 ? 1 : 0, rc == 0 ? 0 : 1);
            }
            var dirs = cat.Id switch
            {
                "temp" => new[] { Path.GetTempPath(), @"C:\Windows\Temp" },
                "prefetch" => new[] { @"C:\Windows\Prefetch" },
                "thumbnails" => new[] { cat.PathText },
                _ => Array.Empty<string>(),
            };
            foreach (var dir in dirs)
            {
                if (!Directory.Exists(dir)) continue;
                var pattern = cat.Id == "thumbnails" ? "thumbcache_*.db" : "*";
                foreach (var f in Directory.EnumerateFiles(dir, pattern, SearchOption.AllDirectories))
                {
                    try { var len = new FileInfo(f).Length; File.Delete(f); freed += len; deleted++; }
                    catch { skipped++; }
                }
            }
            _log.Seal("cleaner", "clean",
                $"cleaner: {cat.Name} — freed {freed / 1_000_000.0:0.0} MB, {deleted} files deleted, {skipped} skipped (in use or protected)");
            return (freed, deleted, skipped);
        }
    }
}
