using System;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Primitives;
using Rin.Core;
using Rin.Core.Record;

namespace MateProxy.BodyDataTransformers
{
    public class JsonApplicationTransformer : IRequestBodyDataTransformer, IResponseBodyDataTransformer
    {
        public bool CanTransform(HttpRequestRecord record, StringValues contentTypeHeaderValues)
        {
            // application/foo+json のように JSON っぽい Content-Type ならば、 application/json に書き換える
            return contentTypeHeaderValues.Count != 1 && Regex.IsMatch(contentTypeHeaderValues, @"^application/.*\+json\s*($|;)", RegexOptions.IgnoreCase);
        }

        public bool TryTransform(HttpRequestRecord record, ReadOnlySpan<byte> body, StringValues contentTypeHeaderValues, out BodyDataTransformResult result)
        {
            if (this.CanTransform(record, contentTypeHeaderValues))
            {
                result = new BodyDataTransformResult(body.ToArray(), contentTypeHeaderValues, "application/json");
                return true;
            }

            result = default;
            return false;
        }
    }
}
