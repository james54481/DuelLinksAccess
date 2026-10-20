# DuelLinksAccess 更新程式 0.1.2

以已發布的 0.1.1 為基礎，沿用 GitHub Releases 檢查、SHA-256、DLL 版本驗證與既有備份機制。本版採完全手動更新，提供 GitHub 正式歷史版本安裝與離線緊急還原。

## 啟動與檢查更新

1. 解壓完整 ZIP，執行 `DuelLinksAccess.Updater.exe`。不需另外安裝 .NET。
2. 啟動只讀取已儲存的遊戲路徑及本機 DLL 版本。不連線 GitHub，也不下載或安裝。
3. 沒有已存路徑時，使用「瀏覽」或輸入遊戲安裝根目錄，例如 `C:\Program Files (x86)\Steam\steamapps\common\Yu-Gi-Oh! Duel Links`。不要選 Mods 子資料夾。
4. 按「檢查更新」才連接指定倉庫 `james54481/DuelLinksAccess` 的最新正式模組 Release。
5. 沒有新版時顯示「目前已經是最新版本」及目前版本號。找到新版時顯示新版版本號、完整更新說明，並將焦點移到說明文字欄。
6. NVDA 會收到「發現新版本，請先閱讀更新說明，再決定是否下載安裝」的無障礙通知。說明亦保留在唯讀文字欄，可用方向鍵逐行閱讀。
7. 閱讀後按「下載並更新」。此時才顯示「確定要下載並安裝此版本嗎？」及目前／目標版本，預設選擇「否」。
8. 選「是」後才下載、驗證、備份並安裝。選「否」不改動模組，保留更新說明並返回說明欄。

已完全移除自動更新核取方塊與自動安裝邏輯。舊設定檔中的 `Automatic` 欄位會被忽略；下次手動檢查或選好路徑儲存時，只保留 `GamePath`。

## 歷史版本管理

- 按「歷史版本管理」才連線指定倉庫的 GitHub Releases，逐頁讀取所有正式模組版本，不顯示草稿、預發布或更新器自身的 Release。
- 依數字版本由新到舊排列，顯示版本號與本機時區的發布日期。上下鍵選取版本，Tab 閱讀完整更新說明，再 Tab 到「下載並安裝選定版本」。
- 說明與清單一起取得；切換項目不需再次連線。缺少唯一的 DLL、有效 SHA-256 或安全下載位置者，保留更新說明並顯示無法安裝原因，停用安裝按鈕。
- 按下載按鈕後顯示目前及目標版本。允許升版、降版與同版本重裝，但一律先確認；預設「否」，選「否」或按 Escape／取消均保留清單且不修改 DLL。
- 確認後才下載，驗證檔名、指定倉庫與標籤、大小、SHA-256、DLL 組件名稱與版本，再檢查遊戲已關閉、備份目前 DLL 並安全替換。
- 清單連線或解析失敗時明確顯示錯誤，不安裝任何檔案。不使用不完整的分頁結果。

## 離線緊急還原

- 「還原上次備份」仍從本機最近的有效備份還原，完全不連線 GitHub。
- 先顯示目前版本、目標版本與備份時間供確認，再重新驗證路徑、SHA-256 與 DLL 組件版本。
- 確認遊戲已關閉後，先備份目前 DLL，再還原；原有備份保留，無效備份不刪除。
- 完成後顯示還原版本。若本次工作階段已取得該版更新說明則顯示快取，否則明確提示無快取，仍不連線。

## 鍵盤與 NVDA

Tab／Shift+Tab 瀏覽欄位與按鈕，Enter 或空白鍵啟動按鈕。
Alt+B 瀏覽、Alt+C 檢查更新、Alt+U 下載並更新、Alt+R 還原上次備份、Alt+H 歷史版本管理、Alt+X 關閉。
歷史管理內使用上下鍵選取、Tab 閱讀說明、Alt+I 下載並安裝選定版本、Esc 或 Alt+X 關閉；確認框內 Escape 取消。

更新與還原成功時會送出含版本號的 Windows 無障礙完成通知；不支援通知時顯示完成對話框。可以用 `--preview` 預覽主視窗，並按 Alt+T 測試完成通知；預覽模式不連線或修改遊戲。

## 檔案、權限與安全檢查

只更新已安裝的 `Mods\DuelLinksAccess.dll`，不負責首次安裝模組或 MelonLoader。
安裝前仍須通過下載大小、GitHub SHA-256、DLL 組件名稱與版本核對，並檢查遊戲執行狀態。驗證只讀取 DLL 中繼資料，不執行 DLL。

沿用同磁碟暫存驗證、原子替換與 `.bak` 備份，備份附 `.sha256`。不以 `.dll` 作為備份副檔名，避免被當作另一份模組載入。兩個更新程式同時執行時，以檔案鎖阻擋重複替換。符號連結或重新解析點路徑不允許更新。

偏好設定位置為 `%LOCALAPPDATA%\DuelLinksAccessUpdater\settings.json`，只保存遊戲路徑。程式不自行提權；權限不足時會提示使用者手動以系統管理員身分執行。

本程式只更新模組，不會自我更新。GitHub 帳號、發布來源及檔案安全仍由維護者負責；SHA-256 不等於作者數位簽章。

## 開發與發布

原始碼位於 `updater/`，沿用本專案 MIT 授權，內附微軟 .NET 元件聲明。
先執行 `dotnet test tests/DuelLinksAccess.Tests/DuelLinksAccess.Tests.csproj -c Release`，再執行 `scripts/Publish-Updater.ps1` 封裝 Windows x64 ZIP。
此腳本不會部署到遊戲或建立 GitHub Release。

更新程式應以獨立標籤發布，不能設為模組 latest；模組更新只接受數字版本標籤，如 `v1.4.2.2`，且 Release 必須提供名稱為 `DuelLinksAccess.dll` 的獨立附件，含有效 GitHub SHA-256 摘要。DLL 組件版本必須與標籤相符。

正式下載：[更新程式 0.1.2](https://github.com/james54481/DuelLinksAccess/releases/tag/updater-v0.1.2)。下載 `DuelLinksAccess-Updater-0.1.2-win-x64.zip`，解壓後執行；舊版 0.1.1 仍保留。

## 驗證狀態（2026-10-20）

- 354 項自動測試通過，涵蓋 GitHub 分頁、正式版本篩選與數字排序、下載驗證、升版、降版、同版重裝及備份還原。
- 在隔離測試目錄注入替換後驗證失敗，確認升版、降版及重裝都能回復原 DLL；亦測試下載失敗、遊戲執行中及檔案鎖阻擋。
- 實際 GitHub 清單載入 1.4.2.2、1.4.2.1，已核對方向鍵切換、Tab 閱讀、Enter 確認及取消流程。
- 真實遊戲 DLL 與 MelonLoader 未修改；完整替換測試僅使用隔離目錄內的測試 DLL。
- 使用者已回報此測試版操作通過，確認發布；不代表所有電腦環境均已驗證。
- 原 0.1.2 測試包與已發布的 0.1.1 保留；正式包為 `DuelLinksAccess-Updater-0.1.2-win-x64.zip`。

參考：[GitHub Releases API](https://docs.github.com/en/rest/releases/releases)、[Windows Forms 無障礙資訊](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/provide-accessibility-information)。
