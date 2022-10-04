using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Primitives;
using Rin.Core;
using Rin.Core.Record;

namespace MateProxy.BodyDataTransformers
{
    public class BodyDataTransformerChain : IRequestBodyDataTransformer, IResponseBodyDataTransformer
    {
        private readonly IReadOnlyList<IBodyDataTransformer> _transformers;

        public BodyDataTransformerChain(IReadOnlyList<IBodyDataTransformer> transformers)
        {
            this._transformers = transformers;
        }

        public bool CanTransform(HttpRequestRecord record, StringValues contentTypeHeaderValues)
        {
            return this._transformers.Any(transformer => transformer.CanTransform(record, contentTypeHeaderValues));
        }

        public bool TryTransform(HttpRequestRecord record, ReadOnlySpan<byte> body, StringValues contentTypeHeaderValues, out BodyDataTransformResult result)
        {
            var transformed = false;
            result = default;

            foreach (var transformer in this._transformers)
            {
                var prevResult = result;
                if (transformer.TryTransform(record, body, contentTypeHeaderValues, out result))
                {
                    transformed = true;
                    body = result.Body;
                    contentTypeHeaderValues = result.TransformedContentType;
                }
                else
                {
                    result = prevResult;
                }
            }

            return transformed;
        }

        public static IRequestBodyDataTransformer DefaultRequestBodyDataTransformer { get; } = new BodyDataTransformerChain(new IBodyDataTransformer[] {
            new JsonApplicationTransformer(),
        });

        public static IResponseBodyDataTransformer DefaultResponseBodyDataTransformer { get; } = new BodyDataTransformerChain(new IBodyDataTransformer[] {
            new UngzipTransformer(),
            new JsonApplicationTransformer(),
        });
    }
}
