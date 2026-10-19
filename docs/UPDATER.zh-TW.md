# DuelLinksAccess 模組更新程式

獨立的 Windows 64 位元桌面程式，版本 0.1.1。更新來源固定為
`james54481/DuelLinksAccess` 的最新正式 GitHub Release。原始碼位於 `updater/`，沿用本專案 MIT 授權。

下載：[更新程式 0.1.1](https://github.com/james54481/DuelLinksAccess/releases/tag/updater-v0.1.1)。更新程式以獨立標籤發布，不設為模組的最新正式 Release，以免影響 DLL 更新查詢。

## 使用方式

1. 解壓縮完整更新程式壓縮檔，執行 `DuelLinksAccess.Updater.exe`。不必放在遊戲資料夾內，也不必安裝 .NET。
2. 程式會尋找 Steam 的遊戲安裝位置；找不到時，使用「瀏覽」或直接輸入含 `dlpc.exe` 的資料夾。
3. 初次確認路徑後，按「檢查更新」。已記住路徑的使用者下次開啟便會自動檢查。
4. 預設勾選「啟動時自動更新」。若有新版且遊戲已關閉，程式會下載、驗證、備份並替換模組；取消勾選後改由「下載並更新」按鈕操作。勾選狀態會在下一次檢查更新時儲存。
5. 若遊戲仍在執行，程式會提示先關閉遊戲，再按「下載並更新」。程式不會終止或啟動遊戲，也不會在背景長駐等待。
6. 更新有問題時，關閉遊戲，按「還原上次備份」並確認。還原也會先備份目前版本。還原完成後顯示實際還原的 DLL 版本，並從對應 GitHub 標籤取得該版本說明；若未提供說明或連線失敗，會清除新版說明並明確提示，還原結果不受影響。

本程式只更新**已安裝**的模組，不負責安裝 MelonLoader、Tolk、NVDA 或首次安裝模組。不會自行提高權限；若安裝目錄拒絕寫入，會提示使用者手動以系統管理員身分執行。

Tab／Shift+Tab 瀏覽欄位與按鈕，Enter 或空白鍵啟動按鈕。快捷鍵：Alt+B 瀏覽、Alt+A 自動更新、Alt+C 檢查、Alt+U 更新、Alt+R 還原、Alt+X 關閉。狀態與更新說明均為可聚焦、唯讀的文字欄位，可使用 NVDA 閱讀與複製；更新及還原成功時會發送 Windows 無障礙完成通知，包含模組版本號，供 NVDA 即時報讀，不依賴使用者切回視窗。系統不支援通知時改用完成對話框；其他作業完成時焦點移到更新狀態。

本機偏好儲存在 `%LOCALAPPDATA%\DuelLinksAccessUpdater\settings.json`，內容只有遊戲路徑與自動更新選項。

## 發布模組新版時必須做的事

- 使用正式數字版本，例如 `1.4.2.1`，同步修改模組專案版本與 `MelonInfo`，通過測試後重新編譯。
- 在自己的倉庫建立正式 Release，標籤使用 `v1.4.2.1` 等形式。草稿及預發布版本不會更新；目前不支援帶 `-beta` 或 `-zhTW` 後綴的標籤。
- 附加名為 **`DuelLinksAccess.dll`** 的獨立附件。只放 ZIP 或原始碼不足以供更新程式使用。
- 確認 GitHub Release API 的附件 `size` 與 `digest: sha256:...` 有效。缺少摘要時更新程式會停止，不略過驗證。
- Release 標籤必須與 DLL 的組件版本一致。每次提供新版 DLL 都須提高版本；同版本不同雜湊不會自動覆蓋。
- 附上更新日誌、對應來源與授權文件；不要附加遊戲、Unity 組件或帳號日誌。
- 更新程式本身可另外提供 ZIP。它目前只更新模組 DLL，不會自我更新。

正式版本須提供上述 DLL 附件。沒有正式 Release 或缺少正確附件時會顯示說明，不會替換檔案。

## 驗證、備份與限制

- 固定來源為上述倉庫，附件須使用 HTTPS GitHub 下載位置。
- 嚴格檢查下載大小、GitHub SHA-256、有效 .NET 組件名稱 `DuelLinksAccess` 與版本；不載入或執行下載的 DLL 進行驗證。
- 更新前與暫存完成後檢查 `dlpc` 程序。另以檔案鎖阻擋兩個更新程式同時替換；檔案已被鎖定時停止。
- 在 Mods 中建立暫存檔，落盤並驗證後，以同磁碟原子替換產生備份；替換後再驗證，後續步驟失敗時嘗試還原原檔。
- 備份使用 `DuelLinksAccess.backup.*.bak` 與 `.sha256`，避免 MelonLoader 把備份當成另一個模組載入。備份不會自動清除。
- 拒絕更新含符號連結或重新解析點的路徑。只有 `Mods\DuelLinksAccess.dll`、更新程式自己的暫存、鎖定與備份檔會被寫入。
- SHA-256 確認檔案與倉庫發布內容一致，並非作者的獨立數位簽章；維護者須保護 GitHub 帳號與發布權限。此測試版執行檔尚未簽章。
- 自動測試使用合成 DLL 及隔離暫存資料夾，不使用真實遊戲檔案。2026-10-19 共 321 項測試通過（含 38 項更新程式測試），更新程式與模組編譯皆零警告、零錯誤。已檢查預覽視窗的無障礙名稱；仍需 NVDA 使用者試用，以及首次正式 Release 的實際下載驗證。

## 開發與封裝

先執行 `dotnet test tests/DuelLinksAccess.Tests/DuelLinksAccess.Tests.csproj -c Release`。
再執行 `scripts/Publish-Updater.ps1`。腳本會建置免安裝 .NET 的 Windows x64 單一執行檔，附上說明、授權及 SHA-256，並產生 ZIP。它不會建立 GitHub Release，也不會部署到遊戲。

介面檢查可使用 `DuelLinksAccess.Updater.exe --preview`；此模式禁止連線與更新，可按 Alt+T 測試完成通知，不修改檔案。

實作參考：[GitHub Releases API](https://docs.github.com/en/rest/releases/releases)、[Windows Forms 無障礙控制項屬性](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/provide-accessibility-information)。
