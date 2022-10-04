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
        /// リクエストに含まれる X-Forwarded ヘッダーを転送先へのリクエストにもコピーするか。
        /// </summary>
        public bool CopyXForwardedHeaders { get; set; } = true;

        /// <summary>
        /// このプロキシを通過したことを表す X-Forwarded ヘッダーを転送先へのリクエストに追加するか。
        /// </summary>
        public bool AddXForwardedHeaders { get; set; } = true;

        /// <summary>
        /// 転送先へのリクエストの Host ヘッダー。
        /// </summary>
        public HostHeaderMode HostHeaderMode { get; set; } = HostHeaderMode.Upstream;

        public void Validate()
        {
            if (string.IsNullOrEmpty(this.Path))
                throw new OptionValidationException($"Path が指定されていません。 (Route '{this.Name}')");

            if (this.Path[0] != '/')
                throw new OptionValidationException($"Path は「/」から開始してください。 (Route '{this.Name}')");

            if (string.IsNullOrEmpty(this.Upstream))
                throw new OptionValidationException($"Upstream が指定されていません。 (Route '{this.Name}')");

            switch (this.HostHeaderMode)
            {
                case HostHeaderMode.Upstream:
                case HostHeaderMode.PreserveHost:
                case HostHeaderMode.FirstXForwardedHost:
                case HostHeaderMode.LastXForwardedHost:
                    break;
                default:
                    throw new OptionValidationException($"HostHeaderMode が受理できない値 '{this.HostHeaderMode}' です。 Upstream、PreserveHost、FirstXForwardedHost、LastXForwardedHost のうちひとつを指定してください。");
            }
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(this.Name)
                ? $"{this.Path} -> {this.Upstream}"
                : $"{this.Name} ({this.Path} -> {this.Upstream})";
        }
    }

    public enum HostHeaderMode
    {
        /// <summary>
        /// <see cref="RouteOptions.Upstream"/> で指定した URI を使用します。
        /// </summary>
        Upstream,
        /// <summary>
        /// リクエストで指定された Host ヘッダーを使用します。
        /// </summary>
        PreserveHost,
        /// <summary>
        /// X-Forwarded-Host の最初の値を使用します。
        /// </summary>
        FirstXForwardedHost,
        /// <summary>
        /// X-Forwarded-Host の最後の値を使用します。
        /// </summary>
        LastXForwardedHost,
    }
}
