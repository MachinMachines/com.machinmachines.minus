
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;
using System.IO;
using System;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

namespace StudioManette.minus
{
    public class MinusWindow : EditorWindow
    {
        private static readonly int WIDTH_CASE_PACKAGE = 100;

        /* visible properties */
        public MinusSettingsObject currentMinusSettings;

        private List<PackageManifestItem> cortexPackageList;
        private List<PackageManifestItem> minusPackageList;

        private Dictionary<string, string> cortexProjectSettingFiles;
        private Dictionary<string, string> minusProjectSettingFiles;


        /* ui properties */
        private GUIStyle validStyle;
        private GUIStyle wrongStyle;
        private bool showPackages;
        private Vector2 scrollPosPackages;
        private bool showProjectSettings;
        private Vector2 scrollPosProjectSettings;

        /* properties for async operations */
        private ListRequest _listRequest;
        private AddRequest _addRequest;
        private bool isRunningAsyncOperation = false;

        [MenuItem("Studio Manette/Minus")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(MinusWindow), false, "Minus - Studio Manette");
        }

        public void OnEnable()
        {
            Init();
        }


        public void OnGUI()
        {
            //GROUP 1 : PROPERTIES
            EditorGUILayout.BeginVertical("Box");
            {
                EditorGUILayout.LabelField("PROPERTIES", EditorStyles.boldLabel);

                currentMinusSettings = (MinusSettingsObject)EditorGUILayout.ObjectField(currentMinusSettings, typeof(MinusSettingsObject), false);
            }
            EditorGUILayout.EndVertical();

            //GROUP 2 : BUTTONS
            if (GUILayout.Button("Synchronize"))
            {
                SynchronizeLocalPackages();
                SynchronizeCortexPackages();
                SynchronizeLocalProjectSettings();
                SynchronizeCortexProjectSettings();
            }

            /*
            Texture2D tex2 = new Texture2D(2,2);

            Color fillColor = new Color(0.1f, 0.1f, 0.1f) ;
            var fillColorArray = tex2.GetPixels();

            for (var i = 0; i < fillColorArray.Length; ++i)
            {
                fillColorArray[i] = fillColor;
            }
            tex2.SetPixels(fillColorArray);
            tex2.Apply();

            GUIStyle foldOutStyle = new GUIStyle(EditorStyles.foldoutHeader);
            foldOutStyle.normal.background = tex2;
            */

            
            if (isRunningAsyncOperation)
            {
                EditorGUILayout.LabelField("Waiting for synchronization...");
            }
            else
            {
                //GROUP 3 : DISPLAY PACKAGES INFO

                EditorGUILayout.BeginVertical("box");
                showPackages = EditorGUILayout.Foldout(showPackages, "PACKAGES", EditorStyles.foldoutHeader);
                if (showPackages)
                {
                    DisplayPackages();
                }
                EditorGUILayout.EndVertical();

                //GROUP 4 : DISPLAY PROJECT SETTINGS INFO

                EditorGUILayout.BeginVertical("box");
                showProjectSettings = EditorGUILayout.Foldout(showProjectSettings, "PROJECT SETTINGS", EditorStyles.foldoutHeader);
                if (showProjectSettings)
                {
                    DisplayProjectSettings();
                }
                EditorGUILayout.EndVertical();
            }
        }

