
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;
using System.IO;
using System;

namespace StudioManette.minus
{
    public class MinusWindow : EditorWindow
    {
        //visible properties
        public MinusSettingsObject currentMinusSettings;

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

            if (GUILayout.Button("Synchronize Self"))
            {
                Synchronize();
            }

            if (GUILayout.Button("Read Cortex Manifest"))
            {
                ReadCortexManifest();
            }
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
            request = Client.List();
            EditorApplication.update += ProgressListPackage;
        }


        static void ProgressListPackage()
        {
            if (request.IsCompleted)
            {
                if (request.Status == StatusCode.Success)
                {
                    foreach (UnityEditor.PackageManager.PackageInfo pcInfo in request.Result)
                    {
                        Debug.Log("package info : " + pcInfo.name + " / version : " + pcInfo.version);
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

                Debug.Log("AAA"); 
                while ((strLine = reader.ReadLine()) != null)
                {
                    if (strLine.Contains("\"dependencies\": {")) break ;
                }

                List<PackageManifestItem> packageList = new List<PackageManifestItem>();

                Debug.Log("BBB");
                while ((strLine = reader.ReadLine()) != null)
                {
                    if (strLine.Contains("},")) break;
                    else
                    {
                        Debug.Log("package line : " + strLine);
                        var result = strLine.Split(":");
                        packageList.Add(new PackageManifestItem(result[0], result[1]));
                    }
                }

                Debug.Log("CCC");

                foreach (PackageManifestItem pmi in packageList)
                {
                    Debug.Log("name : " + pmi.packageName + " / " + pmi.packageVersion );
                }
            }
            /*
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("error", "error : " + e.Message, "ok");
            }
            */
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
