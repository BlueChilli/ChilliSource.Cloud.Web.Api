using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace ChilliSource.Cloud.Web.Api
{
    public class FileResult : IHttpActionResult
    {
        private readonly string _filename;
        private readonly Stream _content;

        public FileResult(string filename, Stream content)
        {
            if (filename == null) throw new ArgumentNullException("filename");
            if (content == null) throw new ArgumentNullException("content");

            _filename = filename;
            _content = content;
            this.ContentDisposition = "inline";
            try
            {
                this.ContentType = MimeMapping.GetMimeMapping(Path.GetExtension(filename));
            }
            catch { }
        }

        public string ContentDisposition { get; set; }

        public string ContentType { get; set; }

        public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(_content),
            };

            response.Content.Headers.ContentType = new MediaTypeHeaderValue(this.ContentType);
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue(this.ContentDisposition)
            {
                FileName = _filename
            };
            return Task.FromResult(response);
        }
    }
}