using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

// Renamed to IAWindow (Interaction Authoring Studio Window)
public class IAWindow : EditorWindow
{
    [MenuItem("Tools/Interaction Authoring Studio")]
    public static void ShowWindow()
    {
        GetWindow<IAWindow>("Interaction Authoring Studio");
    }

    // --- Private Fields ---
    private int selectedTab;
    private string searchText = "";
    private Vector2 scrollPosition;

    // --- Icons ---
    private GUIContent plusIconContent;
    private GUIContent refreshIconContent;

    // --- Data Lists (for the old tab) ---
    private List<Type> dataTypes = new List<Type>();
    private List<ScriptableObject> dataAssets = new List<ScriptableObject>();
    private Dictionary<Type, bool> typeFoldouts = new Dictionary<Type, bool>();

    // --- Styles ---
    private GUIStyle tabStyle;

    //================================================================================
    // UNITY EVENT METHODS
    //================================================================================

    private void OnEnable()
    {
        // Load data
        RefreshDataLists();
        
        // Load icons
        plusIconContent = EditorGUIUtility.IconContent("Toolbar Plus");
        refreshIconContent = EditorGUIUtility.IconContent("Refresh");

        // Initialize GUI styles
        InitializeStyles();
    }

    /// <summary>
    /// This is the main render loop for the EditorWindow.
    /// It is kept clean by delegating tasks to other methods.
    /// </summary>
    private void OnGUI()
    {
        // Styles must be initialized here (not OnEnable) as they can be lost on domain reload
        if (tabStyle == null)
        {
            InitializeStyles();
        }

        DrawToolbar();
        DrawActiveTabContent();
    }

    //================================================================================
    // CORE DRAWING & LAYOUT
    //================================================================================

    /// <summary>
    /// Initializes custom GUI styles used in the window.
    /// </summary>
    private void InitializeStyles()
    {
        // This style stops the toolbar buttons from stretching
        tabStyle = new GUIStyle(EditorStyles.toolbarButton);
        tabStyle.stretchWidth = false;
    }

    /// <summary>
    /// Draws the top toolbar with the main navigation tabs.
    /// </summary>
    private void DrawToolbar()
    {
        // Updated with new tab structure
        selectedTab = GUILayout.Toolbar(
            selectedTab, 
            new string[] { "Data Types (Old)", "Actions", "Triggers", "Custom Data" }, 
            tabStyle 
        );
        EditorGUILayout.Space();
    }

    /// <summary>
    /// Acts as a router, drawing the content for the currently selected tab.
    /// </summary>
    private void DrawActiveTabContent()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        switch (selectedTab)
        {
            case 0: // Old "Data Types" tab
                DrawTypesTab(); 
                break;
                
            case 1: // New "Actions" tab
                DrawActionsTab();
                break;

            case 2: // New "Triggers" tab
                DrawTriggersTab();
                break;

            case 3: // New "Custom Data" tab
                DrawCustomDataTab();
                break;
        }

