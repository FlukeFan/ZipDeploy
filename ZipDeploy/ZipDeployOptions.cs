namespace ZipDeploy
{
    public class ZipDeployOptions
    {
        private string _restartUrl;

        public string RestartUrl => _restartUrl;

        /// <summary>Specify the IIS (not Kestrel) URL that is used to restart the application</summary>
        public ZipDeployOptions UseRestartUrl(string restartUrl)
        {
            _restartUrl = restartUrl;
            return this;
        }
    }
}
