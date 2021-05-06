namespace ZipDeploy
{
    public interface IProcessWebConfig
    {
        string Process(string zippedConfig);
    }

    public class ProcessWebConfig : IProcessWebConfig
    {
        public virtual string Process(string zippedConfig)
        {
            return zippedConfig;
        }
    }
}
