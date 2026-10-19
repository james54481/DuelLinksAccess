using Xunit;
namespace DuelLinksAccess.Tests;
public sealed class AccessibleUiPolicyTests
{
 [Theory]
 [InlineData("確定", "confirm")][InlineData("確認", "confirm")][InlineData("是", "confirm")][InlineData("交換", "confirm")]
 [InlineData("交易", "confirm")][InlineData(" 交易 ", "confirm")][InlineData("交易說明", null)]
 [InlineData("取消", "cancel")][InlineData("否", "cancel")][InlineData("關閉", "cancel")][InlineData("Cancel", "cancel")]
 [InlineData("Button", null)][InlineData("今天", null)]
 public void ConfirmationActionIsUnambiguous(string label,string expected) => Assert.Equal(expected, AccessibleUiPolicy.ConfirmationAction(label));
 [Theory][InlineData("ButtonYes","confirm")][InlineData("btnCancel","cancel")][InlineData("ButtonSet",null)][InlineData("Yesterday",null)]
 public void SemanticFallbackDoesNotGuessFromPosition(string name,string expected)=>Assert.Equal(expected,AccessibleUiPolicy.SemanticAction(name));
 [Theory][InlineData("否",true)][InlineData("是",true)][InlineData("確定",true)][InlineData("1",false)][InlineData(" ",false)]
 public void ChineseSingleCharacterButtonsAreReadable(string text,bool expected)=>Assert.Equal(expected,AccessibleUiPolicy.IsMeaningfulLabel(text));
 [Fact] public void MixedTextKeepsChineseBodyAndRemovesDuplicates()=>Assert.Equal("今天。活動公告。繁體中文正文",AccessibleUiPolicy.CombineText(new[]{"今天","活動公告"},new[]{"今天","繁體中文正文"}));
 [Theory][InlineData(true,true,false,true)][InlineData(false,true,false,false)][InlineData(true,false,false,false)][InlineData(true,true,true,false)]
 public void PurchaseRequiresAvailableNativeControl(bool active,bool ready,bool soldOut,bool expected)=>Assert.Equal(expected,AccessibleUiPolicy.CanPurchase(active,ready,soldOut));
}
