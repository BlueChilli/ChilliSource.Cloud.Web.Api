using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ChilliSource.Cloud.Web.Api
{
    public class StreamedHttpContent : HttpContent
    {
        private Stream _inputStream;

        public StreamedHttpContent(Stream inputStream)
        {
            if (inputStream == null)
                throw new ArgumentNullException("inputStream");

            _inputStream = inputStream;
        }

        protected override async Task SerializeToStreamAsync(Stream outputStream, TransportContext context)
        {
            if (_inputStream == null)
                throw new ApplicationException("_inputStream already read.");

            using (_inputStream)
            using (outputStream)
            {
                try
                {
                    await outputStream.FlushAsync();
                    await StreamUtils.CopyStreamAsync(_inputStream, outputStream, 32 * 1024);
                }
                finally
                {
                    outputStream.Close();
                    _inputStream.Close();
                }
            }

            _inputStream = null;
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }
    }

    internal static class StreamUtils
    {
        public static async Task CopyStreamAsync(Stream inputStream, Stream outputStream, int bufferSize)
        {
            var buffer = new byte[bufferSize];

            int read;
            while ((read = await inputStream.ReadAsync(buffer, 0, bufferSize)) > 0)
            {
                await outputStream.WriteAsync(buffer, 0, read);
                await outputStream.FlushAsync();
            }
        }
    }
}
