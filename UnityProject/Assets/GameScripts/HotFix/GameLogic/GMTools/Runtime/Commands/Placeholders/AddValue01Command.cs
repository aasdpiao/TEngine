using UnityEngine;

public class AddValue01Command : GMCommandBase
{
    public override string CommandName => "addvalue01";
    public override string Description => "增加测试值 01";
    public override string Usage => "addvalue01 <数量>";
    public override string Alias => "增加01";
    public override string Group => "增加";

    protected override GMCommandParameter[] InitializeParameters()
    {
        return new GMCommandParameter[]
        {
            GMCommandParameter.Integer("数量", "要增加的数量")
                .SetRequired()
                .SetOptional("1")
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