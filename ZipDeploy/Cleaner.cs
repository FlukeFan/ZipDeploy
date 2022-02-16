using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ZipDeploy
{
    public interface ICleaner
    {
        Task DeleteObsoleteFilesAsync();
    }

    public class Cleaner : ICleaner
    {
        private ILogger<Cleaner> _logger;
        private FsUtil _fsUtil;

        public Cleaner(ILogger<Cleaner> logger)
        {
            _logger = logger;
            _fsUtil = new FsUtil(logger);
        }

        public virtual Task DeleteObsoleteFilesAsync()
        {
            _logger.LogInformation("Deleting obsoleted files");
            var obsoleteFileCount = 0;

            foreach (var fullName in Directory.GetFiles(".", "*", SearchOption.AllDirectories).Select(f => _fsUtil.NormalisePath(f)))
                if (_fsUtil.IsForDelete(fullName))
                {
                    _fsUtil.DeleteFile(fullName);
                    obsoleteFileCount++;
                }

            _logger.LogInformation("Deleted obsoleted files count={count}", obsoleteFileCount);
            return Task.CompletedTask;
        }
    }
}
