# uResolver
Tool for automation resolving of Umbraco packages installed via Umbraco repository.
Note: tool does not support restoring of local packages(installed not from Umbraco repository)

Usage 
-----
1. Include '%project_folder%\App_Data\packages\installed\installedPackages.config' file to sourse control (for tracking packages from ohter developers in your team)
2. Open %project_folder% in cmd. Run `uresolver -h %host_name% -u %user_name% -p %password%`
3. Left cmd opened. The tool will restore all package from config and watch for changes in config
