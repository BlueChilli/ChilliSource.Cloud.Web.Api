using ChilliSource.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.WebHost;

namespace ChilliSource.Cloud.Web.Api
{
    /// <summary>
    /// Allows the use of streamed input messages streams to be streamed (instead of buffered)
    /// </summary>
    public class NoBufferPolicySelector : WebHostBufferPolicySelector
    {
        IEnumerable<string> _streamedRelativePaths;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public NoBufferPolicySelector()
        {
            _streamedRelativePaths = ArrayExtensions.EmptyArray<string>();
        }

        /// <summary>
        /// Specifies which relatives paths should have a streamed input. (e.g. "~/api/upload/")
        /// </summary>
        public IEnumerable<string> StreamedInputRelativePaths
        {
            get
            {
                return _streamedRelativePaths;
            }
            set
            {
                _streamedRelativePaths = value.ToList();
            }
        }

        public override bool UseBufferedInputStream(object hostContext)
        {
            var context = hostContext as HttpContextBase;

            if (context != null && context.Request != null)
            {
                var buffered = UseBufferedInputStream(context.Request);

                if (!buffered)
                    return false;
            }

            return base.UseBufferedInputStream(hostContext);
        }

        public bool UseBufferedInputStream(HttpRequestBase request)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            var executionFilePath = request.AppRelativeCurrentExecutionFilePath;

            var buffered = !StreamedInputRelativePaths.Any(path => executionFilePath.StartsWith(path, StringComparison.InvariantCultureIgnoreCase));
            return buffered;
        }

        public override bool UseBufferedOutputStream(HttpResponseMessage response)
        {
            if (response.Content is StreamedHttpContent)
                return false;

            //base class already checks for PushStreamContent and StreamContent
            return base.UseBufferedOutputStream(response);
        }
    }
}
