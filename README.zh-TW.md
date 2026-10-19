# DuelLinksAccess 繁體中文維護版

本分支以 [zersiax/DuelLinksAccess](https://github.com/zersiax/DuelLinksAccess) v1.4.2、commit `7259d9a1b8bc74ce7e657df790776d81d262eee1` 為基礎，維護繁體中文語系及無障礙操作修正。這是社群修改分支，並非原作者的官方版本。

維護倉庫：https://github.com/james54481/DuelLinksAccess

## 開發狀態

目前為尚未正式發布的開發版。R2～R21 是本機測試包標記，不是 GitHub 正式版本號。功能的驗證範圍與限制請見更新日誌。

組件版本目前仍沿用 1.4.2。正式發布前須同步更新專案版本、`src/Main.cs` 的 `MelonInfo`、發布標籤及套件名稱，並用對應原始碼重新編譯；不要直接將歷次測試 DLL 改名當成正式發布版。

## 內容

- 可擴充的 `en`、`zh-TW` 語系資源，英文為 fallback。
- 保留遊戲自身提供的卡名、敘述及介面文字；中文漢字不再單憑 CJK 字元範圍被排除為日文。
- 票券交換、額外牌組召喚、效果選擇、角色選擇與決鬥數量選單等操作修正。
- 主動技能操作及抽牌替代技能的選擇流程。
- 修正部分模組字典翻譯，保持與遊戲文字一致。
- 新增遊戲選項設定的鍵盤操作支援，包含音量、靜音、決鬥參數及語言下載選單。

詳見 [更新日誌](CHANGELOG.zh-TW.md)、[語系架構說明](docs/LOCALIZATION-zh-TW.md)。

## 編譯與測試

沿用上游的 [編譯說明](docs/BUILDING.md)。可攜式測試不需要遊戲組件：

```powershell
dotnet test tests/DuelLinksAccess.Tests/DuelLinksAccess.Tests.csproj -c Release
```

完整模組編譯需要本機的遊戲及 MelonLoader 產生的組件。先通過測試，再建置；使用自己的安裝路徑：

```powershell
dotnet build DuelLinksAccess.csproj -c Release -p:DuelLinksPath="你的遊戲安裝目錄" -p:DeployToMods=false -p:SkipCopyToMods=true
```

輸出為 `bin/Release/net6.0/DuelLinksAccess.dll`。編譯不代表完成安裝。替換模組前應關閉遊戲並備份原 DLL。

## 發布與授權

保留原作者 Florian Beijers 的 [MIT 授權及著作權聲明](LICENSE.txt)。本分支的修改沿用 MIT 授權；作者與貢獻來源依原始碼及 Git 歷史記錄保留。

第三方函式庫另有各自授權，詳見 [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)。如發佈 Tolk 或 NVDA controller client，必須另行核對其授權、來源及發布材料，不能僅附本專案 MIT 授權。

禁止將遊戲／Unity 組件、反編譯遊戲碼、帳號日誌或個人資料放進本倉庫或發布包。獨立更新程式已有本機開發版，使用方式及發布條件請見 [更新程式說明](docs/UPDATER.zh-TW.md)；尚未正式發布。

正式發布請依 [上游發布流程](docs/RELEASING.md) 檢查，並提供對應 Git tag 的原始碼、更新說明、授權及 SHA-256。歷次本機測試包、診斷資料及測試暫存均不納入 Git 提交。
