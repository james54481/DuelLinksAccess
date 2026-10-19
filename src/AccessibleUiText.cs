using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace DuelLinksAccess
{
 public static class AccessibleUiText
 {
  public static string Read(GameObject root)
  {
   if(root==null)return "";
   var parts=new List<string>();
   try { foreach(var t in root.GetComponentsInChildren<Il2CppYgomSystem.UI.YgomTextAccessor>(true))
    if(t!=null && t.gameObject!=null && t.gameObject.activeInHierarchy)parts.Add(t.text); } catch { }
   try { foreach(var t in root.GetComponentsInChildren<Text>(true))
    if(t!=null && t.gameObject!=null && t.gameObject.activeInHierarchy)parts.Add(t.text); } catch { }
   return AccessibleUiPolicy.CombineText(parts);
  }
  public static string NoticeLabel(GameObject go,string fallback)
  {
   string text=Read(go);
   // A notice row can render the date on the button and title beside it.
   // Stay within a small row; never concatenate an entire notice list.
   var scope=go.transform.parent;
   for(int depth=0;scope!=null && depth<2;depth++,scope=scope.parent)
   {
    try {
     int controls=0;
     foreach(var control in scope.GetComponentsInChildren<Selectable>(false))
      if(control!=null && control.gameObject.activeInHierarchy)controls++;
     if(controls>2)break;
     string row=Read(scope.gameObject);
     if(!string.IsNullOrWhiteSpace(row))text=row;
    }catch{break;}
   }
   return string.IsNullOrWhiteSpace(text)?fallback:text;
  }
 }
}
