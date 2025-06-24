using UnityEngine;

public class AddItemCommand : GMCommandBase
{
    public override string CommandName => "additem";
    public override string Description => "添加物品到背包";
    public override string Usage => "additem <物品ID> [数量]";
    public override string Alias => "加物品";
    public override string Group => "示例命令";

    // 定义常用物品列表
    private static readonly string[] CommonItems = new[]
    {
        "1001:金币",
        "1002:钻石",
        "2001:初级武器",
        "2002:中级武器",
        "2003:高级武器",
        "3001:生命药水",
        "3002:魔法药水",
        "4001:装备箱",
        "4002:宝石箱"
    };

    protected override GMCommandParameter[] InitializeParameters()
    {
        return new GMCommandParameter[]
        {
            GMCommandParameter.Options("物品ID", "选择要添加的物品", CommonItems),
            GMCommandParameter.Integer("数量", "要添加的物品数量")
                .SetOptional("1")
        };
    }

    public override bool Execute(string[] parameters)
    {
        if (parameters.Length < 1 || parameters.Length > 2)
        {
            Debug.LogError("参数错误! 用法: " + Usage);
            return false;
        }

        // 从选项中提取物品ID
        string itemIdStr = parameters[0].Split(':')[0];
        if (!int.TryParse(itemIdStr, out int itemId))
        {
            Debug.LogError("物品ID必须是一个有效的整数!");
            return false;
        }

        int amount = 1;
        if (parameters.Length == 2 && !int.TryParse(parameters[1], out amount))
        {
            Debug.LogError("数量必须是一个有效的整数!");
            return false;
        }

        // 这里实现添加物品的逻辑
        Debug.Log($"添加物品: ID={itemId}, 数量={amount}");
        return true;
    }
} 