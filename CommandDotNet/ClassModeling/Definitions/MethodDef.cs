﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CommandDotNet.Execution;
using CommandDotNet.Extensions;

namespace CommandDotNet.ClassModeling.Definitions
{
    internal class MethodDef : IMethodDef
    {
        public static readonly Type MiddlewareNextReturnType = typeof(Task<int>);
        public static readonly Type MiddlewareNextParameterType = typeof(Func<CommandContext, Task<int>>);

        private readonly AppConfig _appConfig;
        private IReadOnlyCollection<IArgumentDef> _argumentDefs;
        private IReadOnlyCollection<IArgument> _arguments;
        private ParameterInfo[] _parameters;
        private object[] _values;

        private ParameterInfo _commandContextParameterInfo;
        private ParameterInfo _nextParameterInfo;

        public MethodInfo MethodInfo { get; }

        public IReadOnlyCollection<IArgumentDef> ArgumentDefs => EnsureInitialized(() => _argumentDefs);

        public IReadOnlyCollection<IArgument> Arguments => EnsureInitialized(ref _arguments,
            () => ArgumentDefs.Select(a => a.Argument).ToList().AsReadOnly());

        public IReadOnlyCollection<ParameterInfo> Parameters => EnsureInitialized(() => _parameters);

        public object[] ParameterValues => EnsureInitialized(() => _values);

        public MethodDef(MethodInfo method, AppConfig appConfig)
        {
            MethodInfo = method ?? throw new ArgumentNullException(nameof(method));
            _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
        }

        public static bool IsInterceptorMethod(MethodBase methodBase)
        {
            return methodBase.GetParameters().Any(IsMiddlewareNextType);
        }

        private static bool IsMiddlewareNextType(ParameterInfo parameterInfo)
        {
            return parameterInfo.ParameterType == MiddlewareNextParameterType;
        }

        public object Invoke(CommandContext commandContext, object instance, Func<CommandContext, Task<int>> next)
        {
            if (_nextParameterInfo != null)
            {
                if (next == null)
                {
                    throw new AppRunnerException(
                        $"Invalid operation. {nameof(Func<CommandContext, Task<int>>)} {_nextParameterInfo.Name} parameter not provided for method: {_nextParameterInfo.Member.FullName()}. " +
                        $"Check middleware to ensure it hasn't misconfigured the {nameof(CommandContext.InvocationContext)}");
                }
                _values[_nextParameterInfo.Position] = next;
            }
            if (_commandContextParameterInfo != null)
            {
                _values[_commandContextParameterInfo.Position] = commandContext;
            }
            return MethodInfo.Invoke(instance, _values);
        }

        private T EnsureInitialized<T>(ref T current, Func<T> getValues)
        {
            if (current == null)
            {
                current = getValues();
            }

            return current;
        }

        private T EnsureInitialized<T>(Func<T> getValues)
        {
            if (_argumentDefs == null)
            {
                Initialize();
            }
            return getValues();
        }

        private void Initialize()
        {
            _parameters = MethodInfo.GetParameters();

            var isMiddleware = _parameters.Any(IsMiddlewareNextType);

            var argumentMode = isMiddleware
                ? ArgumentMode.Option
                : _appConfig.AppSettings.DefaultArgumentMode;
            
            _values = new object[_parameters.Length];
            
            var parametersByName = _parameters.ToDictionary(
                p => p.Name,
                p => (param: p, args: GetArgsFromParameter(p, argumentMode).ToList()));

            var arguments = parametersByName.Values
                .OrderBy(v => v.param.Position)
                .SelectMany(p => p.args)
                .ToList();

            _argumentDefs = arguments.AsReadOnly();
        }

        private IEnumerable<IArgumentDef> GetArgsFromParameter(ParameterInfo parameterInfo, ArgumentMode argumentMode)
        {
            if (parameterInfo.ParameterType.InheritsFrom<IArgumentModel>())
            {
                return GetArgumentsFromModel(
                    parameterInfo.ParameterType,
                    argumentMode,
                    null,
                    value => _values[parameterInfo.Position] = value);
            }

            if (parameterInfo.ParameterType == typeof(CommandContext))
            {
                _commandContextParameterInfo = parameterInfo;
                return Enumerable.Empty<IArgumentDef>();
            }

            if (IsMiddlewareNextType(parameterInfo))
            {
                _nextParameterInfo = parameterInfo;
                return Enumerable.Empty<IArgumentDef>();
            }

            return new ParameterArgumentDef(
                    parameterInfo,
                    GetArgumentType(parameterInfo, argumentMode),
                    _appConfig,
                    value => _values[parameterInfo.Position] = value)
                .ToEnumerable();
        }

        private IEnumerable<IArgumentDef> GetArgsFromProperty(PropertyInfo propertyInfo, ArgumentMode argumentMode, object modelInstance) =>
            propertyInfo.PropertyType.InheritsFrom<IArgumentModel>()
                ? GetArgumentsFromModel(
                    propertyInfo.PropertyType,
                    argumentMode,
                    propertyInfo.GetValue(modelInstance),
                    value => propertyInfo.SetValue(modelInstance, value))
                : new PropertyArgumentDef(
                        propertyInfo,
                        GetArgumentType(propertyInfo, argumentMode),
                        _appConfig,
                        modelInstance)
                    .ToEnumerable();

        private IEnumerable<IArgumentDef> GetArgumentsFromModel(Type modelType, ArgumentMode argumentMode, object existingDefault, Action<object> instanceCreated)
        {
            // Enhancement: add IDependencyResolver.TryResolve and try resolve first.
            var instance = existingDefault ?? Activator.CreateInstance(modelType);

            if (existingDefault == null)
            {
                instanceCreated?.Invoke(instance);
            }

            return modelType
                .GetDeclaredProperties()
                .SelectMany(propertyInfo => GetArgsFromProperty(propertyInfo, argumentMode, instance));
        }

        private static ArgumentType GetArgumentType(ICustomAttributeProvider info, ArgumentMode argumentMode)
        {
            return info.IsOption(argumentMode) ? ArgumentType.Option : ArgumentType.Operand;
        }

        public override string ToString()
        {
            return $"{MethodInfo.GetType().Name}:{MethodInfo?.DeclaringType?.Name}.{MethodInfo.Name}(" +
                   $"{MethodInfo.GetParameters().Select(p => $"{p.ParameterType} {p.Name}").ToCsv()})";
        }
    }
}