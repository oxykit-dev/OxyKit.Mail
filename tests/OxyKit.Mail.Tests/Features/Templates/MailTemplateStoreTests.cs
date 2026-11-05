using System.IO.Compression;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OxyKit.Mail.Features.Templates;

namespace OxyKit.Mail.Tests.Features.Templates;

public sealed class MailTemplateStoreTests : IDisposable
{
    private readonly string _root;

    public MailTemplateStoreTests()
    {
        _root = Path.Combine(
            Path.GetTempPath(),
            "oxykit-mail-templates-" + Guid.NewGuid().ToString("N")
        );
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public async Task SaveRenderDelete_RoundTrip()
    {
        var store = CreateStore();
        await using var zip = CreateZip(
            ("subject.txt", "Hi {{ name }}"),
            ("body.html", "<p>Hello {{ name }}</p>")
        );

        var ct = TestContext.Current.CancellationToken;
        var saved = await store.SaveFromZipAsync("greet", zip, ct);
        saved.IsError.Should().BeFalse();

        var data = JsonSerializer.SerializeToElement(new { name = "Ada" });
        var rendered = store.Render("greet", data);
        rendered.IsError.Should().BeFalse();
        rendered.Value.Subject.Should().Be("Hi Ada");
        rendered.Value.HtmlBody.Should().Contain("Hello Ada");

        var deleted = await store.DeleteAsync("greet", ct);
        deleted.IsError.Should().BeFalse();
        store.Describe("greet").IsError.Should().BeTrue();
    }

    [Fact]
    public void Render_HtmlEncodesBodyStrings()
    {
        var dir = Path.Combine(_root, "safe");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "subject.txt"), "S");
        File.WriteAllText(Path.Combine(dir, "body.html"), "<p>{{ name }}</p>");

        var rendered = CreateStore()
            .Render("safe", JsonSerializer.SerializeToElement(new { name = "<b>x</b>" }));

        rendered.IsError.Should().BeFalse();
        rendered.Value.HtmlBody.Should().Contain("&lt;b&gt;x&lt;/b&gt;");
    }

    [Fact]
    public void Render_RejectsSubjectLineBreaks()
    {
        var dir = Path.Combine(_root, "subj");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "subject.txt"), "Hi {{ name }}");
        File.WriteAllText(Path.Combine(dir, "body.html"), "<p>ok</p>");

        var rendered = CreateStore()
            .Render("subj", JsonSerializer.SerializeToElement(new { name = "Ada\r\nBcc: evil@example.com" }));

        rendered.IsError.Should().BeTrue();
        rendered.FirstError.Description.Should().Contain("single line");
    }

    [Fact]
    public async Task Save_RejectsOversizedBody()
    {
        var store = CreateStore(maxBodyChars: 20);
        await using var zip = CreateZip(
            ("subject.txt", "Hi"),
            ("body.html", new string('x', 100))
        );

        var saved = await store.SaveFromZipAsync("big", zip, TestContext.Current.CancellationToken);
        saved.IsError.Should().BeTrue();
        saved.FirstError.Description.Should().Contain("body.html");
        Directory.Exists(Path.Combine(_root, "big")).Should().BeFalse();
    }

    private MailTemplateStore CreateStore(int maxBodyChars = 50_000) =>
        new(
            Options.Create(
                new MailTemplateOptions
                {
                    TemplatesRoot = _root,
                    MaxSubjectChars = 10_000,
                    MaxBodyChars = maxBodyChars,
                    MaxTemplatePackBytes = 1_000_000,
                }
            ),
            new TestHostEnvironment()
        );

    private static MemoryStream CreateZip(params (string Path, string Content)[] files)
    {
        var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (path, content) in files)
            {
                using var writer = new StreamWriter(archive.CreateEntry(path).Open(), Encoding.UTF8);
                writer.Write(content);
            }
        }

        ms.Position = 0;
        return ms;
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "OxyKit.Mail.Tests";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
