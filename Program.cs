using System;
using System.IO;
using System.Threading.Tasks;
using ImageProcessing;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.InputEncoding  = System.Text.Encoding.UTF8;

// ── Configuration ─────────────────────────────────────────────────────────────
string baseDir       = AppDomain.CurrentDomain.BaseDirectory;
string inputImage    = @"C:\test.png";
string filteredImage = Path.Combine(baseDir, "test_filtered.png");

// tessdata is in the 'data' subfolder of the project (copied to output)
string tessDataPath  = Path.Combine(baseDir, "data");
string tessLanguage  = "ara+eng";
// ──────────────────────────────────────────────────────────────────────────────

PrintBanner();
PrintConfig(inputImage, filteredImage, tessDataPath);

// ── Pre-flight checks ─────────────────────────────────────────────────────────
if (!RunPreflightChecks(inputImage, tessDataPath, tessLanguage))
    return;

// ── Step 1: DocumentFilter ────────────────────────────────────────────────────
Section("Step 1 – Document Filter (Emgu.CV)");

var filter = new DocumentFilter();
bool filtered = filter.Execute(inputImage, filteredImage, threshold: 150);

if (!filtered || !File.Exists(filteredImage))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("[ERROR] Image filtering failed — cannot continue.");
    Console.ResetColor();
    return;
}

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"  ✅ Filtered image ready ({new FileInfo(filteredImage).Length / 1024} KB)");
Console.ResetColor();

// ── Step 2: OCR Benchmark ─────────────────────────────────────────────────────
Section("Step 2 – Running OCR Engines");

var benchmark = new OcrBenchmark(tessDataPath, tessLanguage);

Console.WriteLine("  ⏳ Running Windows OCR...");
var winTask = benchmark.RunWindowsOcrAsync(filteredImage);

Console.WriteLine("  ⏳ Running Tesseract OCR...");
var tessResult = benchmark.RunTesseractOcr(filteredImage);
var winResult  = await winTask;

// ── Step 3: Display Results ───────────────────────────────────────────────────
Section("Step 3 – Results Comparison");
PrintResult(winResult,  "🪟");
PrintResult(tessResult, "📖");

// ── Step 4: Performance Summary ──────────────────────────────────────────────
Section("Step 4 – Performance Summary");
PrintPerformanceTable(winResult, tessResult);

Console.WriteLine();
Console.ForegroundColor = ConsoleColor.DarkGray;
Console.WriteLine($"  Filtered image saved to: {filteredImage}");
Console.ResetColor();

// ── Helpers ───────────────────────────────────────────────────────────────────

static void PrintBanner()
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("╔══════════════════════════════════════════════════════╗");
    Console.WriteLine("║       OCR Benchmark: Windows OCR vs Tesseract        ║");
    Console.WriteLine("╚══════════════════════════════════════════════════════╝");
    Console.ResetColor();
}

static void PrintConfig(string input, string filtered, string tessdata)
{
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine($"\n  Input image   : {input}");
    Console.WriteLine($"  Filtered image: {filtered}");
    Console.WriteLine($"  Tessdata path : {tessdata}");
    Console.ResetColor();
}

static bool RunPreflightChecks(string inputImage, string tessDataPath, string tessLanguage)
{
    Section("Pre-flight Checks");
    bool ok = true;

    // Input image
    if (!File.Exists(inputImage))
    {
        Fail($"Input image not found: {inputImage}");
        ok = false;
    }
    else
        Pass($"Input image found: {inputImage}");

    // Tessdata folder
    if (!Directory.Exists(tessDataPath))
    {
        Fail($"Tessdata folder not found: {tessDataPath}");
        ok = false;
    }
    else
    {
        Pass($"Tessdata folder found: {tessDataPath}");
        var files = Directory.GetFiles(tessDataPath, "*.traineddata");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"    Available traineddata files ({files.Length}):");
        foreach (var f in files)
            Console.WriteLine($"      • {Path.GetFileName(f)}");
        Console.ResetColor();

        foreach (string lang in tessLanguage.Split('+'))
        {
            string expected = Path.Combine(tessDataPath, lang.Trim() + ".traineddata");
            if (!File.Exists(expected))
            { Fail($"Missing: {Path.GetFileName(expected)}"); ok = false; }
            else
                Pass($"Language file found: {lang}.traineddata");
        }
    }

    if (!ok)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("\n  ❌ Pre-flight failed. Fix the above issues and re-run.");
        Console.ResetColor();
    }
    return ok;
}

