using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Il2CppYgomGame.Shop;
namespace DuelLinksAccess
{
 public static class PurchaseAccessibility
 {
  public static bool IsAvailable(PurchaseButton button)
  {
   if(button==null)return false;
   try {
    bool sold=button.TryCast<PurchaseButtonSoldOut>()!=null;
    var control=button.ButtonObj?.GetComponent<Selectable>() ?? button.gameObject.GetComponentInChildren<Selectable>();
    return AccessibleUiPolicy.CanPurchase(button.gameObject.activeInHierarchy,
      button.isReady && (control==null || control.interactable),sold);
   }catch{return false;}
  }
  public static List<PurchaseButton> Find(GameObject root)
  {
   var result=new List<PurchaseButton>();if(root==null)return result;
   try{foreach(var b in root.GetComponentsInChildren<PurchaseButton>(false))if(IsAvailable(b))result.Add(b);}catch{}
   return result;
  }
  public static string Label(PurchaseButton button)
  {
   string text=AccessibleUiText.Read(button.gameObject);
   string kind=button.TryCast<PurchaseButtonGem>()!=null ? Loc.Get("ui_purchase_gems")
    : button.TryCast<PurchaseButtonSale>()!=null ? Loc.Get("ui_purchase_paid") : Loc.Get("ui_purchase_action");
   return string.IsNullOrWhiteSpace(text)?kind:kind+"，"+text;
  }
  public static bool Activate(GameObject go)
  {
   var b=go?.GetComponent<PurchaseButton>();if(b==null)return false;
   if(!IsAvailable(b)){ScreenReader.Say(Loc.Get("ui_action_unavailable"));return true;}
   b.OnTouch();return true;
  }
 }
}
