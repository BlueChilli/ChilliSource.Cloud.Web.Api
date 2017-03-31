using System;
using System.IO;
using System.Web;

namespace ChilliSource.Cloud.Web.Api
{
    public class HttpFile : HttpPostedFileBase
    {
        public HttpFile(string fileName, string mediaType, Stream stream)
        {
            _FileName = fileName;
            _ContentType = mediaType;
            _InputStream = stream;
            _ContentLength = Convert.ToInt32(InputStream.Length);
        }

        int _ContentLength;
        string _ContentType;
        string _FileName;
        Stream _InputStream;

        public override int ContentLength { get { return _ContentLength; } }

        public override string ContentType { get { return _ContentType; } }

        public override string FileName { get { return _FileName; } }

        public override Stream InputStream { get { return _InputStream; } }

        public override void SaveAs(string filename)
        {
            using (var file = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
            {
                _InputStream.CopyTo(file);

                file.Flush();
                file.Close();
            }
        }
    }
}
