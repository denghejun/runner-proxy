using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newegg.OZZO.RunnerProxy.Helpers;
using Newegg.OZZO.RunnerProxy.Models;

namespace Newegg.OZZO.RunnerProxy.Core
{
    public class NormalRunner : InnerRunner
    {
        public override void Run(Action action, RunnerOption option)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                if (option.MailOptionConstructor != null)
                {
                    RunnerMailSender.Send(option.MailOptionConstructor(new RunnerMailOptionContext(e,action)));
                }

                throw;
            }
        }

        public override void Run<T>(Action<T> action, T request, RunnerOption<T> option)
        {
            try
            {
                action(request);
            }
            catch (Exception e)
            {
                if (option.MailOptionConstructor != null)
                {
                    RunnerMailSender.Send(option.MailOptionConstructor(new RunnerMailOptionContext<T>(request, true, e,action)));
                }

                throw;
            }
        }

        public override T Run<T>(Func<T> func, RunnerOption<T> option)
        {
            var response = default(T);
            try
            {
                response = func();
                return response;
            }
            catch (Exception e)
            {
                if (option.MailOptionConstructor != null)
                {
                    RunnerMailSender.Send(option.MailOptionConstructor(new RunnerMailOptionContext<T>(response, false, e,func)));
                }

                throw;
            }
        }

        public override TResponse Run<TRequest, TResponse>(Func<TRequest, TResponse> func, TRequest request, RunnerOption<TRequest, TResponse> option)
        {
            var response = default(TResponse);
            try
            {
                response = func(request);
                return response;
            }
            catch (Exception e)
            {
                if (option.MailOptionConstructor != null)
                {
                    RunnerMailSender.Send(option.MailOptionConstructor(new RunnerMailOptionContext<TRequest, TResponse>(request, response, e,func)));
                }

                throw;
            }
        }
    }
}
