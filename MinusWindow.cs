
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

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
    }
}
