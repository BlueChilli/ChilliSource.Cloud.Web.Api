using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ChilliSource.Cloud.Web.Api
{
    public class CompressedHttpContent : StreamedHttpContent
    {
        private string encodingType;

        public static StreamedHttpContent Create(HttpRequestMessage request, Stream inputStream, long? length)
        {
            if ((length == null || length > 1024) && request.Headers.AcceptEncoding != null && request.Headers.AcceptEncoding.Count > 0)
            {
                string encodingType = request.Headers.AcceptEncoding.First().Value;

                try
                {
                    return new CompressedHttpContent(inputStream, encodingType);
                }
                catch
                {
                    /* encoding not supported */
                }
            }

            return new StreamedHttpContent(inputStream);
        }

        public CompressedHttpContent(Stream inputStream, string encodingType)
            : base(inputStream)
        {
            if (inputStream == null)
            {
                throw new ArgumentNullException("inputStream");
            }

            if (encodingType == null)
            {
                throw new ArgumentNullException("encodingType");
            }

            this.encodingType = encodingType.ToLowerInvariant();

            if (this.encodingType != "gzip" && this.encodingType != "deflate")
            {
                throw new InvalidOperationException(string.Format("Encoding '{0}' is not supported. Only supports gzip or deflate encoding.", this.encodingType));
            }

            this.Headers.ContentEncoding.Add(encodingType);
            this.Headers.ContentLength = null; //Removes content-length
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            var compressedStream = stream;

            if (encodingType == "gzip")
            {
                compressedStream = new GZipStream(stream, CompressionMode.Compress, leaveOpen: false);
            }
            else if (encodingType == "deflate")
            {
                compressedStream = new DeflateStream(stream, CompressionMode.Compress, leaveOpen: false);
            }

            await stream.FlushAsync();
            await base.SerializeToStreamAsync(compressedStream, context);
        }
    }
}
