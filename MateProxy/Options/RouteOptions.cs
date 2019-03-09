namespace MateProxy.Options
{
    public class RouteOptions
    {
        /// <summary>
        /// ルート名。表示用なので、省略もできます。
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// このルーティングを行うパス。
        /// 例: <c>/</c>, <c>/foo</c>
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// 転送先を URI で指定します。
        /// 例: <c>https://host</c>
        /// </summary>
        public string Upstream { get; set; }

        /// <summary>
        /// リクエストに含まれる X-Forwarded ヘッダーを転送先へのリクエストにも付与するか。
        /// </summary>
        public bool CopyXForwardedHeaders { get; set; } = true;

        /// <summary>
        /// このプロキシを通過したことを表す X-Forwarded ヘッダーを転送先へのリクエストに付与するか。
        /// </summary>
        public bool AddXForwardedHeaders { get; set; } = true;

        /// <summary>
        /// 証明書の検証をスキップするか。
        /// </summary>
        public bool SkipVerifyServerCertificate { get; set; }

        public void Validate()
        {
            if (string.IsNullOrEmpty(this.Path))
                throw new OptionValidationException($"Path が指定されていません。 (Route '{this.Name}')");

            if (this.Path[0] != '/')
                throw new OptionValidationException($"Path は「/」から開始してください。 (Route '{this.Name}')");

            if (string.IsNullOrEmpty(this.Upstream))
                throw new OptionValidationException($"Upstream が指定されていません。 (Route '{this.Name}')");
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(this.Name)
                ? $"{this.Path} -> {this.Upstream}"
                : $"{this.Name} ({this.Path} -> {this.Upstream})";
        }
    }
}
