/*
 * Copyright (c) Johannes Knittel
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
using System;
using System.Security.Cryptography;
using System.Text;
using Flurl;
using static System.Net.Mime.MediaTypeNames;

namespace IeeeVisUploaderWebApp.Helpers
{
    public class UrlSigner
    {

        private readonly SHA256 _sha256 = SHA256.Create();
        private readonly string _privateKey;
        private readonly string _tokenKey;

        public UrlSigner()
        {
            _privateKey = DataProvider.Settings.AuthSignaturePrivateKey;
            _tokenKey = DataProvider.Settings.BunnyTokenKey;
        }

        public UrlSigner(string privateKey, string tokenKey)
        {
            _privateKey = privateKey;
            _tokenKey = tokenKey;
        }
       

        public string GetUrlAuth(string? action, string? uid)
        {
            return GetUrlAuth(action + uid);
        }

        public string GetUrlAuth(string? uid, string? itemId, long expiry)
        {
            return GetUrlAuth("upload" + itemId + uid + expiry);
        }

        public string GetUrlAuth(string? content)
        {
            var bytes = Encoding.UTF8.GetBytes("URL|" + _privateKey + content);
            var hash = _sha256.ComputeHash(bytes)[..8];
            return BytesToTokenBase64(hash);
        }

        public string GetFileSha256Checksum(string path)
        {
            using var f = File.OpenRead(path);
            var bytes = _sha256.ComputeHash(f);
            return Convert.ToHexString(bytes);
        }


        public string GetTokenBase64EncodedHash(string toHash)
        {
            var bytes = Encoding.UTF8.GetBytes(toHash);
            var hash = _sha256.ComputeHash(bytes);
            return BytesToTokenBase64(hash);
        }

        public static string BytesToTokenBase64(byte[] bytes)
        {
            var s = Convert.ToBase64String(bytes, Base64FormattingOptions.None).ToCharArray();
            var len = 0;
            for (;len < s.Length; len++)
            {
                var ch = s[len];
                if (ch == '=')
                    break;
                if (ch == '+')
                    s[len] = '-';
                else if (ch == '/')
                    s[len] = '_';
            }

            return new string(s.AsSpan(0, len));

        }
        public string SignBunnyUrl(string fileUrl, DateTimeOffset expiry, bool isDirectory = false)
        {
            var securityKey = _tokenKey;
            var url = new Url(fileUrl);
            var signaturePath = url.Path;

            var expires = expiry.ToUnixTimeSeconds().ToString();

            // Sort query parameters before generating base hash
            var hashableBase = $"{securityKey}{signaturePath}{expires}";
            var sortedParams = url.QueryParams.OrderBy(x => x.Name).ToList(); // sort & remove old items
            url.QueryParams.Clear();

            // Set sorted parameters and generate hash
            for (int i = 0; i < sortedParams.Count; i++)
            {
                url.SetQueryParam(sortedParams[i].Name, sortedParams[i].Value);
                hashableBase += (i == 0 ? "" : "&") + $"{sortedParams[i].Name}={sortedParams[i].Value}";
            }

            var token = GetTokenBase64EncodedHash(hashableBase);

            // Overwrite the token_path to urlencode it for the final url
            //url.SetQueryParam("token_path", config.TokenPath);

            // Add expires
            url.SetQueryParam("expires", expires);

            if (isDirectory)
                return url.Root + "/bcdn_token=" + token + "&" + url.Query + url.Path;
            
            return url.Root + url.Path + "?token=" + token + "&" + url.Query;
        }


        public static bool SafeCompareEquality(string s1, string s2)
        {
            var maxLen = Math.Max(s1.Length, s2.Length);
            var minLen = Math.Min(s1.Length, s2.Length);
            var isCorrect = true;
            for (int i = 0; i < maxLen; i++)
            {
                if (i >= minLen || s1[i] != s2[i])
                {
                    isCorrect = false;
                }
            }

            return isCorrect;
        }
    }
}
