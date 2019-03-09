using System;
using System.IO;
using System.IO.Compression;
using Microsoft.Extensions.Primitives;
using Rin.Core;
using Rin.Core.Record;

namespace MateProxy
{
    public class UngzipTransformer : BodyDataTransformer
    {
        public override bool CanTransform(HttpRequestRecord record, StringValues contentTypeHeaderValues)
        {
            if (!record.ResponseHeaders.TryGetValue("Content-Encoding", out var contentEncodingValues)
                || contentEncodingValues.Count != 1)
                return false;

            var contentEncoding = contentEncodingValues[0];

            return string.Equals(contentEncoding, "gzip", StringComparison.OrdinalIgnoreCase)
                || string.Equals(contentEncoding, "deflate", StringComparison.OrdinalIgnoreCase);
        }

        public override BodyDataTransformResult Transform(HttpRequestRecord record, byte[] body, StringValues contentTypeHeaderValues)
        {
            var contentEncoding = (string)record.ResponseHeaders["Content-Encoding"];
            byte[] result;

            if (string.Equals(contentEncoding, "gzip", StringComparison.OrdinalIgnoreCase))
            {
                using (var source = new MemoryStream(body))
                using (var destination = new MemoryStream())
                using (var gzip = new GZipStream(source, CompressionMode.Decompress))
                {
                    gzip.CopyTo(destination);
                    result = destination.ToArray();
                }
            }
            else if (string.Equals(contentEncoding, "deflate", StringComparison.OrdinalIgnoreCase))
            {
                using (var source = new MemoryStream(body))
                using (var destination = new MemoryStream())
                using (var gzip = new DeflateStream(source, CompressionMode.Decompress))
                {
                    gzip.CopyTo(destination);
                    result = destination.ToArray();
                }
            }
            else
            {
                throw new InvalidOperationException(contentEncoding);
            }

            var contentType = (string)contentTypeHeaderValues;
            return new BodyDataTransformResult(result, contentType, contentType);
        }
    }
}
