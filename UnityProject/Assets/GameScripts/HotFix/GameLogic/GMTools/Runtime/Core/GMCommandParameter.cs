using System;

public class GMCommandParameter
{
    public string Name { get; set; }
    public string Description { get; set; }
    public ParamType Type { get; set; }
    public bool IsRequired { get; set; }
    public string DefaultValue { get; set; }
    public string[] OptionValues { get; set; }
    public int Index { get; internal set; }

    // 基础构造函数
    public GMCommandParameter()
    {
        IsRequired = true;
        DefaultValue = "";
        Index = -1;
    }

    // 便捷构造函数
    public GMCommandParameter(string name, ParamType type, string description = "", int index = -1)
    {
        Name = name;
        Type = type;
        Description = description;
        IsRequired = true;
        DefaultValue = "";
        Index = index;
    }

    // 链式调用方法
    public GMCommandParameter SetRequired(bool required = true)
    {
        IsRequired = required;
        return this;
    }

    public GMCommandParameter SetOptional(string defaultValue = "")
    {
        IsRequired = false;
        DefaultValue = defaultValue;
        return this;
    }

    public GMCommandParameter SetDescription(string description)
    {
        Description = description;
        return this;
    }

    public GMCommandParameter SetOptions(params string[] options)
    {
        Type = ParamType.Options;
        OptionValues = options;
        return this;
    }

    // 静态工厂方法
    public static GMCommandParameter Integer(string name, string description = "")
    {
        return new GMCommandParameter(name, ParamType.Integer, description);
    }

    public static GMCommandParameter Float(string name, string description = "")
    {
        return new GMCommandParameter(name, ParamType.Float, description);
    }

    public static GMCommandParameter String(string name, string description = "")
    {
        return new GMCommandParameter(name, ParamType.String, description);
    }

    public static GMCommandParameter Boolean(string name, string description = "")
    {
        return new GMCommandParameter(name, ParamType.Boolean, description);
    }

    public static GMCommandParameter Options(string name, string description, params string[] options)
    {
        return new GMCommandParameter(name, ParamType.Options, description).SetOptions(options);
    }
} 