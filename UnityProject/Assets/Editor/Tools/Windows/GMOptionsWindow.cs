using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.Linq;
using System;
using System.Collections.Generic;

public class GMOptionsWindow : EditorWindow
{
    [SerializeField] private VisualTreeAsset visualTree;
    [SerializeField] private StyleSheet styleSheet;

    private TextField searchField;
    private ScrollView optionsContainer;
    private string[] options;
    private Action<string> onOptionSelected;
    private string currentValue;
    private string currentSelectedOption;

    public static void Show(string[] options, string currentValue, Action<string> onSelected)
    {
        try
        {
            // 先关闭已存在的窗口
            var existingWindow = GetWindow<GMOptionsWindow>();
            if (existingWindow != null)
            {
                existingWindow.Close();
            }

            currentValue = string.Empty;
            // 创建新窗口
            var window = CreateInstance<GMOptionsWindow>();
            window.titleContent = new GUIContent("选择选项");
            window.options = options;
            window.currentValue = currentValue;
            window.onOptionSelected = onSelected;
            window.minSize = new Vector2(300, 400);
            window.maxSize = new Vector2(500, 600);

            // 计算窗口位置
            Vector2 pos = EditorGUIUtility.GUIToScreenPoint(Event.current != null ? Event.current.mousePosition : Vector2.zero);
            if (Event.current == null)
            {
                pos = GUIUtility.GUIToScreenPoint(Input.mousePosition);
            }

            // 确保窗口在屏幕内
            float screenWidth = Screen.currentResolution.width;
            float screenHeight = Screen.currentResolution.height;
            pos.x = Mathf.Clamp(pos.x, 0, screenWidth - 300);
            pos.y = Mathf.Clamp(pos.y, 0, screenHeight - 400);

            window.position = new Rect(pos.x, pos.y, 300, 400);
            window.ShowPopup();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error showing options window: {e}");
        }
    }

    private void OnEnable()
    {
        try
        {
            InitializeUI();
            SetupKeyboardNavigation();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error in OnEnable: {e}");
        }
    }

    private void InitializeUI()
    {
        // 创建标题栏
        var titleBar = new VisualElement();
        titleBar.AddToClassList("title-bar");

        // 创建标题
        var titleLabel = new Label("选择选项");
        titleLabel.AddToClassList("title-label");
        titleBar.Add(titleLabel);

        // 创建关闭按钮
        var closeButton = new Button(() => Close()) { text = "×" };
        closeButton.AddToClassList("close-button");
        titleBar.Add(closeButton);

        rootVisualElement.Add(titleBar);

        // 创建搜索框
        searchField = new TextField();
        searchField.AddToClassList("options-search");
        searchField.RegisterValueChangedCallback(evt => FilterOptions(evt.newValue));
        rootVisualElement.Add(searchField);

        // 创建选项容器
        optionsContainer = new ScrollView();
        optionsContainer.AddToClassList("options-list");
        rootVisualElement.Add(optionsContainer);

        // 添加样式
        var styleSheet = Resources.Load<StyleSheet>("GMWindows");
        if (styleSheet != null)
        {
            rootVisualElement.styleSheets.Add(styleSheet);
        }

        // 确保立即显示所有选项并聚焦到搜索框
        EditorApplication.delayCall += () => {
            if (options != null)
            {
                UpdateOptionsList(options);
                searchField.Focus();
                if (!string.IsNullOrEmpty(currentValue))
                {
                    searchField.value = currentValue;
                    searchField.SelectAll();
                }
            }
        };
    }

    private void SetupKeyboardNavigation()
    {
        rootVisualElement.RegisterCallback<KeyDownEvent>(evt => {
            switch (evt.keyCode)
            {
                case KeyCode.Escape:
                    Close();
                    evt.StopPropagation();
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    if (!string.IsNullOrEmpty(currentSelectedOption))
                    {
                        SelectOption(currentSelectedOption);
                    }
                    evt.StopPropagation();
                    break;
            }
        });
    }

    private void UpdateOptionsList(string[] items, string filter = "")
    {
        if (optionsContainer == null) return;
        
        optionsContainer.Clear();

        if (items == null || items.Length == 0)
        {
            var noItems = new Label("无可用选项");
            noItems.AddToClassList("option-item");
            noItems.style.color = new Color(0.7f, 0.7f, 0.7f);
            noItems.style.unityTextAlign = TextAnchor.MiddleCenter;
            optionsContainer.Add(noItems);
            return;
        }

        var filteredOptions = string.IsNullOrEmpty(filter) 
            ? items 
            : items.Where(opt => 
                opt.ToLower().Contains(filter.ToLower()) || 
                (opt.Contains(":") && opt.Split(':')[1].ToLower().Contains(filter.ToLower()))
            );

        foreach (var option in filteredOptions)
        {
            var button = new Button(() => SelectOption(option));
            button.text = option;
            button.AddToClassList("option-item");
            if (option == currentValue)
            {
                button.AddToClassList("selected");
            }
            optionsContainer.Add(button);
        }

        // 如果没有匹配项，显示提示
        if (!filteredOptions.Any())
        {
            var noResult = new Label("无匹配结果");
            noResult.AddToClassList("option-item");
            noResult.style.color = new Color(0.7f, 0.7f, 0.7f);
            noResult.style.unityTextAlign = TextAnchor.MiddleCenter;
            optionsContainer.Add(noResult);
        }
    }

    private void FilterOptions(string searchText)
    {
        UpdateOptionsList(options, searchText);
    }

    private void SelectOption(string option)
    {
        onOptionSelected?.Invoke(option);
        Close();
    }
} 