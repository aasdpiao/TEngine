#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine;
using UnityEditorInternal;
using System;
using System.Text.RegularExpressions;
using System.Text;
using System.IO;
using TEngine;

[CustomEditor(typeof(UIFormBindData), true)]
public class UIFormBindDataInspector : Editor
{
    private const string HELP_TITLE = "使用说明";
    private const string HELP_DOC = "1.打开UI界面预制体.\n2.右键节点'UIForm Tools'子菜单,添加/移除变量.\n3.在Inspector面板点击功能按钮生成变量代码.";
    private const float FIELD_PREFIX_WIDTH = 80;
    private const float FIELD_TYPE_WIDTH = 220;

    private SerializedProperty _fields;
    private static bool _addToFieldToggle;
    private static bool _removeToFieldToggle;
    private ReorderableList _reorderableList;
    private UIFormBindData _uiForm;
    private static bool _showSelectTypeMenu;
    private static AccessModifier _selectedAccessModifier;

    private GUIContent _prefixContent;
    private GUIContent _typeContent;
    private static GUIStyle _varLabelGUIStyle;
    private GUIContent _generateVarBtTitle;
    private GUIContent _openUiLogicBtTitle;
    private GUIContent _copyToClipboardBtTitle;
    private static GUIStyle _highlightBtStyle;

    // 添加脚本设置
    private SerializedProperty _scriptNameProp;
    private SerializedProperty _scriptPathProp;
    private bool _showScriptSettings = false;
    private bool _shouldSelectFolder = false;

    [InitializeOnLoadMethod]
    static void InitEditor()
    {
        Selection.selectionChanged = () =>
        {
            _addToFieldToggle = false;
            _removeToFieldToggle = false;
        };

        EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyItemOnGUI;
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyItemOnGUI;
    }

    private static void OnHierarchyItemOnGUI(int instanceID, Rect rect)
    {
        OpenSelectComponentMenuListener(rect);
        InitializeStyles();

        var curDrawNode = EditorUtility.InstanceIDToObject(instanceID);
        if (curDrawNode == null) return;
        
        var uiForm = GetSerializeFieldTool(curDrawNode as GameObject);
        if (uiForm == null) return;
        
        var fields = uiForm.SerializeFieldArr;
        foreach (var item in fields)
        {
            if (item == null || item.Target == null) continue;
            if (item.Target == curDrawNode)
            {
                var displayContent = EditorGUIUtility.TrTextContent(
                    string.Format("{0} {1} {2}", 
                    item.VarPrefix.ToString().ToLower(), 
                    GetDisplayVarTypeName(item.VarType), 
                    item.VarName));

                var itemLabelRect = GUILayoutUtility.GetRect(displayContent, _varLabelGUIStyle);
                itemLabelRect.y = rect.y;
                itemLabelRect.width = Mathf.Min(rect.width * 0.4f, itemLabelRect.width);
                itemLabelRect.x = rect.xMax - itemLabelRect.width - 110;
                
                if (itemLabelRect.width > 100)
                {
                    GUI.Label(itemLabelRect, displayContent, _varLabelGUIStyle);
                }
                break;
            }
        }
    }

