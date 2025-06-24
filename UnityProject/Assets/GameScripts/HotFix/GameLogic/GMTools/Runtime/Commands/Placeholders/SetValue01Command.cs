using UnityEngine;

public class SetValue01Command : GMCommandBase
{
    public override string CommandName => "setvalue01";
    public override string Description => "设置测试值 01";
    public override string Usage => "setvalue01 <数值>";
    public override string Alias => "设置01";
    public override string Group => "设置";

    protected override GMCommandParameter[] InitializeParameters()
    {
        return new GMCommandParameter[]
        {
            GMCommandParameter.Integer("数值", "要设置的数值")
                .SetRequired()
                .SetOptional("0")
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