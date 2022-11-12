﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Interpreter.Solver;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Functions
{
    internal static partial class Library
    {
        public static async ValueTask<FormulaValue> Minimize(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            return await ObjectiveFunction(ObjectiveFunctionGoal.Minimize, runner, context, irContext, args);
        }

        public static async ValueTask<FormulaValue> Maximize(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            return await ObjectiveFunction(ObjectiveFunctionGoal.Maximize, runner, context, irContext, args);
        }

        // Minimize/Maximize(source:*, "name", Sum(term * variable))
        public static async ValueTask<FormulaValue> ObjectiveFunction(ObjectiveFunctionGoal goal, EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var source = (TableValue)args[0];
            var name = (LambdaFormulaValue)args[1];
            var arg1 = (LambdaFormulaValue)args[2];
            List<NumberValue> resultTable = null;

            var solver = runner.FunctionServices.GetService<ISolver>(null);

            foreach (LambdaFormulaValue condition in args.Skip(2))
            {
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

                    var goalName = await name.EvalAsync(runner, new EvalVisitorContext(childContext, context.StackDepthCounter));
                    if (goalName is not StringValue)
                    {
                        return new ErrorValue(irContext, new ExpressionError()
                        {
                            Message = $"The second parameter must be evaluated to a string. Type found={goalName.Type.ToString()}",
                            Span = irContext.SourceContext,
                            Kind = ErrorKind.InvalidFunctionUsage
                        });
                    }

                    // Expression format: Sum(term * variable)
                    var visitor = new ObjectiveFunctionEvalVisitor(runner);
                    var res = await condition.EvalAsync(visitor, new EvalVisitorContext(childContext, context.StackDepthCounter));
                    if (res is ErrorValue errorValue)
                    {
                        return res;
                    }

                    var numValue = res as NumberValue;
                    if (resultTable == null)
                    {
                        resultTable.Add(numValue);
                    }

                    solver.AddObjectiveFunction(goal, visitor.Terms.Select(t => t.Item1).ToArray(), visitor.Terms.Select(t => t.Item2).ToArray(), (goalName as StringValue).Value);
                }
            }

            return FormulaValue.NewSingleColumnTable(resultTable);
        }

        // AddConstraints(source:*, "name", Sum(variable) < 40 * 60, ...)
        public static async ValueTask<FormulaValue> AddConstraint(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var source = (TableValue)args[0];
            var name = (LambdaFormulaValue)args[1];
            var arg1 = (LambdaFormulaValue)args[2];

            var solver = runner.FunctionServices.GetService<ISolver>(null);

            foreach (LambdaFormulaValue condition in args.Skip(2))
            {
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

                    var constraintName = await name.EvalAsync(runner, new EvalVisitorContext(childContext, context.StackDepthCounter));
                    if (constraintName is not StringValue)
                    {
                        return new ErrorValue(irContext, new ExpressionError()
                        {
                            Message = $"The second parameter must be evaluated to a string. Type found={constraintName.Type.ToString()}",
                            Span = irContext.SourceContext,
                            Kind = ErrorKind.InvalidFunctionUsage
                        });
                    }

                    // Expression format: [If(variable, )] Sum/Max/Min(expression) op expression
                    var visitor = new ConstraintEvalVisitor(runner);
                    var res = await condition.EvalAsync(visitor, new EvalVisitorContext(childContext, context.StackDepthCounter));
                    if (res is ErrorValue errorValue)
                    {
                        return res;
                    }

                    if (res is not BooleanValue boolValue ||
                        !boolValue.Value)
                    {
                        return FormulaValue.NewSingleColumnTable(new BooleanValue(condition.IRContext, false));
                    }

                    // Call the add constraint in the solver
                    //  Translate the var names
                    if (string.IsNullOrEmpty(visitor.ConditionalVariable))
                    {
                        solver.AddConstraint(visitor.Terms.Select(t => t.Item1).ToArray(), visitor.Terms.Select(t => t.Item2).ToArray(), visitor.Operator, visitor.Number, (constraintName as StringValue).Value);
                    }
                    else
                    {
                        solver.AddConstraint(visitor.ConditionalVariable, visitor.Terms.Select(t => t.Item1).ToArray(), visitor.Terms.Select(t => t.Item2).ToArray(), visitor.Operator, visitor.Number, (constraintName as StringValue).Value);
                    }
                }
            }

            var resultContext = new IRContext(irContext.SourceContext, FormulaType.Boolean);
            return FormulaValue.NewSingleColumnTable(new BooleanValue(resultContext, true));
        }
    }
}
