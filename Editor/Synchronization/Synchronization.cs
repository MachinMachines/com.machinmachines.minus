// Copyright 2022 MachinMachines
//
// Licensed under the Apache License, Version 2.0 (the "License")
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace MachinMachines
{
    namespace Minus
    {
        public static class Synchronization
        {

            internal static readonly string STR_MISSING_PACKAGE = "none";

            /*
             * Returns a dictionnary of <string,string> :
             * key : each file contained in _directory
             * value : MD5 of given file.
             */
            public static Dictionary<string, string> GetHashedFilesOfDirectory(string _directory)
            {
                Dictionary<string, string> tmpDict = new Dictionary<string, string>();

                DirectoryInfo info = new DirectoryInfo(_directory);
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

            public static List<PackageManifestItem> GetExternalPackagesList(string packageDirectory)
            {
                List<PackageManifestItem> packageList = new List<PackageManifestItem>();
                string strLine;

                StreamReader reader = new StreamReader(packageDirectory + "/manifest.json");

                while ((strLine = reader.ReadLine()) != null)
                {
                    if (strLine.Contains("\"dependencies\": {")) break;
                }

                while ((strLine = reader.ReadLine()) != null)
                {
                    if (strLine.Contains("},")) break;
                    else
                    {
                        //regex : \"(.*)\"\: \"(.*)\"[\,]*
                        Regex kPackageVersionRegex = new Regex("\"(.*)\"\\: \"(.*)\"[\\,]*", RegexOptions.Compiled | RegexOptions.Singleline);

                        MatchCollection matches = kPackageVersionRegex.Matches(strLine);
                        //Debug.Log("matches count : " + matches.Count);
                        if (matches.Count > 0)
                        {
                            string package = matches[0].Groups[1].Value;
                            string version = matches[0].Groups[2].Value;

                            //si l'assetprefix n'est pas renseigné ce n'est forcément pas un asset
                            packageList.Add(new PackageManifestItem(package, version));
                        }
                    }
                }

                return packageList;
            }

            internal static string FindPackageVersion(List<PackageManifestItem> thisProjectFiles, string _packageName)
            {
                PackageManifestItem tpmPackage = thisProjectFiles.FirstOrDefault(t => t.packageName == _packageName);

                return tpmPackage != null ? tpmPackage.packageVersion : STR_MISSING_PACKAGE;
            }

            internal static string FindProjectSettingFile(Dictionary<string, string> thisProjectFiles,  string _fileName)
            {
                foreach (KeyValuePair<string, string> kvp in thisProjectFiles)
                {
                    if (_fileName.Equals(kvp.Key))
                    {
                        return kvp.Value;
                    }
                }
                return "None";
            }


            public static string LogCompareFilesInfo(List<PackageManifestItem> primaryProjectFiles, List<PackageManifestItem> localProjectFiles, bool isVerbose = false)
            {
                List<PackageManifestItem> validPackages = new();
                List<PackageManifestItem> missingPackages = new();
                Dictionary<PackageManifestItem, string> invalidPackages = new();

                foreach (PackageManifestItem pmi in primaryProjectFiles)
                {
                    string localPackageVersion = FindPackageVersion(localProjectFiles, pmi.packageName);
                    if (localPackageVersion.Equals(pmi.packageVersion))
                    {
                        validPackages.Add(pmi);
                    }
                    else if (localPackageVersion.Equals(Synchronization.STR_MISSING_PACKAGE))
                    {
                        missingPackages.Add(pmi);
                    }
                    else invalidPackages.Add(pmi, localPackageVersion);
                }

                string logText = "";

                if (invalidPackages.Count == 0) logText += "=== OK ===";
                else logText += "=== INVALID ===";

                foreach (KeyValuePair<PackageManifestItem, string> invalidValue in invalidPackages)
                {
                    logText += "\n Invalid Package : " + invalidValue.Key.packageName + " / local version : " + invalidValue.Value + " - primary version : " + invalidValue.Key.packageVersion;
                }
                if (isVerbose)
                {
                    foreach (PackageManifestItem validPMI in validPackages)
                    {
                        logText += "\n Valid Package " + validPMI.packageName + " / version : " + validPMI.packageVersion;
                    }
                    foreach (PackageManifestItem missingPMI in missingPackages)
                    {
                        logText += "\n Missing Package : " + missingPMI.packageName + " / primary version : " + missingPMI.packageVersion; ;
                    }
                }

                return logText;
            }

            public static string LogCompareFilesInfo(Dictionary<string,string> primaryProjectFiles, Dictionary<string, string> localProjectFiles, bool isVerbose = false)
            {
                List<string> validSettings = new();
                List<string> invalidSettings = new();

                foreach (KeyValuePair<string, string> kvp in primaryProjectFiles)
                {
                    string checksumFromThis = FindProjectSettingFile(localProjectFiles, kvp.Key);
                    if (kvp.Value.Equals(checksumFromThis))
                    {
                        validSettings.Add(kvp.Key);
                    }
                    else invalidSettings.Add(kvp.Key);
                }

                string logText = "";

                if (validSettings.Count == 0) logText += "=== OK ===";
                else logText += "=== INVALID ===";

                foreach (string invalidValue in invalidSettings)
                {
                    logText += "\n Invalid Settings : " + invalidValue;
                }
                if (isVerbose)
                {
                    foreach (string validSetting in validSettings)
                    {
                        logText += "\n Valid Settings : " + validSetting;
                    }
                }

                return logText;
            }
        }
    }
}
