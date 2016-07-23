using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newegg.OZZO.WMS.Infrastructure.Common;

namespace Newegg.OZZO.RunnerProxy.Models
{
    public class RunnerCache
    {
        public string ID { get; set; }
        public string ExceptionMessage { get; set; }
        public CacheMethodInfo MethodInfo { get; set; }
        public CacheRunnerOptionInfo RunnerOptionInfo { get; set; }
        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is RunnerCache))
            {
                return false;
            }
            else
            {
                var cache = obj as RunnerCache;
                this.MethodInfo = this.MethodInfo ?? new CacheMethodInfo();
                this.RunnerOptionInfo = this.RunnerOptionInfo ?? new CacheRunnerOptionInfo();
                cache.MethodInfo = cache.MethodInfo ?? new CacheMethodInfo();
                cache.RunnerOptionInfo = cache.RunnerOptionInfo ?? new CacheRunnerOptionInfo();

                return cache.MethodInfo.MethodName.AreEqual(this.MethodInfo.MethodName)
                    && cache.MethodInfo.MethodOwnerType.AreEqual(this.MethodInfo.MethodOwnerType)
                    && cache.MethodInfo.MethodParameter.AreEqual(this.MethodInfo.MethodParameter)
                    && cache.RunnerOptionInfo.IsSuccessMethodName.AreEqual(this.RunnerOptionInfo.IsSuccessMethodName)
                    && cache.RunnerOptionInfo.IsSuccessMethodOwnerType.AreEqual(this.RunnerOptionInfo.IsSuccessMethodOwnerType)
                    && cache.RunnerOptionInfo.IsSuccessMethodRequestParameter.AreEqual(this.RunnerOptionInfo.IsSuccessMethodRequestParameter);
            }
        }
    }

    public class CacheMethodInfo
    {
        public string MethodOwnerType { get; set; }

        public string MethodName { get; set; }

        public string MethodParameter { get; set; }

        [IgnoreDataMember]
        public MethodInfo Method
        {
            get
            {
                Regex r = new Regex("([+]{1}.*?,)");
                var anonymousOwnerType = r.Matches(this.MethodOwnerType).Count > 0 ? r.Matches(this.MethodOwnerType)[0].Value : string.Empty;
                var methodOwnerType = Type.GetType(this.MethodOwnerType) ?? Type.GetType(this.MethodOwnerType.Replace(anonymousOwnerType, ","));
                if (methodOwnerType != null)
                {
                    return methodOwnerType.GetMethod(this.MethodName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
                }
                else
                {
                    return null;
                }
            }
        }

        [IgnoreDataMember]
        public object MethodOwnerInstance
        {
            get
            {
                if (this.Method == null)
                {
                    return null;
                }
                else
                {
                    Regex r = new Regex("([+]{1}.*?,)");
                    var anonymousOwnerType = r.Matches(this.MethodOwnerType).Count > 0 ? r.Matches(this.MethodOwnerType)[0].Value : string.Empty;
                    var methodOwnerType = Type.GetType(this.MethodOwnerType) ?? Type.GetType(this.MethodOwnerType.Replace(anonymousOwnerType, ","));
                    return this.Method.IsStatic ? null : Activator.CreateInstance(methodOwnerType);
                }
            }
        }

        [IgnoreDataMember]
        public object Parameter
        {
            get
            {
                if (this.Method == null)
                {
                    return null;
                }
                else
                {
                    var parameters = this.Method.GetParameters();
                    var param = (parameters == null || parameters.Count() == 0) ? null : ServiceStack.Text.JsonSerializer.DeserializeFromString(this.MethodParameter, parameters[0].ParameterType);
                    return param;
                }
            }
        }

        [IgnoreDataMember]
        public bool HasParameter
        {
            get
            {
                if (this.Method == null)
                {
                    return false;
                }
                else
                {
                    var parameters = this.Method.GetParameters();
                    return parameters != null && parameters.Count() > 0;
                }
            }
        }

        public static CacheMethodInfo CreateFrom(Action action)
        {
            return action == null ? null : new CacheMethodInfo()
            {
                MethodName = action.Method.Name,
                MethodOwnerType = action.Method.DeclaringType.AssemblyQualifiedName,
            };
        }
        public static CacheMethodInfo CreateFrom<T>(Action<T> action, T request)
        {
            return action == null ? null : new CacheMethodInfo()
            {
                MethodName = action.Method.Name,
                MethodOwnerType = action.Method.DeclaringType.AssemblyQualifiedName,
                MethodParameter = ServiceStack.Text.JsonSerializer.SerializeToString<T>(request)
            };
        }
        public static CacheMethodInfo CreateFrom<T>(Func<T> func, T response)
        {
            return func == null ? null : new CacheMethodInfo()
            {
                MethodName = func.Method.Name,
                MethodOwnerType = func.Method.DeclaringType.AssemblyQualifiedName
            };
        }
        public static CacheMethodInfo CreateFrom<TRequest, TResponse>(Func<TRequest, TResponse> func, TRequest request, TResponse response)
        {
            return func == null ? null : new CacheMethodInfo()
            {
                MethodName = func.Method.Name,
                MethodOwnerType = func.Method.DeclaringType.AssemblyQualifiedName,
                MethodParameter = ServiceStack.Text.JsonSerializer.SerializeToString<TRequest>(request)
            };
        }
    }

    public class CacheRunnerOptionInfo
    {
        public string IsSuccessMethodOwnerType { get; set; }
        public string IsSuccessMethodName { get; set; }
        public string IsSuccessMethodRequestParameter { get; set; }
        //public string IsSuccessMethodResponseParameter { get; set; }

        [IgnoreDataMember]
        public MethodInfo IsSuccessMethod
        {
            get
            {
                Regex r = new Regex("([+]{1}.*?,)");
                var anonymousOwnerType = r.Matches(this.IsSuccessMethodOwnerType).Count > 0 ? r.Matches(this.IsSuccessMethodOwnerType)[0].Value : string.Empty;
                var methodOwnerType = Type.GetType(this.IsSuccessMethodOwnerType) ?? Type.GetType(this.IsSuccessMethodOwnerType.Replace(anonymousOwnerType, ","));
                if (methodOwnerType != null)
                {
                    return methodOwnerType.GetMethod(this.IsSuccessMethodName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
                }
                else
                {
                    return null;
                }
            }
        }

        [IgnoreDataMember]
        public object IsSuccessMethodOwnerInstance
        {
            get
            {
                if (this.IsSuccessMethod == null)
                {
                    return null;
                }
                else
                {
                    Regex r = new Regex("([+]{1}.*?,)");
                    var anonymousOwnerType = r.Matches(this.IsSuccessMethodOwnerType).Count > 0 ? r.Matches(this.IsSuccessMethodOwnerType)[0].Value : string.Empty;
                    var methodOwnerType = Type.GetType(this.IsSuccessMethodOwnerType) ?? Type.GetType(this.IsSuccessMethodOwnerType.Replace(anonymousOwnerType, ","));
                    return this.IsSuccessMethod.IsStatic ? null : Activator.CreateInstance(methodOwnerType);
                }
            }
        }

        public static CacheRunnerOptionInfo CreateFrom(Action action, RunnerOption option)
        {
            return (action == null || option.IsSuccessVerifier == null) ? null : new CacheRunnerOptionInfo()
            {
                IsSuccessMethodName = option.IsSuccessVerifier.Method.Name,
                IsSuccessMethodOwnerType = option.IsSuccessVerifier.Method.DeclaringType.AssemblyQualifiedName,
            };
        }
        public static CacheRunnerOptionInfo CreateFrom<T>(Action<T> action, T request, RunnerOption<T> option)
        {
            return (action == null || option.IsSuccessVerifier == null) ? null : new CacheRunnerOptionInfo()
            {
                IsSuccessMethodName = option.IsSuccessVerifier.Method.Name,
                IsSuccessMethodOwnerType = option.IsSuccessVerifier.Method.DeclaringType.AssemblyQualifiedName,
                IsSuccessMethodRequestParameter = ServiceStack.Text.JsonSerializer.SerializeToString<T>(request)
            };
        }
        public static CacheRunnerOptionInfo CreateFrom<T>(Func<T> func, T response, RunnerOption<T> option)
        {
            return (func == null || option.IsSuccessVerifier == null) ? null : new CacheRunnerOptionInfo()
            {
                IsSuccessMethodName = option.IsSuccessVerifier.Method.Name,
                IsSuccessMethodOwnerType = option.IsSuccessVerifier.Method.DeclaringType.AssemblyQualifiedName,
                //IsSuccessMethodResponseParameter = ServiceStack.Text.JsonSerializer.SerializeToString<T>(response)
            };
        }
        public static CacheRunnerOptionInfo CreateFrom<TRequest, TResponse>(Func<TRequest, TResponse> func, TRequest request, TResponse response, RunnerOption<TRequest, TResponse> option)
        {
            return (func == null || option.IsSuccessVerifier == null) ? null : new CacheRunnerOptionInfo()
            {
                IsSuccessMethodName = option.IsSuccessVerifier.Method.Name,
                IsSuccessMethodOwnerType = option.IsSuccessVerifier.Method.DeclaringType.AssemblyQualifiedName,
                IsSuccessMethodRequestParameter = ServiceStack.Text.JsonSerializer.SerializeToString<TRequest>(request),
                //IsSuccessMethodResponseParameter = ServiceStack.Text.JsonSerializer.SerializeToString<TResponse>(response)
            };
        }
    }
}
