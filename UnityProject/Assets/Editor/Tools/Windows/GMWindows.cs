using System.Collections.Generic;
using UnityEngine.UIElements;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class GMWindows : EditorWindow
{
    private VisualElement commandContainer;
    private VisualElement paramContainer;
    private VisualElement historyContainer;
    private Label commandName;
    private Label commandDesc;
    private Label commandUsage;
    private Button executeButton;
    private Button historyTab;
    private Button favoriteTab;
    private TextField searchField;
    private string selectedCommand;
    private bool showingFavorites;
    private string currentGroup;
    private string searchText = "";

    //快捷键 ctrl + Q
    [MenuItem("Window/GM Tools %Q")]
    public static void ShowWindow()
    {
        try
        {
            // 确保 GMCommandManager 已初始化
            if (GMCommandManager.Instance == null)
            {
                Debug.LogError("Failed to initialize GMCommandManager!");
                return;
            }

            GMWindows wnd = GetWindow<GMWindows>();
            wnd.titleContent = new GUIContent("GM Tools");
            wnd.minSize = new Vector2(800, 600);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in ShowWindow: {e}");
        }
    }

    private void CreateGUI()
    {
        try
        {
            // 加载UXML
            var visualTree = Resources.Load<VisualTreeAsset>("GMWindows");
            if (visualTree == null)
            {
                Debug.LogError("Failed to load GMWindows.uxml!");
                return;
            }

            // 清除现有内容
            rootVisualElement.Clear();

            // 实例化UXML
            visualTree.CloneTree(rootVisualElement);

            // 初始化UI
            InitializeUI();

            Debug.Log("GMWindows UI initialized successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error initializing GMWindows: {e}");
        }
    }

    private void InitializeUI()
    {
        try
        {
            // 确保 GMCommandManager 已初始化
            if (GMCommandManager.Instance == null)
            {
                Debug.LogError("GMCommandManager is not initialized!");
                return;
            }

            // 获取UI元素引用
            commandContainer = rootVisualElement.Q<VisualElement>("command-container");
            if (commandContainer == null) throw new System.Exception("command-container not found");

            paramContainer = rootVisualElement.Q<VisualElement>("param-container");
            if (paramContainer == null) throw new System.Exception("param-container not found");

            historyContainer = rootVisualElement.Q<VisualElement>("history-container");
            if (historyContainer == null) throw new System.Exception("history-container not found");

            commandName = rootVisualElement.Q<Label>("command-name");
            if (commandName == null) throw new System.Exception("command-name not found");

            commandDesc = rootVisualElement.Q<Label>("command-desc");
            if (commandDesc == null) throw new System.Exception("command-desc not found");

            commandUsage = rootVisualElement.Q<Label>("command-usage");
            if (commandUsage == null) throw new System.Exception("command-usage not found");

            executeButton = rootVisualElement.Q<Button>("execute-btn");
            if (executeButton == null) throw new System.Exception("execute-btn not found");

            historyTab = rootVisualElement.Q<Button>("history-tab");
            if (historyTab == null) throw new System.Exception("history-tab not found");

            favoriteTab = rootVisualElement.Q<Button>("favorite-tab");
            if (favoriteTab == null) throw new System.Exception("favorite-tab not found");

            searchField = rootVisualElement.Q<TextField>("search-field");
            if (searchField == null) throw new System.Exception("search-field not found");

            // 注册事件
            var clearBtn = rootVisualElement.Q<Button>("clear-btn");
            if (clearBtn != null) clearBtn.clicked += ClearSearch;

            executeButton.clicked += ExecuteCommand;
            historyTab.clicked += () => ShowHistory(false);
            favoriteTab.clicked += () => ShowHistory(true);
            searchField.RegisterValueChangedCallback(evt => {
                searchText = evt.newValue;
                UpdateCommandGrid(currentGroup);
            });

            // 延迟初始化，确保命令注册完成
            EditorApplication.delayCall += () => {
                // 初始化分组
                InitializeGroups();
                UpdateHistory();
            };
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in InitializeUI: {e}");
        }
    }

    private void InitializeGroups()
    {
        try
        {
            var tabGroup = rootVisualElement.Q<VisualElement>("tab-group");
            if (tabGroup == null)
            {
                Debug.LogError("tab-group element not found!");
                return;
            }

            tabGroup.Clear();

            var groups = GMCommandManager.Instance.GetAllGroups();
            if (groups == null || !groups.Any())
            {
                Debug.LogWarning("No command groups found!");
                return;
            }

            // 先创建所有按钮
            foreach (var group in groups)
            {
                if (string.IsNullOrEmpty(group)) continue;

                var button = new Button(() => SelectGroup(group)) { text = group };
                button.AddToClassList("tab-btn");
                tabGroup.Add(button);
            }

            // 然后选择第一个分组
            var firstGroup = groups.FirstOrDefault();
            if (!string.IsNullOrEmpty(firstGroup))
            {
                var firstButton = tabGroup.Children().OfType<Button>().FirstOrDefault();
                if (firstButton != null)
                {
                    firstButton.AddToClassList("selected");
                    SelectGroup(firstGroup);
                }
            }

            Debug.Log($"Initialized {groups.Count()} command groups");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in InitializeGroups: {e}");
        }
    }

    private void SelectGroup(string group)
    {
        try
        {
            currentGroup = group;
            // 更新分组按钮样式
            var tabGroup = rootVisualElement.Q<VisualElement>("tab-group");
            if (tabGroup != null)
            {
                foreach (var button in tabGroup.Children().OfType<Button>())
                {
                    button.RemoveFromClassList("selected");
                    if (button.text == group)
                    {
                        button.AddToClassList("selected");
                    }
                }
            }

            // 更新命令列表
            UpdateCommandGrid(group);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in SelectGroup: {e}");
        }
    }

    private void UpdateCommandGrid(string group = null)
    {
        try
        {
            if (commandContainer == null)
            {
                Debug.LogError("commandContainer is null");
                return;
            }

            if (GMCommandManager.Instance == null)
            {
                Debug.LogError("GMCommandManager.Instance is null");
                return;
            }

            commandContainer.Clear();

            IEnumerable<GMCommandBase> commands;
            try
            {
                commands = group != null 
                    ? GMCommandManager.Instance.GetCommandsByGroup(group)
                    : GMCommandManager.Instance.GetAllCommands().Select(cmd => GMCommandManager.Instance.GetCommand(cmd));

                if (commands == null)
                {
                    Debug.LogWarning("No commands found");
                    return;
                }

                // 应用搜索过滤
                if (!string.IsNullOrEmpty(searchText))
                {
                    commands = commands.Where(cmd => 
                        cmd.CommandName.Contains(searchText, System.StringComparison.OrdinalIgnoreCase) ||
                        cmd.Alias.Contains(searchText, System.StringComparison.OrdinalIgnoreCase) ||
                        cmd.Description.Contains(searchText, System.StringComparison.OrdinalIgnoreCase)
                    );
                }

                foreach (var command in commands)
                {
                    if (command == null) continue;

                    var button = new Button(() => SelectCommand(command.CommandName));
                    button.text = command.Alias;
                    button.tooltip = command.Description;
                    button.AddToClassList("command-btn");
                    commandContainer.Add(button);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error getting commands: {e}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in UpdateCommandGrid: {e}");
        }
    }

    private void SelectCommand(string commandName)
    {
        selectedCommand = commandName;
        var command = GMCommandManager.Instance.GetCommand(commandName);
        if (command == null) return;

        // 更新命令信息
        this.commandName.text = $"{command.Alias} ({command.CommandName})";
        commandDesc.text = command.Description;
        commandUsage.text = $"用法: {command.Usage}";

        // 更新参数面板
        UpdateParameterPanel(command);
    }

    private void UpdateParameterPanel(GMCommandBase command)
    {
        paramContainer.Clear();

        foreach (var param in command.Parameters)
        {
            var paramField = new GMParameterField(param);
            paramContainer.Add(paramField);
        }
    }

    private void ExecuteCommand()
    {
        if (string.IsNullOrEmpty(selectedCommand)) return;

        // 收集参数
        var parameters = paramContainer.Children()
            .OfType<GMParameterField>()
            .Select(field => field.Value)
            .ToArray();

        // 构建完整命令
        string fullCommand = selectedCommand;
        if (parameters.Length > 0)
        {
            fullCommand += " " + string.Join(" ", parameters);
        }

        // 执行命令
        bool success = GMCommandManager.Instance.ExecuteCommand(fullCommand);

        // 添加到历史记录
        GMCommandHistory.AddCommand(fullCommand, parameters, success);
        UpdateHistory();
    }

    private void ShowHistory(bool showFavorites)
    {
        showingFavorites = showFavorites;
        historyTab.RemoveFromClassList("selected");
        favoriteTab.RemoveFromClassList("selected");

        if (showFavorites)
            favoriteTab.AddToClassList("selected");
        else
            historyTab.AddToClassList("selected");

        UpdateHistory();
    }

    private void UpdateHistory()
    {
        historyContainer.Clear();

        if (showingFavorites)
        {
            foreach (var favorite in GMCommandHistory.GetFavorites().OrderByDescending(f => f.addTime))
            {
                AddHistoryItem(favorite.command, true, true, favorite.description);
            }
        }
        else
        {
            foreach (var entry in GMCommandHistory.GetHistory().AsEnumerable().Reverse())
            {
                AddHistoryItem(entry.command, entry.success, GMCommandHistory.IsFavorite(entry.command));
            }
        }
    }

    private void AddHistoryItem(string command, bool success, bool isFavorite, string description = "")
    {
        var item = new VisualElement();
        item.AddToClassList("history-item");

        // 命令文本
        var cmdButton = new Button(() => ParseAndSelectCommand(command));
        cmdButton.text = command;
        cmdButton.AddToClassList("history-command");
        if (!string.IsNullOrEmpty(description))
        {
            cmdButton.tooltip = description;
        }
        item.Add(cmdButton);

        // 状态图标
        var status = new Label(success ? "✓" : "✗");
        status.AddToClassList("history-status");
        status.AddToClassList(success ? "success" : "failure");
        item.Add(status);

        // 收藏按钮
        var favorite = new Button(() => ToggleFavorite(command));
        favorite.text = isFavorite ? "★" : "☆";
        favorite.AddToClassList("history-favorite");
        item.Add(favorite);

        historyContainer.Add(item);
    }

    private void ParseAndSelectCommand(string commandLine)
    {
        try
        {
            string[] parts = commandLine.Split(' ');
            if (parts.Length > 0)
            {
                // 先选择命令
                SelectCommand(parts[0]);

                // 如果有参数，设置参数值
                if (parts.Length > 1)
                {
                    var paramFields = paramContainer.Children().OfType<GMParameterField>().ToList();
                    for (int i = 0; i < paramFields.Count && (i + 1) < parts.Length; i++)
                    {
                        paramFields[i].SetValue(parts[i + 1]);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing command: {e}");
        }
    }

    private void ToggleFavorite(string command)
    {
        if (GMCommandHistory.IsFavorite(command))
        {
            GMCommandHistory.RemoveFavorite(command);
        }
        else
        {
            GMCommandHistory.AddFavorite(command, new string[0]);
        }
        UpdateHistory();
    }

    private void ClearSearch()
    {
        if (showingFavorites)
        {
            if (EditorUtility.DisplayDialog("清空收藏", "是否清空所有收藏？", "确定", "取消"))
            {
                GMCommandHistory.ClearFavorites();
            }
        }
        else
        {
            if (EditorUtility.DisplayDialog("清空历史", "是否清空所有历史记录？", "确定", "取消"))
            {
                GMCommandHistory.Clear();
            }
        }
        UpdateHistory();
    }
} 