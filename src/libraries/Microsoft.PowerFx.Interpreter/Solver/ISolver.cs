// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerFx.Interpreter.Solver
{
    public interface ISolver
    {
        public bool AddConstraint(IReadOnlyCollection<double> coefficients, IReadOnlyCollection<string> variableNames, ConstraintOperator op, double rhsValue);
    }
}
