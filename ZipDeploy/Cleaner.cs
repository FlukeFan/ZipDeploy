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
            _logger.LogInformation("Deleting obsoleted files and directories");
            var obsoleteFileCount = 0;
            var obsoleteDirectoryCount = 0;

            foreach (var fullName in Directory.GetFiles(".", "*", SearchOption.AllDirectories).Select(f => _fsUtil.NormalisePath(f)))
                if (_fsUtil.IsForDelete(fullName))
                {
                    _fsUtil.DeleteFile(fullName);
                    obsoleteFileCount++;
                }

            foreach (var fullName in Directory.GetDirectories(".", "*", SearchOption.AllDirectories).Select(f => _fsUtil.NormalisePath(f)))
                if (_fsUtil.IsForDelete(fullName))
                {
                    _fsUtil.DeleteDirectory(fullName);
                    obsoleteDirectoryCount++;
                }

            _logger.LogInformation("Deleted {obsoleteFileCount} obsolete files and {obsoleteDirectoryCount} obsolete directories", obsoleteFileCount, obsoleteDirectoryCount);
            return Task.CompletedTask;
        }
    }
}
