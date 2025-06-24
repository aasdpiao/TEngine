using System;
using System.Collections.Generic;
using System.Linq;
using GameLogic;
using UnityEngine;

public class GMCommandManager
{
    private static GMCommandManager instance;
    public static GMCommandManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new GMCommandManager();
            }
            return instance;
        }
    }

    private Dictionary<string, GMCommandBase> commandDict = new Dictionary<string, GMCommandBase>();

    private GMCommandManager()
    {
        RegisterCommandsAutomatically();
    }

    private void RegisterCommandsAutomatically()
    {
        // 使用生成的代码注册命令
        GMCommandRegistry.RegisterAllCommands(this);
    }

    public void RegisterCommand(GMCommandBase command)
    {
        string commandName = command.CommandName.ToLower();
        if (!commandDict.ContainsKey(commandName))
        {
            commandDict.Add(commandName, command);
        }
        else
        {
            Debug.LogWarning($"命令 {commandName} 已经存在，跳过注册");
        }
    }

    public bool ExecuteCommand(string commandLine)
    {
        string[] parts = commandLine.Split(' ');
        if (parts.Length == 0) return false;

        string commandName = parts[0].ToLower();
        if (commandDict.TryGetValue(commandName, out GMCommandBase command))
        {
            try
            {
                string[] parameters = new string[parts.Length - 1];
                Array.Copy(parts, 1, parameters, 0, parts.Length - 1);
                return command.Execute(parameters);
            }
            catch (Exception e)
            {
                Debug.LogError($"执行命令 {commandName} 时发生错误: {e.Message}");
                return false;
            }
        }

        Debug.LogError($"未知命令: {commandName}");
        return false;
    }

    public List<string> GetAllCommands()
    {
        return commandDict.Values
            .Select(cmd => $"{cmd.Alias}({cmd.CommandName}) - {cmd.Description}")
            .ToList();
    }

    public string GetCommandUsage(string commandName)
    {
        if (commandDict.TryGetValue(commandName.ToLower(), out GMCommandBase command))
        {
            return $"用法: {command.Usage}\n描述: {command.Description}";
        }
        return "未知命令";
    }

    public string GetCommandAlias(string commandName)
    {
        if (commandDict.TryGetValue(commandName.ToLower(), out GMCommandBase command))
        {
            return command.Alias;
        }
        return commandName;
    }

    public IEnumerable<string> GetAllGroups()
    {
        return commandDict.Values
            .Select(cmd => cmd.Group)
            .Distinct()
            .OrderBy(g => g);
    }

    public string GetCommandGroup(string commandName)
    {
        if (commandDict.TryGetValue(commandName.ToLower(), out GMCommandBase command))
        {
            return command.Group;
        }
        return "其他";
    }

    public IEnumerable<GMCommandBase> GetCommandsByGroup(string group)
    {
        return commandDict.Values
            .Where(cmd => cmd.Group == group)
            .OrderBy(cmd => cmd.Alias);
    }

    public GMCommandBase GetCommand(string commandName)
    {
        if (commandDict.TryGetValue(commandName.ToLower(), out GMCommandBase command))
        {
            return command;
        }
        return null;
    }
}