static void Pass(string msg) { Console.ForegroundColor = ConsoleColor.Green;  Console.WriteLine($"  ✅ {msg}"); Console.ResetColor(); }
static void Fail(string msg) { Console.ForegroundColor = ConsoleColor.Red;    Console.WriteLine($"  ❌ {msg}"); Console.ResetColor(); }

static void Section(string title)
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("┌─────────────────────────────────────────────────────");
    Console.WriteLine($"│  {title}");
    Console.WriteLine("└─────────────────────────────────────────────────────");
    Console.ResetColor();
}

static void PrintResult(ImageProcessing.OcrEngineResult r, string icon)
{
    Console.ForegroundColor = ConsoleColor.Magenta;
    Console.WriteLine($"\n  {icon}  {r.EngineName}");
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine($"  Time: {r.Elapsed.TotalMilliseconds:F0} ms");
    Console.ResetColor();

    if (!string.IsNullOrEmpty(r.Error))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  ⚠️  Error: {r.Error}");
        Console.ResetColor();
        return;
    }

    if (string.IsNullOrWhiteSpace(r.Text))
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("  (no text extracted)");
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("  ── Extracted Text ──────────────────────────────");
        Console.WriteLine(r.Text.TrimEnd());
        Console.WriteLine("  ────────────────────────────────────────────────");
        int wordCount = r.Text.Split(new[] {' ', '\n', '\r'}, StringSplitOptions.RemoveEmptyEntries).Length;
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  Word count: ~{wordCount}");
    }
    Console.ResetColor();
}

static void PrintPerformanceTable(
    ImageProcessing.OcrEngineResult win,
    ImageProcessing.OcrEngineResult tess)
{
    int winMs  = (int)win.Elapsed.TotalMilliseconds;
    int tessMs = (int)tess.Elapsed.TotalMilliseconds;

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"  {"Engine",-32} {"Time (ms)",10}   {"Words",8}   {"Status",-15}");
    Console.WriteLine($"  {"──────",32} {"──────────",10}   {"─────",8}   {"──────────────",15}");
    PrintRow("🪟  Windows OCR",   winMs,  tessMs, win.Text,  win.Error);
    PrintRow("📖  Tesseract OCR", tessMs, winMs,  tess.Text, tess.Error);
    Console.ResetColor();

    if (string.IsNullOrEmpty(win.Error) && string.IsNullOrEmpty(tess.Error))
    {
        string winner = winMs < tessMs ? "🪟  Windows OCR"
                      : winMs == tessMs ? "Tie"
                      : "📖  Tesseract OCR";
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n  🏆  Fastest engine: {winner}");
        Console.ResetColor();
    }
}

static void PrintRow(string name, int myMs, int otherMs, string text, string error)
{
    int words = string.IsNullOrWhiteSpace(text) ? 0
        : text.Split(new[] {' ', '\n', '\r'}, StringSplitOptions.RemoveEmptyEntries).Length;
    string status = !string.IsNullOrEmpty(error) ? "❌ Error"
        : myMs <= otherMs ? "✅ Faster" : "🐢 Slower";
    Console.ForegroundColor = !string.IsNullOrEmpty(error) ? ConsoleColor.Red : ConsoleColor.White;
    Console.WriteLine($"  {name,-32} {myMs,10}   {words,8}   {status,-15}");
    Console.ResetColor();
}