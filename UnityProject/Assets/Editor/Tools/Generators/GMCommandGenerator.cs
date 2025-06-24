using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

public class GMCommandGenerator : AssetPostprocessor
{
    // 在Unity编辑器启动时执行
    [InitializeOnLoadMethod]
    static void Initialize()
    {
        GenerateCommandRegistry();
    }

    // 在脚本被修改时执行
    static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        bool shouldGenerate = false;

        // 检查是否有命令相关的脚本被修改
        foreach (string path in importedAssets.Concat(deletedAssets).Concat(movedAssets))
        {
            if (path.EndsWith(".cs") && (path.Contains("/Commands/") || path.Contains("Command.cs")))
            {
                shouldGenerate = true;
                break;
            }
        }

        if (shouldGenerate)
        {
            GenerateCommandRegistry();
        }
    }

    [MenuItem("Tools/Generate Command Registry", false, 100)]
    public static void GenerateCommandRegistry()
    {
        // 扫描所有命令类
        var commandTypes = TypeCache.GetTypesDerivedFrom<GMCommandBase>()
            .Where(t => !t.IsAbstract && t.IsClass)
            .OrderBy(t => t.FullName) // 保持生成代码的稳定性
            .ToList();

        // 检查是否需要重新生成
        string filePath = "Assets/GameScripts/HotFix/GameLogic/GMTools/Runtime/Generated/GMCommandRegistry.cs";
        if (File.Exists(filePath) && !HasCommandsChanged(commandTypes, filePath))
        {
            return; // 如果命令没有变化，不需要重新生成
        }

        // 生成代码
        var code = GenerateRegistryCode(commandTypes);
        
        // 保存生成的代码
        string directoryPath = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        // 如果内容相同，不要重写文件（避免不必要的资源刷新）
        if (File.Exists(filePath))
        {
            string existingContent = File.ReadAllText(filePath);
            if (existingContent == code)
            {
                return;
            }
        }

        File.WriteAllText(filePath, code);
        AssetDatabase.Refresh();
        Debug.Log($"Generated command registry with {commandTypes.Count} commands.");
    }

    private static bool HasCommandsChanged(List<System.Type> currentCommands, string filePath)
    {
        if (!File.Exists(filePath))
            return true;

        string content = File.ReadAllText(filePath);
        foreach (var type in currentCommands)
        {
            if (!content.Contains(type.FullName))
                return true;
        }

        // 检查是否有已删除的命令
        var existingCommands = content.Split('\n')
            .Where(line => line.Contains("manager.RegisterCommand"))
            .Select(line => line.Split(new[] { "new ", "(" }, StringSplitOptions.RemoveEmptyEntries)[1])
            .ToList();

        return existingCommands.Count != currentCommands.Count;
    }

    private static string GenerateRegistryCode(List<System.Type> commandTypes)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("// This file is auto-generated. Do not modify it manually.");
        sb.AppendLine("// Generated at: " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine();
        sb.AppendLine("namespace GameLogic");
        sb.AppendLine("{");
        sb.AppendLine("    public static class GMCommandRegistry");
        sb.AppendLine("    {");
        sb.AppendLine("        public static void RegisterAllCommands(GMCommandManager manager)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (manager == null) return;");
        sb.AppendLine();

        // 为每个命令生成注册代码
        foreach (var type in commandTypes)
        {
            sb.AppendLine($"            manager.RegisterCommand(new {type.FullName}());");
        }

        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }
} 