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
                var executionFilePath = context.Request.AppRelativeCurrentExecutionFilePath;

                var streamed = StreamedInputRelativePaths.Any(path => executionFilePath.StartsWith(path, StringComparison.InvariantCultureIgnoreCase));
                if (streamed)
                    return false;
            }

            return base.UseBufferedInputStream(hostContext);
        }

        //Base class already checks for PushStreamContent and StreamContent
        //public override bool UseBufferedOutputStream(HttpResponseMessage response)
        //{
        //    return base.UseBufferedOutputStream(response);
        //}
    }
}