        EditorGUILayout.EndScrollView();
    }

    //================================================================================
    // NEW TAB DRAWING METHODS (TASK 1)
    //================================================================================
    private void DrawActionsTab()
    {
        // --- 1. SEKMEYE ÖZEL ARAÇ ÇUBUĞU ---
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        {
            // --- 1a. 'Create New' Dropdown Düğmesi ---
            if (EditorGUILayout.DropdownButton(plusIconContent, FocusType.Passive, EditorStyles.toolbarButton, GUILayout.Width(28)))
            {
                // Bu, artık genel bir fonksiyon değil, 'Action' sekmesine özel bir menü açar
                ShowActionCreationMenu(); 
            }

            // --- 1b. Yeniden Kullanılabilir Arama Çubuğu ---
            DrawSearchField(ref actionSearchText);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(5);

        // --- 2. AĞAÇ GÖRÜNÜMÜ ---
        EditorGUILayout.LabelField("Available Actions (C# Scripts):", EditorStyles.boldLabel);

        // Buraya 'actionTypes', 'actionAssets' ve 'actionSearchText' kullanan
        // DrawTypeAssetTreeView(...) çağrın gelecek.
        // ...
    }

    /// <summary>
    /// Draws the 'Triggers' tab, showing BaseTrigger scripts and assets.
    /// </summary>
    private void DrawTriggersTab()
    {
        EditorGUILayout.LabelField("Triggers Tab", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("This tab will manage all 'BaseTrigger' types and templates.", MessageType.Info);
    }

    /// <summary>
    /// Draws the 'Custom Data' tab for advanced script/asset creation.
    /// </summary>
    private void DrawCustomDataTab()
    {
        EditorGUILayout.LabelField("Custom Data Creation", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("This tab will be a general-purpose tool for creating any new script templates or SO assets.", MessageType.Info);
    }

    //================================================================================
    // REFACTORED & REUSABLE COMPONENTS (TASK 2)
    //================================================================================

    /// <summary>
    /// (REUSABLE) Draws only the search text field and the 'Clear' (X) button.
    /// This is highly reusable across all tabs.
    /// </summary>
    /// <param name="search">The string variable holding the search text (passed by ref).</param>
    private void DrawSearchField(ref string search)
    {
        // --- 1. Search Field ---
        search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);
        
        // --- 2. 'Clear Search' Button ---
        if (GUILayout.Button("X", EditorStyles.toolbarButton, GUILayout.Width(22)))
        {
            search = ""; // Clear the text
            GUI.FocusControl(null); // Remove focus from the search field
        }
    }
    // Örnek olarak 'Actions' sekmesini dolduralım
    // (Yeni veri listelerine ihtiyacın olacak, ama şimdilik mantığa odaklan)
    private string actionSearchText = ""; 

    
    /// <summary>
    /// Shows a GenericMenu with template options for creating a new ACTION script.
    /// </summary>
    private void ShowActionCreationMenu()
    {
        GenericMenu menu = new GenericMenu();

        // Bu yolları kendi Templates klasörüne göre güncelle
        string templatePathBasic = "Assets/InteractionAuthoringStudio/Editor/Templates/NewActionTemplate_Basic.cs.txt";
        string templatePathPhysics = "Assets/InteractionAuthoringStudio/Editor/Templates/NewActionTemplate_Physics.cs.txt";

        // Menüye "Basic" Eylem şablonunu ekle
        menu.AddItem(
            new GUIContent("New Action (from Basic Template)"), 
            false, // 'false' = işaretli değil
            OnTemplateSelected, // Tıklandığında çağrılacak fonksiyon
            templatePathBasic   // Bu 'templatePathBasic' string'i OnTemplateSelected fonksiyonuna 'object' olarak gönderilir
        );

        // Menüye "Physics" Eylem şablonunu ekle
        menu.AddItem(
            new GUIContent("New Action (from Physics Template)"), 
            false, 
            OnTemplateSelected, 
            templatePathPhysics
        );
        
        // Menüyü göster
        menu.ShowAsContext();
    }

    /// <summary>
    /// This is the callback function called when a user clicks an item in the GenericMenu.
    /// </summary>
    /// <param name="templatePathObj">The data (user data) passed from menu.AddItem. We must cast it to a string.</param>
    private void OnTemplateSelected(object templatePathObj)
    {
        string templatePath = (string)templatePathObj;

        // Artık hangi şablonun seçildiğini biliyoruz.
        // Eski 'CreateNewScriptTemplate' fonksiyonunu bu bilgiyi kullanacak şekilde çağır.
        // (CreateNewScriptTemplate fonksiyonunu da bu 'templatePath'i alacak şekilde güncellemen gerekecek)
        
        // CreateNewScriptFromTemplate(templatePath);
        
        Debug.Log($"User selected template: {templatePath}");
    }


    private void DrawSearchToolbar(ref string search, Action createAction)
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        // --- 1. 'Create New' Button ---
        if (EditorGUILayout.DropdownButton(plusIconContent, FocusType.Passive, EditorStyles.toolbarButton, GUILayout.Width(28)))
        {
            createAction?.Invoke(); // Call the provided action
        }
        
        // --- 2. Search Field ---
        search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);
        
        // --- 3. 'Clear Search' Button ---
        if (GUILayout.Button("X", EditorStyles.toolbarButton, GUILayout.Width(22)))
        {
            search = ""; // Clear the text
            GUI.FocusControl(null); // Remove focus from the search field
        }
        
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(5);
    }

    /// <summary>
    /// (REUSABLE) Draws the main tree view for a given set of Types and Assets.
    /// </summary>
    /// <param name="types">The list of C# Types (e.g., GameAction, BaseTrigger) to display.</param>
    /// <param name="assets">The complete list of .asset files to filter from.</param>
    /// <param name="foldoutState">A dictionary to store the expanded/collapsed state of each Type.</param>
    /// <param name="search">The current search filter text.</param>
    private void DrawTypeAssetTreeView(List<Type> types, List<ScriptableObject> assets, Dictionary<Type, bool> foldoutState, string search)
    {
        // Filter the types based on the search text
        List<Type> filteredTypes = types.Where(t => 
            string.IsNullOrEmpty(search) || t.Name.ToLower().Contains(search.ToLower())
        ).ToList();

        if (filteredTypes.Count == 0)
        {
            EditorGUILayout.HelpBox("No matching data types found for '" + search + "'.", MessageType.Info);
        }

        // Loop over the filtered list of C# script Types
        foreach (Type type in filteredTypes)
        {
            if (!foldoutState.ContainsKey(type))
            {
                foldoutState.Add(type, false);
            }

            // Find all .asset files that are of this specific C# Type
            List<ScriptableObject> assetsOfType = assets.Where(asset => asset.GetType() == type).ToList();

            // --- Draw the Foldout Header ---
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            {
                string foldoutLabel = $"{type.Name} ({assetsOfType.Count} assets)";
                foldoutState[type] = EditorGUILayout.Foldout(foldoutState[type], foldoutLabel, true, EditorStyles.foldout);

                GUILayout.FlexibleSpace(); 

                // --- 'Create New Asset' Button ---
                if (GUILayout.Button("+ Create New Asset", GUILayout.Width(150)))
                {
                    CreateNewAsset(type); // Call the asset creation utility
                }
            }
            EditorGUILayout.EndHorizontal();

            // --- Draw the Child Assets (if foldout is expanded) ---
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
    /// Draws the main "Data Types" tab content.
    /// This is now refactored to use the new reusable methods.
    /// </summary>
    private void DrawTypesTab()
    {
        // --- 1. Draw the reusable toolbar ---
        DrawSearchToolbar(ref searchText, CreateNewScriptTemplate);
        
        EditorGUILayout.Space();

        // --- 2. Help Box ---
        if (dataTypes.Count == 0)
        {
            EditorGUILayout.HelpBox("No C# scripts found that inherit from 'ScriptableObject' and 'ISystemData'.", MessageType.Warning);
            EditorGUILayout.HelpBox("Use the 'CREATE NEW DATA TYPE' button above to create one.", MessageType.Info);
            return;
        }
        
        EditorGUILayout.LabelField("Available Data Types (C# Scripts):", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // --- 3. Draw the reusable tree view ---
        DrawTypeAssetTreeView(dataTypes, dataAssets, typeFoldouts, searchText);
    }

    //================================================================================
    // BACKEND LOGIC & UTILITIES
    //================================================================================

    /// <summary>
    /// A placeholder function for a future feature tab.
    /// </summary>
    private void DrawFutureTabPlaceholder()
    {
        EditorGUILayout.LabelField("Future Tab Content", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("This tab is a placeholder for your next tool feature.", MessageType.Info);
    }

    /// <summary>
    /// This is the core engine of the tool. It scans the project
    /// using Reflection to find all relevant types and assets.
    /// </summary>
    public void RefreshDataLists()
    {
        // 1. Find Data Types (C# Scripts) - Legacy Reflection Method
        dataTypes.Clear();
        
        var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
        
        foreach (var assembly in assemblies)
        {
            try
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    // This logic is still tied to the old ISystemData
                    // The new tabs (Actions, Triggers) will need their own Refresh logic
                    // to find GameAction and BaseTrigger.
                    if (typeof(ISystemData).IsAssignableFrom(type) && 
                        type.IsSubclassOf(typeof(ScriptableObject)) && 
                        !type.IsAbstract)
                    {
                        dataTypes.Add(type);
                    }
                }
            }
            catch (System.Exception)
            {
                continue;
            }
        }
        
        // 2. Find Data Assets (.asset files)
        dataAssets.Clear();
        
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
    /// Creates a new .asset file instance from a specific C# Type.
    /// This function also tries to find the matching asset folder.
    /// </summary>
    private void CreateNewAsset(Type type)
    {
        string defaultPath = "Assets";
        string scriptPath = FindScriptPath(type); 

        if (!string.IsNullOrEmpty(scriptPath))
        {
            string scriptDirectory = System.IO.Path.GetDirectoryName(scriptPath); 
            string assetFolderName = type.Name + "_Assets";
            string potentialPath = System.IO.Path.Combine(scriptDirectory, assetFolderName);

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

        if (string.IsNullOrEmpty(path))
        {
            return; // User cancelled
        }

        ScriptableObject newAsset = ScriptableObject.CreateInstance(type);

        AssetDatabase.CreateAsset(newAsset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = newAsset;
        EditorGUIUtility.PingObject(newAsset);

        RefreshDataLists(); 
    }

    /// <summary>
    /// This is the "Code Scaffolding" part of the tool.
    /// It creates a new C# script file from a template.
    /// </summary>
    private void CreateNewScriptTemplate()
    {
        string path = EditorUtility.SaveFilePanel(
            "Create New Data Type Script",
            Application.dataPath, 
            "NewDataTemplate.cs",
            "cs"
        );

        if (string.IsNullOrEmpty(path))
        {
            return; // User cancelled
        }

        if (!path.StartsWith(Application.dataPath))
        {
            Debug.LogError("Error: Scripts must be saved inside the project's 'Assets' folder.");
            return;
        }

        string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
        string className = System.Text.RegularExpressions.Regex.Replace(fileName, "[^a-zA-Z0-9_]", "");
        if (string.IsNullOrEmpty(className) || char.IsDigit(className[0]))
        {
            Debug.LogError("Error: Invalid file name. Class name must start with a letter or underscore.");
            return;
        }

        // This template is still hard-coded to ISystemData
        // The "Custom Data" tab will need a more flexible template system
        string template =
$@"using UnityEngine;

/// <summary>
/// This script was automatically generated by the IAWindow tool.
/// </summary>
[CreateAssetMenu(fileName = ""New{className}"", menuName = ""Project Data/{className}"")]
public class {className} : ScriptableObject, ISystemData
{{
    // Add new variables here for designers to use.
    // Example:
    public string ItemName;
    public int ItemID;
    public Sprite ItemIcon;
}}
"; 
        
        string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
        string scriptDirectory = System.IO.Path.GetDirectoryName(relativePath);
        string assetFolderName = className + "_Assets";
        string assetFolderPath = System.IO.Path.Combine(scriptDirectory, assetFolderName);

        if (!AssetDatabase.IsValidFolder(assetFolderPath))
        {
            AssetDatabase.CreateFolder(scriptDirectory, assetFolderName);
            Debug.Log($"Tool: Created new asset folder at: {assetFolderPath}");
        }

        System.IO.File.WriteAllText(path, template);

        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog(
            "Script Created",
            $"{className}.cs and matching folder '{assetFolderName}' were successfully created and are compiling.\n\nPress 'Refresh List' after compilation to see the new type.",
            "OK"
        );
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
            if (System.IO.Path.GetFileNameWithoutExtension(path) == type.Name)
            {
                return path;
            }
        }
        
        return null; // Could not find exact match
    }
}