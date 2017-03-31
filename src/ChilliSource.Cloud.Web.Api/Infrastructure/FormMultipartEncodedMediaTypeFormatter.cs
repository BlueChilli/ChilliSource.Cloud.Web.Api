using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Threading;
using ChilliSource.Cloud.Web.Api.Internal;

namespace ChilliSource.Cloud.Web.Api
{
    public class FormMultipartEncodedMediaTypeFormatter : MediaTypeFormatter
    {
        public FormMultipartEncodedMediaTypeFormatter()
        {
            var mediaTypeHeaderValue = new MediaTypeHeaderValue("multipart/form-data");
            // mediaTypeHeaderValue.Parameters.Add(new NameValueHeaderValue("boundary", "MultipartDataMediaFormatterBoundary1q2w3e"));

            SupportedMediaTypes.Add(mediaTypeHeaderValue);
        }

        public override bool CanReadType(Type type)
        {
            return true;
        }

        public override bool CanWriteType(Type type)
        {
            return true;
        }

        public override Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content,
                                                               IFormatterLogger formatterLogger)
        {
            var httpContentToFormDataConverter = new HttpContentToFormDataConverter();
            return httpContentToFormDataConverter.Convert(content)
                .ContinueWith(t =>
                {
                    var multipartFormData = t.Result;

                    IFormDataConverterLogger logger;
                    if (formatterLogger != null)
                        logger = new FormatterLoggerAdapter(formatterLogger);
                    else
                        logger = new FormDataConverterLogger();

                    var dataToObjectConverter = new FormDataToObjectConverter(multipartFormData, logger);
                    object result = dataToObjectConverter.Convert(type);

                    logger.EnsureNoErrors();
                    return result;
                });
        }

        public override Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content,
                                                TransportContext transportContext)
        {
            if (!content.IsMimeMultipartContent())
                return Task.FromException(new ApplicationException("FormMultipartEncodedMediaTypeFormatter: Unsupported Media Type"));

            var boundaryParameter = content.Headers.ContentType.Parameters.FirstOrDefault(m => m.Name == "boundary" && !String.IsNullOrWhiteSpace(m.Value));
            if (boundaryParameter == null)
                return Task.FromException(new Exception("FormMultipartEncodedMediaTypeFormatter: No boundary was found"));

            var writer = new ObjectToMultipartDataWriter(writeStream);
            return writer.WriteAsync(value, boundaryParameter.Value, CancellationToken.None)
                    .ContinueWith(t =>
                    {
                        content.Headers.ContentLength = writer.BytesWritten;
                    });
        }
    }
}