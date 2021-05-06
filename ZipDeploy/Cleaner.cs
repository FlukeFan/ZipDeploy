using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace ZipDeploy
{
    public interface ICleaner
    {
        void DeleteObsoleteFiles();
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

        public virtual void DeleteObsoleteFiles()
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
        }
    }
}
