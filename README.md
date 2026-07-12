# Windows Helper (windows-helper)

Windows 字型列出與終端機字型自動設定工具 (Windows Font Lister & Terminal Configurator)

這是一個使用 **.NET 9.0 (C#)** 開發的 Windows 主控台工具。它能自動掃描 Windows 系統中安裝的所有字型，篩選出適合終端機使用的**等寬字型 (Monospace Fonts)**，並能一鍵或透過指令行直接將指定的字型設定給 **Tabby Terminal** 與 **Wave Terminal**。

---

## 🚀 功能特色

1. **等寬字型精準偵測**：
   * 透過 GDI+ `Graphics.MeasureString` 精確測量字元（如 `i`, `W`, `m`, ` `, `1`）之寬度，判定字型是否為等寬字型，避免比例字型導致終端機排版混亂。
2. **自動配置終端機字型**：
   * **Tabby Terminal**：自動尋找並安全修改 `%APPDATA%\tabby\config.yaml`。
   * **Wave Terminal**：自動尋找並安全修改 `~/.config/waveterm/settings.json`。
3. **安全備份機制**：
   * 修改任何終端機的設定檔前，皆會自動生成 `.bak` 備份檔（如 `config.yaml.bak`），避免資料遺失。
4. **雙操作模式**：
   * **互動式選單**：直接執行時，會以清單列出所有可用等寬字型，並引導使用者輸入數字與選取終端機。
   * **CLI 參數模式**：支援指令行引數，便於腳本呼叫或快速設定。

---

## 🛠️ 開發環境與技術棧

* **核心**：.NET 9.0 (C#)
* **依賴套件**：`System.Drawing.Common` (v10.0.9)
* **目標平台**：Windows (`net9.0-windows`)

---

## 📂 專案檔案結構

* [Program.cs](file:///C:/Users/kjell/ai/helper/Program.cs)：包含字型偵測、設定檔讀寫、CLI 參數處理及互動選單的完整邏輯。
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
程式會引導您完成設定。

### 2. 指令行參數模式 (CLI)
如果您想快速執行或撰寫腳本：

```powershell
# 顯示說明訊息與所有可用參數
dotnet run -- --help

# 列出系統中所有等寬字型
dotnet run -- --list

# 列出系統中安裝的所有字型名稱 (包括比例字型)
dotnet run -- --all-fonts

# 指定將 Cascadia Code 字型同時套用到 Tabby 與 Wave
dotnet run -- --font "Cascadia Code"

# 僅針對 Tabby Terminal 設定字型
dotnet run -- --font "Cascadia Code" --terminal tabby

# 僅針對 Wave Terminal 設定字型
dotnet run -- --font "Ubuntu Mono" --terminal wave
```

> [!NOTE]
> 在使用 `dotnet run` 傳遞參數時，前方的 `--` 是為了告訴 `dotnet` 主程式將後續的所有參數直接傳遞給本工具。
