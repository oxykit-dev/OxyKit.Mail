namespace OxyKit.Mail.Features.Templates;

public sealed class MailTemplateOptions
{
    public const string SectionName = "MailTemplates";

    /// <summary>Root directory for mail template packs (relative to content root or absolute).</summary>
    public string TemplatesRoot { get; init; } = "templates";

    public int MaxSubjectChars { get; init; } = 10_000;
    public int MaxBodyChars { get; init; } = 500_000;
    public int MaxTemplatePackBytes { get; init; } = 2 * 1024 * 1024;
}
