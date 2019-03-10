using System.Collections.Generic;
using Microsoft.Extensions.Primitives;
using Rin.Core;
using Rin.Core.Record;

namespace MateProxy.BodyDataTransformers
{
    public class BodyDataTransformerChain : BodyDataTransformer
    {
        public delegate BodyDataTransformResult TransformDelegate(HttpRequestRecord record, BodyDataTransformResult previoutResult);

        public IList<TransformDelegate> Transformers { get; } = new List<TransformDelegate>();

        public override bool CanTransform(HttpRequestRecord record, StringValues contentTypeHeaderValues)
        {
            if (contentTypeHeaderValues.Count != 1) return false;

            return this.Transformers.Count > 0;
        }

        public override BodyDataTransformResult Transform(HttpRequestRecord record, byte[] body, StringValues contentTypeHeaderValues)
        {
            var result = new BodyDataTransformResult(body, contentTypeHeaderValues, "");

            foreach (var transformer in this.Transformers)
                result = transformer(record, result);

            return result;
        }

        public static IBodyDataTransformer DefaultRequestBodyDataTransformer { get; } = new BodyDataTransformerChain()
        {
            Transformers =
            {
                CommonTransformers.JsonApplication,
            }
        };

        public static IBodyDataTransformer DefaultResponseBodyDataTransformer { get; } = new BodyDataTransformerChain()
        {
            Transformers =
            {
                ResponseTransformers.Ungzip,
                CommonTransformers.JsonApplication,
            }
        };
    }
}
