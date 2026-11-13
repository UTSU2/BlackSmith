using System.Collections.Generic;
using UnityEngine;

namespace BlackSmith.UI
{
    /// <summary>
    /// 화면 설정(창 모드 / 전체화면, 창 모드 해상도). PlayerPrefs에 저장한다.
    /// 저장된 값이 없으면(처음 실행) 아무것도 바꾸지 않아 기본 전체화면(테두리 없는 창) 그대로다.
    /// 해상도는 창 모드에서만 적용되고 전체화면은 모니터 해상도를 쓴다. 비율은 UI 기준(16:9)에 맞춘 것만 제공한다.
    /// </summary>
    public static class DisplaySettings
    {
        private const string WindowedKey = "Display.Windowed";
        private const string ResolutionKey = "Display.ResolutionIndex";

        private static readonly Vector2Int[] Candidates =
        {
            new Vector2Int(1280, 720),
            new Vector2Int(1600, 900),
            new Vector2Int(1920, 1080),
        };

        private static List<Vector2Int> available;

        /// <summary>창 모드에서 고를 수 있는 해상도. 모니터보다 작은 것만 담고, 하나도 없으면 가장 작은 것 하나를 담는다.</summary>
        public static IReadOnlyList<Vector2Int> AvailableResolutions
        {
            get
            {
                if (available == null)
                {
                    available = new List<Vector2Int>();
                    int maxWidth = Display.main.systemWidth;
                    int maxHeight = Display.main.systemHeight;
                    foreach (Vector2Int size in Candidates)
                    {
                        if (size.x < maxWidth && size.y < maxHeight) available.Add(size);
                    }
                    if (available.Count == 0) available.Add(Candidates[0]);
                }
                return available;
            }
        }

        /// <summary>창 모드를 쓰는가. 저장된 값이 없으면 전체화면이므로 false.</summary>
        public static bool IsWindowed => PlayerPrefs.GetInt(WindowedKey, 0) == 1;

        /// <summary>창 모드 해상도 목록에서 고른 위치. 목록 범위 안으로 맞춰서 돌려준다.</summary>
        public static int ResolutionIndex
        {
            get
            {
                int index = PlayerPrefs.GetInt(ResolutionKey, 0);
                return Mathf.Clamp(index, 0, AvailableResolutions.Count - 1);
            }
        }

        public static Vector2Int CurrentResolution => AvailableResolutions[ResolutionIndex];

        public static void SetWindowed(bool windowed)
        {
            PlayerPrefs.SetInt(WindowedKey, windowed ? 1 : 0);
            PlayerPrefs.Save();
            Apply();
        }

        /// <summary>해상도를 목록 안에서 고른다. 범위를 벗어나면 맞춰서 저장하고, 창 모드일 때만 바로 적용한다.</summary>
        public static void SetResolutionIndex(int index)
        {
            index = Mathf.Clamp(index, 0, AvailableResolutions.Count - 1);
            PlayerPrefs.SetInt(ResolutionKey, index);
            PlayerPrefs.Save();
            if (IsWindowed) Apply();
        }

        private static void Apply()
        {
            if (IsWindowed)
            {
                Vector2Int size = CurrentResolution;
                Screen.SetResolution(size.x, size.y, FullScreenMode.Windowed);
            }
            else
            {
                // 전체화면은 모니터 해상도로 돌아간다. (창 모드에서 줄어든 상태에서 바꿔도 모니터 크기를 쓴다)
                Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, FullScreenMode.FullScreenWindow);
            }
        }

        // 저장된 값이 있을 때만 실행 시 적용한다. 에디터에서는 Game 뷰 크기를 건드리지 않는다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplySaved()
        {
            available = null;
            if (Application.isEditor) return;
            if (!PlayerPrefs.HasKey(WindowedKey)) return;
            Apply();
        }
    }
}
