using System;
using System.Collections.Generic;
using System.IO;
using EchoForum.Application;
using EchoForum.Domain;
using UnityEngine;

namespace EchoForum.Infrastructure
{
    /// <summary>Single local MVP save. It persists progress, never static ScriptableObject content.</summary>
    public sealed class JsonGameSaveRepository : IGameSaveRepository
    {
        private const string SaveName = "echo-archive-save.json";
        private const string LegacyCaseSaveName = "echo-archive-case.json";
        private readonly string directory;
        private readonly string path;

        public JsonGameSaveRepository() : this(UnityEngine.Application.persistentDataPath) { }
        public JsonGameSaveRepository(string directory)
        {
            this.directory = directory;
            path = Path.Combine(directory, SaveName);
        }

        public GameSaveLoadResult Load()
        {
            if (!File.Exists(path)) return MigrateLegacyCaseSave();
            try
            {
                var json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json) || !json.Contains("\"SaveVersion\"")) return new GameSaveLoadResult(GameSaveLoadStatus.Corrupt, null, "存档不可用：内容无法识别。新建档案前会保留备份。");
                var save = JsonUtility.FromJson<GameSaveData>(json);
                if (save == null || save.SaveVersion < 1) return new GameSaveLoadResult(GameSaveLoadStatus.Corrupt, null, "存档不可用：版本不受支持。新建档案前会保留备份。");
                Normalize(save);
                return new GameSaveLoadResult(GameSaveLoadStatus.Valid, save, null);
            }
            catch (Exception)
            {
                return new GameSaveLoadResult(GameSaveLoadStatus.Corrupt, null, "存档不可用：读取失败。新建档案前会保留备份。");
            }
        }

        public void Save(GameSaveData save)
        {
            if (save == null) return;
            Normalize(save);
            Directory.CreateDirectory(directory);
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(save, true));
            File.Copy(temporary, path, true);
            File.Delete(temporary);
        }

        public void BackupUnreadableSave()
        {
            if (!File.Exists(path)) return;
            var backup = path + ".corrupt-" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + ".json";
            File.Copy(path, backup, false);
        }

        private GameSaveLoadResult MigrateLegacyCaseSave()
        {
            var legacyPath = Path.Combine(directory, LegacyCaseSaveName);
            if (!File.Exists(legacyPath)) return new GameSaveLoadResult(GameSaveLoadStatus.Missing, null, null);
            try
            {
                var snapshots = ReadLegacySnapshots(File.ReadAllText(legacyPath));
                if (snapshots.Count == 0) return new GameSaveLoadResult(GameSaveLoadStatus.Missing, null, null);
                var migrated = new GameSaveData { CaseSnapshots = snapshots, ForumState = new ForumState(), Navigation = new NavigationContext() };
                return new GameSaveLoadResult(GameSaveLoadStatus.Valid, migrated, "已发现旧事件进度；创建本地档案后会迁移到总存档。");
            }
            catch (Exception)
            {
                return new GameSaveLoadResult(GameSaveLoadStatus.Missing, null, null);
            }
        }

        private static List<CaseSnapshot> ReadLegacySnapshots(string json)
        {
            var wrapped = JsonUtility.FromJson<LocalCaseSaveFile>(json);
            if (wrapped != null && wrapped.Cases != null && wrapped.Cases.Count > 0) return wrapped.Cases;
            var single = JsonUtility.FromJson<CaseSnapshot>(json);
            return single != null && !string.IsNullOrEmpty(single.CaseId) ? new List<CaseSnapshot> { single } : new List<CaseSnapshot>();
        }

        private static void Normalize(GameSaveData save)
        {
            if (save.ForumState == null) save.ForumState = new ForumState();
            if (save.Navigation == null) save.Navigation = new NavigationContext();
            if (save.CaseSnapshots == null) save.CaseSnapshots = new List<CaseSnapshot>();
            if (save.GlobalFlags == null) save.GlobalFlags = new List<SaveFlag>();
            if (save.SettingsData == null) save.SettingsData = new List<SaveFlag>();
        }
    }
}
