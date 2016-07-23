using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newegg.OZZO.RunnerProxy.Models;
using Newegg.OZZO.WMS.Infrastructure.Common;

namespace Newegg.OZZO.RunnerProxy.Helpers
{
    public static class RunnerCacheManager
    {
        // Fields
        private static string CacheDirectory;
        private static object CacheRunnerStateLocker;
        private static ConcurrentDictionary<string, object> CacheLockers;
        private static CacheRunnerProcessState CacheRunnerState;
        private static object SyncAllCacheFilesLocker;

        // Ctor Static
        static RunnerCacheManager()
        {
            CacheRunnerState = CacheRunnerProcessState.Stop;
            SyncAllCacheFilesLocker = new object();
            CacheRunnerStateLocker = new object();
            CacheLockers = new ConcurrentDictionary<string, object>();
            CacheDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RunnerLocalCache");
        }

        // Properties
        public static List<string> AllCacheFiles
        {
            get
            {
                if (Directory.Exists(RunnerCacheManager.CacheDirectory))
                {
                    return Directory.GetFiles(RunnerCacheManager.CacheDirectory).ToList();
                }
                else
                {
                    return new List<string>();
                }
            }
        }

        // Methods
        private static List<RunnerCache> InnerRead(string fileFullPath)
        {
            if (!File.Exists(fileFullPath))
            {
                return new List<RunnerCache>();
            }

            List<RunnerCache> models = null;
            using (var stream = new FileStream(fileFullPath, FileMode.Open))
            {
                if (stream.CanSeek)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                }

                using (var reader = new StreamReader(stream))
                {
                    models = ServiceStack.Text.JsonSerializer.DeserializeFromReader<List<RunnerCache>>(reader);
                }
            }

