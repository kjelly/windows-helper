using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace WindowsHelper;

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

        // No args → run interactive mode
        if (args.Length == 0)
        {
            RunInteractiveMode(tabbyConfigPath, waveConfigPath);
            return;
        }

        DispatchCli(args, tabbyConfigPath, waveConfigPath);
    }

    // Top-level CLI dispatcher: routes `helper <command> [subcommand] [args...]`
    // to the appropriate subcommand handler.
    static void DispatchCli(string[] args, string tabbyPath, string wavePath)
    {
        string command = args[0].ToLowerInvariant();

        switch (command)
        {
            case "font":
            {
                string[] rest = args.Skip(1).ToArray();
                DispatchFontCommand(rest, tabbyPath, wavePath);
                return;
            }
            case "hotkeys":
            {
                string[] rest = args.Skip(1).ToArray();
                DispatchHotkeysCommand(rest, tabbyPath, wavePath);
                return;
            }
            case "-h":
            case "--help":
            case "help":
                ShowHelp();
                return;
            default:
                Console.WriteLine($"錯誤：未知的命令 \"{args[0]}\"。輸入 --help 查看說明。");
                Environment.ExitCode = 1;
                return;
        }
    }

    // `helper font ...`
    static void DispatchFontCommand(string[] args, string tabbyPath, string wavePath)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            ShowFontHelp();
            return;
        }

        string sub = args[0].ToLowerInvariant();

        switch (sub)
        {
            case "list":
            {
                bool all = args.Skip(1).Any(a => a is "--all" or "-a");
                ListFonts(all);
                return;
            }
            case "set":
            {
                // First non-flag arg is treated as the font name.
                string? fontName = args.Skip(1).FirstOrDefault(a => !a.StartsWith("-"));
                if (string.IsNullOrWhiteSpace(fontName))
                {
                    Console.WriteLine("錯誤：font set 需要指定字型名稱。例：helper font set \"Cascadia Code\"");
                    Environment.ExitCode = 1;
                    return;
                }

                string target = "both";
                int tIdx = Array.IndexOf(args, "--target");
                if (tIdx == -1) tIdx = Array.IndexOf(args, "-t");
                if (tIdx != -1 && tIdx + 1 < args.Length)
                {
                    target = args[tIdx + 1].ToLowerInvariant();
                }

                ApplyFontSettings(fontName, target, tabbyPath, wavePath);
                return;
            }
            case "interactive":
            case "pick":
                RunFontSetupFlow(tabbyPath, wavePath);
                return;
            default:
                Console.WriteLine($"錯誤：未知的 font 子命令 \"{args[0]}\"。輸入 `helper font --help` 查看說明。");
                Environment.ExitCode = 1;
                return;
        }
    }

    // `helper hotkeys ...`
    static void DispatchHotkeysCommand(string[] args, string tabbyPath, string wavePath)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            ShowHotkeysHelp();
            return;
        }

        string sub = args[0].ToLowerInvariant();

        switch (sub)
        {
            case "clean":
            case "reset":
            {
                UpdateTabbyHotkeys(tabbyPath);
                return;
            }
            default:
                Console.WriteLine($"錯誤：未知的 hotkeys 子命令 \"{args[0]}\"。輸入 `helper hotkeys --help` 查看說明。");
                Environment.ExitCode = 1;
                return;
        }
    }

    static void ListFonts(bool all)
    {
        if (all)
        {
            using var installedFonts = new InstalledFontCollection();
            Console.WriteLine("=== 系統內建所有字型 ===");
            foreach (var family in installedFonts.Families.OrderBy(f => f.Name))
            {
                Console.WriteLine($"- {family.Name}");
            }
            return;
        }

        var monoFonts = GetMonospacedFonts();
        Console.WriteLine("=== 系統內建適合終端機的字型 (等寬字型) ===");
        foreach (var font in monoFonts)
        {
            Console.WriteLine($"- {font}");
        }
    }

    static void ShowHelp()
    {
        Console.WriteLine("Windows 終端機設定助手 (Windows Terminal Helper)");
        Console.WriteLine();
        Console.WriteLine("用於管理 Tabby Terminal 與 Wave Terminal 設定的小工具，");
        Console.WriteLine("目前提供兩大子功能：字型 (font) 與快捷鍵 (hotkeys)。");
        Console.WriteLine();
        Console.WriteLine("用法:");
        Console.WriteLine("  helper                       啟動互動式選單");
        Console.WriteLine("  helper <command> [options]   執行指定子命令");
        Console.WriteLine();
        Console.WriteLine("指令:");
        Console.WriteLine("  font                         管理終端機字型 (列出 / 設定)");
        Console.WriteLine("  hotkeys                      管理 Tabby 快捷鍵");
        Console.WriteLine("  help                         顯示此說明訊息");
        Console.WriteLine();
        Console.WriteLine("範例:");
        Console.WriteLine("  helper font list");
        Console.WriteLine("  helper font set \"Cascadia Code\"");
        Console.WriteLine("  helper font set \"Cascadia Code\" --target tabby");
        Console.WriteLine("  helper hotkeys clean --keep-tab-movement");
        Console.WriteLine();
        Console.WriteLine("輸入 `helper <command> --help` 取得子命令的詳細說明。");
    }

    static void ShowFontHelp()
    {
        Console.WriteLine("helper font — 終端機字型管理");
        Console.WriteLine();
        Console.WriteLine("用法:");
        Console.WriteLine("  helper font list [--all]");
        Console.WriteLine("  helper font set <字型名稱> [--target tabby|wave|both]");
        Console.WriteLine("  helper font interactive");
        Console.WriteLine();
        Console.WriteLine("子命令:");
        Console.WriteLine("  list             列出適合終端機使用的等寬字型");
        Console.WriteLine("    --all, -a      改為列出系統中所有字型 (含比例字型)");
        Console.WriteLine("  set <name>       將指定字型寫入終端機設定檔");
        Console.WriteLine("    --target, -t   目標終端機 (tabby / wave / both，預設 both)");
        Console.WriteLine("  interactive      進入互動式字型挑選流程");
    }

    static void ShowHotkeysHelp()
    {
        Console.WriteLine("helper hotkeys — Tabby 快捷鍵管理");
        Console.WriteLine();
        Console.WriteLine("用法:");
        Console.WriteLine("  helper hotkeys clean");
        Console.WriteLine();
        Console.WriteLine("子命令:");
        Console.WriteLine("  clean            清除 Tabby 內建快捷鍵，僅保留核心快速鍵");
        Console.WriteLine("                    保留: 新增/切換/移動分頁、命令選擇器、複製/貼上、縮放、分頁1~9");
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
        while (true)
        {
            Console.WriteLine("=== Windows 終端機設定助手 ===");
            Console.WriteLine();
            Console.WriteLine("可用功能：");
            Console.WriteLine("  1. 字型設定   — 列出 / 設定 Tabby 與 Wave 的終端機字型");
            Console.WriteLine("  2. 快捷鍵管理 — 清除 Tabby 快捷鍵 (僅保留核心快速鍵)");
            Console.WriteLine("  3. 顯示說明   — 列出 CLI 指令用法");
            Console.WriteLine("  0. 離開");
            Console.Write("請選擇功能 (0-3): ");

            string? choice = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(choice) || choice == "0")
            {
                Console.WriteLine("已退出程式。");
                break;
            }

            switch (choice)
            {
                case "1":
                    FontMenu(tabbyPath, wavePath);
                    break;
                case "2":
                    HotkeysMenu(tabbyPath, wavePath);
                    break;
                case "3":
                    ShowHelp();
                    Pause();
                    Console.Clear();
                    break;
                default:
                    Console.WriteLine("無效的選擇，請重新輸入。\n");
                    continue;
            }
        }
    }

    static void FontMenu(string tabbyPath, string wavePath)
    {
        Console.Clear();
        Console.WriteLine("--- 字型設定 ---");
        Console.WriteLine("  1. 列出系統等寬字型");
        Console.WriteLine("  2. 列出系統所有字型");
        Console.WriteLine("  3. 挑選字型並套用至終端機");
        Console.WriteLine("  0. 返回主選單");
        Console.Write("請選擇 (0-3): ");

        string? choice = Console.ReadLine()?.Trim();
        switch (choice)
        {
            case "1":
                ListFonts(all: false);
                Pause();
                break;
            case "2":
                ListFonts(all: true);
                Pause();
                break;
            case "3":
            case "":
                RunFontSetupFlow(tabbyPath, wavePath);
                Pause();
                break;
            case "0":
                break;
            default:
                Console.WriteLine("無效的選擇。");
                Pause();
                break;
        }
        Console.Clear();
    }

    static void HotkeysMenu(string tabbyPath, string wavePath)
    {
        Console.Clear();
        Console.WriteLine("--- 快捷鍵管理 ---");
        Console.WriteLine("  1. 清除 Tabby 快捷鍵 (僅保留核心快速鍵)");
        Console.WriteLine("  0. 返回主選單");
        Console.Write("請選擇 (0-1): ");

        string? choice = Console.ReadLine()?.Trim();
        switch (choice)
        {
            case "1":
            case "":
                UpdateTabbyHotkeys(tabbyPath);
                Pause();
                break;
            case "0":
                break;
            default:
                Console.WriteLine("無效的選擇。");
                Pause();
                break;
        }
        Console.Clear();
    }

    static void Pause()
    {
        Console.WriteLine("\n按下任一鍵返回...");
        Console.ReadKey(true);
    }

    static void RunFontSetupFlow(string tabbyPath, string wavePath)
    {
        Console.WriteLine("\n正在掃描系統內安裝的等寬字型...");
        
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

    static void UpdateTabbyHotkeys(string configPath)
    {
        if (!File.Exists(configPath))
        {
            Console.WriteLine($"[Tabby] 找不到設定檔: {configPath}，無法清除快捷鍵。");
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
            int hotkeysStart = -1;
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].TrimEnd().Equals("hotkeys:"))
                {
                    hotkeysStart = i;
                    break;
                }
            }

            var actionsToDisable = new HashSet<string>
            {
                "toggle-window",
                "copy-current-path",
                "ctrl-c",
                "copy",
                "paste",
                "select-all",
                "clear",
                "zoom-in",
                "zoom-out",
                "reset-zoom",
                "home",
                "end",
                "previous-word",
                "next-word",
                "delete-previous-word",
                "delete-line",
                "delete-next-word",
                "search",
                "pane-focus-all",
                "focus-all-tabs",
                "scroll-to-top",
                "scroll-page-up",
                "scroll-up",
                "scroll-down",
                "scroll-page-down",
                "scroll-to-bottom",
                "restart-telnet-session",
                "restart-ssh-session",
                "launch-winscp",
                "settings-tab",
                "settings",
                "serial",
                "restart-serial-session",
                "new-window",
                "profile",
                "profile-selectors",
                "group-selectors",
                "toggle-fullscreen",
                "close-tab",
                "reopen-tab",
                "toggle-last-tab",
                "rename-tab",
                "next-tab",
                "previous-tab",
                "move-tab-left",
                "move-tab-right",
                "rearrange-panes",
                "duplicate-tab",
                "restart-tab",
                "reconnect-tab",
                "disconnect-tab",
                "explode-tab",
                "combine-tabs",
                "split-right",
                "split-bottom",
                "split-left",
                "split-top",
                "pane-nav-right",
                "pane-nav-down",
                "pane-nav-up",
                "pane-nav-left",
                "pane-nav-previous",
                "pane-nav-next",
                "pane-maximize",
                "close-pane",
                "pane-increase-vertical",
                "pane-decrease-vertical",
                "pane-increase-horizontal",
                "pane-decrease-horizontal",
                "switch-profile",
                "profile-selector",
                "command-selector",
                "open-sftp"
            };

            for (int i = 1; i <= 20; i++) actionsToDisable.Add($"tab-{i}");
            for (int i = 1; i <= 9; i++) actionsToDisable.Add($"pane-nav-{i}");

            int hotkeysEnd = lines.Count;

            if (hotkeysStart != -1)
            {
                // Find end of hotkeys block
                for (int i = hotkeysStart + 1; i < lines.Count; i++)
                {
                    string line = lines[i];
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    // If it starts with non-whitespace and is not comment, it's a new section
                    if (!line.StartsWith(" ") && !line.StartsWith("\t") && !line.StartsWith("#"))
                    {
                        hotkeysEnd = i;
                        break;
                    }
                }

                // Collect any existing actions in the config under hotkeys
                for (int i = hotkeysStart + 1; i < hotkeysEnd; i++)
                {
                    string line = lines[i];
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    string trimmed = line.TrimStart();
                    int indent = line.Length - trimmed.Length;
                    if (indent == 2 && trimmed.Contains(':'))
                    {
                        int colonIdx = trimmed.IndexOf(':');
                        string actionName = trimmed.Substring(0, colonIdx).Trim();
                        if (!string.IsNullOrEmpty(actionName))
                        {
                            actionsToDisable.Add(actionName);
                        }
                    }
                }
            }

            // Remove the kept keys from actionsToDisable so they are not set to []
            actionsToDisable.Remove("new-tab");
            actionsToDisable.Remove("command-selector");
            actionsToDisable.Remove("copy");
            actionsToDisable.Remove("paste");
            actionsToDisable.Remove("next-tab");
            actionsToDisable.Remove("previous-tab");
            actionsToDisable.Remove("zoom-in");
            actionsToDisable.Remove("zoom-out");
            actionsToDisable.Remove("reset-zoom");
            actionsToDisable.Remove("toggle-last-tab");
            actionsToDisable.Remove("move-tab-left");
            actionsToDisable.Remove("move-tab-right");

            for (int i = 1; i <= 9; i++) actionsToDisable.Remove($"tab-{i}");

            // Generate new hotkeys section
            var newHotkeyLines = new List<string>();
            newHotkeyLines.Add("hotkeys:");
            newHotkeyLines.Add("  new-tab:");
            newHotkeyLines.Add("    - Ctrl-Alt-T");
            newHotkeyLines.Add("  command-selector:");
            newHotkeyLines.Add("    - Ctrl-Shift-P");
            newHotkeyLines.Add("  copy:");
            newHotkeyLines.Add("    - Ctrl-Shift-C");
            newHotkeyLines.Add("  paste:");
            newHotkeyLines.Add("    - Ctrl-Shift-V");
            newHotkeyLines.Add("    - Shift-Insert");
            newHotkeyLines.Add("  next-tab:");
            newHotkeyLines.Add("    - Ctrl-.");
            newHotkeyLines.Add("  previous-tab:");
            newHotkeyLines.Add("    - Ctrl-,");
            newHotkeyLines.Add("  toggle-last-tab:");
            newHotkeyLines.Add("    - Ctrl-Tab");
            newHotkeyLines.Add("  zoom-in:");
            newHotkeyLines.Add("    - Ctrl-=");
            newHotkeyLines.Add("  zoom-out:");
            newHotkeyLines.Add("    - Ctrl--");
            newHotkeyLines.Add("  reset-zoom:");
            newHotkeyLines.Add("    - Ctrl-0");

            for (int i = 1; i <= 9; i++)
            {
                newHotkeyLines.Add($"  tab-{i}:");
                newHotkeyLines.Add($"    - Ctrl-{i}");
            }

            newHotkeyLines.Add("  move-tab-left:");
            newHotkeyLines.Add("    - Ctrl-Alt-Left");
            newHotkeyLines.Add("    - Ctrl-Alt-(");
            newHotkeyLines.Add("  move-tab-right:");
            newHotkeyLines.Add("    - Ctrl-Alt-Right");
            newHotkeyLines.Add("    - Ctrl-Alt-.");

            foreach (var action in actionsToDisable.OrderBy(a => a))
            {
                newHotkeyLines.Add($"  {action}: []");
            }

            // Replace or insert
            if (hotkeysStart != -1)
            {
                lines.RemoveRange(hotkeysStart, hotkeysEnd - hotkeysStart);
                lines.InsertRange(hotkeysStart, newHotkeyLines);
            }
            else
            {
                lines.AddRange(newHotkeyLines);
            }

            File.WriteAllLines(configPath, lines);
            Console.WriteLine($"[Tabby] 已成功清除所有內建快捷鍵，保留核心快速鍵 (備份已儲存至 {Path.GetFileName(backupPath)})");
            Console.WriteLine("  保留的快捷鍵: new-tab, command-selector, copy, paste, next-tab, previous-tab, toggle-last-tab, move-tab-left/right, zoom-in/out/reset, tab-1~9");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Tabby] 清除 Tabby Terminal 快捷鍵時發生錯誤: {ex.Message}");
        }
    }
}
