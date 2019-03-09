using System;

namespace MateProxy.Options
{
    public class MateProxyOptions
    {
        /// <summary>
        /// 正規表現パターンに一致するパスへのリクエストを除外します。
        /// </summary>
        public string[] ExcludesByPath { get; set; } =
        {
            @"\.js$", @"\.css$", @"\.svg$"
        };

        /// <summary>
        /// 最新何件のリクエストを保存するか。
        /// 0以下を指定すると、無制限に保存します。
        /// </summary>
        public int RetentionMaxRequest { get; set; } = 100;

        /// <summary>
        /// リクエストログを見るページを配置するパス。
        /// </summary>
        public string InspectorPath { get; set; } = "/rin";

        /// <summary>
        /// リクエストとそのヘッダーを記録する方法の設定。
        /// </summary>
        public RequestCaptureOptions RequestCapture { get; set; } = new RequestCaptureOptions();

        /// <summary>
        /// リクエスト本文を記録する方法の設定。
        /// </summary>
        public RequestBodyCaptureOptions RequestBodyCapture { get; set; } = new RequestBodyCaptureOptions();

        /// <summary>
        /// レスポンス本文を記録する方法の設定。
        /// </summary>
        public ResponseBodyCaptureOptions ResponseBodyCapture { get; set; } = new ResponseBodyCaptureOptions();

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

            (this.RequestCapture ?? new RequestCaptureOptions()).Validate();

            (this.RequestBodyCapture ?? new RequestBodyCaptureOptions()).Validate();

            (this.ResponseBodyCapture ?? new ResponseBodyCaptureOptions()).Validate();

            if (this.Routes == null || this.Routes.Length == 0)
                throw new OptionValidationException("ルーティングが 1 件も設定されていません。");

            foreach (var routeOptions in this.Routes)
                routeOptions.Validate();
        }
    }
}
