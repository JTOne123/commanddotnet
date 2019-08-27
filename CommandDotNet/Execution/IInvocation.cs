﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace CommandDotNet.Execution
{
    public interface IInvocation
    {
        /// <summary>
        /// The arguments associated with the invocation delegate.<br/>
        /// There can be multiple arguments per parameter
        /// if IArgumentModel's are used.<br/>
        /// </summary>
        IReadOnlyCollection<IArgument> Arguments { get; }

        /// <summary>The parameters defined in the invocation delegate.</summary>
        IReadOnlyCollection<ParameterInfo> Parameters { get; }

        /// <summary>
        /// The values parsed from the arguments or <see cref="CommandContext"/>
        /// if defined as a parameter.
        /// </summary>
        object[] ParameterValues { get; }

        /// <summary>The method signature of the delegate.</summary>
        /// <remarks>
        /// Take care when assuming methods will be derived from a class.
        /// This may not always be valid in the future.
        /// </remarks>
        MethodInfo MethodInfo { get; }

        /// <summary>Invokes the instance</summary>
        object Invoke(CommandContext commandContext, object instance, Func<CommandContext, Task<int>> next);
    }
}