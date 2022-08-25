// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Interpreter.Solver
{
    public class SolverObject : IUntypedObject
    {
        private readonly ISolver _solver;

        public SolverObject(ISolver solver)
        {
            _solver = solver;
        }

        public IUntypedObject this[int index] => null;

        public FormulaType Type => ExternalType.ObjectType;

        public bool AddConstraint(IReadOnlyCollection<double> coefficients, IReadOnlyCollection<string> variableNames, ConstraintOperator op, double rhsValue, string constraintName)
        {
            return _solver.AddConstraint(coefficients, variableNames, op, rhsValue, constraintName);
        }

        public int GetArrayLength()
        {
            throw new NotImplementedException();
        }

        public bool GetBoolean()
        {
            throw new NotImplementedException();
        }

        public double GetDouble()
        {
            throw new NotImplementedException();
        }

        public string GetString()
        {
            throw new NotImplementedException();
        }

        public bool TryGetProperty(string value, out IUntypedObject result)
        {
            throw new NotImplementedException();
        }
    }
}
