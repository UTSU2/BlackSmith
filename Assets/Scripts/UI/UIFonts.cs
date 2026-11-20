using UnityEngine;
using UnityEngine.UI;

namespace BlackSmith.UI
{
    /// <summary>
    /// UI Text에 글꼴을 적용한다. UIFontSettings의 크기 기준으로 굵은/가는 글꼴을 고르고,
    /// 설정 에셋이나 글꼴이 없으면 예전처럼 OS 한글 글꼴(맑은 고딕)로 대신한다.
    /// </summary>
    public static class UIFonts
    {
        private static readonly string[] KoreanFonts = { "Malgun Gothic", "맑은 고딕", "Arial" };

        private static UIFontSettings settings;
        private static Font osFont;

        public static void Apply(Text text)
        {
            if (text == null) return;
            text.font = Resolve(text.fontSize);
        }

        /// <summary>root 아래(비활성 포함)의 모든 Text에 적용한다. skip은 건너뛴다.</summary>
        public static void ApplyAll(Component root, Text skip = null)
        {
            if (root == null) return;
            foreach (Text text in root.GetComponentsInChildren<Text>(true))
            {
                if (text == skip) continue;
                Apply(text);
            }
        }

        private static Font Resolve(int fontSize)
        {
            if (settings == null) settings = UIFontSettings.Load();
            Font font = settings != null ? settings.Select(fontSize) : null;
            if (font != null) return font;

            if (osFont == null) osFont = Font.CreateDynamicFontFromOSFont(KoreanFonts, 36);
            return osFont;
        }
    }
}
