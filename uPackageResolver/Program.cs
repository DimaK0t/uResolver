using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Net.Http;
using System.Xml.Linq;
using Fclp;
using Fclp.Internals.Extensions;
using uPackageResolver.Models;

namespace uPackageResolver
{
    internal class Program
    {
        private static readonly string _basePath = Environment.CurrentDirectory;
        private const string _appDataFolder = @"\App_Data\";

        private static void Main(string[] args)
        {
            var options = SetupArguments(args);
            if (options == null || options.Host.IsNullOrEmpty() || options.UserName.IsNullOrEmpty() || options.Password.IsNullOrEmpty())
            {
                return;
            }

            RestorePackages(options.Host, options.UserName, options.Password).Wait();
        }

        private static Args SetupArguments(string[] args)
        {
            var parser = new FluentCommandLineParser<Args>();
            parser.Setup(x => x.Host).As('h').Required().WithDescription("Host name of your site. Requaired");
            parser.Setup(x => x.UserName).As('u').Required().WithDescription("Umbraco`s user name. Requaired");
            parser.Setup(x => x.Password).As('p').Required().WithDescription("Umraco`s password.  Requaired");
            parser.SetupHelp("?", "help").UseForEmptyArgs().WithHeader("Example: uresolver -h site.com -u admin -p password").Callback(text => Console.WriteLine(text));

            var options = parser.Parse(args);
            if (options.HasErrors)
            {
                Console.WriteLine("Type \"-?\" or \"-help\" to get help");
                return null;
            }

            var result = parser.Object;
            return result;
        }

        private static async Task<HttpResponseMessage> Login(string host, string userName, string password, HttpClient client)
        {
            var authParams = new Dictionary<string, string>
                {
                    {"password", password},
                    {"username", userName}
                };

            var authUrl = string.Format("{0}/umbraco/backoffice/UmbracoApi/Authentication/PostLogin", host);
            var response = await client.PostAsync(authUrl, new FormUrlEncodedContent(authParams));
            if (response.StatusCode.Equals(HttpStatusCode.BadRequest))
            {
                throw new HttpRequestException("Cannot login to Umbaco. Recheck credentials.");
            }
            return response;
        }

        private static async Task<HttpResponseMessage> DownloadPackage(string host, PackageModel packageModel, HttpClient client)
        {
            var packagePath = Path.Combine(_basePath + _appDataFolder, packageModel.PackageGuid);
            var packageParams = new Dictionary<string, string>
                    {
                        {"body_tempFile", packagePath },
                    };
            var downloadPackageUrl = string.Format("{2}/umbraco/developer/packages/installer.aspx?repoGuid={0}&guid={1}", packageModel.RepoGuid, packageModel.PackageGuid, host);
            var response = await client.PostAsync(downloadPackageUrl, new FormUrlEncodedContent(packageParams));
            return response;
        }

        private static async Task RestorePackages(string host, string userName, string password)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    if (!host.StartsWith("http://"))
                    {
                        host = "http://" + host;
                    }

                    Console.WriteLine("Trying to log in");
                    var loginResp = await Login(host, userName, password, client);
                    Console.WriteLine("Respons.StatusCode: " + loginResp.StatusCode);
                    Console.WriteLine("Loged in");

                    var packages = GetPackages();
                    Console.WriteLine("Found {0} packages ", packages.Count());
                    foreach (var packageModel in packages)
                    {
                        // download package
                        var model = packageModel;
                        await DownloadPackage(host, packageModel, client).ContinueWith(async (task) =>
                        {
                            try
                            {
                                Console.WriteLine("Trying to download package " + model.PackageGuid);
                                var downloadResul = await task;
                                Console.WriteLine("Respons.StatusCode: " + downloadResul.StatusCode);
                                Console.WriteLine("Downloaded");

                                // copy files to final destinations
                                foreach (var file in GetFiles(model.PackageGuid))
                                {
                                    var soursePath = Path.Combine(_basePath + _appDataFolder, model.PackageGuid, file.FileGuid);
                                    PlaceFileToPackageDirectory(soursePath, file);
                                }
                            }
                            catch (Exception e)
                            {
                                HandleError(e);
                            }
                        });
                    }
                    Console.WriteLine("All packages have beeen restored");
                }
            }
            catch (Exception e)
            {
                HandleError(e);
            }
        }

        private static void HandleError(Exception exception)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(exception.Message);
            Console.WriteLine(exception.InnerException);
            Console.ForegroundColor = ConsoleColor.White;
            Environment.Exit(Environment.ExitCode);
        }

        private static void PlaceFileToPackageDirectory(string soursePath, FileModel file)
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
            var config = XDocument.Load(Path.Combine(_basePath + _appDataFolder, @"packages\installed\installedPackages.config"));
            var packages = config.Elements("packages").Elements("package").Select(x => new PackageModel
            {
                RepoGuid = x.Attribute("repositoryGuid").Value,
                PackageGuid = x.Attribute("packageGuid").Value
            }).ToList();

            return packages.Where(x => !string.IsNullOrEmpty(x.PackageGuid) && !string.IsNullOrEmpty(x.RepoGuid));
        }
    }
}