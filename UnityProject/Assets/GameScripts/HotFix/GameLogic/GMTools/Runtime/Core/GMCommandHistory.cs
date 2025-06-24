using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

[Serializable]
public class CommandHistoryEntry
{
    public string command;     // 命令内容
    public string[] parameters; // 参数历史
    public long timestamp;     // 执行时间戳
    public bool success;       // 执行结果

    public CommandHistoryEntry(string command, string[] parameters, bool success)
    {
        this.command = command;
        this.parameters = parameters;
        this.success = success;
        this.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}

[Serializable]
public class CommandHistoryData
{
    public List<CommandHistoryEntry> entries = new List<CommandHistoryEntry>();
    public Dictionary<string, string[]> paramHistory = new Dictionary<string, string[]>();
    public List<FavoriteCommand> favorites = new List<FavoriteCommand>();
}

[Serializable]
public class FavoriteCommand
{
    public string command;      // 命令内容
    public string[] parameters; // 参数
    public string description;  // 收藏描述
    public long addTime;        // 添加时间

    public FavoriteCommand(string command, string[] parameters, string description = "")
    {
        this.command = command;
        this.parameters = parameters;
        this.description = description;
        this.addTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}

public class GMCommandHistory
{
    private const string HISTORY_FILE = "gm_command_history.json";
    private const int MAX_HISTORY = 100;

    private static CommandHistoryData historyData;
    private static string FilePath => Path.Combine(Application.persistentDataPath, HISTORY_FILE);

    public static void Initialize()
    {
        LoadHistory();
    }

    public static void AddCommand(string command, string[] parameters, bool success)
    {
        if (historyData == null)
        {
            LoadHistory();
        }

        historyData.entries.Add(new CommandHistoryEntry(command, parameters, success));
        
        // 保存参数历史
        string commandName = command.Split(' ')[0];
        historyData.paramHistory[commandName] = parameters;

        // 限制历史记录数量
        while (historyData.entries.Count > MAX_HISTORY)
        {
            historyData.entries.RemoveAt(0);
        }

        SaveHistory();
    }

    public static List<CommandHistoryEntry> GetHistory()
    {
        if (historyData == null)
        {
            LoadHistory();
        }
        return historyData.entries;
    }

    public static bool TryGetParamHistory(string commandName, out string[] parameters)
    {
        if (historyData == null)
        {
            LoadHistory();
        }
        return historyData.paramHistory.TryGetValue(commandName, out parameters);
    }

    private static void LoadHistory()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                historyData = JsonUtility.FromJson<CommandHistoryData>(json);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"加载命令历史失败: {e.Message}");
        }

        historyData ??= new CommandHistoryData();
    }

    public static void SaveHistory()
    {
        try
        {
            string json = JsonUtility.ToJson(historyData, true);
            File.WriteAllText(FilePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"保存命令历史失败: {e.Message}");
        }
    }

    public static void AddFavorite(string command, string[] parameters, string description = "")
    {
        if (historyData == null)
        {
            LoadHistory();
        }

        // 检查是否已经收藏
        if (!historyData.favorites.Any(f => f.command == command))
        {
            historyData.favorites.Add(new FavoriteCommand(command, parameters, description));
            SaveHistory();
        }
    }

    public static void RemoveFavorite(string command)
    {
        if (historyData == null)
        {
            LoadHistory();
        }

        historyData.favorites.RemoveAll(f => f.command == command);
        SaveHistory();
    }

    public static List<FavoriteCommand> GetFavorites()
    {
        if (historyData == null)
        {
            LoadHistory();
        }
        return historyData.favorites;
    }

    public static bool IsFavorite(string command)
    {
        if (historyData == null)
        {
            LoadHistory();
        }
        return historyData.favorites.Any(f => f.command == command);
    }

    public static void Clear()
    {
        if (historyData == null)
        {
            LoadHistory();
        }
        
        // 保留收藏记录，只清除历史
        var favorites = historyData.favorites;
        historyData = new CommandHistoryData
        {
            favorites = favorites
        };
        SaveHistory();
    }

    public static void ClearFavorites()
    {
        if (historyData == null)
        {
            LoadHistory();
        }
        historyData.favorites.Clear();
        SaveHistory();
    }
} 