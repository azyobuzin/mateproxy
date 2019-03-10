using System.Text.RegularExpressions;
using Rin.Core;
using Rin.Core.Record;

namespace MateProxy.BodyDataTransformers
{
    public class CommonTransformers
    {
        public static BodyDataTransformResult JsonApplication(HttpRequestRecord record, BodyDataTransformResult previousResult)
        {
            // application/foo+json のように JSON っぽい Content-Type ならば、 application/json に書き換える
            if (Regex.IsMatch(previousResult.ContentType, @"^application/.*\+json\s*($|;)", RegexOptions.IgnoreCase))
            {
                return new BodyDataTransformResult(
                    previousResult.Body,
                    previousResult.ContentType,
                    "application/json"
                );
            }

            return previousResult;
        }
    }
}
