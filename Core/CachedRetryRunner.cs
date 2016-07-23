using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newegg.OZZO.RunnerProxy.Helpers;
using Newegg.OZZO.RunnerProxy.Models;

namespace Newegg.OZZO.RunnerProxy.Core
{
    public class CachedRetryRunner : RetryRunner
    {
        public override void Run(Action action, RunnerOption option)
        {
            option.OnFailedAfterMaxRetryCount += (s, e) =>
            {
                RunnerCacheManager.New(action, option, RetryRunner.MaxRetryCountFailedException(option.RetryCount));
            };

            try
            {
                base.Run(action, option);
            }
            catch (Exception e)
            {
                RunnerCacheManager.New(action, option, e);
                throw;
            }
        }

        public override void Run<T>(Action<T> action, T request, RunnerOption<T> option)
        {
            option.OnFailedAfterMaxRetryCount += (s, e) =>
            {
                RunnerCacheManager.New(action, request, option, RetryRunner.MaxRetryCountFailedException(option.RetryCount));
            };

            try
            {
                base.Run(action, request, option);
            }
            catch (Exception e)
            {
                RunnerCacheManager.New(action, request, option, e);
                throw;
            }
        }

        public override T Run<T>(Func<T> func, RunnerOption<T> option)
        {
            var response = default(T);
            option.OnFailedAfterMaxRetryCount += (s, e) =>
            {
                RunnerCacheManager.New(func, response, option, RetryRunner.MaxRetryCountFailedException(option.RetryCount));
            };

            try
            {
                response = base.Run(func, option);
                return response;
            }
            catch (Exception e)
            {
                RunnerCacheManager.New(func, response, option, e);
                throw;
            }
        }

        public override TResponse Run<TRequest, TResponse>(Func<TRequest, TResponse> func, TRequest request, RunnerOption<TRequest, TResponse> option)
        {
            var response = default(TResponse);
            option.OnFailedAfterMaxRetryCount += (s, e) =>
            {
                RunnerCacheManager.New(func, request, response, option, RetryRunner.MaxRetryCountFailedException(option.RetryCount));
            };

            try
            {
                response = base.Run(func, request, option);
                return response;
            }
            catch (Exception e)
            {
                RunnerCacheManager.New(func, request, response, option, e);
                throw;
            }
        }
    }
}
