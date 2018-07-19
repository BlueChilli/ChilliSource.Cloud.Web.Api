using ChilliSource.Cloud.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ChilliSource.Cloud.Web.Api
{
    public class MultiPartPipedStreamProvider : MultipartStreamProvider
    {
        private List<PipedStreamManager> _createdStreams = new List<PipedStreamManager>();
        IPipeActionRunner _pipeActionRunner = null;
        PipedStreamOptions _options = null;

        public MultiPartPipedStreamProvider()
        {
            //default action does nothing (the stream will get closed by the action runner)
            _pipeActionRunner = new PipeActionRunner<object>(this, (content, headers, stream, cancellationToken) => { return null; });
        }

        public MultiPartPipedStreamProvider(PipedStreamOptions options)
            : this()
        {
            _options = options;
        }

        /// <summary>
        /// Amend filenames to remove surrounding quotes and remove path from IE
        /// </summary>
        private static string FixFilename(string originalFileName)
        {
            if (string.IsNullOrWhiteSpace(originalFileName))
                return string.Empty;

            var result = originalFileName.Trim();

            // remove leading and trailing quotes
            result = result.Trim('"');

            // remove full path versions
            if (result.Contains("\\"))
                result = Path.GetFileName(result);

            return result;
        }

        public IPipeActionRunner<T> SetStreamedFileAction<T>(Func<StreamedHttpFile, CancellationToken, Task<T>> asyncAction)
        {
            return this.SetStreamedPipeAction<T>(async (content, headers, stream, cancellationToken) =>
            {
                string fileName = FixFilename(headers.ContentDisposition.FileName);
                string mediaType = headers.ContentType?.MediaType;

                var postedFile = new StreamedHttpFile(fileName, mediaType, stream);

                return await asyncAction(postedFile, cancellationToken);
            });
        }

        public IPipeActionRunner<T> SetStreamedPipeAction<T>(Func<HttpContent, HttpContentHeaders, Stream, CancellationToken, Task<T>> asyncAction)
        {
            var runner = new PipeActionRunner<T>(this, asyncAction);
            _pipeActionRunner = runner;

            return runner;
        }

        public override Stream GetStream(HttpContent parent, HttpContentHeaders headers)
        {
            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }

            if (headers == null)
            {
                throw new ArgumentNullException("headers");
            }

            var pipedStream = _options == null ? new PipedStreamManager() : new PipedStreamManager(_options);
            _pipeActionRunner.CreateNewReaderTaskForPipe(parent, headers, pipedStream);

            return pipedStream.CreateWriter(throwsFailedWrite: true);
        }
    }

    internal interface IPipeActionRunner
    {
        void CreateNewReaderTaskForPipe(HttpContent parent, HttpContentHeaders headers, PipedStreamManager pipedStream);
    }

    public interface IPipeActionRunner<T>
    {
        Task<List<T>> RunPipeActionForContent(HttpContent httpContent, CancellationToken cancellationToken);
    }

    internal class PipeActionRunner<T> : IPipeActionRunner<T>, IPipeActionRunner
    {
        MultiPartPipedStreamProvider _streamProvider;
        Func<HttpContent, HttpContentHeaders, Stream, CancellationToken, Task<T>> _asyncAction;
        BufferBlock<Func<CancellationToken, Task<T>>> _runtimeActions = null;

        public PipeActionRunner(MultiPartPipedStreamProvider streamProvider, Func<HttpContent, HttpContentHeaders, Stream, CancellationToken, Task<T>> asyncAction)
        {
            if (asyncAction == null)
                throw new ArgumentNullException("asyncAction");

            _streamProvider = streamProvider;
            _asyncAction = asyncAction;
        }

        public void CreateNewReaderTaskForPipe(HttpContent parent, HttpContentHeaders headers, PipedStreamManager pipedStream)
        {
            //This won't get executed just yet. Adding to the list of runtime actions only.
            _runtimeActions.Post(async (CancellationToken ct) =>
            {
                using (var reader = pipedStream.CreateReader())
                {
                    return await _asyncAction(parent, headers, reader, ct);
                }

                //pipe closed on reader.Dispose()
            });
        }

        public async Task<List<T>> RunPipeActionForContent(HttpContent httpContent, CancellationToken cancellationToken)
        {
            _runtimeActions = new BufferBlock<Func<CancellationToken, Task<T>>>();

            var multiPartTask = Task.Run(async () =>
            {
                //We need to call this first so the main stream gets parsed into FileStreams.
                try
                {
                    var readAsMultiPartTask = await httpContent.ReadAsMultipartAsync(_streamProvider, cancellationToken);
                }
                finally
                {
                    _runtimeActions.Complete();
                }
            });

            var results = new List<T>();
            var exceptions = new List<Exception>();

            try
            {
                while (await _runtimeActions.OutputAvailableAsync(cancellationToken))
                {
                    var runtimeAction = await _runtimeActions.ReceiveAsync(cancellationToken);

                    try
                    {
                        results.Add(await runtimeAction(cancellationToken));
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }
            }
            finally
            {
                _runtimeActions.Complete();
                //makes sure the multipart task finalizes
                await multiPartTask;
            }

            if (exceptions.Count != 0)
            {
                if (exceptions.Count == 1)
                {
                    throw exceptions[0];
                }
                else
                {
                    throw new AggregateException(exceptions);
                }
            }

            return results;
        }
    }
}
