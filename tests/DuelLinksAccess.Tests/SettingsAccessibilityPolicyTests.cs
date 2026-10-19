using Xunit;
namespace DuelLinksAccess.Tests;

public sealed class SettingsAccessibilityPolicyTests
{
    [Theory]
    [InlineData(true, "已靜音")]
    [InlineData(false, "已解除靜音")]
    [InlineData(null, "已切換靜音設定")]
    public void MuteFeedbackReflectsActualStateAndNeverGuessesUnknownState(bool? muted, string expected)
        => Assert.Equal(expected, LocalizationCatalog.Load("zh-TW").Get(SettingsAccessibilityPolicy.MuteFeedbackKey(muted)));

    [Fact]
    public void EachAudioRowReadsItsOwnNativeMuteStateAfterInvokingTheGame()
    {
        string source = File.ReadAllText(Path.Combine(LocalizationCatalogTests.FindRepositoryRoot(), "src", "DialogHandler.Settings.cs"));
        Assert.Contains("() => vc.OnBgmMute(), () => vc.dst.BgmMute", source);
        Assert.Contains("() => vc.OnSeMute(), () => vc.dst.SeMute", source);
        Assert.Contains("() => vc.OnVoiceMute(), () => vc.dst.VoiceMute", source);
        Assert.True(source.IndexOf("mute();") < source.IndexOf("muted = mutedState()"));
    }
    [Fact]
    public void MutedSliderCannotPreventUnmutingButNativeMuteButtonIsStillGuarded()
    {
        string source = File.ReadAllText(Path.Combine(LocalizationCatalogTests.FindRepositoryRoot(), "src", "DialogHandler.Settings.cs"));
        string audio = source[source.IndexOf("            void AudioRow(")..source.IndexOf("            void ToggleRow(")];
        Assert.Contains("checkSelectable: false", audio);
        Assert.Contains("!Live(muteButton.gameObject)", audio);
        Assert.Contains("!selectable.IsInteractable()", audio);
        Assert.Contains("muteButton.sprite == vc.disabledButton", audio);
        Assert.Contains("announce: false", audio);
        Assert.True(audio.IndexOf("mute();") < audio.IndexOf("ScreenReader.Say"));
    }

    [Fact]
    public void SliderAdjustmentsSpeakOnlyTheNumberAndRetainDisabledSliderGuard()
    {
        string source = File.ReadAllText(Path.Combine(LocalizationCatalogTests.FindRepositoryRoot(), "src", "DialogHandler.cs"));
        string adjust = source[source.IndexOf("        private void AdjustSlider(")..source.IndexOf("        private void ActivateCurrentItem(")];
        Assert.Contains("!slider.IsInteractable()", adjust);
        Assert.Contains("ScreenReader.Say(val.ToString());", adjust);
        Assert.DoesNotContain("if (item.SettingsTarget != null) AnnounceCurrentItem();", adjust);
    }
    [Theory]
    [InlineData("en")]
    [InlineData("zh-TW")]
    public void CombinedAudioAndChoiceRowsExplainEnterAndArrowKeys(string locale)
    {
        var catalog = LocalizationCatalog.Load(locale);
        foreach (string key in new[] { "settings_bgm_mute", "settings_se_mute", "settings_voice_mute", "settings_choice_help" })
            Assert.Contains("Enter", catalog.Get(key));
    }

    [Fact]
    public void AudioRowsUseNativeMuteActionsAndChoicesRequireExplicitConfirmation()
    {
        string source = File.ReadAllText(Path.Combine(LocalizationCatalogTests.FindRepositoryRoot(), "src", "DialogHandler.Settings.cs"));
        Assert.Contains("AudioRow(vc.bgmSlider, vc.bgmButton", source);
        Assert.Contains("AudioRow(vc.seSlider, vc.seButton", source);
        Assert.Contains("AudioRow(vc.voiceSlider, vc.voiceButton", source);
        Assert.DoesNotContain("Button(vc.bgmButton", source);
        Assert.DoesNotContain("Button(vc.seButton", source);
        Assert.DoesNotContain("Button(vc.voiceButton", source);
        string navigation = source[source.IndexOf("rows[rows.Count - 1].AdjustChoice")..source.IndexOf("            void Button(")];
        Assert.DoesNotContain("OnPointerClick", navigation);
        Assert.Contains("!toggle.IsInteractable()", source);
    }
    [Fact]
    public void ControlsRemainInTheirOwnRowsInsteadOfAllSlidersComingFirst()
    {
        var controls = new List<(string name, int[] path)>
        {
            ("Voice slider", new[] { 2, 0 }), ("Music mute", new[] { 0, 1 }),
            ("Effects slider", new[] { 1, 0 }), ("Music slider", new[] { 0, 0 }),
            ("Voice mute", new[] { 2, 1 }), ("Effects mute", new[] { 1, 1 })
        };
        controls.Sort((a, b) => SettingsAccessibilityPolicy.ComparePaths(a.path, b.path));
        Assert.Equal(new[] { "Music slider", "Music mute", "Effects slider", "Effects mute", "Voice slider", "Voice mute" },
            controls.Select(c => c.name));
    }

    [Fact]
    public void NestedControlsCannotMovePastTheirFollowingSection()
    {
        Assert.True(SettingsAccessibilityPolicy.ComparePaths(new[] { 1, 8, 2, 0 }, new[] { 2, 0 }) < 0);
        Assert.True(SettingsAccessibilityPolicy.ComparePaths(new[] { 1 }, new[] { 1, 0 }) < 0);
        Assert.Equal(0, SettingsAccessibilityPolicy.ComparePaths(new[] { 3, 2 }, new[] { 3, 2 }));
    }

    [Fact]
    public void SliderSpeechContainsItsNameValueAndRange()
    {
        var catalog = LocalizationCatalog.Load("zh-TW");
        string label = catalog.Get("settings_slider_item", "背景音樂音量", 70, 0, 100, 3, 12);
        Assert.Contains("背景音樂音量", label);
        Assert.Contains("目前 70", label);
        Assert.Contains("0 到 100", label);
        Assert.Contains("第 3 項", label);
    }

    [Fact]
    public void RadioSpeechIncludesGroupOptionAndSelectedState()
    {
        var catalog = LocalizationCatalog.Load("zh-TW");
        Assert.Equal("相機：玩家視角，已選取", catalog.Get("settings_radio_item", "相機", "玩家視角", catalog.Get("settings_selected")));
        Assert.Equal("角色台詞：開", catalog.Get("settings_toggle_item", "角色台詞", catalog.Get("settings_on")));
    }
}
