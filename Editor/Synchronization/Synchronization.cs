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
using UnityEditor;

namespace StudioManette
{
    namespace Minus
    {
        public static class Synchronization
        {

            /// <summary>
            /// Settings
            /// </summary>
            public static readonly string SETTINGS_PRIMARY_PROJECT_PATH = "primaryProjectPath";

            /// <summary>
            /// Consts
            /// </summary>
            /// 
            public static readonly string STR_VALID_LOG = "=== OK ===";
            internal static readonly string STR_INVALID_LOG = "=== INVALID ===";
            internal static readonly string STR_MISSING_PCK_OR_PJS = "None";
            internal static readonly string STR_FOLDER_PACKAGES = "Packages";
            internal static readonly string STR_FOLDER_PROJECTSETTINGS = "ProjectSettings";
            internal static readonly string STR_INVALID_LOGS = "ProjectSettings";

            /// <summary>
            /// Getters
            /// </summary>
            /// 
            public static string PrimaryPath
            {
                get { return MinusSettings.instance.Get<string>(SETTINGS_PRIMARY_PROJECT_PATH, SettingsScope.User); }
            }

            public static string PrimaryPackagesDirectory
            {
                get {
                    return (PrimaryPath == null) ? null : Path.Combine(PrimaryPath, STR_FOLDER_PACKAGES) ;
                }
            }

            public static string PrimarySettingsDirectory
            {
                get
                {
                    return (PrimaryPath == null) ? null : Path.Combine(PrimaryPath, STR_FOLDER_PROJECTSETTINGS);
                }
            }

            public static string ThisPackagesDirectory
            {
                get { return Path.Combine(Directory.GetCurrentDirectory(), STR_FOLDER_PACKAGES); }
            }

            public static string ThisSettingsDirectory
            {
                get
                { return Path.Combine(Directory.GetCurrentDirectory(), STR_FOLDER_PROJECTSETTINGS); }
            }


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

            /// <summary>
            /// Finds the version of a package in a given list of project files based on the package name.
            /// </summary>
            /// <param name="thisProjectFiles">The list of package manifest items to search within.</param>
            /// <param name="_packageName">The name of the package whose version is to be retrieved.</param>
            /// <returns>The version of the specified package if found; otherwise, returns "None".</returns>
            internal static string FindPackageVersion(List<PackageManifestItem> thisProjectFiles, string _packageName)
            {
                PackageManifestItem tpmPackage = thisProjectFiles.FirstOrDefault(t => t.packageName == _packageName);

                return tpmPackage != null ? tpmPackage.packageVersion : STR_MISSING_PCK_OR_PJS;
            }

            /// <summary>
            /// Finds the value of a project setting in a given dictionary based on the file name.
            /// </summary>
            /// <param name="thisProjectFiles">The dictionary of project files to search within.</param>
            /// <param name="_fileName">The key (file name) whose value is to be retrieved.</param>
            /// <returns>The value associated with the specified key if found; otherwise, returns "None".</returns>

            internal static string FindProjectSettingFile(Dictionary<string, string> thisProjectFiles,  string _fileName)
            {
                foreach (KeyValuePair<string, string> kvp in thisProjectFiles)
                {
                    if (_fileName.Equals(kvp.Key))
                    {
                        return kvp.Value;
                    }
                }
                return STR_MISSING_PCK_OR_PJS;
            }

            /// <summary>
            /// Compares the list of primary project files with local project files and logs the status of packages.
            /// </summary>
            /// <param name="primaryProjectFiles">The list of primary package manifest items.</param>
            /// <param name="localProjectFiles">The list of local package manifest items.</param>
            /// <param name="isProcessingLocalPackages">Indicates whether the function should process local packages. Default value is false.</param>
            /// <param name="isVerbose">Indicates whether the function should return verbose logging. Default value is false.</param>
            /// <returns>A string containing the log of the package comparison. 
            /// If there is no invalid packages, this string is beginning with === OK === 
            /// If there is any invalid packages, this string is beginning with === INVALID ===.</returns>
            public static string LogCompareFilesInfo(List<PackageManifestItem> primaryProjectFiles, List<PackageManifestItem> localProjectFiles, bool isProcessingLocalPackages = false ,bool isVerbose = false)
            {
                List<PackageManifestItem> validPackages = new();
                List<PackageManifestItem> missingPackages = new();
                List<PackageManifestItem> localPackages = new();
                Dictionary<PackageManifestItem, string> invalidPackages = new();

                foreach (PackageManifestItem pmi in primaryProjectFiles)
                {
                    string localPackageVersion = FindPackageVersion(localProjectFiles, pmi.packageName);
                    if (isProcessingLocalPackages && localPackageVersion.Contains("file:"))
                    {
                        localPackages.Add(pmi);
                    }
                    else if (localPackageVersion.Equals(pmi.packageVersion))
                    {
                        validPackages.Add(pmi);
                    }
                    else if (localPackageVersion.Equals(Synchronization.STR_MISSING_PCK_OR_PJS))
                    {
                        missingPackages.Add(pmi);
                    }
                    else invalidPackages.Add(pmi, localPackageVersion);
                }

                string logText = "PACKAGES STATUS : ";

                if (invalidPackages.Count == 0) logText += STR_VALID_LOG;
                else logText += STR_INVALID_LOG;

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
                    foreach (PackageManifestItem localPMI in localPackages)
                    {
                        logText += "\n Local Package : " + localPMI.packageName + " / primary version : " + localPMI.packageVersion; ;
                    }
                    foreach (PackageManifestItem missingPMI in missingPackages)
                    {
                        logText += "\n Missing Package : " + missingPMI.packageName + " / primary version : " + missingPMI.packageVersion; ;
                    }
                }
                return logText;
            }

            /// <summary>
            /// Compares the list of primary project files with local project files and logs the status of project settings.
            /// </summary>
            /// <param name="primaryProjectFiles">The list of primary package manifest items.</param>
            /// <param name="localProjectFiles">The list of local package manifest items.</param>
            /// <param name="isVerbose">Indicates whether the function should return verbose logging. Default value is false.</param>
            /// <returns>A string containing the log of the package comparison. 
            /// If there is no invalid Settings, this string is beginning with === OK === 
            /// If there is any invalid Settings, this string is beginning with === INVALID ===.</returns>
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

                string logText = "PROJECT SETTINGS STATUS : ";

                if (invalidSettings.Count == 0) logText += STR_VALID_LOG;
                else logText += STR_INVALID_LOG;

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
