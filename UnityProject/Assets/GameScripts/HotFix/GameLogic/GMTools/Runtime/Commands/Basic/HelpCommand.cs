using UnityEngine;

public class HelpCommand : GMCommandBase
{
    public override string CommandName => "help";
    public override string Description => "显示所有可用的命令或特定命令的用法";
    public override string Usage => "help [命令名称]";
    public override string Alias => "帮助";
    public override string Group => "示例命令";

    public override bool Execute(string[] parameters)
    {
        if (parameters.Length == 0 || string.IsNullOrEmpty(parameters[0]))
        {
            // 显示所有命令
            var commands = GMCommandManager.Instance.GetAllCommands();
            Debug.Log("可用命令列表:");
            foreach (var command in commands)
            {
                Debug.Log(command);
            }
        }
        else
        {
            // 显示特定命令的用法
            string usage = GMCommandManager.Instance.GetCommandUsage(parameters[0]);
            Debug.Log(usage);
        }
        return true;
    }
} 