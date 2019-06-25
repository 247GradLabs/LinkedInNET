
#if ASYNCTASKS
namespace Sparkle.LinkedInNET.Internals
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml.Serialization;

    /// <summary>
    /// Base class for LinkedIn APIs.
    /// </summary>
    partial class BaseApi
    {
        internal async Task<bool> ExecuteQueryAsync(RequestContext context)
        {
            // https://developer.linkedin.com/documents/request-and-response-headers

            if (context == null)
                throw new ArgumentNullException("context");
            if (string.IsNullOrEmpty(context.Method))
                throw new ArgumentException("The value cannot be empty", "context.Method");
            if (string.IsNullOrEmpty(context.UrlPath))
                throw new ArgumentException("The value cannot be empty", "context.UrlPath");

            bool isOctet = false;
            if (context.PostDataType == "application/octet-stream")
                isOctet = true;

            var request = (HttpWebRequest)HttpWebRequest.Create(isOctet ? context.UploadUrl : context.UrlPath);
            request.Method = context.Method;
            request.UserAgent = LibraryInfo.UserAgent;
            if (context.PostDataType == "multipart/form-data" || context.PostDataType == "application/octet-stream")
            {
            }
            else if (!isOctet && context.UrlPath.Contains("ugcPosts"))
            {             
                request.Headers.Add("X-Restli-Protocol-Version", "2.0.0");
            }
            else
            {
                request.Headers.Add("x-li-format", "json");
            }

            if (context.AcceptLanguages != null)
            {
                request.Headers.Add(HttpRequestHeader.AcceptLanguage, string.Join(",", context.AcceptLanguages));
            }

            // user authorization
            if (context.UserAuthorization != null)
            {
                if (string.IsNullOrEmpty(context.UserAuthorization.AccessToken))
                    throw new ArgumentException("The value cannot be empty", "context.UserAuthorization.AccessToken");

                request.Headers.Add("Authorization", "Bearer " + context.UserAuthorization.AccessToken);
            }

            foreach (var header in context.RequestHeaders)
            {
                request.Headers[header.Key] = header.Value;
            }

            // post stuff?
            if (context.PostData != null)
            {
                try
                {
                    if (context.PostDataType == "multipart/form-data")
                    {
                        var boundary = "asdflknasdlkfnalkvvxcmvlzxcvznxclkvnzxlkcvzlkxcvklzxcnv";
                        var formData = await GetMultipartFormDataAsync(boundary, context.PostData);

                        request.Method = "POST";
                        request.Timeout = 10000;
                        request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                        request.ContentType = "multipart/form-data; boundary=" + boundary;
                        request.ContentLength = formData.Length;
                        request.KeepAlive = true;

                        // Send the form data to the request.
                        using (Stream requestStream = request.GetRequestStream())
                        {
                            await requestStream.WriteAsync(formData, 0, formData.Length);
                            requestStream.Close();
                        }
                    }
                    else if (context.PostDataType == "application/octet-stream")
                    {
                        request.ContentType = "application/octet-stream";
                        request.ContentLength = context.PostData.Length;
                        request.Method = "PUT";
                        request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                        request.Timeout = 10000;
                        
                        // Send the data to the request.
                        using (Stream requestStream = request.GetRequestStream())
                        {
                            requestStream.Write(context.PostData, 0, context.PostData.Length);
                            requestStream.Close();
                        }
                    }
                    else
                    {
                        if (context.PostDataType != null)
                            request.ContentType = context.PostDataType;

                        ////request.ContentLength = context.PostData.Length;
                        var stream = await request.GetRequestStreamAsync();
                        await stream.WriteAsync(context.PostData, 0, context.PostData.Length);
                        await stream.FlushAsync();
                    }
                }
                catch (WebException ex)
                {
                    throw new InvalidOperationException("Error POSTing to API (" + ex.Message + ")", ex);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Error POSTing to API (" + ex.Message + ")", ex);
                }
            }

            // get response
            HttpWebResponse response;
            WebException webException = null;
            try
            {
                response = (HttpWebResponse)await request.GetResponseAsync();
                context.HttpStatusCode = (int)response.StatusCode;
                context.ResponseHeaders = response.Headers;

                var readStream = response.GetResponseStream();
                await BufferizeResponseAsync(context, readStream);

                // check HTTP code
                if (!(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created))
                {
                    throw new InvalidOperationException("Error from API (HTTP " + (int)(response.StatusCode) + ")");
                }
                               
                return true;
            }
            catch (WebException ex)
            {
                webException = ex;
            }

            if (webException != null)
            {
                response = (HttpWebResponse)webException.Response;

                if (response != null)
                {
                    context.HttpStatusCode = (int)response.StatusCode;
                    context.ResponseHeaders = response.Headers;
                    
                    var stream = response.GetResponseStream();
                    if (stream != null)
                    {
                        await BufferizeResponseAsync(context, stream);

                        var responseString = await new StreamReader(context.ResponseStream, Encoding.UTF8).ReadToEndAsync();

                        context.ResponseStream.Seek(0L, SeekOrigin.Begin);
                        return false;
                    }

                    throw new InvalidOperationException("Error from API (HTTP " + (int)(response.StatusCode) + "): " + webException.Message, webException);
                }
                else
                {
                    throw new InvalidOperationException("Error from API: " + webException.Message, webException);
                }
            }
            else
            {
                return true;
            }
        }

        private static async Task<byte[]> GetMultipartFormDataAsync(string boundary, byte[] imageBytes)
        {
            var encoding = Encoding.UTF8;
            Stream formDataStream = new System.IO.MemoryStream();

            var fileName = Guid.NewGuid().ToString() + ".png";

            // Add just the first part of this param, since we will write the file data directly to the Stream
            string header = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"\r\nContent-Type: {3}\r\n\r\n",
                boundary,
                "source",
                fileName,
                "image/png");

            await formDataStream.WriteAsync(encoding.GetBytes(header), 0, encoding.GetByteCount(header));

            // Write the file data directly to the Stream, rather than serializing it to a string.
            await formDataStream.WriteAsync(imageBytes, 0, imageBytes.Length);


            // Add the end of the request.  Start with a newline
            string footer = "\r\n--" + boundary + "--\r\n";
            await formDataStream.WriteAsync(encoding.GetBytes(footer), 0, encoding.GetByteCount(footer));

            // Dump the Stream into a byte[]
            formDataStream.Position = 0;
            byte[] formData = new byte[formDataStream.Length];
            await formDataStream.ReadAsync(formData, 0, formData.Length);
            formDataStream.Close();

            return formData;
        }

        private static async Task BufferizeResponseAsync(RequestContext context, Stream readStream)
        {
            if (context.BufferizeResponseStream)
            {
                var memory = new MemoryStream();
                context.ResponseStream = memory;
                byte[] buffer = new byte[1024 * 1024];
                int readBytes = 0;
                while ((readBytes = await readStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await memory.WriteAsync(buffer, 0, readBytes);
                }

                memory.Seek(0L, SeekOrigin.Begin);
            }
            else
            {
                context.ResponseStream = readStream;
            }
        }
    }
}
#endif
