<h1>Quickstart</h1>

Execute the following:

    dotnet new mvc
    dotnet add package ZipDeploy

Open `Startup.cs`, and add:

    using ZipDeploy;

    ...

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseZipDeploy(options => options
                .UseIisUrl("http://url.to.open.app/through/iis"));

Now you can do:

    dotnet publish

... zip up the generated `publish` folder, and FTP it up to the root of your running site.

A more detailed walkthrough is covered here:  <a href="walkthrough.html">Walkthrough</a>
