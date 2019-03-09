namespace MateProxy.Options
{
    public abstract class CaptureOptionsBase<TStorageKind>
    {
        /// <summary>
        /// レコードの保存先。
        /// </summary>
        public TStorageKind Storage { get; set; }

        /// <summary>
        /// <see cref="Storage"/> が File のとき、レコードを保存するディレクトリを指定します。
        /// </summary>
        public string Directory { get; set; }

        /// <summary>
        /// <see cref="Storage"/> が Redis のとき、 Redis のサーバーへの接続方法を指定します。
        /// 指定方法は https://stackexchange.github.io/StackExchange.Redis/Configuration を参照してください。
        /// </summary>
        public string RedisConnectionConfiguration { get; set; } = "localhost";

        /// <summary>
        /// <see cref="Storage"/> が Redis のとき、キー（リクエストID）の前に付与する文字列を指定します。
        /// </summary>
        public string RedisKeyPrefix { get; set; }

        public abstract void Validate();

        protected abstract bool StorageIsFile();

        protected abstract bool StorageIsRedis();

        protected virtual void Validate(string configKey)
        {
            if (this.StorageIsFile())
            {
                if (string.IsNullOrEmpty(this.Directory))
                    throw new OptionValidationException($"{configKey}:Storage が File ですが、 Directory が指定されていません。");
            }

            if (this.StorageIsRedis())
            {
                if (string.IsNullOrEmpty(this.Directory))
                    throw new OptionValidationException($"{configKey}:Storage が Redis ですが、 RedisConnectionConfiguration が指定されていません。");
            }
        }
    }
}