        private void DisplayPackages()
        {
            //Display Headers
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Package name", EditorStyles.boldLabel, GUILayout.Width(WIDTH_CASE_PACKAGE*3));
            EditorGUILayout.LabelField("Primary", EditorStyles.boldLabel, GUILayout.Width(WIDTH_CASE_PACKAGE));
            EditorGUILayout.LabelField("This", EditorStyles.boldLabel, GUILayout.Width(WIDTH_CASE_PACKAGE));
            EditorGUILayout.LabelField("", GUILayout.Width(WIDTH_CASE_PACKAGE));
            EditorGUILayout.EndHorizontal();

            //Display Each Package
            if (cortexPackageList.Count > 0)
            {
                scrollPosPackages = EditorGUILayout.BeginScrollView(scrollPosPackages);
                foreach (PackageManifestItem package in cortexPackageList)
                {
                    string packageVersionMinus = FindPackageVersionInMinus(package.packageName);
                    bool isVersionValid = packageVersionMinus.Equals(package.packageVersion);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(package.packageName, GUILayout.Width(WIDTH_CASE_PACKAGE * 3));
                    EditorGUILayout.LabelField(package.packageVersion, GUILayout.Width(WIDTH_CASE_PACKAGE));
                    EditorGUILayout.LabelField(packageVersionMinus, isVersionValid ? validStyle : wrongStyle, GUILayout.Width(WIDTH_CASE_PACKAGE));

                    if (isVersionValid)
                    {
                        GUILayout.Label("Up to Date", GUILayout.Width(WIDTH_CASE_PACKAGE));
                    }
                    else
                    {
                        if (GUILayout.Button("Update", GUILayout.Width(WIDTH_CASE_PACKAGE)))
                        {
                            UpdatePackage(package.packageName, package.packageVersion);
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.LabelField("please synchronize first.");
            }
        }

        private void UpdatePackage(string packageName, string newVersion)
        {
            if (EditorUtility.DisplayDialog("Warning", "Do you really want to update the package " + packageName + "to version " + newVersion + " ? ", "Yes", "No"))
            {
                // - To install a specific version of a package, use a package identifier ("name@version"). This is the only way to install a pre-release version.
                string identifier = packageName + "@" + newVersion;
                _addRequest = Client.Add(identifier);
                EditorApplication.update += ProgressAddPackage;
            }
        }

        void ProgressAddPackage()
        {
            isRunningAsyncOperation = true;
            if (_addRequest.IsCompleted)
            {
                if (_addRequest.Status == StatusCode.Success)
                {
                    UnityEditor.PackageManager.PackageInfo pcInfo = _addRequest.Result;
                    {
                        Debug.Log("package well installed.");
                        Debug.Log("package info : " + pcInfo.name + " / version : " + pcInfo.version);
                        //minusPackageList.Add(new PackageManifestItem(pcInfo.name, pcInfo.version));
                    }
                }
                else if (_addRequest.Status >= StatusCode.Failure)
                {
                    Debug.Log(_listRequest.Error.message);
                }
                isRunningAsyncOperation = false;
                EditorApplication.update -= ProgressAddPackage;
            }
        }

        private string FindPackageVersionInMinus(string _packageName)
        {
            foreach (PackageManifestItem package in minusPackageList)
            {
                if (_packageName.Equals(package.packageName))
                { 
                    return package.packageVersion;
                }
            }
            return "missing";
        }

        private void DisplayProjectSettings()
        {
            if (cortexProjectSettingFiles !=null && cortexProjectSettingFiles.Count > 0)
            {
                scrollPosProjectSettings = EditorGUILayout.BeginScrollView(scrollPosProjectSettings);
                foreach (KeyValuePair<string, string> kvp in cortexProjectSettingFiles)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(kvp.Key);
                    EditorGUILayout.LabelField(kvp.Value);

                    string checksumFromMinus = FindProjectSettingFileInMinus(kvp.Key);
                    bool isChecksumValid = kvp.Value.Equals(checksumFromMinus);
                    EditorGUILayout.LabelField(checksumFromMinus, isChecksumValid ? validStyle : wrongStyle);

                    if (isChecksumValid)
                    {
                        GUILayout.Label("Up to Date", GUILayout.Width(WIDTH_CASE_PACKAGE));
                    }
                    else
                    {
                        if (GUILayout.Button("Update", GUILayout.Width(WIDTH_CASE_PACKAGE)))
                        {
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.LabelField("please synchronize first.");
            }
        }

        private string FindProjectSettingFileInMinus(string _fileName)
        {
            foreach (KeyValuePair<string, string> kvp in minusProjectSettingFiles)
            {
                if (_fileName.Equals(kvp.Key))
                {
                    return kvp.Value;
                }
            }
            return "None";
        }

        private void Init()
        {
            //styles
            validStyle = new GUIStyle(EditorStyles.label);
            validStyle.normal.textColor = Color.green;
            wrongStyle = new GUIStyle(EditorStyles.label);
            wrongStyle.normal.textColor = Color.yellow;

            List<MinusSettingsObject> settingsList = Utils.EditorAssets.FindAssetsByType<MinusSettingsObject>();
            if (settingsList.Count == 0)
            {
                Debug.LogError("There is no MinusSettingsObject in the project, please create one (Create/StudioManette/MinusSettings Asset).");
            }
            else if (settingsList.Count > 1)
            {
                Debug.LogError("There is more than one MinusSettingsObject, you should have only one. Please remove the extra ones.");
            }
            else
            {
                currentMinusSettings = settingsList[0];
            }
        }

        private void SynchronizeCortexProjectSettings()
        {
            cortexProjectSettingFiles = SynchronizeProjectSettings(currentMinusSettings.primaryProject.path + "/ProjectSettings");
        }

        private void SynchronizeLocalProjectSettings()
        {
            minusProjectSettingFiles = SynchronizeProjectSettings(Directory.GetCurrentDirectory() + "/ProjectSettings");
        }

        private Dictionary<string, string> SynchronizeProjectSettings(string projectSettingsDirectory)
        {
            Dictionary<string, string>  tmpDict = new Dictionary<string, string>();

            DirectoryInfo info = new DirectoryInfo(projectSettingsDirectory);
            FileInfo[] fileInfo = info.GetFiles();
            foreach (FileInfo file in fileInfo)
            {
                string hashStr = "none";
                MD5 md5 = MD5.Create();

                using (var stream = File.OpenRead(file.FullName))
                {
                   var hash = md5.ComputeHash(stream);
                   hashStr = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
                tmpDict.Add(file.Name, hashStr);
            }
            return tmpDict;
        }

        private void SynchronizeLocalPackages()
        {
            minusPackageList = new List<PackageManifestItem>();

            _listRequest = Client.List();
            EditorApplication.update += ProgressListPackage;
        }

        void ProgressListPackage()
        {
            isRunningAsyncOperation = true;
            if (_listRequest.IsCompleted)
            {
                if (_listRequest.Status == StatusCode.Success)
                {
                    foreach (UnityEditor.PackageManager.PackageInfo pcInfo in _listRequest.Result)
                    {
                        Debug.Log("package info : " + pcInfo.name + " / version : " + pcInfo.version);
                        minusPackageList.Add(new PackageManifestItem(pcInfo.name, pcInfo.version));
                    }
                }
                else if (_listRequest.Status >= StatusCode.Failure)
                {
                    Debug.Log(_listRequest.Error.message);
                }
                isRunningAsyncOperation = false;
                EditorApplication.update -= ProgressListPackage;
            }
        }

        void SynchronizeCortexPackages()
        {
            //try
            {
                StreamReader reader = new StreamReader(currentMinusSettings.primaryProject.path + "/Packages/manifest.json");

                string strLine;

                while ((strLine = reader.ReadLine()) != null)
                {
                    if (strLine.Contains("\"dependencies\": {")) break ;
                }

                cortexPackageList = new List<PackageManifestItem>();

                while ((strLine = reader.ReadLine()) != null)
                {
                    if (strLine.Contains("},")) break;
                    else
                    {
                        Debug.Log("package line : " + strLine);

                        //regex : \"(.*)\"\: \"(.*)\"[\,]*
                        Regex kPackageVersionRegex = new Regex("\"(.*)\"\\: \"(.*)\"[\\,]*", RegexOptions.Compiled | RegexOptions.Singleline);

                        MatchCollection matches = kPackageVersionRegex.Matches(strLine);
                        Debug.Log("matches count : " + matches.Count);
                        if (matches.Count > 0)
                        {
                            string package = matches[0].Groups[1].Value;
                            string version = matches[0].Groups[2].Value;

                            cortexPackageList.Add(new PackageManifestItem(package, version));
                        }
                    }
                }
            }
        }
    }

    [System.Serializable]
    public class PackageManifestItem
    {
        public string packageName;
        public string packageVersion;

        public PackageManifestItem(string _packageName, string _packageVersion)
        {
            this.packageName = _packageName;
            this.packageVersion = _packageVersion;
        }
    }
}
