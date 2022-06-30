using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StudioManette.minus
{
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
