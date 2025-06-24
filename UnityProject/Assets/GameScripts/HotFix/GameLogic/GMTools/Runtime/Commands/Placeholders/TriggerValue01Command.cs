using UnityEngine;

public class TriggerValue01Command : GMCommandBase
{
    public override string CommandName => "triggervalue01";
    public override string Description => "触发测试值 01";
    public override string Usage => "triggervalue01 <状态>";
    public override string Alias => "触发01";
    public override string Group => "触发";

    protected override GMCommandParameter[] InitializeParameters()
    {
        return new GMCommandParameter[]
        {
            GMCommandParameter.Integer("状态", "开启或关闭")
                .SetRequired()
                .SetOptional("false")
        };
    }

    public override bool Execute(string[] parameters)
    {
        if (parameters.Length != 1)
        {
            Debug.LogError("参数错误! 用法: " + Usage);
            return false;
        }

        if (!int.TryParse(parameters[0], out int value))
        {
            Debug.LogError("参数必须是一个有效的整数!");
            return false;
        }
        Debug.Log($"执行命令，参数值: {value}");

        return true;
    }
}