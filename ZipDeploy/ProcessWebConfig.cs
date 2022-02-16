namespace ZipDeploy
{
    public interface IProcessWebConfig
    {
        byte[] Process(byte[] zippedConfig);
    }

    public class ProcessWebConfig : IProcessWebConfig
    {
        public virtual byte[] Process(byte[] zippedConfig)
        {
            return zippedConfig;
        }
    }
}
