# Windows Helper (windows-helper)

**Windows 終端機設定助手 (Windows Terminal Helper)**

這是一個使用 **.NET 9.0 (C#)** 開發的 Windows 主控台工具，用於管理 **Tabby Terminal** 與 **Wave Terminal** 的設定。目前提供兩大子功能：

* **字型 (font)** — 掃描系統等寬字型，並一鍵套用至終端機設定檔
* **快捷鍵 (hotkeys)** — 清除 Tabby 內建快捷鍵，僅保留核心快速鍵 (含縮放 Ctrl-=/Ctrl--/Ctrl-0)

> 兩項功能並列，沒有主從關係；想用哪一個就跑對應的子命令。

---

## 🚀 功能特色

1. **等寬字型精準偵測**：
   * 透過 GDI+ `Graphics.MeasureString` 精確測量字元（如 `i`, `W`, `m`, ` `, `1`）之寬度，判定字型是否為等寬字型，避免比例字型導致終端機排版混亂。
2. **自動配置終端機字型**：
   * **Tabby Terminal**：自動尋找並安全修改 `%APPDATA%\tabby\config.yaml`。
   * **Wave Terminal**：自動尋找並安全修改 `~/.config/waveterm/settings.json`。
3. **Tabby 快捷鍵清理**：
   * 將 Tabby 內建快捷鍵批次設為空，僅保留核心快速鍵 (新增分頁、命令選擇器、複製/貼上、上下分頁、終端機縮放、分頁1~9快速切換)，可選擇是否額外保留分頁移動快速鍵。
4. **安全備份機制**：
   * 修改任何終端機的設定檔前，皆會自動生成 `.bak` 備份檔（如 `config.yaml.bak`），避免資料遺失。
5. **雙操作模式**：
   * **互動式選單**：直接執行時，以並列式選單列出所有子功能，引導使用者選擇。
   * **CLI 子命令模式**：支援 `helper <command> [subcommand] [args...]`，便於腳本呼叫或快速設定。

---

## 🛠️ 開發環境與技術棧

* **核心**：.NET 9.0 (C#)
* **依賴套件**：`System.Drawing.Common` (v10.0.9)
* **目標平台**：Windows (`net9.0-windows`)

---

## 📂 專案檔案結構

* [Program.cs](file:///C:/Users/kjell/ai/helper/Program.cs)：包含 CLI dispatcher、字型偵測、設定檔讀寫、互動選單的完整邏輯。
* [windows-helper.csproj](file:///C:/Users/kjell/ai/helper/windows-helper.csproj)：專案設定檔與相依套件宣告。
* [.gitignore](file:///C:/Users/kjell/ai/helper/.gitignore)：過濾 .NET 建置產出物。

---

## 📖 使用說明

請在專案目錄下，以命令提示字元或 PowerShell 執行以下指令：

### 1. 互動式模式（推薦）

不帶任何參數執行：

```powershell
dotnet run
```

主選單會並列顯示「字型設定 / 快捷鍵管理 / 顯示說明」三個選項，輸入數字即可進入對應子選單。

### 2. CLI 子命令模式

#### 顯示說明

```powershell
dotnet run -- --help
dotnet run -- font --help
dotnet run -- hotkeys --help
```

#### `font` — 字型管理

```powershell
# 列出系統中所有等寬字型
dotnet run -- font list

# 列出系統中安裝的所有字型名稱 (含比例字型)
dotnet run -- font list --all

# 將字型同時套用到 Tabby 與 Wave
dotnet run -- font set "Cascadia Code"

# 僅針對 Tabby Terminal 設定字型
dotnet run -- font set "Cascadia Code" --target tabby

# 僅針對 Wave Terminal 設定字型
dotnet run -- font set "Ubuntu Mono" --target wave

# 進入互動式字型挑選流程
dotnet run -- font interactive
```

#### `hotkeys` — 快捷鍵管理

```powershell
# 清除 Tabby 快捷鍵，僅保留核心快速鍵
dotnet run -- hotkeys clean

# 清除時額外保留分頁移動快速鍵 (Ctrl-Shift-PageUp/Down)
dotnet run -- hotkeys clean --keep-tab-movement
```

> [!NOTE]
> 在使用 `dotnet run` 傳遞參數時，前方的 `--` 是為了告訴 `dotnet` 主程式將後續的所有參數直接傳遞給本工具。
