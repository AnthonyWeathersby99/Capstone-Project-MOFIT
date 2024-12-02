using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class AwsSigV4Signer
{
    private const string Algorithm = "AWS4-HMAC-SHA256";
    private const string AWSService = "execute-api";

    public static async Task<HttpResponseMessage> SendSignedRequest(string url, string method, string region, string accessKey, string secretKey)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        var now = DateTime.UtcNow;
        var dateStamp = now.ToString("yyyyMMdd");
        var amzDate = now.ToString("yyyyMMddTHHmmssZ");

        request.Headers.Add("X-Amz-Date", amzDate);

        var canonicalRequest = CreateCanonicalRequest(request, "");
        var stringToSign = CreateStringToSign(canonicalRequest, dateStamp, region, amzDate);
        var signature = CalculateSignature(stringToSign, dateStamp, region, secretKey);

        var authorizationHeader = $"{Algorithm} Credential={accessKey}/{dateStamp}/{region}/{AWSService}/aws4_request, SignedHeaders=host;x-amz-date, Signature={signature}";
        request.Headers.TryAddWithoutValidation("Authorization", authorizationHeader);

        var client = new HttpClient();
        return await client.SendAsync(request);
    }

    private static string CreateCanonicalRequest(HttpRequestMessage request, string bodyHash)
    {
        var canonicalHeaders = $"host:{request.RequestUri.Host}\nx-amz-date:{request.Headers.GetValues("X-Amz-Date").First()}\n";
        var signedHeaders = "host;x-amz-date";

        return $"{request.Method}\n{request.RequestUri.PathAndQuery}\n\n{canonicalHeaders}\n{signedHeaders}\n{bodyHash}";
    }

    private static string CreateStringToSign(string canonicalRequest, string dateStamp, string region, string amzDate)
    {
        var credentialScope = $"{dateStamp}/{region}/{AWSService}/aws4_request";
        var hashedCanonicalRequest = Hash(canonicalRequest);
        return $"{Algorithm}\n{amzDate}\n{credentialScope}\n{hashedCanonicalRequest}";
    }

    private static string CalculateSignature(string stringToSign, string dateStamp, string region, string secretKey)
    {
        var kSecret = Encoding.UTF8.GetBytes($"AWS4{secretKey}");
        var kDate = HmacSha256(dateStamp, kSecret);
        var kRegion = HmacSha256(region, kDate);
        var kService = HmacSha256(AWSService, kRegion);
        var kSigning = HmacSha256("aws4_request", kService);

        return ToHexString(HmacSha256(stringToSign, kSigning));
    }

    private static byte[] HmacSha256(string data, byte[] key)
    {
        using (var hash = new HMACSHA256(key))
        {
            return hash.ComputeHash(Encoding.UTF8.GetBytes(data));
        }
    }

    private static string Hash(string input)
    {
        using (var sha256 = SHA256.Create())
        {
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return ToHexString(bytes);
        }
    }

    private static string ToHexString(byte[] array)
    {
        return BitConverter.ToString(array).Replace("-", "").ToLowerInvariant();
    }
}