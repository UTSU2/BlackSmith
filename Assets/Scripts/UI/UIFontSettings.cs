using UnityEngine;

namespace BlackSmith.UI
{
    /// <summary>
    /// UI 글꼴 설정(정적 데이터). 폰트 크기가 boldMinFontSize 이상이면 굵은 글꼴, 그 미만이면 가는 글꼴을 쓴다.
    /// 적용은 UIFonts가 한다.
    /// </summary>
    [CreateAssetMenu(fileName = "UIFontSettings", menuName = "BlackSmith/UI Font Settings")]
    public class UIFontSettings : ScriptableObject
    {
        public const string ResourcePath = "UI/UIFontSettings";

        [Tooltip("제목 등 큰 글씨에 쓰는 글꼴.")]
        [SerializeField] private Font boldFont;
        [Tooltip("그 외 글씨에 쓰는 글꼴.")]
        [SerializeField] private Font lightFont;
        [Tooltip("폰트 크기가 이 값 이상이면 굵은 글꼴을 쓴다.")]
        [Min(1)]
        [SerializeField] private int boldMinFontSize = 48;

        public static UIFontSettings Load()
        {
            return Resources.Load<UIFontSettings>(ResourcePath);
        }

        /// <summary>폰트 크기에 맞는 글꼴. 해당 글꼴이 비어 있으면 null.</summary>
        public Font Select(int fontSize)
        {
            return fontSize >= boldMinFontSize ? boldFont : lightFont;
        }
    }
}
