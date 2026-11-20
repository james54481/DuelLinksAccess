namespace DuelLinksAccess
{
    public static class ScenarioControlPolicy
    {
        public static bool IsScenario(string screen) => screen == "ScenarioPlayerPart";
        public static string ButtonKey(string screen, string name)
        {
            if (!IsScenario(screen)) return null;
            return name switch
            {
                "FastForwardButton" or "FastForwardButton(Clone)" => "scenario_fast_forward",
                "SkipButton" or "SkipButton(Clone)" => "scenario_skip",
                "AutoButton" or "AutoButton(Clone)" => "scenario_auto",
                "LogButton" or "LogButton(Clone)" => "scenario_log",
                _ => null
            };
        }
    }
}
