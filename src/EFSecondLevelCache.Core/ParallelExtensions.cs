using System;
using System.Threading;

namespace EFSecondLevelCache.Core
{
    /// <summary>
    /// Reader writer locking utils
    /// </summary>
    public static class ParallelExtensions
    {
        /// <summary>
        /// Tries to enter the lock in read mode, with an optional integer time-out.
        /// </summary>
        public static void TryReadLocked(this ReaderWriterLockSlim readerWriterLock,
                                         Action action, int timeout = Timeout.Infinite)
        {
            if (!readerWriterLock.TryEnterReadLock(timeout))
            {
                throw new TimeoutException();
            }
            try
            {
                action();
            }
            finally
            {
                readerWriterLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Tries to enter the lock in write mode, with an optional time-out.
        /// </summary>
        public static void TryWriteLocked(this ReaderWriterLockSlim readerWriterLock,
                                          Action action, int timeout = Timeout.Infinite)
        {
            if (!readerWriterLock.TryEnterWriteLock(timeout))
            {
                throw new TimeoutException();
            }
            try
            {
                action();
            }
            finally
            {
                readerWriterLock.ExitWriteLock();
            }
        }
    }
}