
<h1>ZipDeploy docs</h1>

When an ASP.NET <u>Core</u> site is running, it locks the assemblies that are in use.  This prevents the old ASP.NET style x-copy deployment.

ZipDeploy is a small library to allow you to zip up your publish folder and deploy it by FTP-ing the resulting zip up to a running site.  This can prevent errors like ERROR_FILE_IN_USE, or "locked by an external process":  <a href="https://github.com/aspnet/Home/issues/694" target="_blank">https://github.com/aspnet/Home/issues/694</a>

<ul>
    <li><a href="quickstart.html">Quickstart</a></li>
    <li><a href="walkthrough.html">Walkthrough</a></li>
</ul>
