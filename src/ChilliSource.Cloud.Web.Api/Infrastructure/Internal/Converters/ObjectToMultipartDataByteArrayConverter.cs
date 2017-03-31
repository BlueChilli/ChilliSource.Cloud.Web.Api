using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.IO;
using System.Web;
using System.Threading.Tasks;
using System.Threading;

namespace ChilliSource.Cloud.Web.Api.Internal
{   
    internal class ObjectToMultipartDataWriter
    {
        Stream _outputStream;
        long _bytesWritten;
        private const int BUFFER_SIZE = 4096;

        public long BytesWritten { get { return _bytesWritten; } }

        public ObjectToMultipartDataWriter(Stream outputStream)
        {
            if (outputStream == null)
                throw new ArgumentNullException("outputStream");

            _outputStream = new BufferedStream(outputStream, BUFFER_SIZE);
            _bytesWritten = 0;
        }

        public async Task WriteAsync(object value, string boundary, CancellationToken cancellationToken)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            if (String.IsNullOrWhiteSpace(boundary))
                throw new ArgumentNullException("boundary");

            List<KeyValuePair<string, object>> propertiesList = ConvertObjectToFlatPropertiesList(value);
            await WriteMultipartFormDataBytes(propertiesList, boundary, cancellationToken);
            await _outputStream.FlushAsync().ConfigureAwait(false);
        }

        private List<KeyValuePair<string, object>> ConvertObjectToFlatPropertiesList(object value)
        {
            var propertiesList = new List<KeyValuePair<string, object>>();
            if (value is FormData)
            {
                FillFlatPropertiesListFromFormData((FormData)value, propertiesList);
            }
            else
            {
                FillFlatPropertiesListFromObject(value, "", propertiesList);
            }

            return propertiesList;
        }

        private void FillFlatPropertiesListFromFormData(FormData formData, List<KeyValuePair<string, object>> propertiesList)
        {
            foreach (var field in formData.Fields)
            {
                propertiesList.Add(new KeyValuePair<string, object>(field.Name, field.Value));
            }
            foreach (var field in formData.Files)
            {
                propertiesList.Add(new KeyValuePair<string, object>(field.Name, field.Value));
            }
        }

        private void FillFlatPropertiesListFromObject(object obj, string prefix, List<KeyValuePair<string, object>> propertiesList)
        {
            if (obj != null)
            {
                Type type = obj.GetType();

                if (obj is IDictionary)
                {
                    var dict = obj as IDictionary;
                    int index = 0;
                    foreach (var key in dict.Keys)
                    {
                        string indexedKeyPropName = String.Format("{0}[{1}].Key", prefix, index);
                        FillFlatPropertiesListFromObject(key, indexedKeyPropName, propertiesList);

                        string indexedValuePropName = String.Format("{0}[{1}].Value", prefix, index);
                        FillFlatPropertiesListFromObject(dict[key], indexedValuePropName, propertiesList);

                        index++;
                    }
                }
                else if (obj is ICollection)
                {
                    var list = obj as ICollection;
                    int index = 0;
                    foreach (var indexedPropValue in list)
                    {
                        string indexedPropName = String.Format("{0}[{1}]", prefix, index);
                        FillFlatPropertiesListFromObject(indexedPropValue, indexedPropName, propertiesList);

                        index++;
                    }
                }
                else if (type.IsCustomNonEnumerableType())
                {
                    foreach (var propertyInfo in type.GetPublicAccessibleProperties())
                    {
                        string propName = String.IsNullOrWhiteSpace(prefix)
                                              ? propertyInfo.Name
                                              : String.Format("{0}.{1}", prefix, propertyInfo.Name);
                        object propValue = propertyInfo.GetValue(obj);

                        FillFlatPropertiesListFromObject(propValue, propName, propertiesList);
                    }
                }
                else
                {
                    propertiesList.Add(new KeyValuePair<string, object>(prefix, obj));
                }
            }
        }

        private async Task Write(byte[] bytes, int offset, CancellationToken cancellationToken)
        {
            var count = bytes.Length - offset;
            await _outputStream.WriteAsync(bytes, 0, count)
                    .ContinueWith(t => _bytesWritten += count)  //It should be safe to add without lock because the caller should Await the operation
                    .ConfigureAwait(false);
        }

        private async Task WriteFrom(Stream source, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[32 * 1024];
            while (true)
            {
                var bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                
                if (bytesRead != 0)
                    await _outputStream.WriteAsync(buffer, 0, bytesRead, cancellationToken)
                            .ContinueWith(t => _bytesWritten += bytesRead, TaskContinuationOptions.OnlyOnRanToCompletion) //It should be safe to add without lock because the caller should Await the operation
                            .ConfigureAwait(false);
                else
                    break;
            }
        }

        private async Task WriteMultipartFormDataBytes(List<KeyValuePair<string, object>> postParameters, string boundary, CancellationToken cancellationToken)
        {
            Encoding encoding = Encoding.UTF8;
            bool needsCLRF = false;

            foreach (var param in postParameters)
            {
                // Add a CRLF to allow multiple parameters to be added.
                // Skip it on the first parameter, add it to subsequent parameters.
                if (needsCLRF)
                    await this.Write(encoding.GetBytes("\r\n"), 0, cancellationToken);

                needsCLRF = true;

                if (param.Value is HttpPostedFileBase)
                {
                    HttpPostedFileBase HttpPostedFileBaseToUpload = (HttpPostedFileBase)param.Value;

                    // Add just the first part of this param, since we will write the file data directly to the Stream
                    string header =
                        string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"\r\nContent-Type: {3}\r\n\r\n",
                            boundary,
                            param.Key,
                            HttpPostedFileBaseToUpload.FileName ?? param.Key,
                            HttpPostedFileBaseToUpload.ContentType ?? "application/octet-stream");

                    await this.Write(encoding.GetBytes(header), 0, cancellationToken);

                    if (HttpPostedFileBaseToUpload.InputStream != null)
                    {
                        // Write the file data directly to the Stream, rather than serializing it to a string.
                        await this.WriteFrom(HttpPostedFileBaseToUpload.InputStream, cancellationToken);
                    }
                }
                else
                {
                    string objString = "";
                    if (param.Value != null)
                    {
                        var typeConverter = param.Value.GetType().GetToStringConverter();
                        if (typeConverter != null)
                        {
                            objString = typeConverter.ConvertToString(null, CultureInfo.CurrentCulture, param.Value);
                        }
                        else
                        {
                            throw new Exception(String.Format("Type \"{0}\" cannot be converted to string", param.Value.GetType().FullName));
                        }
                    }

                    string postData =
                        string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}",
                                      boundary,
                                      param.Key,
                                      objString);
                    await this.Write(encoding.GetBytes(postData), 0, cancellationToken);
                }
            }

            // Add the end of the request.  Start with a newline
            string footer = "\r\n--" + boundary + "--\r\n";
            await this.Write(encoding.GetBytes(footer), 0, cancellationToken);
        }
    }
}
