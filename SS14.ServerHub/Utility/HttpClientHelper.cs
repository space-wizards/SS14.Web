using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;

namespace SS14.ServerHub.Utility;

public static class HttpClientHelper
{
    public static async Task<byte[]> GetLimitedJsonResponseBody(
        this HttpClient client, 
        Uri uri,
        int maxSize,
        CancellationToken cancel = default)
    {
        // Very advanced dance to be able to save the response while limiting it,
        // and actually being able to clearly tell whether the response was too big.
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
        var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancel);

        response.EnsureSuccessStatusCode();
        var buffer = new byte[maxSize];
        var memoryStream = new MemoryStream(buffer);
        var stream = await response.Content.ReadAsStreamAsync(cancel);
        var success = await StreamHelper.CopyToLimitedAsync(stream, memoryStream, buffer.Length, cancel);
        if (!success)
            throw new ResponseTooLargeException();

        return buffer[..(int)memoryStream.Position];
    }

    public class ResponseTooLargeException : Exception
    {
        public ResponseTooLargeException() : base("Response body size exceeded limit") 
        {
        }
    }
}