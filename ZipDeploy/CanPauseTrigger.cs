using System.Threading;

namespace ZipDeploy
{
    public interface ICanPauseTrigger
    {
        void Release(SemaphoreSlim semaphore);
    }

    public class CanPauseTrigger : ICanPauseTrigger
    {
        public virtual void Release(SemaphoreSlim semaphore)
        {
            semaphore.Release();
        }
    }
}
