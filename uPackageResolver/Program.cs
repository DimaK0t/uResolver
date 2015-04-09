using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using Fclp;
using Fclp.Internals.Extensions;
using uPackageResolver.Login;
using uPackageResolver.Packages;

namespace uPackageResolver
{
    internal class Program
    {
        private static readonly AuthManager _authManager = new AuthManager();
        private static readonly PackageManager _packageManager = new PackageManager();
        private static string _basePath = Environment.CurrentDirectory;
        private static string _appDataFolder = Environment.CurrentDirectory + @"\App_Data\";

        private static void Main(string[] args)
        {
            var options = SetupArguments(args);
            if (options == null || options.Host.IsNullOrEmpty() || options.UserName.IsNullOrEmpty() ||
                options.Password.IsNullOrEmpty())
            {
                return;
            }

            RestorePackages(options.Host, options.UserName, options.Password).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    HandleError(task.Exception);
                }
            }).Wait();
        }

        private static async Task RestorePackages(string host, string userName, string password)
        {
            using (var client = new HttpClient())
            {
                if (!host.StartsWith("http://"))
                {
                    host = "http://" + host;
                }

                Console.WriteLine("Trying to log in");
                var loginResp = await _authManager.LoginAsync(host, userName, password, client);
                Console.WriteLine("Respons.StatusCode: " + loginResp.StatusCode);
                Console.WriteLine("Loged in");

                var installedPackagesConfigPath = Path.Combine(_appDataFolder,
                    @"packages\installed\installedPackages.config");
                var packages = _packageManager.GetInstalledPackages(installedPackagesConfigPath).ToList();
                Console.WriteLine("Found {0} packages ", packages.Count());
                foreach (var packageModel in packages)
                {
                    // download package
                    var model = packageModel;
                    var packagePath = Path.Combine(_basePath + _appDataFolder, packageModel.PackageGuid);
                    Console.WriteLine("Trying to download package " + model.PackageGuid);
                    var downloadResul = await _packageManager.DownloadPackageAsync(packagePath, host, packageModel, client); ;
                    Console.WriteLine("Respons.StatusCode: " + downloadResul.StatusCode);
                    Console.WriteLine("Downloaded");

                    // copy files to final destinations
                    var manifestPath = Path.Combine(_appDataFolder, model.PackageGuid, @"package.xml");
                    foreach (var file in _packageManager.GetPackageFiles(manifestPath))
                    {
                        var from = _basePath + file.OrgPath;
                        var to = Path.Combine(_appDataFolder, model.PackageGuid,
                            file.FileGuid);
                        _packageManager.PlaceFileToPackageDirectory(from, to, file.OrgName);
                    }
                }

                Console.WriteLine("All packages have beeen restored");
            }
        }

        private static Args SetupArguments(string[] args)
        {
            var parser = new FluentCommandLineParser<Args>();
            parser.Setup(x => x.Host).As('h').Required().WithDescription("Host name of your site. Requaired");
            parser.Setup(x => x.UserName).As('u').Required().WithDescription("Umbraco`s user name. Requaired");
            parser.Setup(x => x.Password).As('p').Required().WithDescription("Umraco`s password.  Requaired");
            parser.SetupHelp("?", "help")
                .UseForEmptyArgs()
                .WithHeader("Example: uresolver -h site.com -u admin -p password")
                .Callback(text => Console.WriteLine(text));

            var options = parser.Parse(args);
            if (options.HasErrors)
            {
                Console.WriteLine("Type \"-?\" or \"-help\" to get help");
                return null;
            }

            var result = parser.Object;
            return result;
        }

        private static void HandleError(Exception exception)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(exception.Message);
            Console.WriteLine(exception.InnerException);
            Console.ForegroundColor = ConsoleColor.White;
            
            //quit on error
            Environment.Exit(Environment.ExitCode);
        }
    }
}