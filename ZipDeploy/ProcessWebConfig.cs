using System.Threading.Tasks;

namespace ZipDeploy
{
    public interface IProcessWebConfig
    {
        Task<byte[]> ProcessAsync(byte[] zippedConfig);
    }

    public class ProcessWebConfig : IProcessWebConfig
    {
        public virtual Task<byte[]> ProcessAsync(byte[] zippedConfig)
        {
            return Task.FromResult(zippedConfig);
        }
    }
}
