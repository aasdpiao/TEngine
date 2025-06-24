using UnityEngine;

public class SetLevelCommand : GMCommandBase
{
    public override string CommandName => "setlevel";
    public override string Description => "设置角色等级";
    public override string Usage => "setlevel <等级>";
    public override string Alias => "设置等级";
    public override string Group => "示例命令";

    public override bool Execute(string[] parameters)
    {
        if (parameters.Length != 1)
        {
            Debug.LogError("参数错误! 用法: " + Usage);
            return false;
        }

        if (int.TryParse(parameters[0], out int level))
        {
            // 这里实现设置等级的逻辑
            Debug.Log($"设置等级为: {level}");
            return true;
        }

        Debug.LogError("等级必须是一个有效的数字!");
        return false;
    }
} 