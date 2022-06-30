using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace StudioManette.minus
{
    public static class Synchronization
    {
        /*
         * 
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

                        packageList.Add(new PackageManifestItem(package, version));
                    }
                }
            }

            return packageList;
        }
    }
}
