using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO; // Required for Path operations

/// <summary>
/// Interaction Authoring Studio Window.
/// Provides a UI for managing Actions, Triggers, Conditions, and creating new logic 
/// from C# script templates.
/// </summary>
public class IAWindow : EditorWindow
{
    [MenuItem("Tools/Interaction Authoring Studio")]
    public static void ShowWindow()
    {
        GetWindow<IAWindow>("Interaction Authoring Studio");
    }

    // --- Constants: Folder Paths ---
    // These paths are dynamic and will be set in OnEnable
    private string coreTemplatePath;
    private string customTemplatePath;

    // --- Private Fields: UI State ---
    private int selectedTab;
    private Vector2 mainScrollPosition;
    
    // --- Icons ---
    private GUIContent plusIconContent;
    private GUIContent refreshIconContent;
    private GUIContent scriptIcon; // For the template grid

    // --- Styles ---
    private GUIStyle tabStyle;
    private GUIStyle darkBoxStyle; // For the template browser background
    private GUIStyle gridButtonStyle; // For the "medium icon" look

    // --- Data Lists (for the "Old" tab) ---
    private string oldSearchText = "";
    private List<Type> dataTypes = new List<Type>();
    private List<ScriptableObject> dataAssets = new List<ScriptableObject>();
    private Dictionary<Type, bool> typeFoldouts = new Dictionary<Type, bool>();

    // --- Data Lists (Actions Tab) ---
    private string actionSearchText = "";
    private List<Type> actionTypes = new List<Type>();
    private List<ScriptableObject> actionAssets = new List<ScriptableObject>();
    private Dictionary<Type, bool> actionTypeFoldouts = new Dictionary<Type, bool>();
    private List<TextAsset> coreActionTemplates = new List<TextAsset>();
    private List<TextAsset> customActionTemplates = new List<TextAsset>();

    // --- Data Lists (Triggers Tab) ---
    private string triggerSearchText = "";
    private List<Type> triggerTypes = new List<Type>();
    private Dictionary<Type, bool> triggerTypeFoldouts = new Dictionary<Type, bool>();
    private List<TextAsset> coreTriggerTemplates = new List<TextAsset>();
    private List<TextAsset> customTriggerTemplates = new List<TextAsset>();
    
    // --- Data Lists (Conditions Tab) [NEW] ---
    private string conditionSearchText = "";
    private List<Type> conditionTypes = new List<Type>();
    private List<ScriptableObject> conditionAssets = new List<ScriptableObject>();
    private Dictionary<Type, bool> conditionTypeFoldouts = new Dictionary<Type, bool>();
    private List<TextAsset> coreConditionTemplates = new List<TextAsset>();
    private List<TextAsset> customConditionTemplates = new List<TextAsset>();
    
    // --- UI State: Template Browser ---
    private bool showTemplateBrowser = false; // The main foldout
    private int templateBrowserTab = 0; // "Core" vs "Custom" tabs
    private Vector2 templateGridScroll;
    
    // --- UI State: Template Preview (Right Pane) ---
    private TextAsset selectedTemplate; // The currently selected template
    private string templatePreviewText = "";
    private Vector2 templatePreviewScroll;


    //================================================================================
    // UNITY EVENT METHODS
    //================================================================================

    private void OnEnable()
    {
        // 1. Find the package paths FIRST
        InitializePaths();
        
        // 2. Load data for all tabs
        RefreshDataLists(); // For "Old Data Types" tab
        RefreshActionData();
        RefreshTriggerData();
        RefreshConditionData(); // <-- NEW

        // 3. Load icons
        plusIconContent = EditorGUIUtility.IconContent("Toolbar Plus");
        refreshIconContent = EditorGUIUtility.IconContent("Refresh");
        scriptIcon = EditorGUIUtility.IconContent("cs Script Icon"); // Icon for grid
        
        // 4. Initialize GUI styles (they are reset on domain reload)
        InitializeStyles();
    }

