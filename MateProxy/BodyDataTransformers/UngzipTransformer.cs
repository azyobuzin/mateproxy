using System;
using System.IO;
using System.IO.Compression;
using Microsoft.Extensions.Primitives;
using Rin.Core;
using Rin.Core.Record;

namespace MateProxy.BodyDataTransformers
{
    public class UngzipTransformer : IResponseBodyDataTransformer
    {
        public bool CanTransform(HttpRequestRecord record, StringValues contentTypeHeaderValues)
        {
            if (!record.ResponseHeaders.TryGetValue("Content-Encoding", out var contentEncodingValues))
                return false;

            var contentEncoding = (string)contentEncodingValues;
            return IsGzip(contentEncoding) || IsDeflate(contentEncoding);
        }

        public unsafe bool TryTransform(HttpRequestRecord record, ReadOnlySpan<byte> body, StringValues contentTypeHeaderValues, out BodyDataTransformResult result)
        {
            if (!record.ResponseHeaders.TryGetValue("Content-Encoding", out var contentEncodingValues))
            {
                result = default;
                return false;
            }

            var contentEncoding = (string)contentEncodingValues;
            using var destination = new MemoryStream(Math.Max(body.Length, unchecked(body.Length * 2)));

            fixed (byte* p = body)
            {
                using var source = new UnmanagedMemoryStream(p, body.Length);

                if (IsGzip(contentEncoding))
                {
                    using var gzip = new GZipStream(source, CompressionMode.Decompress);
                    gzip.CopyTo(destination);
                }
                else if (IsDeflate(contentEncoding))
                {
                    using var deflate = new DeflateStream(source, CompressionMode.Decompress);
                    deflate.CopyTo(destination);
                }
                else
                {
                    result = default;
                    return false;
                }
            }

            var contentType = (string)contentTypeHeaderValues;
            result = new BodyDataTransformResult(destination.ToArray(), contentType, contentType);
            return true;
        }

        private static bool IsGzip(string contentEncoding) =>
            string.Equals(contentEncoding, "gzip", StringComparison.OrdinalIgnoreCase);

        private static bool IsDeflate(string contentEncoding) =>
            string.Equals(contentEncoding, "deflate", StringComparison.OrdinalIgnoreCase);
    }
}
