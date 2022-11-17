// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.PowerFx.Interpreter.Solver;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    internal class SolverEngine : ISolver
    {
        public List<string> CommandaReceived { get; private set; }

        public bool AddConstraint(IReadOnlyCollection<double> coefficients, IReadOnlyCollection<string> variableNames, ConstraintOperator op, double rhsValue, string constraintName)
        {
            return AddConstraint(string.Empty, coefficients, variableNames, op, rhsValue, constraintName);
        }

        public bool AddConstraint(string conditionalVariableName, IReadOnlyCollection<double> coefficients, IReadOnlyCollection<string> variableNames, ConstraintOperator op, double rhsValue, string constraintName)
        {
            var coef = coefficients.ToImmutableList();
            var vars = variableNames.ToImmutableList();

            var cmd = new StringBuilder();

            if (!string.IsNullOrEmpty(constraintName)) 
            {
                cmd.Append(constraintName);
                cmd.Append(':');
            }

            if (!string.IsNullOrEmpty(conditionalVariableName))
            {
                cmd.Append("If(");
                cmd.Append(conditionalVariableName);
                cmd.Append(")");
            }

            for (var index = 0; index < variableNames.Count; index++)
            {
                if (index != 0)
                {
                    cmd.Append(" + ");
                }

                cmd.Append(coef[index]);
                cmd.Append("*");
                cmd.Append(vars[index]);
            }

            cmd.Append(op);
            cmd.Append(rhsValue);
            CommandaReceived.Add(cmd.ToString());
            return true;
        }

        public bool AddObjectiveFunction(ObjectiveFunctionGoal goal, IReadOnlyCollection<double> coefficients, IReadOnlyCollection<string> variableNames, string constraintName)
        {
            var coef = coefficients.ToImmutableList();
            var vars = variableNames.ToImmutableList();

            var cmd = new StringBuilder();

            cmd.Append(goal);
            cmd.Append('[');
            if (!string.IsNullOrEmpty(constraintName))
            {
                cmd.Append(constraintName);
                cmd.Append("]:");
            }

            for (var index = 0; index < variableNames.Count; index++)
            {
                if (index != 0)
                {
                    cmd.Append(" + ");
                }

                cmd.Append(coef[index]);
                cmd.Append("*");
                cmd.Append(vars[index]);
            }

            CommandaReceived.Add(cmd.ToString());
            return true;
        }

        public void AssertCommandsReceived(string[] expected)
        {
            Assert.Equal(expected.Length, CommandaReceived.Count);
            for (var index = 0; index < expected.Length; index++)
            {
                Assert.Equal(expected[index], CommandaReceived[index]);
            }
        }
    }
}
