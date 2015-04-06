using System;
using System.Management.Automation;

namespace uPackageResolver
{
    public class Args
    {
        public string Host { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        public string GetUsage()
        {
            return "Please read user manual!" + Environment.NewLine;
        }
    }
}
