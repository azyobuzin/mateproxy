namespace MateProxy.Options
{
    public class RequestBodyCaptureOptions : BodyCaptureOptions
    {
        public RequestBodyCaptureOptions()
        {
            this.Storage = BodyStorageKind.InMemory;
            this.Directory = "./Captures/RequestBodies";
            this.RedisKeyPrefix = "MateProxy:RequestBodies:";
        }

        public override void Validate() => this.Validate("RequestBodyCapture");
    }

    public class ResponseBodyCaptureOptions : BodyCaptureOptions
    {
        public ResponseBodyCaptureOptions()
        {
            this.Storage = BodyStorageKind.InMemory;
            this.Directory = "./Captures/ResponseBodies";
            this.RedisKeyPrefix = "MateProxy:ResponseBodies:";
        }

        public override void Validate() => this.Validate("ResponseBodyCapture");
    }

    public abstract class BodyCaptureOptions : CaptureOptionsBase<BodyStorageKind>
    {
        protected override bool StorageIsRedis() => this.Storage == BodyStorageKind.Redis;

        protected override bool StorageIsFile() => this.Storage == BodyStorageKind.File;

        protected override void Validate(string configKey)
        {
            switch (this.Storage)
            {
                case BodyStorageKind.None:
                case BodyStorageKind.InMemory:
                case BodyStorageKind.Redis:
                case BodyStorageKind.File:
                    break;
                default:
                    throw new OptionValidationException($"{configKey}:Storage が受理できない値 '{this.Storage}' です。");
            }

            base.Validate(configKey);
        }
    }

    public enum BodyStorageKind
    {
        /// <summary>
        /// キャプチャを行いません。
        /// </summary>
        None,
        InMemory,
        Redis,
        File,
    }
}