    /// <summary>
    /// This is the main render loop for the EditorWindow.
    /// </summary>
    private void OnGUI()
    {
        // Styles must be re-initialized here if they are lost
        if (tabStyle == null || darkBoxStyle == null || gridButtonStyle == null)
        {
            InitializeStyles();
        }

        // --- Main Layout ---
        DrawToolbar();

        mainScrollPosition = EditorGUILayout.BeginScrollView(mainScrollPosition);
        DrawActiveTabContent();
        EditorGUILayout.EndScrollView();
    }

    //================================================================================
    // CORE DRAWING & LAYOUT
    //================================================================================

    /// <summary>
    /// Finds the dynamic root path of this package to locate templates.
    /// </summary>
    private void InitializePaths()
    {
        string[] guids = AssetDatabase.FindAssets("t:Script IAWindow");
        if (guids.Length == 0)
        {
            Debug.LogError("IAWindow: Could not find IAWindow.cs script asset. Template paths will be broken.");
            return;
        }
        string scriptPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        string editorFolderPath = Path.GetDirectoryName(Path.GetDirectoryName(scriptPath));

        coreTemplatePath = editorFolderPath + "/Templates";
        customTemplatePath = editorFolderPath + "/CustomTemplates";
    }

    /// <summary>
    /// Initializes custom GUI styles used in the window.
    /// </summary>
    private void InitializeStyles()
    {
        tabStyle = new GUIStyle(EditorStyles.toolbarButton);
        tabStyle.stretchWidth = false;

        darkBoxStyle = new GUIStyle(EditorStyles.helpBox); 

        gridButtonStyle = new GUIStyle(EditorStyles.miniButton);
        gridButtonStyle.alignment = TextAnchor.LowerCenter;
        gridButtonStyle.imagePosition = ImagePosition.ImageAbove;
        gridButtonStyle.fixedWidth = 80;
        gridButtonStyle.fixedHeight = 80;
    }

    /// <summary>
    /// Draws the top toolbar with the main navigation tabs.
    /// </summary>
    private void DrawToolbar()
    {
        // --- UPDATED: Added "Conditions" tab ---
        selectedTab = GUILayout.Toolbar(
            selectedTab, 
            new string[] { "Data Types (Old)", "Actions", "Conditions", "Triggers",  "Custom Data" }, 
            tabStyle 
        );
        EditorGUILayout.Space();
    }

    /// <summary>
    /// Acts as a router, drawing the content for the currently selected tab.
    /// </summary>
    private void DrawActiveTabContent()
    {
        // --- UPDATED: Swapped cases 2 and 3 to match Toolbar ---
        switch (selectedTab)
        {
            case 0: // Old "Data Types" tab
                DrawTypesTab(); 
                break;
            case 1: // New "Actions" tab
                DrawActionsTab();
                break;
            case 2: // [NEW] "Conditions" tab
                DrawConditionsTab(); // DOĞRU
                break;
            case 3: // New "Triggers" tab
                DrawTriggersTab(); // DOĞRU
                break;
            case 4: // New "Custom Data" tab
                DrawCustomDataTab();
                break;
        }
    }

    //================================================================================
    // TAB: ACTIONS
    //================================================================================

