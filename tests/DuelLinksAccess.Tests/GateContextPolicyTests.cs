using Xunit;
namespace DuelLinksAccess.Tests;
public class GateContextPolicyTests
{
    [Theory]
    [InlineData(true, "gate_context_event")]
    [InlineData(false, "gate_context_normal")]
    [InlineData(null, "gate_context_unknown")]
    public void UsesNativeKindAndKeepsUnknown(bool? isEvent, string key)
        => Assert.Equal(key, GateContextPolicy.Key(isEvent));
}
