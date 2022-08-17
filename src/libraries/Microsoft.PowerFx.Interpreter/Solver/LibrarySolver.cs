// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Interpreter.Solver;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Functions
{
    internal static partial class Library
    {
        // AddConstraints(Sum(DurationInMin) < 40 * 60, ...)
        public static async ValueTask<FormulaValue> AddConstraint(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var source = (TableValue)args[0];
            var arg1 = (LambdaFormulaValue)args[1];

            var obj = context.SymbolContext.GetScopeVar(new Core.IR.Symbols.ScopeSymbol(0), "Solver");
            SolverObject solver = null;
            if (obj is UntypedObjectValue untypedObject)
            {
                solver = (SolverObject)untypedObject.Impl;
            }
            else
            {
                return new BooleanValue(irContext, false);
            }

            foreach (LambdaFormulaValue condition in args.Skip(1))
            {
                // Expression format: Sum/Max/Min(expression) op expression
                var visitor = new ConstraintEvalVisitor(runner);
                foreach (var row in source.Rows)
                {
                    SymbolContext childContext;
                    if (row.IsValue)
                    {
                        childContext = context.SymbolContext.WithScopeValues(row.Value);
                    }
                    else
                    {
                        childContext = context.SymbolContext.WithScopeValues(RecordValue.Empty());
                    }

                    // condition evals to a boolean 
                    var res = await condition.EvalAsync(visitor, new EvalVisitorContext(childContext, context.StackDepthCounter));
                    if (res is ErrorValue errorValue)
                    {
                        return res;
                    }

                    if (res is not BooleanValue boolValue ||
                        !boolValue.Value)
                    {
                        return new BooleanValue(condition.IRContext, false);
                    }
                }

                // Call the add constraint in the solver
                //  Translate the var names
                solver.AddConstraint(visitor.Terms.Select(t => t.Item1).ToArray(), visitor.Terms.Select(t => t.Item2).ToArray(), visitor.Operator, visitor.Number);
            }

            var resultContext = new IRContext(irContext.SourceContext, FormulaType.Boolean);
            return new BooleanValue(resultContext, true);
        }
    }
}
