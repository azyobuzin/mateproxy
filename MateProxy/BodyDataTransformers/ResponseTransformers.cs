using System;
using System.IO;
using System.IO.Compression;
using Rin.Core;
using Rin.Core.Record;

namespace MateProxy.BodyDataTransformers
{
    public class ResponseTransformers
    {
        public static BodyDataTransformResult Ungzip(HttpRequestRecord record, BodyDataTransformResult previousResult)
        {
            var contentEncoding = (string)record.ResponseHeaders["Content-Encoding"];

            if (string.Equals(contentEncoding, "gzip", StringComparison.OrdinalIgnoreCase))
            {
                using (var source = new MemoryStream(previousResult.Body))
                using (var destination = new MemoryStream())
                using (var gzip = new GZipStream(source, CompressionMode.Decompress))
                {
                    gzip.CopyTo(destination);

                    return new BodyDataTransformResult(
                        destination.ToArray(),
                        previousResult.ContentType,
                        previousResult.TransformedContentType);
                }
            }
            else if (string.Equals(contentEncoding, "deflate", StringComparison.OrdinalIgnoreCase))
            {
                using (var source = new MemoryStream(previousResult.Body))
                using (var destination = new MemoryStream())
                using (var gzip = new DeflateStream(source, CompressionMode.Decompress))
                {
                    gzip.CopyTo(destination);

                    return new BodyDataTransformResult(
                        destination.ToArray(),
                        previousResult.ContentType,
                        previousResult.TransformedContentType);
                }
            }

            return previousResult;
        }
    }
}
