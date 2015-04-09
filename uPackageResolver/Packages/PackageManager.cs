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
        public async Task<HttpResponseMessage> DownloadPackageAsync(string packagePath, string host, PackageModel packageModel, HttpClient client)
        {
            var packageParams = new Dictionary<string, string>
            {
                {"body_tempFile", packagePath},
            };
            var downloadPackageUrl = string.Format(
                "{2}/umbraco/developer/packages/installer.aspx?repoGuid={0}&guid={1}", packageModel.RepoGuid,
                packageModel.PackageGuid, host);
            var response = await client.PostAsync(downloadPackageUrl, new FormUrlEncodedContent(packageParams));
            return response;
        }

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
