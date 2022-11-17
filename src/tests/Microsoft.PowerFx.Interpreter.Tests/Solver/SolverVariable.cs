// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Microsoft.PowerFx.Interpreter.Solver;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class SolverVariable<T> : ISolverVariable
    {
        private static object objectNextIndex = new BigInteger();

        private string _variableName = null;

        public T Value { get; set; }

        public SolverVariable(string variableName, T value)
        {
            _variableName = variableName;
            Value = value;
        }

        public string GetVariableName()
        {
            if (_variableName == null)
            {
                lock (objectNextIndex)
                {
                    _variableName = $"object{objectNextIndex}";
                    objectNextIndex = ((BigInteger)objectNextIndex) + 1;
                }
            }

            return _variableName;
        }
    }
}
