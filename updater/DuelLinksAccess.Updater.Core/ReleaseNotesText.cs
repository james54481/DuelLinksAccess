namespace DuelLinksAccess.Updater;

public static class ReleaseNotesText
{
    // Native multiline edit controls need CRLF for predictable arrow-key reading.
    public static string ForDisplay(string? text) => (text ?? "").ReplaceLineEndings("\r\n");
}
