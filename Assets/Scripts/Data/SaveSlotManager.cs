using System;
using System.IO;
using UnityEngine;

namespace BlackSmith.Data
{
    /// <summary>
    /// 8개 슬롯 저장/불러오기. 슬롯 번호는 0~7이고 파일은 persistentDataPath/saves/slot_N.json.
    /// 자동 저장은 없으며 호출한 때만 저장한다.
    /// </summary>
    public static class SaveSlotManager
    {
        public const int SlotCount = 8;

        private static string Folder => Path.Combine(Application.persistentDataPath, "saves");

        private static string GetPath(int slot) => Path.Combine(Folder, $"slot_{slot}.json");

        public static bool IsValidSlot(int slot) => slot >= 0 && slot < SlotCount;

        public static bool IsEmpty(int slot)
        {
            return !IsValidSlot(slot) || !File.Exists(GetPath(slot));
        }

        /// <summary>슬롯의 저장 시각. 비었거나 읽을 수 없으면 false.</summary>
        public static bool TryGetSavedAt(int slot, out DateTime savedAt)
        {
            savedAt = default;
            if (!TryReadFile(slot, out SaveFile file)) return false;
            return file.TryGetSavedAt(out savedAt);
        }

        public static bool Save(int slot, PlayerData player)
        {
            if (!IsValidSlot(slot) || player == null) return false;

            string path = GetPath(slot);
            string tempPath = path + ".tmp";
            try
            {
                Directory.CreateDirectory(Folder);
                string json = JsonUtility.ToJson(new SaveFile(player), true);
                File.WriteAllText(tempPath, json);

                // 임시 파일에 먼저 쓰고 교체해서 저장 도중 종료돼도 기존 파일이 깨지지 않게 한다.
                if (File.Exists(path)) File.Delete(path);
                File.Move(tempPath, path);
                player.MarkSaved();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSlotManager] 슬롯 {slot} 저장 실패: {e.Message}");
                return false;
            }
        }

        public static bool TryLoad(int slot, out PlayerData player)
        {
            player = null;
            if (!TryReadFile(slot, out SaveFile file)) return false;

            if (file.Player == null)
            {
                Debug.LogWarning($"[SaveSlotManager] 슬롯 {slot}에 플레이어 데이터가 없습니다.");
                return false;
            }

            player = file.Player;
            return true;
        }

        public static void Delete(int slot)
        {
            if (!IsValidSlot(slot)) return;
            try
            {
                string path = GetPath(slot);
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSlotManager] 슬롯 {slot} 삭제 실패: {e.Message}");
            }
        }

        private static bool TryReadFile(int slot, out SaveFile file)
        {
            file = null;
            if (IsEmpty(slot)) return false;

            try
            {
                file = JsonUtility.FromJson<SaveFile>(File.ReadAllText(GetPath(slot)));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSlotManager] 슬롯 {slot} 읽기 실패: {e.Message}");
                return false;
            }

            if (file == null || file.Version < 1 || file.Version > SaveFile.CurrentVersion)
            {
                Debug.LogWarning($"[SaveSlotManager] 슬롯 {slot}의 파일 형식을 알 수 없습니다.");
                file = null;
                return false;
            }
            return true;
        }
    }
}
