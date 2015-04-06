using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Xml.Linq;
using Fclp;
using uPackageResolver.Models;

namespace uPackageResolver
{
    internal class Program
    {
        public static string Host { get; set; }

        private static string _basePath = Environment.CurrentDirectory;
        private const string _appDataFolder = @"\App_Data\";

        private static void Main(string[] args)
        {
            var options = SetupArguments(args);
            if (options == null)
            {
                return;
            }

            try
            {
                DownloadPackages(options.Host, options.UserName, options.Password);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.InnerException);
                Console.ForegroundColor = ConsoleColor.White;
            }
            
        }

        private static Args SetupArguments(string[] args)
        {
            var parser = new FluentCommandLineParser<Args>();
            parser.Setup(x => x.Host).As('h').Required().WithDescription("Host name of your site. Requaired");
            parser.Setup(x => x.UserName).As('u').Required().WithDescription("Umbraco`s user name. Requaired");
            parser.Setup(x => x.UserName).As('p').Required().WithDescription("Umraco`s password.  Requaired");
            parser.SetupHelp("?").WithHeader("Example: uresolver -h site.com -u admin -p password").Callback( text => Console.WriteLine(text));

            var options = parser.Parse(args);
            if (options.HasErrors)
            {
                parser.HelpOption.ShowHelp(parser.Options);
                return null;
            }

            var result = parser.Object;
            return result;
        }

        private static async void DownloadPackages(string host, string userName, string password)
        {
            using (var client = new HttpClient())
            {
                var authParams = new Dictionary<string, string>
                {
                    {"password", password},
                    {"username", userName}
                };

                if (!host.StartsWith("http://"))
                {
                    host = "http://" + host;
                }
                var authUrl = string.Format("{0}/umbraco/backoffice/UmbracoApi/Authentication/PostLogin", host);

                await Post(client, authUrl, new FormUrlEncodedContent(authParams));

                foreach (var packageModel in GetPackages())
                {
                    // download package
                    var packagePath = Path.Combine(_basePath + _appDataFolder, packageModel.PackageGuid);
                    var packageParams = new Dictionary<string, string>
                    {
                        {"body_tempFile", packagePath },
                    };
                    var downloadPackageUrl = string.Format("{2}/umbraco/developer/packages/installer.aspx?repoGuid={0}&guid={1}", packageModel.RepoGuid, packageModel.PackageGuid, host);
                    await Post(client, downloadPackageUrl, new FormUrlEncodedContent(packageParams));

                    // copy files to final destinations
                    foreach (var file in GetFiles(packageModel.PackageGuid))
                    {
                        var soursePath = Path.Combine(_basePath + _appDataFolder, packageModel.PackageGuid, file.FileGuid);
                        PlaceFileToPackageDirectory(soursePath, file);
                    }
                }
            }
        }

        private static void PlaceFileToPackageDirectory(string soursePath,FileModel file)
        {
            var destDirectory = _basePath + file.OrgPath;
            if (!Directory.Exists(destDirectory))
            {
                Directory.CreateDirectory(destDirectory);
            }

            var destFile = Path.Combine(destDirectory, file.OrgName);
            if (!File.Exists(destFile))
            {
                File.Copy(soursePath, destFile);
            }
        }

        private static IEnumerable<FileModel> GetFiles(string packageGuid)
        {
            var manifest = XDocument.Load(Path.Combine(_basePath + _appDataFolder, packageGuid, @"package.xml"));
            var files = manifest.Root.Element("files").Elements("file");
            return files.Select(x => new FileModel()
            {
                FileGuid = x.Element("guid").Value,
                OrgName = x.Element("orgName").Value,
                OrgPath = x.Element("orgPath").Value
            });
        }

        private static IEnumerable<PackageModel> GetPackages()
        {
            var config = XDocument.Load(Path.Combine( _basePath + _appDataFolder,@"packages\installed\installedPackages.config"));
            var packages = config.Elements("packages").Elements("package").Select(x => new PackageModel
            {
                RepoGuid = x.Attribute("repositoryGuid").Value,
                PackageGuid = x.Attribute("packageGuid").Value
            }).ToList();

            return packages.Where(x => !string.IsNullOrEmpty(x.PackageGuid) && !string.IsNullOrEmpty(x.RepoGuid));
        }

        private static async Task Post(HttpClient client, string url, HttpContent content)
        {
            Console.WriteLine("Send request to: " + url);
            var response = await client.PostAsync(url, content);
            Console.WriteLine("Response: " + response.StatusCode);
            Console.WriteLine("***********");
        }
    }
}