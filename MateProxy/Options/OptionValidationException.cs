using System;

namespace MateProxy.Options
{
    public class OptionValidationException : Exception
    {
        public OptionValidationException(string message)
            : base(message)
        {
        }

        public OptionValidationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
