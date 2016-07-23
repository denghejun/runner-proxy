using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newegg.OZZO.RunnerProxy.Helpers;
using Newegg.OZZO.RunnerProxy.Models;

namespace Newegg.OZZO.RunnerProxy.Core
{
    public class RetryRunner : InnerRunner
    {
        public const string MESSAGE_NORMAL_RETRYFAILED_AFTERMAXCOUNT = "This function not throw exception, but the response was still not expected after retry {0} times.";

        public override void Run(Action action, RunnerOption option)
        {
            try
            {
                int retryCount = option.RetryCount;
                RetryRunner.Retry(ref retryCount, option.RetryInterval, action, option);
            }
            catch (Exception e)
            {
                if (option.MailOptionConstructor != null)
                {
                    RunnerMailSender.Send(option.MailOptionConstructor(new RunnerMailOptionContext(e, action)));
                }

                throw;
            }
        }

        public override void Run<T>(Action<T> action, T request, RunnerOption<T> option)
        {
            try
            {
                int retryCount = option.RetryCount;
                RetryRunner.Retry(ref retryCount, option.RetryInterval, action, request, option);
            }
            catch (Exception e)
            {
                if (option.MailOptionConstructor != null)
                {
                    RunnerMailSender.Send(option.MailOptionConstructor(new RunnerMailOptionContext<T>(request, true, e, action)));
                }

                throw;
            }
        }

        public override T Run<T>(Func<T> func, RunnerOption<T> option)
        {
            var response = default(T);
            try
            {
                int retryCount = option.RetryCount;
                response = RetryRunner.Retry(ref retryCount, option.RetryInterval, func, option);
                return response;
            }
            catch (Exception e)
            {
                if (option.MailOptionConstructor != null)
                {
                    RunnerMailSender.Send(option.MailOptionConstructor(new RunnerMailOptionContext<T>(response, false, e, func)));
                }

                throw;
            }
        }

        public override TResponse Run<TRequest, TResponse>(Func<TRequest, TResponse> func, TRequest request, RunnerOption<TRequest, TResponse> option)
        {
            var response = default(TResponse);
            try
            {
                int retryCount = option.RetryCount;
                response = RetryRunner.Retry(ref retryCount, option.RetryInterval, func, request, option);
                return response;
            }
            catch (Exception e)
            {
                if (option.MailOptionConstructor != null)
                {
                    RunnerMailSender.Send(option.MailOptionConstructor(new RunnerMailOptionContext<TRequest, TResponse>(request, response, e, func)));
                }

                throw;
            }
        }

        private static void Retry(ref int retryCounts, TimeSpan interval, Action action, RunnerOption option)
        {
            try
            {
                action();
                if (option.IsSuccessVerifier == null || option.IsSuccessVerifier())
                {
                    return;
                }
                else if (retryCounts > 0)
                {
                    Thread.Sleep(interval);
                    --retryCounts;
                    Retry(ref retryCounts, interval, action, option);
                }
                else
                {
                    if (option.MailOptionConstructor != null)
                    {
                        RunnerMailSender.Send(option.MailOptionConstructor(new RunnerMailOptionContext(MaxRetryCountFailedException(option.RetryCount), action)));
                    }

                    option.RaiseOnFailedAfterMaxRetryCount();
                }
            }
            catch (Exception)
            {
                if (retryCounts > 0)
                {
                    Thread.Sleep(interval);
                    --retryCounts;
                    Retry(ref retryCounts, interval, action, option);
                }
                else
                {
                    throw;
                }
            }
        }

        private static void Retry<T>(ref int retryCounts, TimeSpan interval, Action<T> action, T request, RunnerOption<T> option)
        {
            try
            {
                action(request);
                if (option.IsSuccessVerifier == null || option.IsSuccessVerifier(request))
                {
                    return;
                }
                else if (retryCounts > 0)
                {
                    Thread.Sleep(interval);
                    --retryCounts;
                    Retry(ref retryCounts, interval, action, request, option);
                }
                else
                {
                    if (option.MailOptionConstructor != null)
                    {
                        RunnerMailSender.Send(option.MailOptionConstructor(new RunnerMailOptionContext<T>(request, true, MaxRetryCountFailedException(option.RetryCount), action)));
                    }

                    option.RaiseOnFailedAfterMaxRetryCount(request);
                }
            }
            catch (Exception)
            {
                if (retryCounts > 0)
                {
                    Thread.Sleep(interval);
                    --retryCounts;
                    Retry(ref retryCounts, interval, action, request, option);
                }
                else
                {
                    throw;
                }
            }
        }

        private static T Retry<T>(ref int retryCounts, TimeSpan interval, Func<T> func, RunnerOption<T> option)
        {
            var response = default(T);

            try
            {
                response = func();
                if (option.IsSuccessVerifier == null || option.IsSuccessVerifier(response))
                {
                    return response;
                }
                else if (retryCounts > 0)
                {
                    Thread.Sleep(interval);
                    --retryCounts;
                    return Retry(ref retryCounts, interval, func, option);
                }
                else
                {
                    if (option.MailOptionConstructor != null)
                    {
                        RunnerMailSender.Send(option.MailOptionConstructor(new RunnerMailOptionContext<T>(response, false, MaxRetryCountFailedException(option.RetryCount), func)));
                    }

                    option.RaiseOnFailedAfterMaxRetryCount(response);
                    return response;
                }
            }
            catch (Exception)
            {
                if (retryCounts > 0)
                {
                    Thread.Sleep(interval);
                    --retryCounts;
                    return Retry(ref retryCounts, interval, func, option);
                }
                else
                {
                    throw;
                }
            }
        }

        private static TResponse Retry<TRequest, TResponse>(ref int retryCounts, TimeSpan interval, Func<TRequest, TResponse> func, TRequest request, RunnerOption<TRequest, TResponse> option)
        {
            var response = default(TResponse);

            try
            {
                response = func(request);
                if (option.IsSuccessVerifier == null || option.IsSuccessVerifier(request, response))
                {
                    return response;
                }
                else if (retryCounts > 0)
                {
                    Thread.Sleep(interval);
                    --retryCounts;
                    return Retry(ref retryCounts, interval, func, request, option);
                }
                else
                {
                    if (option.MailOptionConstructor != null)
                    {
                        RunnerMailSender.Send(option.MailOptionConstructor(new RunnerMailOptionContext<TRequest, TResponse>(request, response, MaxRetryCountFailedException(option.RetryCount), func)));
                    }

                    option.RaiseOnFailedAfterMaxRetryCount(request, response);
                    return response;
                }
            }
            catch (Exception)
            {
                if (retryCounts > 0)
                {
                    Thread.Sleep(interval);
                    --retryCounts;
                    return Retry(ref retryCounts, interval, func, request, option);
                }
                else
                {
                    throw;
                }
            }
        }

        public static Exception MaxRetryCountFailedException(int maxRetryCount)
        {
            return new Exception(string.Format(MESSAGE_NORMAL_RETRYFAILED_AFTERMAXCOUNT, maxRetryCount));
        }
    }
}
