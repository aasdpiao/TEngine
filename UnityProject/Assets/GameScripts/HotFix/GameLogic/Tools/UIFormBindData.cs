using UnityEngine;
using System;
using System.Collections.Generic;
/// <summary>
/// UI form base class that all UI panels should inherit from.
/// Provides automatic variable binding and code generation functionality.
/// </summary>
public class UIFormBindData : MonoBehaviour, ISerializeFieldTool
{
    [SerializeField, HideInInspector] 
    private SerializeFieldData[] _fields = Array.Empty<SerializeFieldData>();
    
    [SerializeField]
    private string _scriptPath = "";  // 脚本保存路径
    
    private readonly Dictionary<string, SerializeFieldData> _fieldCache = new();
    
    public SerializeFieldData[] SerializeFieldArr
    {
        get => _fields ?? Array.Empty<SerializeFieldData>();
        set
        {
            _fields = value ?? Array.Empty<SerializeFieldData>();
            RefreshCache();
        }
    }

    public string ScriptPath
    {
        get => _scriptPath;
        set => _scriptPath = value;
    }

    protected virtual void OnEnable()
    {
        RefreshCache();
    }
    
    private void RefreshCache()
    {
        _fieldCache.Clear();
        if (_fields == null) return;
        
        foreach (var field in _fields)
        {
            if (field != null && !string.IsNullOrEmpty(field.VarName))
            {
                _fieldCache[field.VarName] = field;
            }
        }
    }
    public bool TryGetField(string varName, out SerializeFieldData field)
    {
        return _fieldCache.TryGetValue(varName, out field);
    }

    public void ClearFields()
    {
        _fields = Array.Empty<SerializeFieldData>();
        _fieldCache.Clear();
    }
}

/// <summary>
/// Data structure for storing UI variable binding information
/// </summary>
[Serializable]
public class SerializeFieldData
{
    [SerializeField] private string varName;
    [SerializeField] private GameObject target;
    [SerializeField] private string varType;
    [SerializeField] private AccessModifier varPrefix;

    public string VarName 
    { 
        get => varName;
        set => varName = !string.IsNullOrWhiteSpace(value) ? value : throw new ArgumentException("Variable name cannot be empty");
    }
    
    public GameObject Target
    {
        get => target;
        set => target = value;
    }

    public string VarType
    {
        get => varType;
        set => varType = !string.IsNullOrWhiteSpace(value) ? value : throw new ArgumentException("Variable type cannot be empty");
    }

    public AccessModifier VarPrefix
    {
        get => varPrefix;
        set => varPrefix = value;
    }

    public SerializeFieldData(string varName, GameObject target = null)
    {
        VarName = varName;
        Target = target;
        varPrefix = AccessModifier.Private; // Default to private
    }
}

/// <summary>
/// Access modifier for UI variables
/// </summary>
public enum AccessModifier
{
    Private,
    Protected,
    Public
}

/// <summary>
/// Interface for UI variable serialization tools
/// </summary>
public interface ISerializeFieldTool
{
    SerializeFieldData[] SerializeFieldArr { get; set; }
    bool TryGetField(string varName, out SerializeFieldData field);
    void ClearFields();
} 