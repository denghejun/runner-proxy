using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Newegg.OZZO.RunnerProxy.Models
{
    public abstract class RunnerOptionBase
    {
        public RunnerOptionBase()
        {
            this.RetryCount = 0;
            this.RetryInterval = TimeSpan.FromMilliseconds(1000);
            this.RetryOption = RetryOption.None;
        }
        public RetryOption RetryOption { get; set; }
        public int RetryCount { get; set; }
        public TimeSpan RetryInterval { get; set; }
    }

    public class RunnerOption : RunnerOptionBase
    {
        public Func<bool> IsSuccessVerifier { get; set; }
        public event EventHandler OnFailedAfterMaxRetryCount;
        public Func<RunnerMailOptionContext, RunnerMailOption> MailOptionConstructor { get; set; }
        public void RaiseOnFailedAfterMaxRetryCount()
        {
            if (this.OnFailedAfterMaxRetryCount != null)
            {
                this.OnFailedAfterMaxRetryCount(this, EventArgs.Empty);
            }
        }
        public static RunnerOption Default
        {
            get
            {
                return new RunnerOption()
                {
                    IsSuccessVerifier = () => { return true; }
                };
            }
        }
    }

    public class RunnerOption<T> : RunnerOptionBase
    {
        public Func<T, bool> IsSuccessVerifier { get; set; }
        public event EventHandler<EventArgs<T>> OnFailedAfterMaxRetryCount;
        public Func<RunnerMailOptionContext<T>, RunnerMailOption> MailOptionConstructor { get; set; }
        public void RaiseOnFailedAfterMaxRetryCount(T state)
        {
            if (this.OnFailedAfterMaxRetryCount != null)
            {
                this.OnFailedAfterMaxRetryCount(this, new EventArgs<T>() { State = state });
            }
        }
        public static RunnerOption<T> Default
        {
            get
            {
                return new RunnerOption<T>()
                {
                    IsSuccessVerifier = param => { return true; }
                };
            }
        }
    }

    public class RunnerOption<TRequest, TResponse> : RunnerOptionBase
    {
        public Func<TRequest, TResponse, bool> IsSuccessVerifier { get; set; }
        public event EventHandler<EventArgs<TRequest, TResponse>> OnFailedAfterMaxRetryCount;
        public Func<RunnerMailOptionContext<TRequest, TResponse>, RunnerMailOption> MailOptionConstructor { get; set; }
        public void RaiseOnFailedAfterMaxRetryCount(TRequest request, TResponse response)
        {
            if (this.OnFailedAfterMaxRetryCount != null)
            {
                this.OnFailedAfterMaxRetryCount(this, new EventArgs<TRequest, TResponse>() { Request = request, Response = response });
            }
        }

        public static RunnerOption<TRequest, TResponse> Default
        {
            get
            {
                return new RunnerOption<TRequest, TResponse>()
                {
                    IsSuccessVerifier = (request, response) => { return true; }
                };
            }
        }
    }

    public class EventArgs<T> : EventArgs
    {
        public T State { get; set; }
    }

    public class EventArgs<TRequest, TResponse> : EventArgs
    {
        public TRequest Request { get; set; }
        public TResponse Response { get; set; }
    }

    public enum RetryOption
    {
        None,
        BaseOnRetryCounts,
        LocalCacheRetryAfterMaxRetryCount
    }
}
