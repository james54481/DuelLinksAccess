using System;
using System.Collections.Generic;
namespace DuelLinksAccess
{
 public static class AccessibleUiPolicy
 {
  public static string ConfirmationAction(string label)
  {
   switch ((label ?? "").Trim().ToUpperInvariant())
   {
    case "YES": case "OK": case "CONFIRM": case "TRADE": case "EXCHANGE":
    case "確定": case "確認": case "是": case "交換": case "兌換": case "交易": case "はい": return "confirm";
    case "NO": case "CANCEL": case "CLOSE": case "取消": case "否": case "關閉":
    case "いいえ": case "キャンセル": return "cancel";
    default: return null;
   }
  }
  public static string SemanticAction(string name)
  {
   string n=(name ?? "").Replace("_", "").Replace("-", "").ToLowerInvariant();
   switch(n)
   {
    case "yesbutton": case "buttonyes": case "btnyes": case "okbutton": case "buttonok": case "btnok": case "confirmbutton": case "buttonconfirm": case "btnconfirm": return "confirm";
    case "nobutton": case "buttonno": case "btnno": case "cancelbutton": case "buttoncancel": case "btncancel": case "closebutton": case "buttonclose": case "btnclose": return "cancel";
    default: return null;
   }
  }
  public static string CombineText(params IEnumerable<string>[] sources)
  {
   var seen=new HashSet<string>(StringComparer.Ordinal);var parts=new List<string>();
   foreach(var source in sources){if(source==null)continue;foreach(var value in source){var text=SpeechTextFormatter.StripRichText(value)?.Trim();if(string.IsNullOrWhiteSpace(text))continue;if(seen.Add(text))parts.Add(text);}}
   return string.Join("。",parts);
  }
  public static bool IsMeaningfulLabel(string text)
  {
   if(string.IsNullOrWhiteSpace(text))return false;
   text=text.Trim();
   bool digits=true;foreach(char c in text)if(!char.IsDigit(c)){digits=false;break;}
   return !digits && (text.Length > 1 || char.IsLetter(text[0]));
  }
  public static bool CanPurchase(bool active,bool ready,bool soldOut)=>active && ready && !soldOut;
 }
}
