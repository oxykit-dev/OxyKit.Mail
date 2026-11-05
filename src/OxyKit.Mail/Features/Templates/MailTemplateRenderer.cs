using System.Net;
using System.Text.Json;
using Scriban;
using Scriban.Runtime;

namespace OxyKit.Mail.Features.Templates;

/// <summary>
/// Scriban render for mail packs. Body strings from <c>data</c> are HTML-encoded;
/// subject strings are left as plain text.
/// </summary>
internal static class MailTemplateRenderer
{
    public static string Render(string templateText, JsonElement data, bool htmlEncodeStrings)
    {
        Template template;
        try
        {
            template = Template.Parse(templateText);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Template parse failed: {Sanitize(ex.Message)}");
        }

        if (template.HasErrors)
        {
            var message = string.Join("; ", template.Messages.Select(m => m.Message));
            throw new ArgumentException($"Template parse failed: {Sanitize(message)}");
        }

        var scriptObject = new ScriptObject();
        if (data.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in data.EnumerateObject())
            {
                scriptObject[property.Name] = ToScribanValue(property.Value, htmlEncodeStrings);
            }
        }

        var context = new TemplateContext();
        context.PushGlobal(scriptObject);

        try
        {
            return template.Render(context);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Template render failed: {Sanitize(ex.Message)}");
        }
    }

    private static object? ToScribanValue(JsonElement element, bool htmlEncodeStrings) =>
        element.ValueKind switch
        {
            JsonValueKind.Object => ToScriptObject(element, htmlEncodeStrings),
            JsonValueKind.Array => ToScriptArray(element, htmlEncodeStrings),
            JsonValueKind.String => htmlEncodeStrings
                ? WebUtility.HtmlEncode(element.GetString() ?? string.Empty)
                : element.GetString() ?? string.Empty,
            JsonValueKind.Number when element.TryGetDecimal(out var decimalValue) => decimalValue,
            JsonValueKind.Number when element.TryGetInt64(out var longValue) => longValue,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            _ => htmlEncodeStrings
                ? WebUtility.HtmlEncode(element.ToString())
                : element.ToString(),
        };

    private static ScriptObject ToScriptObject(JsonElement element, bool htmlEncodeStrings)
    {
        var obj = new ScriptObject();
        foreach (var property in element.EnumerateObject())
        {
            obj[property.Name] = ToScribanValue(property.Value, htmlEncodeStrings);
        }

        return obj;
    }

    private static ScriptArray ToScriptArray(JsonElement element, bool htmlEncodeStrings)
    {
        var array = new ScriptArray();
        foreach (var item in element.EnumerateArray())
        {
            array.Add(ToScribanValue(item, htmlEncodeStrings));
        }

        return array;
    }

    private static string Sanitize(string message) =>
        message.Length <= 500 ? message : message[..500] + "…";
}
