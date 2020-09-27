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
        private ZipDeployOptions _options;
        private FsUtil _fsUtil;

        public Cleaner(ILogger<Cleaner> logger, ZipDeployOptions options)
        {
            _logger = logger;
            _options = options;
            _fsUtil = new FsUtil(logger);
        }

        public void DeleteObsoleteFiles()
        {
            foreach (var fullName in Directory.GetFiles(".", "*", SearchOption.AllDirectories).Select(f => _fsUtil.NormalisePath(f)))
                if (Path.GetFileName(fullName).StartsWith("zzz_") && fullName.EndsWith(".fordelete.txt"))
                    _fsUtil.DeleteFile(fullName);
        }

    }
}
