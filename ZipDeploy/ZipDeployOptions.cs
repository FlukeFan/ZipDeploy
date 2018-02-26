using System.Collections.Generic;

namespace ZipDeploy
{
    public class ZipDeployOptions
    {
        private string          _iisUrl;
        private IList<string>   _pathsToIgnore = new List<string>();

        public string           IisUrl        => _iisUrl;
        public IList<string>    PathsToIgnore => _pathsToIgnore;

        /// <summary>Specify the IIS (not Kestrel) URL that is used to make a request to the application</summary>
        public ZipDeployOptions UseIisUrl(string iisUrl)
        {
            _iisUrl = iisUrl;
            return this;
        }

        /// <summary>Specify any paths to ignore (e.g., "log.txt", or "logs/", or "uploads\today")</summary>
        public ZipDeployOptions IgnorePathStarting(string path)
        {
            _pathsToIgnore.Add(path);
            return this;
        }
    }
}
