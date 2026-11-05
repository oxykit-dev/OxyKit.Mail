namespace OxyKit.Mail.Features.Templates;

public sealed record MailTemplateInfo(string Id);

public sealed record MailTemplateDetail(string Id, IReadOnlyList<string> Files);

public sealed record MailTemplateListResponse(IReadOnlyList<MailTemplateInfo> Templates);

public sealed record MailTemplateDetailResponse(string Id, IReadOnlyList<string> Files);

public sealed record MailRenderedTemplate(string Subject, string HtmlBody);
