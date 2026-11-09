using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RaceDay.Web.Services;

public class ApiResult<T>
{
    public int StatusCode { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }

    public bool Success => StatusCode is >= 200 and < 300;
    public bool Unauthorized => StatusCode == 401;
    public bool Forbidden => StatusCode == 403;
    public bool NotFound => StatusCode == 404;
}

/// <summary>
/// The only way the MVC app talks to the outside world: every call goes through the Part 2 API.
/// It forwards the logged-in user's API session cookie, which is kept in the MVC session.
/// </summary>
public class ApiClient
{
    private const string SessionCookieKey = "ApiSessionCookie";
    private const string ApiCookieName = ".RaceDay.Session";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;

    public ApiClient(HttpClient http, IHttpContextAccessor accessor)
    {
        _http = http;
        _accessor = accessor;
    }

    private ISession Session => _accessor.HttpContext!.Session;

    public Task<ApiResult<T>> GetAsync<T>(string url) => SendAsync<T>(HttpMethod.Get, url, null);
    public Task<ApiResult<T>> PostAsync<T>(string url, object? body = null) => SendAsync<T>(HttpMethod.Post, url, ToJson(body));
    public Task<ApiResult<T>> PutAsync<T>(string url, object? body = null) => SendAsync<T>(HttpMethod.Put, url, ToJson(body));
    public Task<ApiResult<object>> DeleteAsync(string url) => SendAsync<object>(HttpMethod.Delete, url, null);

    /// <summary>Sends an uploaded file to the API as multipart/form-data.</summary>
    public Task<ApiResult<T>> PostFileAsync<T>(string url, IFormFile file, string fieldName = "file")
    {
        var content = new MultipartFormDataContent();

        var fileContent = new StreamContent(file.OpenReadStream());
        fileContent.Headers.ContentType = MediaTypeHeaderValue.TryParse(file.ContentType, out var mediaType)
            ? mediaType
            : new MediaTypeHeaderValue("application/octet-stream");

        content.Add(fileContent, fieldName, Path.GetFileName(file.FileName));
        return SendAsync<T>(HttpMethod.Post, url, content);
    }

    /// <summary>Forgets the stored API session (used on logout).</summary>
    public void ForgetSession() => Session.Remove(SessionCookieKey);

    private static StringContent? ToJson(object? body) =>
        body == null ? null : new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, HttpContent? content)
    {
        using var request = new HttpRequestMessage(method, url) { Content = content };

        var cookie = Session.GetString(SessionCookieKey);
        if (!string.IsNullOrEmpty(cookie))
            request.Headers.Add("Cookie", cookie);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new ApiResult<T>
            {
                StatusCode = 503,
                Error = "The RaceDay service is currently unavailable. Please try again shortly."
            };
        }

        using (response)
        {
            CaptureSessionCookie(response);
            var body = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                T? data = default;
                if (!string.IsNullOrWhiteSpace(body) && typeof(T) != typeof(object))
                {
                    try { data = JsonSerializer.Deserialize<T>(body, JsonOptions); }
                    catch (JsonException)
                    {
                        return new ApiResult<T>
                        {
                            StatusCode = 502,
                            Error = "The RaceDay service returned an unexpected response."
                        };
                    }
                }
                return new ApiResult<T> { StatusCode = (int)response.StatusCode, Data = data };
            }

            return new ApiResult<T>
            {
                StatusCode = (int)response.StatusCode,
                Error = ExtractError(body, response.StatusCode)
            };
        }
    }

    private void CaptureSessionCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values)) return;

        var sessionCookie = values.FirstOrDefault(v => v.StartsWith(ApiCookieName + "=", StringComparison.Ordinal));
        if (sessionCookie != null)
            Session.SetString(SessionCookieKey, sessionCookie.Split(';')[0]);
    }

    /// <summary>Turns the API's JSON error into a friendly sentence, so raw errors never reach the user.</summary>
    private static string ExtractError(string content, HttpStatusCode status)
    {
        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                    return message.GetString() ?? "The request could not be completed.";

                if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    var messages = errors.EnumerateObject()
                        .Where(p => p.Value.ValueKind == JsonValueKind.Array)
                        .SelectMany(p => p.Value.EnumerateArray())
                        .Select(e => e.GetString())
                        .Where(m => !string.IsNullOrWhiteSpace(m));

                    var joined = string.Join(" ", messages);
                    if (joined.Length > 0) return joined;
                }
            }
        }
        catch (JsonException) { }

        return status switch
        {
            HttpStatusCode.Unauthorized => "Please log in to continue.",
            HttpStatusCode.Forbidden => "You do not have permission to do that.",
            HttpStatusCode.NotFound => "We could not find what you were looking for.",
            _ => "Something went wrong. Please try again."
        };
    }
}