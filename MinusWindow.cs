
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;
using System.IO;
using System;
using System.Text.RegularExpressions;

namespace StudioManette.minus
{
    public class MinusWindow : EditorWindow
    {
        //visible properties
        public MinusSettingsObject currentMinusSettings;

        private List<PackageManifestItem> cortexPackageList;
        private List<PackageManifestItem> minusPackageList;

        private Vector2 scrollPosPackages;
        bool showPackages;

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
                Synchronize();
                ReadCortexManifest();
            }

            //GROUP 3 : DISPLAY PROJECT SETTINGS INFO
            EditorGUILayout.LabelField("PROJECT SETTINGS", EditorStyles.boldLabel);

            //GROUP 4 : DISPLAY PACKAGES INFO
            showPackages = EditorGUILayout.Foldout(showPackages, "PACKAGES", EditorStyles.foldoutHeader);
            if (showPackages)
            {
                DisplayPackages();
            }

        }

        private void DisplayPackages()
        {
            //Afficher les packages de cortex

            GUIStyle validStyle = new GUIStyle(EditorStyles.label);
            validStyle.normal.textColor = Color.green;
            GUIStyle wrongStyle = new GUIStyle(EditorStyles.label);
            wrongStyle.normal.textColor = Color.yellow;

            scrollPosPackages = EditorGUILayout.BeginScrollView(scrollPosPackages);
            foreach (PackageManifestItem package in cortexPackageList)
            {
                string packageVersionMinus = FindPackageVersionInMinus(package.packageName);
                bool isVersionValid = packageVersionMinus.Equals(package.packageVersion);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(package.packageName);
                EditorGUILayout.LabelField(package.packageVersion, GUILayout.Width(100));
                EditorGUILayout.LabelField(packageVersionMinus, isVersionValid ? validStyle : wrongStyle, GUILayout.Width(100));

                if (isVersionValid)
                {
                    GUILayout.Space(100);
                }
                else
                {
                    if (GUILayout.Button("Update", GUILayout.Width(100)))
                    {
                        //Synchronize();
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        string FindPackageVersionInMinus(string _packageName)
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

        private void Init()
        {
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


        static ListRequest request;

        private void Synchronize()
        {
            Debug.Log("Synchronize !! ");

            minusPackageList = new List<PackageManifestItem>();

            request = Client.List();
            EditorApplication.update += ProgressListPackage;
        }


        void ProgressListPackage()
        {
            if (request.IsCompleted)
            {
                if (request.Status == StatusCode.Success)
                {
                    foreach (UnityEditor.PackageManager.PackageInfo pcInfo in request.Result)
                    {
                        Debug.Log("package info : " + pcInfo.name + " / version : " + pcInfo.version);
                        minusPackageList.Add(new PackageManifestItem(pcInfo.name, pcInfo.version));
                    }
                }
                else if (request.Status >= StatusCode.Failure)
                {
                    Debug.Log(request.Error.message);
                }

                EditorApplication.update -= ProgressListPackage;
            }
        }

        void ReadCortexManifest()
        {
            //try
            {
                StreamReader reader = new StreamReader(currentMinusSettings.primaryProject.path + "/Packages/manifest.json");
                //string strManifest = reader.ReadToEnd();
                //Debug.Log("strManifest : " + strManifest);

                string strLine;

                // Debug.Log("AAA"); 

                while ((strLine = reader.ReadLine()) != null)
                {
                    if (strLine.Contains("\"dependencies\": {")) break ;
                }

                cortexPackageList = new List<PackageManifestItem>();

                // Debug.Log("BBB");

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

                // Debug.Log("CCC");

                foreach (PackageManifestItem pmi in cortexPackageList)
                {
                    Debug.Log("name : " + pmi.packageName + " / version : " + pmi.packageVersion );
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
