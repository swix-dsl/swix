using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SimpleWixDsl.Swix
{
    public static class ReflectionUtils
    {
        public static T GetSingleAttributeOrNull<T>(this MemberInfo target, bool inherit = false)
            where T : class
        {
            var attrs = target.GetCustomAttributes(typeof (T), inherit);
            if (attrs.Length == 1)
                return (T)attrs[0];
            return null;
        }

        public static bool CompatibleWithGenericCallerDelegate(this MethodInfo mi, Type delegateType)
        {
            var delegateMi = delegateType.GetMethod("Invoke");
            if (mi.ReturnType != delegateMi.ReturnType) return false;

            var delegateParams = delegateMi.GetParameters();
            if (delegateParams[0].ParameterType != typeof (object)) return false;

            var methodParams = mi.GetParameters();
            if (methodParams.Length + 1 != delegateParams.Length) return false;

            return !methodParams.Where((t, i) => t.ParameterType != delegateParams[i + 1].ParameterType).Any();
        }

        public static T CreateCallDelegate<T>(this MethodInfo mi, Type targetType)
        {
            var miParams = mi.GetParameters();

            var parameterExps = new List<ParameterExpression>
                {
                    Expression.Parameter(typeof(object), "callTarget")
                };
            parameterExps.AddRange(miParams.Select(paramInfo => Expression.Parameter(paramInfo.ParameterType, paramInfo.Name)));

            var callTarget = parameterExps[0];
            var convertedCallTarget = Expression.Convert(callTarget, targetType);
            var methodCall = Expression.Call(convertedCallTarget, mi, parameterExps.Skip(1));
            return Expression.Lambda<T>(methodCall, parameterExps).Compile();

        }
    }
}
