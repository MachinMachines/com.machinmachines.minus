using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StudioManette.minus
{
    [CreateAssetMenu(fileName = "MinusSettings", menuName = "StudioManette/MinusSettings Asset", order = 1)]
    public class MinusSettingsObject : ScriptableObject
    {
        public ProjectInstance primaryProject;
    }

    [System.Serializable]
    public class ProjectInstance
    {

        public string path;

        public ProjectInstance(string _path)
        {
            path = _path;
        }
    }
}
