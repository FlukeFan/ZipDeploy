namespace ZipDeploy
{
    public class ZipDeployOptions
    {
        private string _iisUrl;

        public string IisUrl => _iisUrl;

        /// <summary>Specify the IIS (not Kestrel) URL that is used to make a request to the application</summary>
        public ZipDeployOptions UseIisUrl(string iisUrl)
        {
            _iisUrl = iisUrl;
            return this;
        }
    }
}
