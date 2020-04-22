using System;
using System.Diagnostics;
using System.Text;
using Windows.Storage;

namespace GhostOverlay
{
    public class OAuthToken
    {
        public static DateTimeOffset DefaultExpirationTime = new DateTimeOffset();

        public OAuthToken()
        {
        }

        public OAuthToken(string accessToken, string refreshToken, int accessTokenExpiresInSeconds,
            int refreshTokenExpiresInSeconds)
        {
            AccessToken = accessToken;
            RefreshToken = refreshToken;

            SetAccessTokenExpiration(accessTokenExpiresInSeconds);
            SetRefreshTokenExpiration(refreshTokenExpiresInSeconds);
        }

        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTimeOffset AccessTokenExpiration { get; set; }
        public DateTimeOffset RefreshTokenExpiration { get; set; }

        public static OAuthToken RestoreTokenFromSettings()
        {
            var accessToken = AppState.ReadSetting(SettingsKey.AccessToken, "");
            var refreshToken = AppState.ReadSetting(SettingsKey.RefreshToken, "");

            var accessTokenExpiration = AppState.ReadSetting(SettingsKey.AccessTokenExpiration, DefaultExpirationTime);
            var refreshTokenExpiration = AppState.ReadSetting(SettingsKey.RefreshTokenExpiration, DefaultExpirationTime);

            Debug.WriteLine($"restored access token {accessToken}");

            var tokenData = new OAuthToken
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiration = accessTokenExpiration,
                RefreshTokenExpiration = refreshTokenExpiration
            };

            return tokenData;
        }

        public void SaveToSettings()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            AppState.SaveSetting(SettingsKey.AccessToken, AccessToken);
            AppState.SaveSetting(SettingsKey.RefreshToken, RefreshToken);
            AppState.SaveSetting(SettingsKey.AccessTokenExpiration, AccessTokenExpiration);
            AppState.SaveSetting(SettingsKey.RefreshTokenExpiration, RefreshTokenExpiration);
        }

        public void SetAccessTokenExpiration(int expiresInSeconds)
        {
            var date = DateTimeOffset.Now;
            date = date.AddSeconds(expiresInSeconds);
            AccessTokenExpiration = date;
        }

        public void SetRefreshTokenExpiration(int expiresInSeconds)
        {
            var date = DateTimeOffset.Now;
            date = date.AddSeconds(expiresInSeconds);
            RefreshTokenExpiration = date;
        }


        public bool IsValid()
        {
            var accessTokenValidity = AccessTokenIsValid();
            var refreshTokenValidity = RefreshTokenIsValid();

            return accessTokenValidity || refreshTokenValidity;
        }

        public bool AccessTokenIsValid()
        {
            var timeValid = AccessTokenExpiration.CompareTo(DateTimeOffset.Now) > 0;
            return !string.IsNullOrEmpty(AccessToken) && timeValid;
        }

        public bool RefreshTokenIsValid()
        {
            var timeValid = RefreshTokenExpiration.CompareTo(DateTimeOffset.Now) > 0;
            return !string.IsNullOrEmpty(RefreshToken) && timeValid;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("OAuthTokenData\n");
            sb.Append($"    AccessToken: {AccessToken}\n");
            sb.Append($"    RefreshToken: {RefreshToken}\n");
            sb.Append($"    AccessTokenExpiration: {AccessTokenExpiration}\n");
            sb.Append($"    RefreshTokenExpiration: {RefreshTokenExpiration}\n");
            return sb.ToString();
        }
    }
}