    private static void InitializeStyles()
    {
        if (_varLabelGUIStyle == null)
        {
            _varLabelGUIStyle = new GUIStyle(EditorStyles.helpBox)
            {
                stretchWidth = false,
                stretchHeight = false,
                normal = { textColor = Color.white * 0.88f },
                fontStyle = FontStyle.Bold
            };
            _varLabelGUIStyle.hover.textColor = Color.cyan;
        }

        if (_highlightBtStyle == null)
        {
            _highlightBtStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };
        }
    }

    [MenuItem("GameObject/UIForm Tools/Add private", false, priority = -1)]
    private static void AddPrivateVariable2UIForm()
    {
        _selectedAccessModifier = AccessModifier.Private;
        _showSelectTypeMenu = true;
    }

    [MenuItem("GameObject/UIForm Tools/Add protected", false, priority = -1)]
    private static void AddProtectedVariable2UIForm()
    {
        _selectedAccessModifier = AccessModifier.Protected;
        _showSelectTypeMenu = true;
    }

    [MenuItem("GameObject/UIForm Tools/Add public", false, priority = -1)]
    private static void AddPublicVariable2UIForm()
    {
        _selectedAccessModifier = AccessModifier.Public;
        _showSelectTypeMenu = true;
    }

    [MenuItem("GameObject/UIForm Tools/Remove", false, priority = -1)]
    private static void RemoveUIFormVariable()
    {
        if (_removeToFieldToggle) return;
        if (Selection.count <= 0) return;

        var selectGos = Selection.gameObjects;
        foreach (var go in selectGos)
        {
            if (go == null) continue;
            var serializeTool = GetSerializeFieldTool(go);
            if (serializeTool == null) continue;

            var fieldsProperties = serializeTool.SerializeFieldArr;
            if (fieldsProperties == null) continue;

            for (int i = fieldsProperties.Length - 1; i >= 0; i--)
            {
                var field = fieldsProperties[i];
                if (field == null || field.Target == null) continue;
                if (field.Target == go)
                {
                    ArrayUtility.RemoveAt(ref fieldsProperties, i);
                }
            }
            serializeTool.SerializeFieldArr = fieldsProperties;
            EditorUtility.SetDirty(serializeTool as MonoBehaviour);
        }
        _removeToFieldToggle = true;
        _addToFieldToggle = false;
    }

    private static void AddToFields(AccessModifier accessModifier, string varType)
    {
        if (_addToFieldToggle) return;
        if (Selection.count <= 0) return;

        var selectedObjects = Selection.gameObjects;
        foreach (var go in selectedObjects)
        {
            var serializeTool = GetSerializeFieldTool(go);
            if (serializeTool == null) continue;

            var fieldsProperties = serializeTool.SerializeFieldArr;
            if (fieldsProperties == null) continue;

            // 检查是否已经添加过相同的组件
            if (fieldsProperties.Any(f => f != null && f.Target == go && f.VarType == varType))
            {
                EditorUtility.DisplayDialog("添加失败", 
                    $"GameObject '{go.name}' 已经添加过 {GetDisplayVarTypeName(varType)} 组件", 
                    "OK");
                continue;
            }

            var goLogic = serializeTool as MonoBehaviour;
            Undo.RecordObject(goLogic, goLogic.name);

            var field = new SerializeFieldData(GenerateFieldName(fieldsProperties, go), go)
            {
                VarPrefix = accessModifier,
                VarType = varType
            };

            ArrayUtility.Add(ref fieldsProperties, field);
            serializeTool.SerializeFieldArr = fieldsProperties;
            EditorUtility.SetDirty(goLogic);
        }

        _addToFieldToggle = true;
        _removeToFieldToggle = false;
    }

    private void OnEnable()
    {
        InitializeGUIContent();

        _selectedAccessModifier = AccessModifier.Private;
        _showSelectTypeMenu = false;
        _uiForm = (target as UIFormBindData);

        if (_uiForm.SerializeFieldArr == null)
        {
            _uiForm.SerializeFieldArr = Array.Empty<SerializeFieldData>();
        }

        _fields = serializedObject.FindProperty("_fields");
        _scriptNameProp = serializedObject.FindProperty("_scriptName");
        _scriptPathProp = serializedObject.FindProperty("_scriptPath");
        SetupReorderableList();

        EditorApplication.update += HandleEditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= HandleEditorUpdate;
    }

    private void HandleEditorUpdate()
    {
        if (_shouldSelectFolder)
        {
            _shouldSelectFolder = false;
            // 设置相对于项目路径的缺省目录
            string projectRelativePath = "Assets/GameScripts/HotFix/GameLogic/UI";
            string fullPath = Path.Combine(Application.dataPath, "../", projectRelativePath);
            fullPath = Path.GetFullPath(fullPath); // 转换为绝对路径
            string path = EditorUtility.OpenFolderPanel("选择文件夹", fullPath, "");
            // 保存新选择的路径
            if (!string.IsNullOrEmpty(path)) 
            {
                PlayerPrefs.SetString("LastSelectedFolder", path);
            }
            if (path.StartsWith(Application.dataPath))
            {
                string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                _scriptPathProp.stringValue = relativePath;
                serializedObject.ApplyModifiedProperties();
                Repaint();
            }
        }
    }

    private void InitializeGUIContent()
    {
        _generateVarBtTitle = new GUIContent("生成变量代码", "generate or update variables code");
        _openUiLogicBtTitle = new GUIContent("查看变量代码", "open variables code in editor");
        _copyToClipboardBtTitle = new GUIContent("复制到剪切板", "copy variables code to clipboard");
        _prefixContent = new GUIContent();
        _typeContent = new GUIContent();
    }

    private void SetupReorderableList()
    {
        _reorderableList = new ReorderableList(serializedObject, _fields, true, true, true, true)
        {
            drawHeaderCallback = rect => EditorGUI.LabelField(rect, "UI Variables"),
            drawElementCallback = DrawListElement,
            elementHeightCallback = index => EditorGUIUtility.singleLineHeight * 2
        };
    }

    public override void OnInspectorGUI()
    {
        if (_uiForm == null)
            _uiForm = target as UIFormBindData;

        if (_fields == null)
            _fields = serializedObject.FindProperty("_fields");

        if (_reorderableList == null)
            SetupReorderableList();

        if (serializedObject == null)
            return;

        serializedObject.Update();

        DrawToolbar();
        DrawHelpBox();
        if (_reorderableList != null)
            _reorderableList.DoLayoutList();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawToolbar()
    {
        if (_generateVarBtTitle == null || _openUiLogicBtTitle == null)
            InitializeGUIContent();

        InitializeStyles();

        EditorGUILayout.BeginVertical();

        // 编译状态警告
        bool disableAct = EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlaying;
        if (disableAct)
        {
            EditorGUILayout.HelpBox("Waiting for compiling or updating...", MessageType.Warning);
        }

        // 按钮区域
        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginDisabledGroup(disableAct);

        var btnHeight = GUILayout.Height(30);
        if (GUILayout.Button(_generateVarBtTitle, _highlightBtStyle, btnHeight))
        {
            GenerateUIFormVariables(_uiForm);
        }

        if (GUILayout.Button(_copyToClipboardBtTitle, _highlightBtStyle, btnHeight))
        {
            CopyVariablesToClipboard(_uiForm);
        }

        if (GUILayout.Button(_openUiLogicBtTitle, _highlightBtStyle, btnHeight))
        {
            OpenUILogicCode();
        }

        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();

        // 脚本设置区域
        _showScriptSettings = EditorGUILayout.Foldout(_showScriptSettings, "脚本设置", true);
        if (_showScriptSettings)
        {
            EditorGUI.indentLevel++;
            
            EditorGUILayout.BeginVertical("box");
            
            // 脚本名称设置
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("脚本名称");
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField(_uiForm != null ? _uiForm.gameObject.name : "");
                }
            }
            EditorGUILayout.HelpBox("脚本名称与GameObject名保持一致", MessageType.Info);
            
            // 脚本路径设置
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(_scriptPathProp, new GUIContent("保存路径"));
            if (GUILayout.Button("选择", GUILayout.Width(60)))
            {
                _shouldSelectFolder = true;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawHelpBox()
    {
        EditorGUILayout.BeginHorizontal("box");
        if (EditorGUILayout.LinkButton(HELP_TITLE))
        {
            EditorUtility.DisplayDialog(HELP_TITLE, HELP_DOC, "OK");
            GUIUtility.ExitGUI();
        }
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Clear All"))
        {
            _fields.ClearArray();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawListElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        var element = _fields.GetArrayElementAtIndex(index);
        var varNameProp = element.FindPropertyRelative("varName");
        var varTypeProp = element.FindPropertyRelative("varType");
        var targetProp = element.FindPropertyRelative("target");
        var varPrefixProp = element.FindPropertyRelative("varPrefix");

        var lineHeight = EditorGUIUtility.singleLineHeight;
        var spacing = EditorGUIUtility.standardVerticalSpacing;
        
        // First line
        var firstLineRect = new Rect(rect.x, rect.y, rect.width, lineHeight);
        var prefixRect = new Rect(firstLineRect.x, firstLineRect.y, FIELD_PREFIX_WIDTH, lineHeight);
        var typeRect = new Rect(prefixRect.xMax + spacing, firstLineRect.y, FIELD_TYPE_WIDTH, lineHeight);
        var nameRect = new Rect(typeRect.xMax + spacing, firstLineRect.y, firstLineRect.width - prefixRect.width - typeRect.width - spacing * 2, lineHeight);

        // Second line
        var secondLineRect = new Rect(rect.x, firstLineRect.yMax + spacing, rect.width, lineHeight);

        // Draw fields
        EditorGUI.PropertyField(prefixRect, varPrefixProp, GUIContent.none);
        EditorGUI.PropertyField(typeRect, varTypeProp, GUIContent.none);
        EditorGUI.PropertyField(nameRect, varNameProp, GUIContent.none);
        EditorGUI.PropertyField(secondLineRect, targetProp, new GUIContent("Target"));
    }

    private static void OpenSelectComponentMenuListener(Rect rect)
    {
        if (!_showSelectTypeMenu) return;

        var selectedGo = Selection.activeGameObject;
        if (selectedGo == null) return;

        var components = selectedGo.GetComponents<Component>();
        var menuItems = components.Select(c => c.GetType().FullName).ToList();
        menuItems.Insert(0, typeof(GameObject).FullName);

        var contents = menuItems.Select(type => new GUIContent(GetDisplayVarTypeName(type))).ToArray();
        
        rect.width = 200;
        rect.height = Mathf.Max(100, contents.Length * rect.height);
        
        EditorUtility.DisplayCustomMenu(rect, contents, -1, (userData, options, selected) =>
        {
            AddToFields(_selectedAccessModifier, menuItems[selected]);
        }, null);
        
        _showSelectTypeMenu = false;
    }

    private static string GetDisplayVarTypeName(string varFullTypeName)
    {
        if (string.IsNullOrWhiteSpace(varFullTypeName)) return string.Empty;
        return Path.HasExtension(varFullTypeName) ? Path.GetExtension(varFullTypeName).Substring(1) : varFullTypeName;
    }

    private static Type GetSampleType(string fullName)
    {
        return Utility.Assembly.GetType(fullName);
    }

    private static ISerializeFieldTool GetSerializeFieldTool(GameObject go)
    {
        if (go == null) return null;
        var parent = go.transform.parent;
        while (parent != null)
        {
            var mono = parent.GetComponents<MonoBehaviour>().FirstOrDefault(item => item is ISerializeFieldTool);
            if (mono != null)
            {
                return mono as ISerializeFieldTool;
            }
            parent = parent.parent;
        }
        return null;
    }

    private void GenerateUIFormVariables(UIFormBindData uiForm)
    {
        if (uiForm == null) return;
        GenerateUIForm(_uiForm);
        // 使用GameObject名称作为脚本名称
        string scriptName = uiForm.gameObject.name;
        
        // 确定脚本路径
        if (string.IsNullOrWhiteSpace(uiForm.ScriptPath))
        {
            _shouldSelectFolder = true;
            return;
        }

        string basePath = uiForm.ScriptPath;
        
        string scriptFile = Path.Combine(basePath, $"{scriptName}.Variables.cs");
        
        // 转换为系统路径
        if (scriptFile.StartsWith("Assets/"))
        {
            scriptFile = Path.Combine(Application.dataPath, scriptFile.Substring(7));
        }

        var fields = uiForm.SerializeFieldArr;
        if (fields == null || fields.Length <= 0)
        {
            if (File.Exists(scriptFile))
            {
                AssetDatabase.DeleteAsset(scriptFile);
            }
            return;
        }

        // 检查变量名重复
        var duplicateNames = fields
            .Where(f => f != null && !string.IsNullOrEmpty(f.VarName))
            .GroupBy(f => f.VarName)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateNames.Any())
        {
            EditorUtility.DisplayDialog("生成失败", 
                $"存在重复的变量名:\n{string.Join("\n", duplicateNames)}", 
                "OK");
            return;
        }

        GenerateVariablesFile(scriptName, fields, scriptFile, "GameLogic");
        
        EditorUtility.DisplayDialog("生成成功", 
            $"脚本生成到:\n{scriptFile}", "OK");
    }
    
    private void GenerateUIForm(UIFormBindData uiForm)
    {
        if (uiForm == null) return;
        // 使用GameObject名称作为脚本名称
        string scriptName = uiForm.gameObject.name;
        // 确定脚本路径
        string basePath = uiForm.ScriptPath;
        if (string.IsNullOrWhiteSpace(basePath))
        {
            _shouldSelectFolder = true;
            return;
        }
        string scriptFile = Path.Combine(basePath, $"{scriptName}.cs");
        // 转换为系统路径
        if (scriptFile.StartsWith("Assets/"))
        {
            scriptFile = Path.Combine(Application.dataPath, scriptFile.Substring(7));
        }
        if (File.Exists(scriptFile))
        {
            return;
        }
        GenerateFile(scriptName, scriptFile, "GameLogic");
    }

    private void GenerateVariablesFile(string className, SerializeFieldData[] fields, string filePath, string nameSpace)
    {
        var nameSpaces = new HashSet<string> {"UnityEngine.UI"};
        var fieldLines = new List<string>();
        var bindLines = new List<string>();

        foreach (var field in fields)
        {
            if (string.IsNullOrWhiteSpace(field.VarType) || string.IsNullOrWhiteSpace(field.VarName)) continue;
            
            var varType = GetSampleType(field.VarType);
            if (varType == null) continue;

            if (!string.IsNullOrEmpty(varType.Namespace))
            {
                nameSpaces.Add(varType.Namespace);
            }

            string fieldLine = $"{GetAccessModifier(field.VarPrefix)} {varType.Name} {field.VarName};";
            fieldLines.Add(fieldLine);

            // 生成绑定代码
            string bindLine;
            if (varType == typeof(GameObject))
            {
                bindLine = $"\t\t\t{field.VarName} = FindChild(\"{GetGameObjectPath(field.Target)}\").gameObject;";
            }
            else
            {
                bindLine = $"\t\t\t{field.VarName} = FindChildComponent<{varType.Name}>(\"{GetGameObjectPath(field.Target)}\");";
            }
            bindLines.Add(bindLine);
        }

        var sb = new StringBuilder();
        WriteFileHeader(sb);
        WriteNamespaces(sb, nameSpaces);
        WriteClassContent(sb, className, fieldLines, bindLines, nameSpace);

        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        File.WriteAllText(filePath, sb.ToString());
    }
    
    private void GenerateFile(string className, string filePath, string nameSpace)
    {
        var nameSpaces = new HashSet<string> { "UnityEngine"};
        var sb = new StringBuilder();
        WriteNamespaces(sb, nameSpaces);
        WriteContent(sb, className,nameSpace);

        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        File.WriteAllText(filePath, sb.ToString());
    }

    private static void WriteFileHeader(StringBuilder sb)
    {
        sb.AppendLine("//---------------------------------");
        sb.AppendLine("//此文件由工具自动生成,请勿手动修改");
        sb.AppendLine($"//更新自:{SystemInfo.deviceName}");
        sb.AppendLine($"//更新时间:{DateTime.Now}");
        sb.AppendLine("//---------------------------------");
    }

    private static void WriteNamespaces(StringBuilder sb, IEnumerable<string> namespaces)
    {
        foreach (var ns in namespaces)
        {
            sb.AppendLine($"using {ns};");
        }
        sb.AppendLine();
    }

    private static void WriteClassContent(StringBuilder sb, string className, List<string> fieldLines, List<string> bindLines, string nameSpace)
    {
        if (!string.IsNullOrWhiteSpace(nameSpace))
        {
            sb.AppendLine($"namespace {nameSpace}");
            sb.AppendLine("{");
        }

        sb.AppendLine("\t[Window(UILayer.UI)]");

        sb.AppendLine($"\tpublic partial class {className}");
        
        sb.AppendLine("\t{");
        
        // 写入字段
        foreach (var line in fieldLines)
        {
            sb.AppendLine($"\t\t{line}");
        }

        sb.AppendLine();
        
        // 写入OnInit方法
        sb.AppendLine("\t\t#region Auto Generated Code");
        sb.AppendLine("\t\tprivate void BindComponent()");
        sb.AppendLine("\t\t{");
        // 写入组件绑定代码
        foreach (var line in bindLines)
        {
            sb.AppendLine(line);
        }
        
        sb.AppendLine("\t\t}");
        sb.AppendLine("\t\t#endregion");

        sb.AppendLine("\t}");

        if (!string.IsNullOrWhiteSpace(nameSpace))
        {
            sb.AppendLine("}");
        }
    }
    
    private static void WriteContent(StringBuilder sb, string className, string nameSpace)
    {
        if (!string.IsNullOrWhiteSpace(nameSpace))
        {
            sb.AppendLine($"namespace {nameSpace}");
            sb.AppendLine("{");
        }
        var extendName = className.EndsWith("Window") ? "UIWindow" : "UIWidget";
        sb.AppendLine($"\tpublic partial class {className} : {extendName}" );
        sb.AppendLine("\t{");

        sb.AppendLine("\t\tprotected override void ScriptGenerator()");
        sb.AppendLine("\t\t{");
        sb.AppendLine("\t\t\tBindComponent();");
        sb.AppendLine("\t\t}");
        
        sb.AppendLine();
        
        sb.AppendLine();

        sb.AppendLine("\t}");

        if (!string.IsNullOrWhiteSpace(nameSpace))
        {
            sb.AppendLine("}");
        }
    }

    private static string GetAccessModifier(AccessModifier modifier)
    {
        return modifier.ToString().ToLower();
    }

    private void OpenUILogicCode()
    {
        if (_uiForm == null) return;

        string scriptName = _uiForm.gameObject.name;
        if (string.IsNullOrWhiteSpace(_uiForm.ScriptPath))
        {
            _shouldSelectFolder = true;
            return;
        }
        string basePath = _uiForm.ScriptPath;
        string scriptVariablesFile = Path.Combine(basePath, $"{scriptName}.Variables.cs");
        string scriptFile = Path.Combine(basePath, $"{scriptName}.cs");

        // 如果文件不存在，先生成
        if (!File.Exists(Path.Combine(Application.dataPath, scriptVariablesFile.Substring(7))))
        {
            GenerateUIFormVariables(_uiForm);
        }
        // 打开文件
        InternalEditorUtility.OpenFileAtLineExternal(scriptFile, 0);
    }

    private static string GenerateFieldName(SerializeFieldData[] fields, GameObject go)
    {
        string varName = Regex.Replace(go.name, "[^\\w]", string.Empty);
        string baseVarName = GetFieldVarName(varName);

        if (fields == null || fields.Length == 0)
        {
            return baseVarName;
        }

        // 检查是否存在重名，如果存在则添加数字后缀
        int suffix = 1;
        string finalVarName = baseVarName;
        while (fields.Any(item => item != null && item.VarName == finalVarName))
        {
            finalVarName = $"{baseVarName}{suffix}";
            suffix++;
        }

        return finalVarName;
    }

    private static string GetFieldVarName(string str)
    {
        if (string.IsNullOrEmpty(str)) return string.Empty;
        return $"var{char.ToUpper(str[0])}{str.Substring(1)}";
    }

    private static string GetGameObjectPath(GameObject target)
    {
        if (target == null) return string.Empty;
        var path = new StringBuilder(target.name);
        var current = target.transform.parent;
        var uiForm = current.GetComponent<UIFormBindData>();
        while (uiForm == null)
        {
            path.Insert(0, current.name + "/");
            current = current.parent;
            uiForm = current.GetComponent<UIFormBindData>();
        }
        return path.ToString();
    }

    private void CopyVariablesToClipboard(UIFormBindData uiForm)
    {
        if (uiForm == null) return;

        var fields = uiForm.SerializeFieldArr;
        if (fields == null || fields.Length <= 0)
        {
            EditorUtility.DisplayDialog("复制失败", "没有可用的变量代码", "OK");
            return;
        }

        // 检查变量名重复
        var duplicateNames = fields
            .Where(f => f != null && !string.IsNullOrEmpty(f.VarName))
            .GroupBy(f => f.VarName)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateNames.Any())
        {
            EditorUtility.DisplayDialog("复制失败",
                $"存在重复的变量名:\n{string.Join("\n", duplicateNames)}",
                "OK");
            return;
        }

        var sb = new StringBuilder();
        var nameSpaces = new HashSet<string> { "UnityEngine","GameLogic" };
        var fieldLines = new List<string>();
        var bindLines = new List<string>();

        foreach (var field in fields)
        {
            if (string.IsNullOrWhiteSpace(field.VarType) || string.IsNullOrWhiteSpace(field.VarName)) continue;

            var varType = GetSampleType(field.VarType);
            if (varType == null) continue;

            if (!string.IsNullOrEmpty(varType.Namespace))
            {
                nameSpaces.Add(varType.Namespace);
            }

            string fieldLine = $"{GetAccessModifier(field.VarPrefix)} {varType.Name} {field.VarName} = null;";
            fieldLines.Add(fieldLine);

            string bindLine;
            if (varType == typeof(GameObject))
            {
                bindLine = $"\t\t{field.VarName} = this.GetChildObj(\"{GetGameObjectPath(field.Target)}\");";
            }
            else
            {
                bindLine = $"\t\t{field.VarName} = this.GetChildCompByObj<{varType.Name}>(\"{GetGameObjectPath(field.Target)}\");";
            }
            bindLines.Add(bindLine);
        }

        WriteFileHeader(sb);
        WriteNamespaces(sb, nameSpaces);
        WriteClassContent(sb, uiForm.gameObject.name, fieldLines, bindLines, "");

        GUIUtility.systemCopyBuffer = sb.ToString();
        EditorUtility.DisplayDialog("复制成功", "变量代码已复制到剪切板", "OK");
    }
}
#endif 