            return models ?? new List<RunnerCache>();
        }
        private static void InnerWrite(string fileFullPath, List<RunnerCache> caches)
        {
            using (var stream = new FileStream(fileFullPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(ServiceStack.Text.JsonSerializer.SerializeToString(caches));
                stream.Write(bytes, 0, bytes.Length);
            }
        }
        private static void ProcessCacheFiles(ref ConcurrentQueue<string> batchProcessingFilesQ, int workersForEachFile = 0, TimeSpan intervalOfWorkersForEachFile = default(TimeSpan))
        {
            if (batchProcessingFilesQ == null || batchProcessingFilesQ.Count == 0)
            {
                return;
            }
            else
            {
                while (!batchProcessingFilesQ.IsEmpty)
                {
                    string popFile = null;
                    if (batchProcessingFilesQ.TryDequeue(out popFile))
                    {
                        RunnerCacheManager.ProcessCacheFile(popFile, caches =>
                        {
                            if (caches.IsNullOrEmpty())
                            {
                                return null;
                            }

                            var cachesQ = new ConcurrentQueue<RunnerCache>(caches);
                            var success = new ConcurrentQueue<RunnerCache>();
                            var failed = new ConcurrentQueue<RunnerCache>();
                            if (workersForEachFile <= 0)
                            {
                                RunnerCacheManager.ProcessCacheFileDatas(ref cachesQ, ref success, ref failed);
                            }
                            else
                            {
                                var currentFileWorkTasks = new List<Task>();
                                for (int i = 0; i < workersForEachFile; i++)
                                {
                                    currentFileWorkTasks.Add(Task.Factory.StartNew(() =>
                                    {
                                        RunnerCacheManager.ProcessCacheFileDatas(ref cachesQ, ref success, ref failed);
                                    }));
                                }

                                Task.WaitAll(currentFileWorkTasks.ToArray());
                            }

                            return failed.ToList();
                        });
                    }

                    Thread.Sleep(intervalOfWorkersForEachFile);
                }
            }
        }
        private static void ProcessCacheFile(string fileFullPath, Func<List<RunnerCache>, List<RunnerCache>> processor)
        {
            if (processor == null || !File.Exists(fileFullPath))
            {
                return;
            }
            else
            {
                lock (CacheLockers.GetOrAdd(fileFullPath, new object())) // it must be lock the file when it is on a process flow (read+write).
                {
                    var stillFailedCache = processor(InnerRead(fileFullPath));
                    if (stillFailedCache == null || stillFailedCache.Count == 0)
                    {
                        File.Delete(fileFullPath);
                    }
                    else
                    {
                        RunnerCacheManager.InnerWrite(fileFullPath, stillFailedCache);
                    }
                }
            }
        }
        private static void ProcessCacheFileDatas(ref ConcurrentQueue<RunnerCache> cachesQ, ref ConcurrentQueue<RunnerCache> success, ref ConcurrentQueue<RunnerCache> failed)
        {
            if (cachesQ == null || cachesQ.Count == 0)
            {
                return;
            }
            else
            {
                while (!cachesQ.IsEmpty)
                {
                    RunnerCache popCache = null;
                    if (cachesQ.TryDequeue(out popCache))
                    {
                        if (RunnerCacheManager.ExecuteCache(ref popCache))
                        {
                            success.Enqueue(popCache);
                        }
                        else
                        {
                            failed.Enqueue(popCache);
                        }
                    }

                    Thread.Sleep(10);
                }
            }
        }
        private static bool ExecuteCache(ref RunnerCache cache)
        {
            try
            {
                if (cache == null || cache.MethodInfo == null || string.IsNullOrEmpty(cache.MethodInfo.MethodName) || cache.MethodInfo.Method == null)
                {
                    return false;
                }
                else
                {
                    var response = cache.MethodInfo.Method.Invoke(cache.MethodInfo.MethodOwnerInstance, !cache.MethodInfo.HasParameter ? null : new object[] { cache.MethodInfo.Parameter });
                    if (cache.RunnerOptionInfo != null && cache.RunnerOptionInfo.IsSuccessMethod != null)
                    {
                        if (cache.MethodInfo.Method.ReturnType == typeof(void))
                        {
                            return (bool)cache.RunnerOptionInfo.IsSuccessMethod.Invoke(cache.RunnerOptionInfo.IsSuccessMethodOwnerInstance, cache.MethodInfo.HasParameter ? new object[] { cache.MethodInfo.Parameter } : null);
                        }
                        else
                        {
                            return (bool)cache.RunnerOptionInfo.IsSuccessMethod.Invoke(cache.RunnerOptionInfo.IsSuccessMethodOwnerInstance, cache.MethodInfo.HasParameter ? new object[] { cache.MethodInfo.Parameter, response } : new object[] { response });
                        }
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                //Log.WriteException(e);
                return false;
            }
        }
        private static bool TryStartCacheProcess()
        {
            if (CacheRunnerState == CacheRunnerProcessState.Running)
            {
                return false;
            }
            else
            {
                lock (CacheRunnerStateLocker)
                {
                    if (CacheRunnerState == CacheRunnerProcessState.Running)
                    {
                        return false;
                    }
                    else
                    {
                        CacheRunnerState = CacheRunnerProcessState.Running;
                        return true;
                    }
                }
            }
        }

        // Exports
        public static void New(RunnerCache cache)
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                try
                {
                    var currentCacheFileName = string.Format("{0}.txt", DateTime.Now.ToString("yyyy-MM-dd"));
                    var currentCacheFileFullPath = Path.Combine(CacheDirectory, currentCacheFileName);
                    lock (CacheLockers.GetOrAdd(currentCacheFileFullPath, new object())) // only allow one thread read-write( it is a transaction operation ) the same file at the same time.
                    {
                        if (!Directory.Exists(CacheDirectory))
                        {
                            Directory.CreateDirectory(CacheDirectory);
                        }

                        var caches = RunnerCacheManager.InnerRead(currentCacheFileFullPath);
                        if (!caches.Any(m => m.Equals(o)))
                        {
                            caches.Add(o as RunnerCache);
                            RunnerCacheManager.InnerWrite(currentCacheFileFullPath, caches);
                        }
                    }

                    RunnerCacheManager.StartProcessCacheRunners(CacheProcessOption.Default); // auto start thread to process caches when first cache file create success.
                }
                catch (Exception e)
                {
                    Log.WriteException(e);
                }
            }, cache);
        }
        public static void New(Action action, RunnerOption option, Exception e = null)
        {
            New(new RunnerCache()
            {
                ID = Guid.NewGuid().ToString(),
                ExceptionMessage = e == null ? null : e.Message,
                MethodInfo = CacheMethodInfo.CreateFrom(action),
                RunnerOptionInfo = CacheRunnerOptionInfo.CreateFrom(action, option)
            });
        }
        public static void New<T>(Action<T> action, T request, RunnerOption<T> option, Exception e = null)
        {
            New(new RunnerCache()
            {
                ID = Guid.NewGuid().ToString(),
                ExceptionMessage = e == null ? null : e.Message,
                MethodInfo = CacheMethodInfo.CreateFrom(action, request),
                RunnerOptionInfo = CacheRunnerOptionInfo.CreateFrom(action, request, option)
            });
        }
        public static void New<T>(Func<T> func, T response, RunnerOption<T> option, Exception e = null)
        {
            New(new RunnerCache()
            {
                ID = Guid.NewGuid().ToString(),
                ExceptionMessage = e == null ? null : e.Message,
                MethodInfo = CacheMethodInfo.CreateFrom(func, response),
                RunnerOptionInfo = CacheRunnerOptionInfo.CreateFrom(func, response, option)
            });
        }
        public static void New<TRequest, TResponse>(Func<TRequest, TResponse> func, TRequest request, TResponse response, RunnerOption<TRequest, TResponse> option, Exception e = null)
        {
            New(new RunnerCache()
            {
                ID = Guid.NewGuid().ToString(),
                ExceptionMessage = e == null ? null : e.Message,
                MethodInfo = CacheMethodInfo.CreateFrom(func, request, response),
                RunnerOptionInfo = CacheRunnerOptionInfo.CreateFrom(func, request, response, option)
            });
        }
        public static void StartProcessCacheRunners(CacheProcessOption option)
        {
            if (RunnerCacheManager.TryStartCacheProcess())
            {
                var cacheFilesQ = new ConcurrentQueue<string>(RunnerCacheManager.AllCacheFiles);
                int workersForAllCacheFiles = option.WorkersForAllCacheFiles;
                workersForAllCacheFiles = workersForAllCacheFiles <= 0 ? 1 : workersForAllCacheFiles;
                for (int i = 0; i < workersForAllCacheFiles; i++)
                {
                    ThreadPool.QueueUserWorkItem(o =>
                    {
                        while (true)
                        {
                            try
                            {
                                if (cacheFilesQ.IsEmpty) // if any one thread found the Q is Empty, the thread must be flush the Q.
                                {
                                    lock (SyncAllCacheFilesLocker)
                                    {
                                        if (cacheFilesQ.IsEmpty)
                                        {
                                            cacheFilesQ = new ConcurrentQueue<string>(RunnerCacheManager.AllCacheFiles);
                                        }
                                    }
                                }
                                else
                                {
                                    RunnerCacheManager.ProcessCacheFiles(ref cacheFilesQ, option.WorkersForEachFile, option.IntervalOfWorkersForEachFile);
                                }

                                Thread.Sleep(option.IntervalOfWorkersForAllCacheFiles);
                            }
                            catch (Exception e)
                            {
                                Log.WriteException(e);
                                Thread.Sleep(TimeSpan.FromSeconds(option.IntervalOfWorkersForAllCacheFiles.TotalSeconds * 60));
                            }
                        }
                    }, null);
                }
            }
        }
    }

    public enum CacheRunnerProcessState
    {
        Stop,
        Running
    }
}
