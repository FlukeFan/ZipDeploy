<h1>Walkthrough</h1>

This walkthrough will talk you through getting an example of ZipDeploy up and running on your machine using IIS, 7za.exe, and FTP.

For this walkthrough, you will need:
* IIS
* FTP Server
* .NET Core Windows Server Hosting bundle
* `7za.exe`
* `ncftpput.exe`

IIS and FTP server can be enabled in Windows from the "Turn Windows features on or off" dialog.  ".NET Core Windows Server Hosting bundle" needs to be downloaded and installed to add the AspNetCoreModule to IIS.
`7za.exe` and `ncftpput.exe` are freely available downloads; the walkthrough assumes you have them in your PATH.

Checkout your ASP.NET Core MVC application to `C:\Temp\MyApp`, and add the ZipDeploy package:

    CD C:\Temp\MyApp
    dotnet add package ZipDeploy

Open `Startup.cs`, and add:

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseZipDeploy(options => options
                .UseIisUrl("http://localhost:8123"));

Package the application:

    dotnet publish
    move bin\Debug\netcoreapp2.0\publish ..\MySite

Modify the security of the `C:\Temp\MySite` folder to add Everyone with Full Control (purely for demo purposes).

In IIS Manager, add a website with 'Site name' MySite, 'Physical Path' `C:\Temp\MySite`, and 'port' 8123.  Browse to the URL `http://localhost:8123/` to confirm your site is up and running.

In IIS Manager, add an FTP site with 'FTP site name' MyFtpSite and 'Physical path' `C:\Temp\MySite`, select no SSL, and enable anoymous authentication.  In the FTP site Authorization Rules, allow Anonymous Users read and write permissions.  Verify the FTP site is working using `ftp localhost` from a command prompt, logging in with username `anonymous` and a blank password, and typing `ls` to see the list of files in the website.

Make a change to your code that you wish to see reflected after deployment, and package the application again:

    CD C:\Temp\MyApp
    dotnet publish

Re-verify your site is running in the browser (and verify your changes are not present yet).  Now zip the contents of the pubish directory, and FTP the resulting zip into the root of the site.

    7za.exe a publish.zip .\bin\Debug\netcoreapp2.0\publish\*
    ncftpput.exe -S .tmp localhost . publish.zip

Note two things:
* The zip file should be rooted at the same point as the site (note the relative folder and trailing backslash to `7za.exe`).  For example, the `web.config` should be directly inside the zip file, not inside a folder inside the zip file;
* The upload should use a temporary filename until the upload is complete.  The option `-S .tmp` means the file is uploaded as `publish.zip.tmp`, then renamed to `publish.zip` once the upload is complete.

ZipDeploy should detect the presence of the zip file.  It will rename the current binaries (which is allowed even when they are in use), and unzip the new ones.

ZipDeploy then updates the web.config, which makes IIS recycle the ASP.NET Core process.

The next request to IIS will start the new ASP.NET Core process, at which point ZipDeploy will delete the renamed binaries, and unzip any remaining content.

Refresh the browser to verify the changes have been reflected, and notice that ZipDeploy has renamed the zip file to `deployed.zip`.
