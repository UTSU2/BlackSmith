using System;
using UnityEngine;

namespace BlackSmith.Data
{
    /// <summary>
    /// 슬롯 파일 한 개의 내용. version은 나중에 데이터 구조가 바뀔 때 마이그레이션 기준이 된다.
    /// </summary>
    [Serializable]
    public class SaveFile
    {
        public const int CurrentVersion = 1;

        [SerializeField] private int version = CurrentVersion;
        // JsonUtility는 DateTime을 지원하지 않아 ISO 8601 문자열로 저장한다.
        [SerializeField] private string savedAt;
        [SerializeField] private PlayerData player;

        public int Version => version;
        public string SavedAtRaw => savedAt;
        public PlayerData Player => player;

        public SaveFile(PlayerData player)
        {
            this.player = player;
            savedAt = DateTime.Now.ToString("o");
        }

        public bool TryGetSavedAt(out DateTime time)
        {
            return DateTime.TryParse(savedAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out time);
        }
    }
}
