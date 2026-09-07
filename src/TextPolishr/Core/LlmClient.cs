using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TextPolishr.Core;

internal sealed class LlmClient : IDisposable
{
    private readonly HttpClient _httpClient;

    public LlmClient(HttpMessageHandler? handler = null)
    {
        _httpClient = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: true);
    }

    public async Task<string> TransformAsync(
        ProviderSettings provider,
        string apiKey,
        string model,
        string prompt,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            throw new InvalidOperationException("No model configured.");
        }

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);
        var endpoint = CombineEndpoint(provider.BaseUrl, "/chat/completions");
        using var request = CreateRequest(HttpMethod.Post, endpoint, provider, apiKey);

        var body = new JsonObject
        {
            ["model"] = model,
            ["stream"] = false,
            ["messages"] = new JsonArray
            {
                new JsonObject { ["role"] = "user", ["content"] = prompt }
            }
        };

        if (provider.SupportsStructuredOutput)
        {
            body["response_format"] = new JsonObject
            {
                ["type"] = "json_schema",
                ["json_schema"] = new JsonObject
                {
                    ["name"] = "text_polishr_output",
                    ["strict"] = true,
                    ["schema"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["additionalProperties"] = false,
                        ["properties"] = new JsonObject
                        {
                            ["replacement_text"] = new JsonObject { ["type"] = "string" }
                        },
                        ["required"] = new JsonArray("replacement_text")
                    }
                }
            };
        }

        request.Content = JsonContent.Create(body);
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutSource.Token);
        var responseText = await response.Content.ReadAsStringAsync(timeoutSource.Token);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Provider returned HTTP {(int)response.StatusCode}.");
        }

        using var document = JsonDocument.Parse(responseText);
        var content = document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (provider.SupportsStructuredOutput && !string.IsNullOrWhiteSpace(content))
        {
            using var structured = JsonDocument.Parse(content);
            content = structured.RootElement.GetProperty("replacement_text").GetString();
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("The provider returned an empty response.");
        }

        return content;
    }

    public async Task<IReadOnlyList<string>> FetchModelsAsync(
        ProviderSettings provider,
        string apiKey,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        if (provider.ModelsEndpoint is null)
        {
            return [];
        }

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);
        using var request = CreateRequest(HttpMethod.Get, CombineEndpoint(provider.BaseUrl, provider.ModelsEndpoint), provider, apiKey);
        using var response = await _httpClient.SendAsync(request, timeoutSource.Token);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(timeoutSource.Token));
        return document.RootElement.GetProperty("data")
            .EnumerateArray()
            .Select(item => item.GetProperty("id").GetString())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Cast<string>()
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static HttpRequestMessage CreateRequest(
        HttpMethod method,
        Uri endpoint,
        ProviderSettings provider,
        string apiKey)
    {
        var request = new HttpRequestMessage(method, endpoint);
        request.Headers.UserAgent.ParseAdd("TextPolishr/0.1");
        request.Headers.Referrer = new Uri("https://github.com/");
        request.Headers.TryAddWithoutValidation("X-Title", "Text Polishr");

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            if (provider.Id == "anthropic")
            {
                request.Headers.TryAddWithoutValidation("x-api-key", apiKey);
                request.Headers.TryAddWithoutValidation("anthropic-version", "2023-06-01");
            }
            else
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            }
        }

        return request;
    }

    private static Uri CombineEndpoint(string baseUrl, string endpoint) =>
        new(baseUrl.TrimEnd('/') + "/" + endpoint.TrimStart('/'), UriKind.Absolute);

    public void Dispose() => _httpClient.Dispose();
}
