using System;

namespace MateProxy.Options
{
    public class MateProxyOptions
    {
        /// <summary>
        /// 1件以上指定すると、正規表現パターンに一致するパスへのリクエストのみを記録します。
        /// </summary>
        public string[] IncludePatterns { get; set; }

        /// <summary>
        /// 正規表現パターンに一致するパスへのリクエストを除外します。
        /// </summary>
        public string[] ExcludePatterns { get; set; }

        /// <summary>
        /// 最新何件のリクエストを保存するか。
        /// 0以下を指定すると、無制限に保存します。
        /// </summary>
        public int RetentionMaxRequest { get; set; } = 100;

        /// <summary>
        /// リクエストボディ・レスポンスボディを記録するかどうか。
        /// </summary>
        public bool EnableBodyCapturing { get; set; } = true;

        /// <summary>
        /// リクエストログを見るページを配置するパス。
        /// </summary>
        public string InspectorPath { get; set; } = "/rin";

        /// <summary>
        /// リクエストログの保存先。
        /// </summary>
        public StorageKind Storage { get; set; } = StorageKind.InMemory;

        /// <summary>
        /// <see cref="Storage"/> が <see cref="StorageKind.Redis"/> のとき、 Redis のサーバーへの接続方法を指定します。
        /// 指定方法は https://stackexchange.github.io/StackExchange.Redis/Configuration を参照してください。
        /// </summary>
        public string RedisConnectionConfiguration { get; set; } = "localhost";

        /// <summary>
        /// <see cref="Storage"/> が <see cref="StorageKind.Redis"/> のとき、
        /// Redis に挿入されたデータの有効時間を秒数で指定します。
        /// 0以下を指定すると、有効時間を設定しません。
        /// </summary>
        public double RedisExpirationSeconds { get; set; } = 3600;

        /// <summary>
        /// <see cref="Storage"/> が <see cref="StorageKind.Redis"/> のとき、キー（リクエストID）の前に付与する文字列を指定します。
        /// </summary>
        public string RedisKeyPrefix { get; set; } = "Rin.Storage.";

        /// <summary>
        /// 転送先に HTTPS でリクエストするときに、証明書の検証をスキップするか。
        /// </summary>
        public bool SkipVerifyServerCertificate { get; set; }

        /// <summary>
        /// ルーティング設定
        /// </summary>
        public RouteOptions[] Routes { get; set; } = Array.Empty<RouteOptions>();

        public void Validate()
        {
            if (string.IsNullOrEmpty(this.InspectorPath))
                throw new OptionValidationException("InspectorPath が指定されていません。");

            if (this.InspectorPath[0] != '/')
                throw new OptionValidationException("InspectorPath は「/」から開始してください。");

            switch (this.Storage)
            {
                case StorageKind.InMemory:
                case StorageKind.Redis:
                    break;
                default:
                    throw new OptionValidationException($"Storage が受理できない値 '{this.Storage}' です。 InMemory または Redis を指定してください。");
            }

            if (this.Storage == StorageKind.Redis)
            {
                if (string.IsNullOrEmpty(this.RedisConnectionConfiguration))
                    throw new OptionValidationException($"Storage が Redis ですが、 RedisConnectionConfiguration が指定されていません。");
            }

            if (this.Routes == null || this.Routes.Length == 0)
                throw new OptionValidationException("ルーティングが 1 件も設定されていません。");

            foreach (var routeOptions in this.Routes)
                routeOptions.Validate();
        }
    }

    public enum StorageKind
    {
        InMemory,
        Redis,
    }
}
