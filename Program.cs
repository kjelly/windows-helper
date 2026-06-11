using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FontLister;

class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        
        // Paths for Tabby and Wave terminal configurations
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        
        string tabbyConfigPath = Path.Combine(appData, "tabby", "config.yaml");
        string waveConfigPath = Path.Combine(userProfile, ".config", "waveterm", "settings.json");

        // Parse arguments
        if (args.Length > 0)
        {
            HandleCommandLineArgs(args, tabbyConfigPath, waveConfigPath);
            return;
        }

        // Run interactive mode
        RunInteractiveMode(tabbyConfigPath, waveConfigPath);
    }

    static void HandleCommandLineArgs(string[] args, string tabbyPath, string wavePath)
    {
        if (args.Contains("--help") || args.Contains("-h"))
        {
            ShowHelp();
            return;
        }

        if (args.Contains("--list") || args.Contains("-l"))
        {
            var monoFonts = GetMonospacedFonts();
            Console.WriteLine("=== 系統內建適合終端機的字型 (等寬字型) ===");
            foreach (var font in monoFonts)
            {
                Console.WriteLine($"- {font}");
            }
            return;
        }

        if (args.Contains("--all-fonts"))
        {
            using var installedFonts = new InstalledFontCollection();
            Console.WriteLine("=== 系統內建所有字型 ===");
            foreach (var family in installedFonts.Families.OrderBy(f => f.Name))
            {
                Console.WriteLine($"- {family.Name}");
            }
            return;
        }

        // Get --font / -f argument
        string? fontName = null;
        int fontIdx = Array.IndexOf(args, "--font");
        if (fontIdx == -1)
        {
            fontIdx = Array.IndexOf(args, "-f");
        }
        if (fontIdx != -1 && fontIdx + 1 < args.Length)
        {
            fontName = args[fontIdx + 1];
        }

        if (string.IsNullOrEmpty(fontName))
        {
            Console.WriteLine("錯誤：必須提供 --font \"字型名稱\" 參數來進行設定。輸入 --help 查看說明。");
            return;
        }

        // Get --terminal / -t argument
        string targetTerminal = "both";
        int termIdx = Array.IndexOf(args, "--terminal");
        if (termIdx == -1)
        {
            termIdx = Array.IndexOf(args, "-t");
        }
        if (termIdx != -1 && termIdx + 1 < args.Length)
        {
            targetTerminal = args[termIdx + 1].ToLower();
        }

        ApplyFontSettings(fontName, targetTerminal, tabbyPath, wavePath);
    }

    static void ShowHelp()
    {
        Console.WriteLine("字型列出與終端機字型自動設定工具");
        Console.WriteLine();
        Console.WriteLine("用法:");
        Console.WriteLine("  dotnet run [參數]");
        Console.WriteLine();
        Console.WriteLine("參數:");
        Console.WriteLine("  (無參數)                 啟動互動式選單進行字型選擇與設定");
        Console.WriteLine("  -l, --list               列出系統中所有適合終端機的字型 (等寬字型)");
        Console.WriteLine("  --all-fonts              列出系統中安裝的所有字型名稱");
        Console.WriteLine("  -f, --font \"名稱\"        指定要設定的字型名稱 (必填，若有指定其他設定參數)");
        Console.WriteLine("  -t, --terminal [類型]    指定要設定的終端機 (tabby / wave / both，預設為 both)");
        Console.WriteLine("  -h, --help               顯示此說明訊息");
    }

    static List<string> GetMonospacedFonts()
    {
        using var installedFonts = new InstalledFontCollection();
        FontFamily[] families = installedFonts.Families;

        var terminalFonts = new List<string>();

        using var bmp = new Bitmap(1, 1);
        using var g = Graphics.FromImage(bmp);
        using var sf = new StringFormat(StringFormat.GenericTypographic)
        {
            FormatFlags = StringFormatFlags.MeasureTrailingSpaces | StringFormatFlags.NoWrap
        };

        foreach (var family in families)
        {
            if (IsMonospaced(family, g, sf))
            {
                terminalFonts.Add(family.Name);
            }
        }
        return terminalFonts.OrderBy(n => n).ToList();
    }

    static bool IsMonospaced(FontFamily family, Graphics g, StringFormat sf)
    {
        FontStyle? activeStyle = null;
        if (family.IsStyleAvailable(FontStyle.Regular))
            activeStyle = FontStyle.Regular;
        else if (family.IsStyleAvailable(FontStyle.Bold))
            activeStyle = FontStyle.Bold;
        else if (family.IsStyleAvailable(FontStyle.Italic))
            activeStyle = FontStyle.Italic;
        
        if (activeStyle == null)
            return false;

        try
        {
            using var font = new Font(family, 12, activeStyle.Value);
            
            // Measure widths of different characters
            float w1 = g.MeasureString("i", font, PointF.Empty, sf).Width;
            float w2 = g.MeasureString("W", font, PointF.Empty, sf).Width;
            float w3 = g.MeasureString("m", font, PointF.Empty, sf).Width;
            float w4 = g.MeasureString(" ", font, PointF.Empty, sf).Width;
            float w5 = g.MeasureString("1", font, PointF.Empty, sf).Width;

            // In monospace fonts, character widths are identical.
            // We allow a very small tolerance due to float rounding.
            const float epsilon = 0.001f;
            return Math.Abs(w1 - w2) < epsilon &&
                   Math.Abs(w1 - w3) < epsilon &&
                   Math.Abs(w1 - w4) < epsilon &&
                   Math.Abs(w1 - w5) < epsilon;
        }
        catch
        {
            return false;
        }
    }

    static void RunInteractiveMode(string tabbyPath, string wavePath)
    {
        Console.WriteLine("=== Windows 終端機字型自動設定工具 ===");
        Console.WriteLine("正在掃描系統內安裝的等寬字型...");
        
        var monoFonts = GetMonospacedFonts();
        if (monoFonts.Count == 0)
        {
            Console.WriteLine("未偵測到適合終端機的字型，或測量功能無法運作。");
            return;
        }

        Console.WriteLine($"\n偵測到 {monoFonts.Count} 個適合終端機的字型：");
        for (int i = 0; i < monoFonts.Count; i++)
        {
            Console.WriteLine($"{i + 1, 3}. {monoFonts[i]}");
        }

        Console.Write("\n請輸入字型編號以進行設定 (或輸入 0 取消): ");
        string? input = Console.ReadLine();
        if (!int.TryParse(input, out int selection) || selection < 1 || selection > monoFonts.Count)
        {
            Console.WriteLine("已取消設定或輸入錯誤。");
            return;
        }

        string selectedFont = monoFonts[selection - 1];
        Console.WriteLine($"\n您選擇了字型: \"{selectedFont}\"");

        // Detect terminal configuration existence
        bool tabbyExists = File.Exists(tabbyPath);
        bool waveExists = File.Exists(wavePath);

        Console.WriteLine("\n偵測到本機終端機設定檔狀態：");
        Console.WriteLine($"- Tabby Terminal: {(tabbyExists ? "已偵測到設定檔" : "未偵測到")}");
        Console.WriteLine($"- Wave Terminal: {(waveExists ? "已偵測到設定檔" : "未偵測到 (將在設定時自動建立)")}");

        Console.WriteLine("\n請選擇要更新的終端機設定：");
        Console.WriteLine("1. 設定 Tabby Terminal");
        Console.WriteLine("2. 設定 Wave Terminal");
        Console.WriteLine("3. 設定兩者 (預設)");
        Console.Write("請輸入選擇 (1-3, 按 Enter 預設為 3): ");
        
        string? termInput = Console.ReadLine();
        string targetTerminal = "both";
        if (termInput == "1") targetTerminal = "tabby";
        else if (termInput == "2") targetTerminal = "wave";

        ApplyFontSettings(selectedFont, targetTerminal, tabbyPath, wavePath);
    }

    static void ApplyFontSettings(string fontName, string targetTerminal, string tabbyPath, string wavePath)
    {
        if (targetTerminal == "tabby" || targetTerminal == "both")
        {
            UpdateTabbyFont(tabbyPath, fontName);
        }
        if (targetTerminal == "wave" || targetTerminal == "both")
        {
            UpdateWaveFont(wavePath, fontName);
        }
    }

    static void UpdateTabbyFont(string configPath, string fontName)
    {
        if (!File.Exists(configPath))
        {
            Console.WriteLine($"[Tabby] 找不到設定檔: {configPath}，無法進行自動設定。");
            return;
        }
        
        // Create backup
        string backupPath = configPath + ".bak";
        try 
        { 
            File.Copy(configPath, backupPath, true); 
        } 
        catch (Exception ex)
        {
            Console.WriteLine($"[Tabby] 備份設定檔時失敗: {ex.Message}");
        }

        try
        {
            var lines = File.ReadAllLines(configPath).ToList();
            int terminalIdx = -1;
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].TrimEnd().Equals("terminal:"))
                {
                    terminalIdx = i;
                    break;
                }
            }

            if (terminalIdx == -1)
            {
                // terminal: block doesn't exist, append it at the end
                lines.Add("terminal:");
                lines.Add($"  font: {fontName}");
            }
            else
            {
                bool foundFont = false;
                // Look for font: line under terminal: block
                for (int i = terminalIdx + 1; i < lines.Count; i++)
                {
                    string line = lines[i];
                    
                    // If we reach a line that belongs to another top-level key (no spaces/not empty)
                    if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith(" ") && !line.StartsWith("#"))
                    {
                        break;
                    }

                    // Check if it's the font configuration
                    string trimmed = line.TrimStart();
                    if (trimmed.StartsWith("font:"))
                    {
                        int indentLength = line.Length - trimmed.Length;
                        string indent = line.Substring(0, indentLength);
                        lines[i] = $"{indent}font: {fontName}";
                        foundFont = true;
                        break;
                    }
                }

                if (!foundFont)
                {
                    // If font: wasn't found under terminal, insert it under terminal:
                    lines.Insert(terminalIdx + 1, $"  font: {fontName}");
                }
            }

            File.WriteAllLines(configPath, lines);
            Console.WriteLine($"[Tabby] 已成功將字型更新為 \"{fontName}\" (備份已儲存至 {Path.GetFileName(backupPath)})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Tabby] 更新 Tabby Terminal 設定時發生錯誤: {ex.Message}");
        }
    }

    static void UpdateWaveFont(string configPath, string fontName)
    {
        string? dir = Path.GetDirectoryName(configPath);
        if (dir != null && !Directory.Exists(dir))
        {
            try
            {
                Directory.CreateDirectory(dir);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Wave] 建立設定檔目錄失敗: {ex.Message}");
                return;
            }
        }

        if (File.Exists(configPath))
        {
            // Create backup
            string backupPath = configPath + ".bak";
            try 
            { 
                File.Copy(configPath, backupPath, true); 
            } 
            catch (Exception ex)
            {
                Console.WriteLine($"[Wave] 備份設定檔時失敗: {ex.Message}");
            }
        }

        try
        {
            string jsonContent = File.Exists(configPath) ? File.ReadAllText(configPath) : "{}";
            var options = new JsonNodeOptions { PropertyNameCaseInsensitive = true };
            var root = JsonNode.Parse(jsonContent, options) as JsonObject ?? new JsonObject();

            root["term:fontfamily"] = $"{fontName}, monospace";

            var writeOptions = new JsonSerializerOptions { WriteIndented = true };
            string updatedJson = root.ToJsonString(writeOptions);

            File.WriteAllText(configPath, updatedJson);
            Console.WriteLine($"[Wave] 已成功將字型更新為 \"{fontName}, monospace\" (備份已儲存至 {configPath}.bak)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Wave] 更新 Wave Terminal 設定時發生錯誤: {ex.Message}");
        }
    }
}
