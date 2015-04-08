using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using uPackageResolver.Models;

namespace uPackageResolver.Packages
{
    public class PackageManager
    {
        public void PlaceFileToPackageDirectory(string from, string to, string fileName)
        {
            if (!Directory.Exists(from))
            {
                Directory.CreateDirectory(from);
            }

            var destFile = Path.Combine(from, fileName);
            if (!File.Exists(destFile))
            {
                File.Copy(to, destFile);
            }
        }

        public IEnumerable<FileModel> GetPackageFiles(string packageManifestPath)
        {
            var manifest = XDocument.Load(packageManifestPath);
            var files = manifest.Root.Element("files").Elements("file");
            return files.Select(x => new FileModel()
            {
                FileGuid = x.Element("guid").Value,
                OrgName = x.Element("orgName").Value,
                OrgPath = x.Element("orgPath").Value
            });
        }

        public IEnumerable<PackageModel> GetInstalledPackages(string installedPackageConfigPath)
        {
            var config = XDocument.Load(installedPackageConfigPath);
            var packages = config.Elements("packages").Elements("package").Select(x => new PackageModel
            {
                RepoGuid = x.Attribute("repositoryGuid").Value,
                PackageGuid = x.Attribute("packageGuid").Value
            }).ToList();

            return packages.Where(x => !string.IsNullOrEmpty(x.PackageGuid) && !string.IsNullOrEmpty(x.RepoGuid));
        }
    }
}
