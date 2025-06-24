using System.Collections.Generic;
using System.Text.RegularExpressions;

public abstract class GMCommandBase
{
    public abstract string CommandName { get; }
    public abstract string Description { get; }
    public abstract string Usage { get; }
    public abstract string Alias { get; }
    public abstract string Group { get; }

    private GMCommandParameter[] _parameters;
    public  GMCommandParameter[] Parameters
    {
        get
        {
            if (_parameters == null)
            {
                _parameters = InitializeParameters();
                // 确保所有参数都有正确的索引
                for (int i = 0; i < _parameters.Length; i++)
                {
                    _parameters[i].Index = i;
                }
            }
            return _parameters;
        }
    }

    // 新增：保护方法用于初始化参数
    protected virtual GMCommandParameter[] InitializeParameters()
    {
        return ParseParametersFromUsage();
    }

    public virtual void Init()
    {
        
    }
    
    public abstract bool Execute(string[] parameters);

    // 从Usage字符串自动解析参数
    protected GMCommandParameter[] ParseParametersFromUsage()
    {
        var parameters = new List<GMCommandParameter>();
        
        // 匹配 <必选参数> 和 [可选参数]
        var regex = new Regex(@"[<\[](\w+)[>\]]");
        var matches = regex.Matches(Usage);
        
        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            var paramName = match.Groups[1].Value;
            bool isRequired = match.Value.StartsWith("<");
            
            var param = GMCommandParameter.String(paramName, $"{(isRequired ? "请输入" : "[可选]")}{paramName}");
            if (!isRequired)
            {
                param.SetOptional();
            }
            param.Index = i;  // 设置参数索引
            
            parameters.Add(param);
        }
        
        return parameters.ToArray();
    }
}

public enum ParamType
{
    String,
    Integer,
    Float,
    Boolean,
    Options,
} 