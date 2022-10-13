// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Linq;

namespace Microsoft.PowerFx.Functions
{
    internal static partial class Library
    {
        // AddConstraints(source:*, "name", Sum(DurationInMin) < 40 * 60, ...)
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
