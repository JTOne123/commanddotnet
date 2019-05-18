using System;
using System.Collections.Generic;

namespace CommandDotNet.Tests.Utils
{
    public class Inputs
    {
        private Dictionary<Type, object> _inputs = new Dictionary<Type, object>();

        public void Capture(object value)
        {
            // arguments should only be captured once.  don't allow overwrites.
            _inputs.Add(value.GetType(), value);
        }

        public T Get<T>()
        {
            return (T) _inputs[typeof(T)];
        }
    }
}