using QuotesProject.Responses;
using System.Net.Http.Json;

namespace QuotesProject.Api
{
    public class AuthEngine
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly string _grantType;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _username;

        private string? _accessToken;
        private DateTime _expiresAt;

        private readonly SemaphoreSlim _refreshLock = new(1, 1);

        public string? RefreshToken { get; set; }

        public AuthEngine(IHttpClientFactory httpFactory, IConfiguration configuration)
        {
            _httpFactory = httpFactory;
            _grantType = configuration["ApiSettings:GrantType"] ?? throw new InvalidOperationException("ApiSettings:GrantType is not configured.");
            _clientId = configuration["ApiSettings:ClientId"] ?? throw new InvalidOperationException("ApiSettings:ClientId is not configured.");
            _clientSecret = configuration["ApiSettings:ClientSecret"] ?? throw new InvalidOperationException("ApiSettings:ClientSecret is not configured.");
            _username = configuration["ApiSettings:Username"] ?? throw new InvalidOperationException("ApiSettings:Username is not configured.");
        }

        public async Task<string> GetValidTokenAsync()
        {
            if (_accessToken != null && DateTime.Now < _expiresAt)
                return _accessToken;

            await _refreshLock.WaitAsync();
            try
            {
                if (_accessToken != null && DateTime.Now < _expiresAt)
                    return _accessToken;

                if (string.IsNullOrEmpty(RefreshToken))
                    return await GetAccessTokenAsync();  

                try
                {
                    return await RefreshAccessTokenAsync();
                }
                catch (HttpRequestException)
                {
                    // Refresh token expires (90 days), and Intacct can revoke it.
                    // Fall back to the full credential grant instead of dying.
                    return await GetAccessTokenAsync();
                }
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        public async Task<string> GetAccessTokenAsync()
        {
            var collection = new Dictionary<string, string>
            {
                ["grant_type"] = _grantType,
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret,
                ["username"] = _username
            };

            var request = new FormUrlEncodedContent(collection);

            var response = await _httpFactory.CreateClient("auth").PostAsync("https://api.intacct.com/ia/api/v1/oauth2/token", request);

            response.EnsureSuccessStatusCode();

            var token = await response.Content.ReadFromJsonAsync<TokenResponse>();

            _accessToken = token?.AccessToken ?? throw new InvalidOperationException("Token response did not contain an access_token.");
            RefreshToken = token.RefreshToken;

            _expiresAt = DateTime.Now.AddSeconds(token.ExpiresIn - 300);

            return _accessToken;
        }

        public async Task<string> RefreshAccessTokenAsync()
        {
            if (string.IsNullOrEmpty(RefreshToken))
                throw new InvalidOperationException("No refresh token available - call GetAccessTokenAsync first.");

            var collection = new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret,
                ["refresh_token"] = RefreshToken
            };

            var request = new FormUrlEncodedContent(collection);

            var response = await _httpFactory.CreateClient("auth").PostAsync("https://api.intacct.com/ia/api/v1/oauth2/token", request);

            response.EnsureSuccessStatusCode();

            var token = await response.Content.ReadFromJsonAsync<TokenResponse>();

            _accessToken = token?.AccessToken ?? throw new InvalidOperationException("Token response did not contain an access_token.");
            RefreshToken = token.RefreshToken;

            _expiresAt = DateTime.Now.AddSeconds(token.ExpiresIn - 300);

            return _accessToken;
        }
    }
}
