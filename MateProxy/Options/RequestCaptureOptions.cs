namespace MateProxy.Options
{
    public class RequestCaptureOptions : CaptureOptionsBase<RequestStorageKind>
    {
        public RequestCaptureOptions()
        {
            this.Storage = RequestStorageKind.InMemory;
            this.Directory = "./Captures/Requests";
            this.RedisKeyPrefix = "MateProxy:Requests:";
        }

        protected override bool StorageIsRedis() => this.Storage == RequestStorageKind.Redis;

        protected override bool StorageIsFile() => this.Storage == RequestStorageKind.File;

        public override void Validate()
        {
            switch (this.Storage)
            {
                case RequestStorageKind.InMemory:
                case RequestStorageKind.Redis:
                case RequestStorageKind.File:
                    break;
                default:
                    throw new OptionValidationException($"RequestCapture:Storage が受理できない値 '{this.Storage}' です。");
            }

            this.Validate("RequestCapture");
        }
    }

    public enum RequestStorageKind
    {
        InMemory,
        Redis,
        File,
    }
}
