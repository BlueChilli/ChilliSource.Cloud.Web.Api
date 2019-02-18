using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChilliSource.Cloud.Web.Api
{
    public class StreamedHttpFile
    {
        public StreamedHttpFile(string fileName, string contentType, Stream stream)
        {
            _FileName = fileName;
            _ContentType = contentType;
            _InputStream = stream;
        }

        string _ContentType;
        string _FileName;
        Stream _InputStream;

        public string ContentType { get { return _ContentType; } }

        public string FileName { get { return _FileName; } }

        public Stream InputStream { get { return _InputStream; } }
    }
}
