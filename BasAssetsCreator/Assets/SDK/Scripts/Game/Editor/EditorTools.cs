﻿using UnityEditor;
using System.IO;
using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace BS
{
    public class EditorToolsAssetBundleBuilder : EditorWindow
    {
        public static Dictionary<string, bool> exportBundle = new Dictionary<string, bool>();
        public static Dictionary<string, string> modExportDirectories = new Dictionary<string, string>();
        private static Dictionary<string, bool> dropDownModDirSelect = new Dictionary<string, bool>();
        static List<string> bundleNames;
        List<AssetBundleBuild> assetBundleBuilds;
        BuildAssetBundleOptions buildAssetBundleOptions;
        readonly string assetBundleDirectory = "Assets/AssetBundles";
        string streamingAssetsDirectory = "";
        readonly string githubLink = "https://github.com/KospY/BasSDK";
        public static bool exportToModFolders;
        Vector3 scroll0, scroll1;

        [MenuItem("Blade and Sorcery/Asset Bundle Builder")]
        public static void ShowWindow()
        {   //Opens window from toolbar
            EditorWindow.GetWindow<EditorToolsAssetBundleBuilder>("B&S Asset Bundle Builder");
        }

        private void OnFocus()
        {   //Loads saved state of bundle list
            bundleNames = new List<string>(AssetDatabase.GetAllAssetBundleNames());
            foreach (string bundle in bundleNames)
            {
                exportBundle[bundle] = EditorPrefs.GetBool("ExportBundle" + bundle, true);
                modExportDirectories[bundle] = EditorPrefs.GetString("ExportBundleDir" + bundle, null);
            }
            exportToModFolders = EditorPrefs.GetBool("exportToModFolders", exportToModFolders);
        }

        private void OnDisable()
        {
            //Saves current bundle list selection
            try 
            {
                foreach (string bundle in bundleNames)
                {
                    EditorPrefs.SetBool("ExportBundle" + bundle, exportBundle[bundle]);
                    EditorPrefs.SetString("ExportBundleDir" + bundle, modExportDirectories[bundle]);
                }
            }
            catch (Exception) { }
        }

        private void OnGUI()
        {
            UnityWebRequest github = new UnityWebRequest(githubLink);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            scroll0 = GUILayout.BeginScrollView(scroll0); //SCROLL 0 START

            AssetBuilderMenu();

            GUILayout.EndScrollView(); //SCROLL 0 END
            GUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.FlexibleSpace();
        }

        private void AssetBuilderMenu()
        {
            scroll1 = GUILayout.BeginScrollView(scroll1); //SCROLL 1 START
            if (streamingAssetsDirectory == "")
            {
                streamingAssetsDirectory = EditorPrefs.GetString("streamingassetsDir", streamingAssetsDirectory);
            }

            //Displays header text 1
            GUILayout.Label(new GUIContent("Select the following asset bundles to export"), new GUIStyle("BoldLabel"));
            GUILayout.Space(5);

            //Displays asset bundle list

            foreach (string bundle in bundleNames)
            {
                GUILayout.BeginHorizontal();
                exportBundle[bundle] = GUILayout.Toggle(exportBundle[bundle], bundle);
                if (EditorPrefs.GetBool("ExportBundle" + bundle) != exportBundle[bundle])
                {
                    EditorPrefs.SetBool("ExportBundle" + bundle, exportBundle[bundle]);
                }
                EditorGUI.BeginDisabledGroup(!exportBundle[bundle] || streamingAssetsDirectory == null || streamingAssetsDirectory == "" || streamingAssetsDirectory.Substring(streamingAssetsDirectory.LastIndexOf('/') + 1, streamingAssetsDirectory.Length - streamingAssetsDirectory.LastIndexOf('/') - 1) != "StreamingAssets");
                
                GUILayout.BeginVertical();
                try
                {
                    if (modExportDirectories[bundle] == null || modExportDirectories[bundle] == "")
                    {
                        if (GUILayout.Button("Select target mod", new GUIStyle("DropDownButton")))
                        {
                            streamingAssetsDirectory = EditorPrefs.GetString("streamingassetsDir", streamingAssetsDirectory);
                            if (dropDownModDirSelect[bundle])
                            {
                                dropDownModDirSelect[bundle] = false;
                            }
                            else
                            {
                                dropDownModDirSelect[bundle] = true;
                            }

                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Target mod: " + modExportDirectories[bundle].Substring(modExportDirectories[bundle].LastIndexOf('\\') + 1, modExportDirectories[bundle].Length - modExportDirectories[bundle].LastIndexOf('\\') - 1), new GUIStyle("DropDownButton")))
                        {
                            streamingAssetsDirectory = EditorPrefs.GetString("streamingassetsDir", streamingAssetsDirectory);
                            if (dropDownModDirSelect[bundle])
                            {
                                dropDownModDirSelect[bundle] = false;
                            }
                            else
                            {
                                dropDownModDirSelect[bundle] = true;
                            }
                        }
                    }
                    if (dropDownModDirSelect[bundle])
                    {
                        foreach (string file in Directory.EnumerateDirectories(streamingAssetsDirectory))
                        {
                            if (GUILayout.Button(file.Substring(file.LastIndexOf('\\') + 1, file.Length - file.LastIndexOf('\\') - 1), new GUIStyle("radio")))
                            {
                                modExportDirectories[bundle] = file;
                                EditorPrefs.SetString("ExportBundleDir" + bundle, modExportDirectories[bundle]);
                                dropDownModDirSelect[bundle] = false;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    dropDownModDirSelect[bundle] = false;
                    modExportDirectories[bundle] = EditorPrefs.GetString("ExportBundleDir" + bundle, null);
                }
                GUILayout.EndVertical();

                EditorGUI.EndDisabledGroup();
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10);

            buildAssetBundleOptions = (BuildAssetBundleOptions)EditorGUILayout.EnumPopup("Option", buildAssetBundleOptions);

            exportToModFolders = GUILayout.Toggle(exportToModFolders, "Export asset bundles to their respective Mod folders.");
            if (exportToModFolders != EditorPrefs.GetBool("exportToModFolders"))
            {
                EditorPrefs.SetBool("exportToModFolders", exportToModFolders);
            }
            GUILayout.Space(5);
            //Button that builds asset bundles
            if (exportToModFolders)
            {
                GUILayout.Label("Asset Bundles will be exported to " + assetBundleDirectory + " and to mod Folders");
            }
            else
            {
                GUILayout.Label("Asset Bundles will be exported to " + assetBundleDirectory + ".");
            }
            if (GUILayout.Button("Build Asset Bundles"))
            {
                BuildAllAssetBundles();
            }

            GUILayout.EndScrollView(); //SCROLL 1 END
        }

        private void BuildAllAssetBundles()
        {
            assetBundleBuilds = new List<AssetBundleBuild>();
            string assetBundleDirectory = "Assets/AssetBundles";
            string msgEnd = "";
            DirectoryInfo dir = new DirectoryInfo(assetBundleDirectory);
            if (!Directory.Exists(assetBundleDirectory)) Directory.CreateDirectory(assetBundleDirectory);

            foreach (string bundle in bundleNames)
            {
                foreach (FileInfo file in dir.GetFiles("*.*"))
                {
                    if ((file.Extension != ".assets" && file.Extension != ".maps" && file.Extension != ".uma") || exportBundle[bundle])
                    {
                        file.Delete();
                    }
                }
                if (exportBundle[bundle])
                {
                    AssetBundleBuild build = new AssetBundleBuild
                    {
                        assetBundleName = bundle,
                        assetBundleVariant = AssetImporter.GetAtPath(assetBundleDirectory).assetBundleVariant,
                        assetNames = AssetDatabase.GetAssetPathsFromAssetBundle(bundle)
                    };
                    assetBundleBuilds.Add(build);
                }
                try
                {
                    EditorPrefs.SetBool("ExportBundle" + bundle, exportBundle[bundle]);
                }
                catch (Exception)
                {
                    EditorPrefs.SetBool("ExportBundle" + bundle, true);
                }
            }

            BuildPipeline.BuildAssetBundles(assetBundleDirectory, assetBundleBuilds.ToArray(), buildAssetBundleOptions, BuildTarget.StandaloneWindows);

            bundleNames = new List<string>(AssetDatabase.GetAllAssetBundleNames());

            foreach (FileInfo file in dir.GetFiles("*.*"))
            {
                if (bundleNames.Contains(file.Name))
                {
                    string ext = ".assets";
                    foreach (string asset in AssetDatabase.GetAssetPathsFromAssetBundle(file.Name))
                    {
                        Type assetType = AssetDatabase.GetMainAssetTypeAtPath(asset);
                        if (assetType == typeof(SceneAsset))
                        {
                            ext = ".maps";
                            break;
                        }
                        else if (assetType.FullName == "UMA.RaceData" || assetType.FullName == "UMA.SlotDataAsset" || assetType.FullName == "UMA.OverlayDataAsset")
                        {
                            ext = ".uma";
                            break;
                        }
                    }

                    file.MoveTo(file.FullName + ext);
                    if (modExportDirectories[Path.GetFileNameWithoutExtension(file.Name)].Length > 1)
                    {
                        if (exportToModFolders && modExportDirectories.ContainsKey(file.Name.Substring(0, file.Name.Length - ext.Length)))
                        {
                            file.CopyTo(modExportDirectories[Path.GetFileNameWithoutExtension(file.Name)] + "/" + file.Name, true);
                            msgEnd = " and copied into mod folders";
                        }
                    }
                    else
                    {
                        msgEnd = ". Could not copy to mod folders";
                    }
                }
                else if (file.Extension != ".assets" && file.Extension != ".maps" && file.Extension != ".uma")
                {
                    file.Delete();
                }
            }
            AssetDatabase.Refresh();
            if (assetBundleBuilds.ToArray().Length > 0)
            {
                string bundleNames = "";
                string s = "";
                foreach (AssetBundleBuild build in assetBundleBuilds)
                {
                    if (assetBundleBuilds[0].assetBundleName == build.assetBundleName)
                    {
                        bundleNames = " " + build.assetBundleName;
                    }
                    else if (assetBundleBuilds[assetBundleBuilds.Count - 1].assetBundleName == build.assetBundleName)
                    {
                        s = "s";
                        bundleNames = bundleNames + " and " + build.assetBundleName;
                    }
                    else
                    {
                        s = "s";
                        bundleNames = bundleNames + ", " + build.assetBundleName;
                    }
                }
                Debug.Log("Created Asset Bundle" + s + bundleNames + " in " + dir.FullName + msgEnd);
            }
        }

    }

    public class EditorToolsJSONConfig : EditorWindow
    {
        public static Dictionary<string, bool> folderShown = new Dictionary<string, bool>();
        public static Dictionary<string, GameObject> ModObject = new Dictionary<string, GameObject>();
        List<string> modFolders = new List<string>();
        public static string streamingAssetsDirectory;
        public static string selectedModDirectory;
        Vector3 scroll2, scroll3;
        public static string openJson = "";

        [MenuItem("Blade and Sorcery/Mod Configuration")]
        public static void ShowWindow()
        {   //Opens window from toolbar
            GetWindow<EditorToolsJSONConfig>("B&S Mod Configuration");
        }

        private void OnFocus()
        {
            streamingAssetsDirectory = EditorPrefs.GetString("streamingassetsDir", streamingAssetsDirectory);
            selectedModDirectory = EditorPrefs.GetString("selectedMod", selectedModDirectory);
        }

        private void OnDisable()
        {
            EditorPrefs.SetString("streamingassetsDir", streamingAssetsDirectory);
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            JSONConfigMenu();

            GUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.FlexibleSpace();
        }
        public bool selectedName;
        private void JSONConfigMenu()
        {
            //Displays header text 2
            GUILayout.Label(new GUIContent("Configure mod"), new GUIStyle("BoldLabel"));
            GUILayout.Space(5);

            if (streamingAssetsDirectory != null && streamingAssetsDirectory != "" && streamingAssetsDirectory.Substring(streamingAssetsDirectory.LastIndexOf('/') + 1, streamingAssetsDirectory.Length - streamingAssetsDirectory.LastIndexOf('/') - 1) == "StreamingAssets")
            {
                if (GUILayout.Button(streamingAssetsDirectory, new GUIStyle("textField")))
                {
                    streamingAssetsDirectory = EditorUtility.OpenFolderPanel("Select streamingassets folder", "", "");
                }
                modFolders.Clear();
                foreach (string file in Directory.EnumerateDirectories(streamingAssetsDirectory))
                {
                    modFolders.Add(file);
                }
                GUILayout.Space(10);

                if (!modFolders.Contains(selectedModDirectory))
                {
                    selectedModDirectory = "";
                    EditorPrefs.SetString("selectedMod", selectedModDirectory);
                }

                //MOD FOLDER SELECTION LIST
                if (selectedModDirectory == null || selectedModDirectory == "")
                {
                    GUILayout.Label("Select mod to set up or Create a new one");
                    if (GUILayout.Button("Create New Mod"))
                    {
                        CreateWindow<ManifestBuilder>();
                    }
                    GUILayout.Space(5);
                    int modIndex = 0;
                    scroll2 = GUILayout.BeginScrollView(scroll2); //SCROLL 2 START
                    foreach (string mod in modFolders)
                    {
                        if (mod.Substring(mod.LastIndexOf('\\') + 1, mod.Length - mod.LastIndexOf('\\') - 1) != "Default" && mod.Substring(mod.LastIndexOf('\\') + 1, mod.Length - mod.LastIndexOf('\\') - 1) != "SteamVR" && !mod.Substring(mod.LastIndexOf('\\') + 1, mod.Length - mod.LastIndexOf('\\') - 1).StartsWith("_"))
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(10);
                            modIndex++;
                            if (GUILayout.Button(mod.Substring(mod.LastIndexOf('\\') + 1, mod.Length - mod.LastIndexOf('\\') - 1), new GUIStyle("Radio")))
                            {
                                selectedModDirectory = mod;
                                EditorPrefs.SetString("selectedMod", selectedModDirectory);
                            }
                            GUILayout.EndHorizontal();
                        }
                    }
                    GUILayout.EndScrollView(); //SCROLL 2 END

                }
                //JSON ITEMS IN MOD LIST
                else
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.BeginVertical();
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Currently editing " + selectedModDirectory.Substring(selectedModDirectory.LastIndexOf('\\') + 1, selectedModDirectory.Length - selectedModDirectory.LastIndexOf('\\') - 1));

                    if (GUILayout.Button("Back"))
                    {
                        selectedModDirectory = "";
                        EditorPrefs.SetString("selectedMod", selectedModDirectory);
                    }
                    else
                    {
                        GUILayout.EndHorizontal();
                        //Check if there is a manifest.json file
                        bool hasManifest = false;
                        foreach (FileInfo file in new DirectoryInfo(selectedModDirectory).GetFiles())
                        {
                            if (file.Name == "manifest.json")
                            {
                                hasManifest = true;
                            }
                        }
                        if (hasManifest)
                        {
                            if (GUILayout.Button("Edit mod details"))
                            {
                                EditorWindow.GetWindow<ManifestBuilder>("Edit Mod Details");
                            }
                        }
                        GUILayout.EndVertical();
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();

                        scroll3 = GUILayout.BeginScrollView(scroll3); //SCROLL 3 START

                        GUILayout.Space(10);

                        //Display mod items
                        if (hasManifest)
                        {
                            foreach (FileInfo file in new DirectoryInfo(selectedModDirectory).GetFiles())
                            {
                                ModConfigOptions(file);
                            }

                            FolderButtons(selectedModDirectory);

                            GUILayout.Space(10);
                            //Create new JSON button
                            GUILayout.BeginHorizontal();
                            if (GUILayout.Button("New Weapon"))
                            {
                                openJson = "";
                                EditorWindow.GetWindow<WeaponJsonBuilder>("Weapon JSON Builder");

                            }
                            //if (GUILayout.Button("New Map"))
                            //{
                            //    openJson = "";
                            //    EditorWindow.GetWindow<MapJsonBuilder>("Map JSON Builder");

                            //}
                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();

                        }
                        else
                        {
                            EditorGUILayout.HelpBox("This folder does not have a Maifest.json file, would you like to create one?", MessageType.Warning);
                            if (GUILayout.Button("Create new mod"))
                            {
                                EditorWindow.GetWindow<ManifestBuilder>("Create Mod file");
                            }
                        }
                        GUILayout.EndScrollView(); //SCROLL 2 END
                    }
                }

            }
            //No streamingassets folder selected
            else
            {
                GUILayout.BeginHorizontal();
                EditorGUIUtility.labelWidth = GUI.skin.label.CalcSize(new GUIContent()).x;
                EditorStyles.helpBox.stretchWidth = false;
                EditorGUILayout.HelpBox("", MessageType.Warning, false);
                if (streamingAssetsDirectory.Substring(streamingAssetsDirectory.LastIndexOf('/') + 1, streamingAssetsDirectory.Length - streamingAssetsDirectory.LastIndexOf('/') - 1) != "StreamingAssets")
                {
                    EditorGUILayout.BeginVertical();
                    if (GUILayout.Button("Unable to find StreamingAssets. Please try again"))
                    {
                        streamingAssetsDirectory = EditorUtility.OpenFolderPanel("Select streamingassets folder", "", "");
                        EditorPrefs.SetString("streamingassetsDir", streamingAssetsDirectory);
                    }
                    GUILayout.Label(streamingAssetsDirectory);
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    if (GUILayout.Button("The mod folder directory has not been selected"))
                    {
                        streamingAssetsDirectory = EditorUtility.OpenFolderPanel("Select streamingassets folder", "", "");
                        EditorPrefs.SetString("streamingassetsDir", streamingAssetsDirectory);
                    }
                }
                GUILayout.EndHorizontal();
            }
        }

        public static void AddPrefab(string fileName, string prefabDir)
        {

            string ObjectKey = selectedModDirectory.Substring(selectedModDirectory.LastIndexOf('\\') + 1, selectedModDirectory.Length - selectedModDirectory.LastIndexOf('\\') - 1) + "-" + fileName;
            ModObject[ObjectKey] = (GameObject)AssetDatabase.LoadAssetAtPath(prefabDir, typeof(GameObject));
            EditorPrefs.SetString("Object" + ObjectKey, prefabDir);
        }

        private void ModConfigOptions(FileInfo file)
        {
            if (file.Extension == ".json" && file.Name != "manifest.json" && file.Name.Substring(0, 5) == "Item_")
            {
                string ObjectKey = selectedModDirectory.Substring(selectedModDirectory.LastIndexOf('\\') + 1, selectedModDirectory.Length - selectedModDirectory.LastIndexOf('\\') - 1) + "-" + file.Name;
                ModObject[ObjectKey] = (GameObject)AssetDatabase.LoadAssetAtPath(EditorPrefs.GetString("Object" + ObjectKey), typeof(GameObject));
                string jsonContents = File.ReadAllText(file.FullName);

                if (jsonContents == "")
                {
                    return;
                }

                GUILayout.BeginHorizontal();
                if (jsonContents.Substring(jsonContents.IndexOf("\"id\": \"") + 7).Substring(0, jsonContents.Substring(jsonContents.IndexOf("\"id\": \"") + 7).IndexOf("\"")) == "")
                {
                    GUILayout.EndHorizontal();
                    return;
                }
                GUILayout.Label(jsonContents.Substring(jsonContents.IndexOf("\"id\": \"") + 7).Substring(0, jsonContents.Substring(jsonContents.IndexOf("\"id\": \"") + 7).IndexOf("\"")));
                GUILayout.BeginVertical();

                if (ModObject.ContainsKey(ObjectKey))
                {
                    ModObject[ObjectKey] = (GameObject)EditorGUILayout.ObjectField(ModObject[ObjectKey], typeof(GameObject), false);
                }

                if (ModObject[ObjectKey] && AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(ModObject[ObjectKey])).assetBundleName == "")
                {
                    GUILayout.Label("Prefab has no asset bundle assigned.");
                }
                GUILayout.EndVertical();
                if (ModObject[ObjectKey] && AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(ModObject[ObjectKey])).assetBundleName == "")
                {
                    GUILayout.Label(EditorGUIUtility.FindTexture("console.warnicon"));
                    EditorGUI.BeginDisabledGroup(true);
                    GUILayout.Button("Edit Weapon");
                    EditorGUI.EndDisabledGroup();

                }
                else
                { //EDIT EXISTING WEAPON JSON
                    if (!ModObject[ObjectKey]) { GUI.enabled = false; }
                    if (GUILayout.Button("Edit Weapon"))
                    {
                        openJson = file.FullName;
                        WeaponJsonBuilder.prefabKey = ObjectKey;
                        GetWindow<WeaponJsonBuilder>("Item JSON Builder");
                    }
                    GUI.enabled = true;
                }

                if (ModObject[ObjectKey])
                {
                    EditorPrefs.SetString("Object" + ObjectKey, AssetDatabase.GetAssetPath(ModObject[ObjectKey]));
                }
                try
                {
                    GUILayout.FlexibleSpace();
                }
                catch (Exception) { }
                GUILayout.EndHorizontal();
                GUILayout.Space(5);
            }
        }

        private void FolderButtons(string selectedFileDirectory)
        {
            for (int i = 0; i < new DirectoryInfo(selectedFileDirectory).GetDirectories().Length; i++)
            {
                folderShown[selectedFileDirectory] = EditorPrefs.GetBool(new DirectoryInfo(selectedFileDirectory).GetDirectories()[i].FullName);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(new DirectoryInfo(selectedFileDirectory).GetDirectories()[i].Name, new GUIStyle("DropDownButton")))
                {
                    if (folderShown[selectedFileDirectory])
                    {
                        folderShown[selectedFileDirectory] = false;
                        EditorPrefs.SetBool(new DirectoryInfo(selectedFileDirectory).GetDirectories()[i].FullName, false);
                    }
                    else
                    {
                        folderShown[selectedFileDirectory] = true;
                        EditorPrefs.SetBool(new DirectoryInfo(selectedFileDirectory).GetDirectories()[i].FullName, true);
                    }
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                if (folderShown[selectedFileDirectory])
                {
                    foreach (FileInfo file in new DirectoryInfo(new DirectoryInfo(selectedFileDirectory).GetDirectories()[i].FullName).GetFiles())
                    {
                        ModConfigOptions(file);
                    }
                }
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.BeginVertical();
                try
                {
                    if (folderShown[selectedFileDirectory] && new DirectoryInfo(selectedFileDirectory).GetDirectories()[i].GetFiles() != null)
                    {
                        FolderButtons(new DirectoryInfo(selectedFileDirectory).GetDirectories()[i].FullName);
                    }
                }
                catch (Exception)
                {
                    folderShown[selectedFileDirectory] = false;
                    EditorPrefs.SetBool(new DirectoryInfo(selectedFileDirectory).GetDirectories()[i].FullName, false);
                }
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }
        }
    }
}