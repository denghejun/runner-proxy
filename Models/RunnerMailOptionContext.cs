using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Newegg.OZZO.RunnerProxy.Models
{
    public class RunnerMailOptionContext
    {
        private RunnerMailOption _basicMailOption;
        public RunnerMailOptionContext(Exception e, Delegate Method)
        {
            this.Exception = e;
            this.MethodInfo = Method;
        }

        public Delegate MethodInfo { get; private set; }
        public Exception Exception { get; private set; }
        public RunnerMailOption BasicMailOption
        {
            get
            {
                return this._basicMailOption ?? (this._basicMailOption = new RunnerMailOption() { Content = this.ResolveRunnerMailContent() });
            }
            private set
            {
                this._basicMailOption = value;
            }
        }
        protected virtual RunnerMailContent ResolveRunnerMailContent()
        {
            var exception = this.Exception == null ? "N/A" : this.Exception.Message;
            return new RunnerMailContent()
            {
                Subject = RunnerMailContentFormat.MAIL_SUBJECT,
                Body = string.Format(RunnerMailContentFormat.MAIL_BODY_FORMAT, this.MethodInfo.Method.DeclaringType.AssemblyQualifiedName, this.MethodInfo.Method.Name, "N/A", "N/A", exception),
            };
        }
    }

    public class RunnerMailOptionContext<T> : RunnerMailOptionContext
    {
        public RunnerMailOptionContext(T state, bool stateIsRequest, Exception e, Delegate Method)
            : base(e, Method)
        {
            this.State = state;
            this.StateIsRequest = stateIsRequest;
        }

        public T State { get; private set; }
        public bool StateIsRequest { get; private set; }
        protected override RunnerMailContent ResolveRunnerMailContent()
        {
            var stateSerialized = this.State != null ? ServiceStack.Text.JsonSerializer.SerializeToString(State) : "null";
            var request = this.StateIsRequest ? stateSerialized : "N/A";
            var response = !this.StateIsRequest ? stateSerialized : "N/A";
            var exception = this.Exception == null ? "N/A" : this.Exception.Message;
            return new RunnerMailContent()
            {
                Subject = RunnerMailContentFormat.MAIL_SUBJECT,
                Body = string.Format(RunnerMailContentFormat.MAIL_BODY_FORMAT, this.MethodInfo.Method.DeclaringType.AssemblyQualifiedName, this.MethodInfo.Method.Name, request, response, exception),
            };
        }
    }

    public class RunnerMailOptionContext<TRequest, TResponse> : RunnerMailOptionContext
    {
        public RunnerMailOptionContext(TRequest request, TResponse response, Exception e, Delegate Method)
            : base(e, Method)
        {
            this.Request = request;
            this.Response = response;
        }
        public TRequest Request { get; private set; }
        public TResponse Response { get; private set; }
        protected override RunnerMailContent ResolveRunnerMailContent()
        {
            var request = this.Request != null ? ServiceStack.Text.JsonSerializer.SerializeToString(this.Request) : "null";
            var response = this.Response != null ? ServiceStack.Text.JsonSerializer.SerializeToString(this.Response) : "null";
            var exception = this.Exception == null ? "N/A" : this.Exception.Message;
            return new RunnerMailContent()
            {
                Subject = RunnerMailContentFormat.MAIL_SUBJECT,
                Body = string.Format(RunnerMailContentFormat.MAIL_BODY_FORMAT, this.MethodInfo.Method.DeclaringType.AssemblyQualifiedName, this.MethodInfo.Method.Name, request, response, exception),
            };
        }
    }

    public static class RunnerMailContentFormat
    {
        public static readonly string MAIL_SUBJECT = "IR runner monitor report.";
        public static readonly string MAIL_BODY_FORMAT = @"
                                                                               <h3>Runner Monitor Execute Context</h3>
                                                                               <h5>Method Info</h5>  
                                                                                        <p style='font-size:12'><span style='color:purple'>DeclaringType:</span> {0}</p>
                                                                                        <p style='font-size:12'><span style='color:purple'>MethodName:</span> {1}</p>
                                                                               <h5>Request</h5>  <font style='font-size:12'>{2}</font>
                                                                               <h5>Response</h5>  <font style='font-size:12'>{3}</font>
                                                                               <h5>Exception</h5>  <font style='color:red;font-size:12'>{4}</font>
                                                                               <p style='color:gray'>This Email From Newegg IR Team Super Runner Monitor.</p>";
    }
}
