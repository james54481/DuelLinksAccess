using System;
namespace DuelLinksAccess
{
    // james54481, 2026-11-06: initial registration uses a distinct name dialog.
    public static class PlayerNameDialogPolicy
    {
        public static bool IsMatch(string rootName, string text) =>
            rootName == "NameDialog" || rootName == "TutorialNameDialog"
            || text?.Contains("玩家名稱輸入", StringComparison.Ordinal) == true;
        public static bool IsConfirmation(string label) => label == "確定" || label == "接受並繼續";
    }
}
