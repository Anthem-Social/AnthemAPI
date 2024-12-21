using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AnthemAPI.Common;
using AnthemAPI.Common.Helpers;

namespace AnthemAPI.Services;

public class TokenService
{
    private readonly HttpClient _client;
    private readonly IConfiguration _configuration;

    public TokenService(HttpClient client, IConfiguration configuration)
    {
        _configuration = configuration;
        
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_configuration["SpotifyClientId"]}:{_configuration["SpotifyClientSecret"]}"));
        _client = client;
        _client.BaseAddress = new Uri("https://accounts.spotify.com/api/token");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basic);
    }

    public async Task<ServiceResult<string>> Swap(string code)
    {
        try
        {
            var data = new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "redirect_uri", _configuration["SpotifyCallback"]! },
                { "code", code }
            };
            HttpResponseMessage response = await _client.PostAsync("", new FormUrlEncodedContent(data));
            string content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return ServiceResult<string>.Failure($"Error response for token swap: {response}", "AuthorizationService.Swap()");
            }

            JsonElement json = JsonDocument.Parse(content).RootElement;

            if (json.TryGetProperty("refresh_token", out var refreshToken))
            {
                string encrypted = Helpers.Encrypt(refreshToken.GetString()!);
                string result = json.GetRawText().Replace(refreshToken.GetString()!, encrypted);

                return ServiceResult<string>.Success(result);
            }

            return ServiceResult<string>.Failure($"No refresh_token property in JSON. {json}", "AuthorizationService.Swap()");
        }
        catch (Exception e)
        {
            return ServiceResult<string>.Failure($"Failed to swap.\nError: {e}", "AuthorizationService.Swap()");
        }
    }

    public async Task<ServiceResult<string>> Refresh(string refreshToken)
    {
        try
        {
            string decrypted = Helpers.Decrypt(refreshToken);
            var data = new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", decrypted }
            };
            HttpResponseMessage response = await _client.PostAsync("", new FormUrlEncodedContent(data));
            string content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return ServiceResult<string>.Failure($"Error response for token refresh: {response}", "AuthorizationService.Refresh()");
            }

            JsonElement json = JsonDocument.Parse(content).RootElement;

            if (json.TryGetProperty("refresh_token", out var token))
            {
                string encrypted = Helpers.Encrypt(token.GetString()!);
                string result = json.GetRawText().Replace(token.GetString()!, encrypted);

                return ServiceResult<string>.Success(result);
            }
            
            return ServiceResult<string>.Failure($"No refresh_token property in JSON. {json}", "AuthorizationService.Refresh()");
        }
        catch (Exception e)
        {
            return ServiceResult<string>.Failure($"Failed to refresh token.\n{e}", "AuthorizationService.Refresh()");
        }
    }
}