    private void DrawActionsTab()
    {
        EditorGUILayout.BeginHorizontal();

        // --- LEFT PANE ---
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.65f));
        {
            // 1. TOOLBAR
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                if (EditorGUILayout.DropdownButton(plusIconContent, FocusType.Passive, EditorStyles.toolbarButton, GUILayout.Width(28)))
                {
                    ShowActionCreationMenu(); // Action-specific
                }
                DrawSearchField(ref actionSearchText); 
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);

            // 2. TEMPLATE BROWSER
            DrawTemplateBrowser(coreActionTemplates, customActionTemplates);
            
            EditorGUILayout.Space(10); 

            // 3. ASSET TREE VIEW
            EditorGUILayout.LabelField("Action Scripts & Assets", EditorStyles.boldLabel);
            DrawTypeAssetTreeView(actionTypes, actionAssets, actionTypeFoldouts, actionSearchText);
        }
        EditorGUILayout.EndVertical(); 
        
        // --- RIGHT PANE ---
        DrawTemplatePreviewPanel();
        
        EditorGUILayout.EndHorizontal(); 
    }

    //================================================================================
    // TAB: TRIGGERS
    //================================================================================

    private void DrawTriggersTab()
    {
        EditorGUILayout.BeginHorizontal();

        // --- LEFT PANE ---
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.65f));
        {
            // 1. TOOLBAR
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                if (EditorGUILayout.DropdownButton(plusIconContent, FocusType.Passive, EditorStyles.toolbarButton, GUILayout.Width(28)))
                {
                    ShowTriggerCreationMenu(); // Trigger-specific
                }
                DrawSearchField(ref triggerSearchText);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);

            // 2. TEMPLATE BROWSER
            DrawTemplateBrowser(coreTriggerTemplates, customTriggerTemplates);
            
            EditorGUILayout.Space(10); 

            // 3. ASSET TREE VIEW
            EditorGUILayout.LabelField("Trigger Scripts", EditorStyles.boldLabel);
            // MonoBehaviour'lar (asset'leri olmayan) için olan
            // basit ağaç görünümünü çağır:
            DrawTypeTreeView(triggerTypes, triggerTypeFoldouts, triggerSearchText); // <-- DÜZELTİLDİ
        }
        EditorGUILayout.EndVertical(); 

        // --- RIGHT PANE ---
        DrawTemplatePreviewPanel();
        
        EditorGUILayout.EndHorizontal();
    }
    
    //================================================================================
    // TAB: CONDITIONS (NEW)
    //================================================================================

    /// <summary>
    /// (NEW) Draws the 'Conditions' tab.
    /// </summary>
    private void DrawConditionsTab()
    {
        EditorGUILayout.BeginHorizontal();

        // --- LEFT PANE (Master) ---
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.65f));
        {
            // 1. SEARCH/UTILITY TOOLBAR
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                if (EditorGUILayout.DropdownButton(plusIconContent, FocusType.Passive, EditorStyles.toolbarButton, GUILayout.Width(28)))
                {
                    ShowConditionCreationMenu(); // Condition-specific
                }
                DrawSearchField(ref conditionSearchText); 
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);

            // 2. TEMPLATE BROWSER
            DrawTemplateBrowser(coreConditionTemplates, customConditionTemplates);
            
            EditorGUILayout.Space(10); // Separator

            // 3. ASSET TREE VIEW
            EditorGUILayout.LabelField("Condition Scripts & Assets", EditorStyles.boldLabel);
            DrawTypeAssetTreeView(conditionTypes, conditionAssets, conditionTypeFoldouts, conditionSearchText);
        }
        EditorGUILayout.EndVertical(); // End Left Pane
        
        // --- RIGHT PANE (Detail) ---
        DrawTemplatePreviewPanel();
        
        EditorGUILayout.EndHorizontal(); // End Main Split
    }

    //================================================================================
    // TAB: CUSTOM DATA (Placeholder)
    //================================================================================

    private void DrawCustomDataTab()
    {
        EditorGUILayout.LabelField("Custom Data Creation", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("This tab could hold tools for creating new *template files*.", MessageType.Info);
    }

    //================================================================================
    // TAB: DATA TYPES (Old) - Kept as requested
    //================================================================================

    private void DrawTypesTab()
    {
        DrawSearchToolbar(ref oldSearchText, CreateNewScriptTemplate);
        EditorGUILayout.Space();

        if (dataTypes.Count == 0)
        {
            EditorGUILayout.HelpBox("No C# scripts found that inherit from 'ScriptableObject' and 'ISystemData'.", MessageType.Warning);
            EditorGUILayout.HelpBox("Use the 'CREATE NEW DATA TYPE' button above to create one.", MessageType.Info);
            return;
        }
        
        EditorGUILayout.LabelField("Available Data Types (C# Scripts):", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        DrawTypeAssetTreeView(dataTypes, dataAssets, typeFoldouts, oldSearchText);
    }

    //================================================================================
    // REUSABLE UI COMPONENTS
    //================================================================================

    /// <summary>
    /// (REUSABLE) Draws the foldout browser for templates.
    /// </summary>
    private void DrawTemplateBrowser(List<TextAsset> coreTemplates, List<TextAsset> customTemplates)
    {
        showTemplateBrowser = EditorGUILayout.Foldout(showTemplateBrowser, "Templates", true, EditorStyles.foldoutHeader);

        if (showTemplateBrowser)
        {
            EditorGUILayout.BeginVertical(darkBoxStyle);
            {
                templateBrowserTab = GUILayout.Toolbar(templateBrowserTab, new string[] { "Core Templates", "Custom Templates" });
                EditorGUILayout.Space(5);
                
                switch (templateBrowserTab)
                {
                    case 0: 
                        EditorGUILayout.LabelField("Core Templates", EditorStyles.boldLabel);
                        DrawTemplateGridView(coreTemplates);
                        break;
                    case 1: 
                        EditorGUILayout.LabelField("Custom Templates", EditorStyles.boldLabel);
                        DrawTemplateGridView(customTemplates);
                        break;
                }
            }
            EditorGUILayout.EndVertical();
        }
    }

    /// <summary>
    /// (REUSABLE) Draws the "Medium Icon" grid view for templates.
    /// </summary>
    private void DrawTemplateGridView(List<TextAsset> templates)
    {
        if (templates.Count == 0)
        {
            EditorGUILayout.LabelField("No templates found in this category.");
            return;
        }

        float leftPaneWidth = position.width * 0.65f - 40; 
        int columns = Mathf.FloorToInt(leftPaneWidth / (gridButtonStyle.fixedWidth + 10));
        if (columns < 1) columns = 1;
        
        templateGridScroll = EditorGUILayout.BeginScrollView(templateGridScroll, GUILayout.Height(120)); 
        {
            int selection = -1;
            
            GUIContent[] gridIcons = templates.Select(template => 
                new GUIContent(template.name.Replace(".cs.txt", ""), scriptIcon.image, template.name)
            ).ToArray();

            selection = GUILayout.SelectionGrid(-1, gridIcons, columns, gridButtonStyle);
            
            if (selection != -1)
            {
                selectedTemplate = templates[selection]; 
                templatePreviewText = selectedTemplate.text; 
                GUI.FocusControl(null); 
            }
        }
        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// (REUSABLE) Draws the right-hand panel to preview the selected template's code.
    /// </summary>
    private void DrawTemplatePreviewPanel()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandHeight(true));
        
        string title = "Template Preview";
        if (selectedTemplate != null)
        {
            title = selectedTemplate.name.Replace(".cs.txt", ".cs");
        }
        
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        templatePreviewScroll = EditorGUILayout.BeginScrollView(templatePreviewScroll);
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextArea(templatePreviewText, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            EditorGUI.EndDisabledGroup();
        }
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// (REUSABLE) Draws only the search text field and the 'Clear' (X) button.
    /// </summary>
    private void DrawSearchField(ref string search)
    {
        search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);
        if (GUILayout.Button("X", EditorStyles.toolbarButton, GUILayout.Width(22)))
        {
            search = ""; 
            GUI.FocusControl(null);
        }
    }
    
    /// <summary>
    /// (REUSABLE) A generic toolbar with a '+' dropdown and a search field.
    /// </summary>
    private void DrawSearchToolbar(ref string search, Action createAction)
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        if (EditorGUILayout.DropdownButton(plusIconContent, FocusType.Passive, EditorStyles.toolbarButton, GUILayout.Width(28)))
        {
            createAction?.Invoke(); 
        }
        DrawSearchField(ref search);
        
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(5);
    }

    /// <summary>
    /// (REUSABLE) Draws the main tree view for Types that have SO Assets.
    /// </summary>
    private void DrawTypeAssetTreeView(List<Type> types, List<ScriptableObject> assets, Dictionary<Type, bool> foldoutState, string search)
    {
        List<Type> filteredTypes = types.Where(t => 
            string.IsNullOrEmpty(search) || t.Name.ToLower().Contains(search.ToLower())
        ).ToList();

        if (filteredTypes.Count == 0)
        {
            EditorGUILayout.HelpBox("No matching C# scripts found for '" + search + "'.", MessageType.Info);
        }

        foreach (Type type in filteredTypes)
        {
            if (!foldoutState.ContainsKey(type))
            {
                foldoutState.Add(type, false);
            }

            List<ScriptableObject> assetsOfType = assets.Where(asset => asset.GetType() == type).ToList();

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            {
                string foldoutLabel = $"{type.Name} ({assetsOfType.Count} assets)";
                foldoutState[type] = EditorGUILayout.Foldout(foldoutState[type], foldoutLabel, true, EditorStyles.foldout);
                GUILayout.FlexibleSpace(); 
                if (GUILayout.Button("+ Create New Asset", GUILayout.Width(150)))
                {
                    CreateNewAsset(type); 
                }
            }
            EditorGUILayout.EndHorizontal();

            if (foldoutState[type])
            {
                EditorGUI.indentLevel++; 
                if (assetsOfType.Count == 0)
                {
                    EditorGUILayout.LabelField("No .asset files found for this type.");
                }
                else
                {
                    foreach (ScriptableObject asset in assetsOfType)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.ObjectField(asset, typeof(ScriptableObject), false);
                        if (GUILayout.Button("Find", GUILayout.Width(60)))
                        {
                            EditorGUIUtility.PingObject(asset);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
                EditorGUI.indentLevel--; 
            }
        }
    }
    
    /// <summary>
    /// (REUSABLE) Draws a simpler tree view for Types that do not have assets (like MonoBehaviours).
    /// </summary>
    private void DrawTypeTreeView(List<Type> types, Dictionary<Type, bool> foldoutState, string search)
    {
        List<Type> filteredTypes = types.Where(t => 
            string.IsNullOrEmpty(search) || t.Name.ToLower().Contains(search.ToLower())
        ).ToList();

        if (filteredTypes.Count == 0)
        {
            EditorGUILayout.HelpBox("No matching C# scripts found for '" + search + "'.", MessageType.Info);
        }

        foreach (Type type in filteredTypes)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField(type.Name, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace(); 
                if (GUILayout.Button("Find Script", GUILayout.Width(150)))
                {
                    string scriptPath = FindScriptPath(type);
                    if (!string.IsNullOrEmpty(scriptPath))
                    {
                        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath));
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    //================================================================================
    // BACKEND LOGIC & UTILITIES
    //================================================================================

    /// <summary>
    /// Scans the project for the "Old Data Types" (ISystemData).
    /// </summary>
    public void RefreshDataLists()
    {
        dataTypes.Clear();
        dataAssets.Clear();
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            try
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    if (typeof(ISystemData).IsAssignableFrom(type) && 
                        type.IsSubclassOf(typeof(ScriptableObject)) && 
                        !type.IsAbstract)
                    {
                        dataTypes.Add(type);
                    }
                }
            }
            catch (Exception) { continue; }
        }
        
        string[] guids = AssetDatabase.FindAssets("t:ScriptableObject");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ScriptableObject obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (obj is ISystemData)
            {
                dataAssets.Add(obj);
            }
        }
        dataAssets = dataAssets.OrderBy(a => a.name).ToList();
        dataTypes = dataTypes.OrderBy(t => t.Name).ToList();
        this.Repaint();
    }

    /// <summary>
    /// Scans the project to find all Action-related scripts, assets, and templates.
    /// </summary>
    private void RefreshActionData()
    {
        actionTypes.Clear();
        actionAssets.Clear();
        coreActionTemplates.Clear();
        customActionTemplates.Clear();
        
        // 1. Find Types (C# Scripts)
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            try
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    if (typeof(GameAction).IsAssignableFrom(type) && !type.IsAbstract && type != typeof(GameAction))
                    {
                        actionTypes.Add(type);
                    }
                }
            }
            catch (Exception) { continue; }
        }

        // 2. Find Assets (.asset files)
        string[] assetGuids = AssetDatabase.FindAssets("t:GameAction"); 
        foreach (string guid in assetGuids)
        {
            actionAssets.Add(AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid)));
        }

        // 3. Find Core Templates (using AssetDatabase)
        string corePath = coreTemplatePath + "/Actions"; 
        string[] coreTemplateGuids = AssetDatabase.FindAssets("t:TextAsset", new[] { corePath });
        foreach (string guid in coreTemplateGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(".cs.txt"))
            {
                coreActionTemplates.Add(AssetDatabase.LoadAssetAtPath<TextAsset>(path));
            }
        }
        
        // 4. Find Custom Templates (using AssetDatabase)
        string customPath = customTemplatePath + "/Actions";
        string[] customTemplateGuids = AssetDatabase.FindAssets("t:TextAsset", new[] { customPath });
        foreach (string guid in customTemplateGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(".cs.txt"))
            {
                customActionTemplates.Add(AssetDatabase.LoadAssetAtPath<TextAsset>(path));
            }
        }

        actionTypes = actionTypes.OrderBy(t => t.Name).ToList();
        actionAssets = actionAssets.OrderBy(a => a.name).ToList();
        this.Repaint();
    }
    
    /// <summary>
    /// (NEW) Scans the project to find all Trigger-related scripts and templates.
    /// </summary>
    private void RefreshTriggerData()
    {
        triggerTypes.Clear();
        coreTriggerTemplates.Clear();
        customTriggerTemplates.Clear();
        
        // 1. Find Types (C# Scripts)
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            try
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    // Find all classes that INHERIT FROM BaseTrigger
                    if (typeof(BaseTrigger).IsAssignableFrom(type) && !type.IsAbstract && type != typeof(BaseTrigger)) // <-- DÜZELTİLDİ
                    {
                        triggerTypes.Add(type);
                    }
                }
            }
            catch (Exception) { continue; }
        }

        // 2. Find Core Templates (using AssetDatabase)
        string corePath = coreTemplatePath + "/Triggers"; 
        string[] coreTemplateGuids = AssetDatabase.FindAssets("t:TextAsset", new[] { corePath });
        foreach (string guid in coreTemplateGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(".cs.txt"))
            {
                coreTriggerTemplates.Add(AssetDatabase.LoadAssetAtPath<TextAsset>(path));
            }
        }
        
        // 3. Find Custom Templates (using AssetDatabase)
        string customPath = customTemplatePath + "/Triggers";
        string[] customTemplateGuids = AssetDatabase.FindAssets("t:TextAsset", new[] { customPath });
        foreach (string guid in customTemplateGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(".cs.txt"))
            {
                customTriggerTemplates.Add(AssetDatabase.LoadAssetAtPath<TextAsset>(path));
            }
        }

        triggerTypes = triggerTypes.OrderBy(t => t.Name).ToList();
        this.Repaint();
    }

    /// <summary>
    /// (NEW) Scans the project to find all Condition-related scripts and templates.
    /// </summary>
    private void RefreshConditionData()
    {
        conditionTypes.Clear();
        conditionAssets.Clear();
        coreConditionTemplates.Clear();
        customConditionTemplates.Clear();
        
        // 1. Find Types (C# Scripts)
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            try
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    if (typeof(BaseCondition).IsAssignableFrom(type) && !type.IsAbstract && type != typeof(BaseCondition))
                    {
                        conditionTypes.Add(type);
                    }
                }
            }
            catch (Exception) { continue; }
        }

        // 2. Find Assets (.asset files)
        string[] assetGuids = AssetDatabase.FindAssets("t:BaseCondition"); 
        foreach (string guid in assetGuids)
        {
            conditionAssets.Add(AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid)));
        }

        // 3. Find Core Templates (using AssetDatabase)
        string corePath = coreTemplatePath + "/Conditions"; 
        string[] coreTemplateGuids = AssetDatabase.FindAssets("t:TextAsset", new[] { corePath });
        foreach (string guid in coreTemplateGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(".cs.txt"))
            {
                coreConditionTemplates.Add(AssetDatabase.LoadAssetAtPath<TextAsset>(path));
            }
        }
        
        // 4. Find Custom Templates (using AssetDatabase)
        string customPath = customTemplatePath + "/Conditions";
        string[] customTemplateGuids = AssetDatabase.FindAssets("t:TextAsset", new[] { customPath });
        foreach (string guid in customTemplateGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(".cs.txt"))
            {
                customConditionTemplates.Add(AssetDatabase.LoadAssetAtPath<TextAsset>(path));
            }
        }

        conditionTypes = conditionTypes.OrderBy(t => t.Name).ToList();
        conditionAssets = conditionAssets.OrderBy(a => a.name).ToList();
        this.Repaint();
    }
    
    // --- SCRIPT & ASSET CREATION ---

    /// <summary>
    /// Creates a new .asset file instance from a specific C# Type.
    /// </summary>
    private void CreateNewAsset(Type type)
    {
        string defaultPath = "Assets";
        string scriptPath = FindScriptPath(type); 

        if (!string.IsNullOrEmpty(scriptPath))
        {
            string scriptDirectory = Path.GetDirectoryName(scriptPath); 
            string assetFolderName = type.Name + "_Assets";
            string potentialPath = Path.Combine(scriptDirectory, assetFolderName);
            if (AssetDatabase.IsValidFolder(potentialPath))
            {
                defaultPath = potentialPath;
            }
        }

        string path = EditorUtility.SaveFilePanelInProject(
            "Create New Data Asset",
            "New" + type.Name + ".asset",
            "asset",
            "Please select a location for the new data asset.",
            defaultPath
        );

        if (string.IsNullOrEmpty(path)) return; // User cancelled

        ScriptableObject newAsset = ScriptableObject.CreateInstance(type);
        AssetDatabase.CreateAsset(newAsset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = newAsset;
        EditorGUIUtility.PingObject(newAsset);

        // Refresh all relevant tabs
        RefreshDataLists();
        RefreshActionData();
        RefreshConditionData();
    }

    /// <summary>
    /// Creates a new C# script file from the old 'ISystemData' template.
    /// </summary>
    private void CreateNewScriptTemplate()
    {
        CreateNewScriptFromTemplate(
            "Create New Data Type Script", 
            "NewDataTemplate.cs", 
            null // Use the hardcoded old template
        );
    }

    /// <summary>
    /// Shows a menu for creating a new Action script
    /// </summary>
    private void ShowActionCreationMenu()
    {
        GenericMenu menu = new GenericMenu();
        
        foreach (var template in coreActionTemplates)
        {
            string templateName = template.name.Replace(".cs.txt", "");
            menu.AddItem(new GUIContent($"Core/{templateName}"), false, OnTemplateSelected, template);
        }
        foreach (var template in customActionTemplates)
        {
            string templateName = template.name.Replace(".cs.txt", "");
            menu.AddItem(new GUIContent($"Custom/{templateName}"), false, OnTemplateSelected, template);
        }

        if (coreActionTemplates.Count == 0 && customActionTemplates.Count == 0)
        {
            menu.AddDisabledItem(new GUIContent("No Action templates found in 'Editor/Templates/Actions' folder."));
        }
        menu.ShowAsContext();
    }

    /// <summary>
    /// (NEW) Shows a menu for creating a new Trigger script
    /// </summary>
    private void ShowTriggerCreationMenu()
    {
        GenericMenu menu = new GenericMenu();
        
        foreach (var template in coreTriggerTemplates)
        {
            string templateName = template.name.Replace(".cs.txt", "");
            menu.AddItem(new GUIContent($"Core/{templateName}"), false, OnTemplateSelected, template);
        }
        foreach (var template in customTriggerTemplates)
        {
            string templateName = template.name.Replace(".cs.txt", "");
            menu.AddItem(new GUIContent($"Custom/{templateName}"), false, OnTemplateSelected, template);
        }

        if (coreTriggerTemplates.Count == 0 && customTriggerTemplates.Count == 0)
        {
            menu.AddDisabledItem(new GUIContent("No Trigger templates found in 'Editor/Templates/Triggers' folder."));
        }
        menu.ShowAsContext();
    }

    /// <summary>
    /// (NEW) Shows a menu for creating a new Condition script
    /// </summary>
    private void ShowConditionCreationMenu()
    {
        GenericMenu menu = new GenericMenu();
        
        foreach (var template in coreConditionTemplates)
        {
            string templateName = template.name.Replace(".cs.txt", "");
            menu.AddItem(new GUIContent($"Core/{templateName}"), false, OnTemplateSelected, template);
        }
        foreach (var template in customConditionTemplates)
        {
            string templateName = template.name.Replace(".cs.txt", "");
            menu.AddItem(new GUIContent($"Custom/{templateName}"), false, OnTemplateSelected, template);
        }

        if (coreConditionTemplates.Count == 0 && customConditionTemplates.Count == 0)
        {
            menu.AddDisabledItem(new GUIContent("No Condition templates found in 'Editor/Templates/Conditions' folder."));
        }
        menu.ShowAsContext();
    }
    
    /// <summary>
    /// Callback when a template is selected from the '+' menu.
    /// This is now generic and works for Actions, Triggers, or Conditions.
    /// </summary>
    private void OnTemplateSelected(object templateObj)
    {
        TextAsset template = (TextAsset)templateObj;
        CreateNewScriptFromTemplate(
            "Create New Script", 
            template.name.Replace(".cs.txt", ".cs"), 
            template.text
        );
    }
    
    /// <summary>
    /// (REFACTORED) This is the core logic for creating any new script from a template.
    /// </summary>
    private void CreateNewScriptFromTemplate(string title, string defaultFileName, string templateContent)
    {
        string newScriptPath = EditorUtility.SaveFilePanel(
            title,
            Application.dataPath, // Default to Assets/
            defaultFileName,
            "cs"
        );

        if (string.IsNullOrEmpty(newScriptPath)) return; 

        if (!newScriptPath.StartsWith(Application.dataPath))
        {
            Debug.LogError("IAWindow Error: Scripts must be saved inside the project's 'Assets' folder.");
            return;
        }

        string newClassName = Path.GetFileNameWithoutExtension(newScriptPath);
        
        if (!System.Text.RegularExpressions.Regex.IsMatch(newClassName, @"^[a-zA-Z_]\w*$"))
        {
            Debug.LogError($"IAWindow Error: Invalid file name. '{newClassName}' is not a valid C# class name.");
            return;
        }

        // Handle the old "Data Types (Old)" tab case
        if (templateContent == null)
        {
            templateContent = $@"using UnityEngine;
[CreateAssetMenu(fileName = ""New{newClassName}"", menuName = ""Project Data/{newClassName}"")]
public class {newClassName} : ScriptableObject, ISystemData
{{
    // Add new variables here
}}
";
        }

        try
        {
            string finalContent = templateContent.Replace("##SCRIPT_NAME##", newClassName);
            File.WriteAllText(newScriptPath, finalContent);
            AssetDatabase.Refresh();

            string relativePath = "Assets" + newScriptPath.Substring(Application.dataPath.Length);
            UnityEngine.Object newScriptAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(relativePath);
            if (newScriptAsset != null)
            {
                Selection.activeObject = newScriptAsset;
                EditorGUIUtility.PingObject(newScriptAsset);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"IAWindow Error: Failed to create script from template. {ex.Message}");
        }
    }


    /// <summary>
    /// A helper function to find the file path of a C# script (Type).
    /// </summary>
    private string FindScriptPath(Type type)
    {
        string[] guids = AssetDatabase.FindAssets($"t:script {type.Name}");
        if (guids.Length == 0) 
        {
            Debug.LogWarning($"FindScriptPath: Could not find script for type {type.Name}");
            return null;
        }
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == type.Name)
            {
                return path;
            }
        }
        return null; // No exact match found
    }
}

