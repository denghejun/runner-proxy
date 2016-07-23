using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newegg.OZZO.RunnerProxy.Models;

namespace Newegg.OZZO.RunnerProxy.Helpers
{
    public static class RunnerMailSender
    {
        public static bool Send(RunnerMailOption option)
        {
            if (option == null)
            {
                return false;
            }

            if (option.Mailer == null)
            {
                throw new Exception("you must provide implementation for IRunnerMailler before you send email.");
            }

            return option.Mailer.Send(option.Content);
        }
    }

    public interface IRunnerMailer
    {
        bool Send(RunnerMailContent content);
    }
}
