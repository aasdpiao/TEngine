using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

public class GMCommandPlaceholdersGenerator : EditorWindow
{
    [MenuItem("Tools/Generate Command Placeholders", false, 100)]
    public static void GenerateCommandPlaceholders()
    {
        GenerateCommands();
    }
    public static void GenerateCommands()
    {
        string basePath = "Assets/GameScripts/HotFix/GameLogic/GMTools/Runtime/Commands/Placeholders";
        
        // 确保目录存在
        if (!Directory.Exists(basePath))
        {
            Directory.CreateDirectory(basePath);
        }

        // 定义不同类型的命令模板
        var commandTypes = new[]
        {
            new { 
                Prefix = "Set", 
                Description = "设置", 
                ParamType = "Integer",
                ParamName = "数值",
                ParamDesc = "要设置的数值",
                DefaultValue = "0",
                Group = "设置"
            },
            new { 
                Prefix = "Add", 
                Description = "增加", 
                ParamType = "Integer",
                ParamName = "数量",
                ParamDesc = "要增加的数量",
                DefaultValue = "1",
                Group = "增加"
            },
            new { 
                Prefix = "Trigger", 
                Description = "触发", 
                ParamType = "Integer",
                ParamName = "状态",
                ParamDesc = "开启或关闭",
                DefaultValue = "false",
                Group = "触发"
            }
        };

        int index = 1;
        foreach (var type in commandTypes)
        {
            // 每种类型生成20个命令
            for (int i = 1; i <= 1; i++)
            {
                string commandName = $"{type.Prefix}Value{i:D2}";
                string filePath = $"{basePath}/{commandName}Command.cs";
                
                string commandContent = $@"using UnityEngine;

public class {commandName}Command : GMCommandBase
{{
    public override string CommandName => ""{commandName.ToLower()}"";
    public override string Description => ""{type.Description}测试值 {i:D2}"";
    public override string Usage => ""{commandName.ToLower()} <{type.ParamName}>"";
    public override string Alias => ""{type.Description}{i:D2}"";
    public override string Group => ""{type.Group}"";

    protected override GMCommandParameter[] InitializeParameters()
    {{
        return new GMCommandParameter[]
        {{
            GMCommandParameter.{type.ParamType}(""{type.ParamName}"", ""{type.ParamDesc}"")
                .SetRequired()
                .SetOptional(""{type.DefaultValue}"")
        }};
    }}

    public override bool Execute(string[] parameters)
    {{
        if (parameters.Length != 1)
        {{
            Debug.LogError(""参数错误! 用法: "" + Usage);
            return false;
        }}

        {GetExecuteLogic(type.ParamType)}

        return true;
    }}
}}";

                File.WriteAllText(filePath, commandContent, Encoding.UTF8);
                index++;
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"生成完成：{index-1}个带参数的测试命令");
    }

    private static string GetExecuteLogic(string paramType)
    {
        switch (paramType)
        {
            case "Integer":
                return @"if (!int.TryParse(parameters[0], out int value))
        {
            Debug.LogError(""参数必须是一个有效的整数!"");
            return false;
        }
        Debug.Log($""执行命令，参数值: {value}"");";

            case "Boolean":
                return @"if (!bool.TryParse(parameters[0], out bool value))
        {
            Debug.LogError(""参数必须是true或false!"");
            return false;
        }
        Debug.Log($""执行命令，参数值: {value}"");";

            default:
                return "Debug.Log($\"执行命令，参数值: {parameters[0]}\");";
        }
    }
}