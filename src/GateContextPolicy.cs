namespace DuelLinksAccess
{
    internal static class GateContextPolicy
    {
        internal static string Key(bool? isEvent) => isEvent.HasValue
            ? (isEvent.Value ? "gate_context_event" : "gate_context_normal")
            : "gate_context_unknown";
    }
}
