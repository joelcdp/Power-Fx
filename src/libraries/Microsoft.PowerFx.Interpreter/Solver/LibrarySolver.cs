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

            foreach (LambdaFormulaValue condition in args.Skip(1))
            {
                // Expression format: Sum/Max/Min(expression) op expression
                var binaryOp = condition
                var visitor = new CaptureEvalVisitor(runner);
                foreach (var row in source.Rows)
                {
                    visitor.Reset();
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
                    var res = (BooleanValue)await condition.EvalAsync(visitor, new EvalVisitorContext(childContext, context.StackDepthCounter));

                    if (!res.Value)
                    {
                        return new BooleanValue(irContext, false);
                    }
                }

                // Call the add constraint in the solver
                //  Translate the var names
                Console.WriteLine("(");
                foreach (var item in visitor.Terms)
                {
                    Console.WriteLine($"{item.Item1} * {item.Item2}");
                }

                Console.WriteLine(")");
                Console.WriteLine(visitor.Operator);
                Console.WriteLine(visitor.Number);
            }

            return new BooleanValue(irContext, true);
        }
    }
}
