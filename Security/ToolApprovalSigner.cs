using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AIHappey.Vercel.Models;

namespace AIHappey.Vercel.Security;

public sealed class ToolApprovalSignatureException() : Exception(
    "Tool approval signature is missing or invalid. Request a new tool approval; unsigned legacy approvals cannot be submitted.");

/// <summary>
/// Authenticates chat UI tool requests, not the client's decision or output.
/// A null subject is explicitly issuer-only (HeaderAuth); a nonempty subject
/// binds the request to a user (AzureAuth). The two scopes cannot verify each other.
/// Host adapters own configuration and identity resolution. No expiry or replay ledger.
/// </summary>
public sealed class ToolApprovalSigner
{
    private const string Prefix = "v1.";
    private readonly string secret;
    private readonly string? subject;

    public ToolApprovalSigner(string secret, string? subject = null)
    {
        if (string.IsNullOrWhiteSpace(secret))
            throw new ArgumentException("A tool approval signing secret is required.", nameof(secret));
        if (subject is not null && string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("A user-bound approval requires a nonempty subject.", nameof(subject));
        this.secret = secret.Trim();
        this.subject = subject;
    }

    public void ValidateIncoming(IEnumerable<UIMessage> messages)
    {
        foreach (var message in messages)
        foreach (var tool in message.Parts.OfType<ToolInvocationPart>())
        {
            if (tool.Approval is null)
            {
                if (tool.State is "approval-requested" or "approval-responded")
                    throw new ToolApprovalSignatureException();
                continue;
            }

            var approval = tool.Approval;
            if (message.Role != Role.assistant
                || approval.Signature is not { Length: 67 } signature || !signature.StartsWith(Prefix, StringComparison.Ordinal))
                throw new ToolApprovalSignatureException();

            var name = tool.Type is "dynamic-tool" or "tool-call" ? tool.ToolName
                : tool.Type.StartsWith("tool-", StringComparison.Ordinal) ? tool.Type[5..] : null;
            if (string.IsNullOrWhiteSpace(name)
                || tool.ToolName is not null && !string.Equals(tool.ToolName, name, StringComparison.Ordinal))
                throw new ToolApprovalSignatureException();

            try
            {
                var expected = CreateSignature(approval.Id, new ToolCallPart
                {
                    ToolCallId = tool.ToolCallId, ToolName = name, Input = tool.Input,
                    ProviderExecuted = tool.ProviderExecuted, Title = tool.Title
                });
                if (!CryptographicOperations.FixedTimeEquals(
                    Convert.FromHexString(signature[Prefix.Length..]), Convert.FromHexString(expected[Prefix.Length..])))
                    throw new ToolApprovalSignatureException();
            }
            catch (Exception error) when (error is FormatException or JsonException or ArgumentException or OverflowException)
            {
                throw new ToolApprovalSignatureException();
            }
        }
    }

    public string CreateSignature(string approvalId, ToolCallPart tool)
    {
        if (string.IsNullOrWhiteSpace(approvalId) || string.IsNullOrWhiteSpace(tool.ToolCallId)
            || string.IsNullOrWhiteSpace(tool.ToolName))
            throw new ToolApprovalSignatureException();

        // Keep the established v1 bytes for user-bound approvals. A JSON null
        // subject is distinct from every user string, including a literal "null".
        var key = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes("AIHappey.ToolApproval.Key.v1"));
        try
        {
            using var buffer = new MemoryStream();
            using (var writer = new Utf8JsonWriter(buffer))
            {
                writer.WriteStartArray();
                writer.WriteStringValue("AIHappey.ToolApproval.v1");
                writer.WriteStringValue(subject);
                writer.WriteStringValue(approvalId);
                writer.WriteStringValue(tool.ToolCallId);
                writer.WriteStringValue(tool.ToolName);
                writer.WriteStringValue(tool.Title);
                writer.WriteBooleanValue(tool.ProviderExecuted == true);
                WriteCanonical(writer, JsonSerializer.SerializeToElement(tool.Input, JsonSerializerOptions.Web));
                writer.WriteEndArray();
            }
            return Prefix + Convert.ToHexString(HMACSHA256.HashData(key, buffer.ToArray())).ToLowerInvariant();
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    public async IAsyncEnumerable<UIMessagePart> SignOutgoing(
        IAsyncEnumerable<UIMessagePart> stream,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var tools = new Dictionary<string, ToolCallPart>(StringComparer.Ordinal);
        var pending = new Dictionary<string, List<UIMessagePart>>(StringComparer.Ordinal);
        UIMessagePart Sign(UIMessagePart part, string callId, string approvalId)
        {
            var signature = CreateSignature(approvalId, tools[callId]);
            return part switch
            {
                ToolApprovalRequestUIPart value => new ToolApprovalRequestUIPart
                {
                    ApprovalId = value.ApprovalId, ToolCallId = value.ToolCallId, Signature = signature,
                    ApprovalDescriptor = value.ApprovalDescriptor, InputSchemaInput = value.InputSchemaInput,
                    Reason = value.Reason, IsAutomatic = value.IsAutomatic
                },
                ToolApprovalRequestPart value => new ToolApprovalRequestPart
                { ApprovalId = value.ApprovalId, ToolCallId = value.ToolCallId, Signature = signature },
                _ => throw new ToolApprovalSignatureException()
            };
        }

        await foreach (var part in stream.WithCancellation(cancellationToken))
        {
            if (part is ToolCallPart tool)
            {
                tools[tool.ToolCallId] = tool;
                yield return part;
                if (pending.Remove(tool.ToolCallId, out var waiting))
                    foreach (var approval in waiting)
                    {
                        var id = approval is ToolApprovalRequestUIPart ui ? ui.ApprovalId : ((ToolApprovalRequestPart)approval).ApprovalId;
                        yield return Sign(approval, tool.ToolCallId, id);
                    }
                continue;
            }

            // The browser applies output events to the same tool invocation. In
            // agent chat the input deliberately suppresses browser execution,
            // then the output restores ProviderExecuted=false before approval.
            // Sign that effective UI state without changing any emitted events.
            var output = part switch
            {
                ToolOutputAvailablePart value => (value.ToolCallId, value.ProviderExecuted),
                ToolOutputErrorPart value => (value.ToolCallId, value.ProviderExecuted),
                _ => ((string?, bool?))(null, null)
            };
            if (output.Item1 is { } outputId && output.Item2 is { } executed
                && tools.TryGetValue(outputId, out var original))
                tools[outputId] = new ToolCallPart
                {
                    ToolCallId = original.ToolCallId, ToolName = original.ToolName,
                    Title = original.Title, Input = original.Input, ProviderExecuted = executed
                };

            var (callId, approvalId) = part switch
            {
                ToolApprovalRequestUIPart value => (value.ToolCallId, value.ApprovalId),
                ToolApprovalRequestPart value => (value.ToolCallId, value.ApprovalId),
                _ => ((string?, string?))(null, null)
            };
            if (callId is not null)
            {
                if (tools.ContainsKey(callId)) yield return Sign(part, callId, approvalId!);
                else
                {
                    if (!pending.TryGetValue(callId, out var waiting)) pending[callId] = waiting = [];
                    waiting.Add(part);
                }
            }
            else
            {
                if (part is FinishUIPart && pending.Count > 0)
                    throw new InvalidOperationException("Cannot sign a tool approval without its complete tool input.");
                yield return part;
            }
        }
        if (pending.Count > 0)
            throw new InvalidOperationException("Cannot sign a tool approval without its complete tool input.");
    }

    private static void WriteCanonical(Utf8JsonWriter writer, JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var property in value.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
                {
                    if (!names.Add(property.Name)) throw new JsonException("Duplicate tool input property.");
                    writer.WritePropertyName(property.Name); WriteCanonical(writer, property.Value);
                }
                writer.WriteEndObject(); break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in value.EnumerateArray()) WriteCanonical(writer, item);
                writer.WriteEndArray(); break;
            case JsonValueKind.Number:
                // Normalize decimal notation without rounding through double.
                var raw = value.GetRawText();
                var exponentIndex = raw.IndexOfAny(['e', 'E']);
                var mantissa = exponentIndex < 0 ? raw : raw[..exponentIndex];
                var exponent = exponentIndex < 0 ? 0L : long.Parse(raw[(exponentIndex + 1)..], CultureInfo.InvariantCulture);
                var negative = mantissa.StartsWith('-');
                if (negative) mantissa = mantissa[1..];
                var dot = mantissa.IndexOf('.');
                if (dot >= 0) { exponent = checked(exponent - (mantissa.Length - dot - 1)); mantissa = mantissa.Remove(dot, 1); }
                mantissa = mantissa.TrimStart('0');
                if (mantissa.Length == 0) { writer.WriteNumberValue(0); break; }
                var digits = mantissa.TrimEnd('0');
                exponent = checked(exponent + mantissa.Length - digits.Length);
                writer.WriteRawValue((negative ? "-" : "") + digits + "e" + exponent.ToString(CultureInfo.InvariantCulture)); break;
            default: value.WriteTo(writer); break;
        }
    }
}
