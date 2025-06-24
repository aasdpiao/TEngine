using System;
using System.Linq;
using UnityEngine.UIElements;
using UnityEngine;

public class GMParameterField : VisualElement
{
    public string Value { get; private set; }
    
    private readonly GMCommandParameter parameter;
    private readonly TextField inputField;
    
    public GMParameterField(GMCommandParameter param)
    {
        parameter = param;
        Value = param.DefaultValue;
        
        AddToClassList("param-row");
        
        // 创建标签
        var label = new Label(param.Name + ":");
        label.AddToClassList("param-label");
        Add(label);

        var inputContainer = new VisualElement();
        inputContainer.AddToClassList("param-input");
        
        // 根据参数类型创建不同的输入控件
        switch (param.Type)
        {
            case ParamType.Options:
                // 创建输入框容器
                var optionsContainer = new VisualElement();
                optionsContainer.AddToClassList("options-container");
                
                // 创建输入框
                inputField = new TextField();
                inputField.AddToClassList("options-input");
                inputField.value = param.DefaultValue;
                inputField.isReadOnly = true;  // 设置为只读
                inputField.RegisterValueChangedCallback(evt => Value = evt.newValue);
                
                // 添加点击事件以显示选项窗口
                inputField.RegisterCallback<ClickEvent>(evt => {
                    ShowOptionsWindow();
                    evt.StopPropagation();
                });
                optionsContainer.Add(inputField);

                // 创建下拉按钮
                var dropdownButton = new Button(() => ShowOptionsWindow());
                dropdownButton.AddToClassList("dropdown-button");
                dropdownButton.text = "▼";
                
                // 添加鼠标进入/离开事件
                dropdownButton.RegisterCallback<MouseEnterEvent>(evt => 
                    dropdownButton.style.color = new Color(0.8f, 0.8f, 0.8f));
                dropdownButton.RegisterCallback<MouseLeaveEvent>(evt => 
                    dropdownButton.style.color = new Color(0.6f, 0.6f, 0.6f));
                
                optionsContainer.Add(dropdownButton);
                inputContainer.Add(optionsContainer);
                break;
                
            case ParamType.Boolean:
                var toggle = new Toggle();
                toggle.value = bool.Parse(param.DefaultValue);
                toggle.RegisterValueChangedCallback(evt => Value = evt.newValue.ToString());
                inputContainer.Add(toggle);
                break;
                
            default:
                var textField = new TextField();
                textField.value = param.DefaultValue;
                textField.RegisterValueChangedCallback(evt => Value = evt.newValue);
                inputContainer.Add(textField);
                break;
        }
        
        Add(inputContainer);
        
        // 添加描述提示
        if (!string.IsNullOrEmpty(param.Description))
        {
            var desc = new Label(param.Description);
            desc.AddToClassList("param-desc");
            Add(desc);
        }
    }

    private void ShowOptionsWindow()
    {
        try
        {
            GMOptionsWindow.Show(
                parameter.OptionValues,
                Value,
                option => {
                    Value = option;
                    inputField.value = option;
                    inputField.Focus();
                }
            );
        }
        catch (Exception e)
        {
            Debug.LogError($"Error showing options window from parameter field: {e}");
        }
    }

    public void SetValue(string value)
    {
        try
        {
            Value = value;
            if (inputField != null)
            {
                inputField.value = value;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error setting parameter value: {e}");
        }
    }
} 