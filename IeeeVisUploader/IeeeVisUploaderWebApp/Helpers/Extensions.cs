/*
 * Copyright (c) Johannes Knittel
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
namespace IeeeVisUploaderWebApp.Helpers
{
    public static class Extensions
    {

        public static async Task ProxyForwardHttpResponse(this HttpContext context, HttpResponseMessage response, IEnumerable<(string key, string value)>? headersToSet = null)
        {

            var resp = context.Response;

            resp.StatusCode = (int)response.StatusCode;
            foreach (var header in response.Headers)
            {
                try
                {
                    resp.Headers[header.Key] = header.Value.ToArray();
                }
                catch (Exception)
                {
                }
            }
            foreach (var header in response.Content.Headers)
            {
                try
                {
                    resp.Headers[header.Key] = header.Value.ToArray();
                }
                catch (Exception)
                {
                }
            }

            if (headersToSet != null)
            {
                foreach ((string key, string value) in headersToSet)
                {
                    try
                    {
                        resp.Headers[key] = value;
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            resp.Headers.Remove("transfer-encoding");
            await using var stream = await response.Content.ReadAsStreamAsync();
            await stream.CopyToAsync(resp.Body, context.RequestAborted);
        }
    }
}
