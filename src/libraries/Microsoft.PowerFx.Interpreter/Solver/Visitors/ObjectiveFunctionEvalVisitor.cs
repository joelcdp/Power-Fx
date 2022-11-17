// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.IR.Symbols;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Interpreter.Solver
{
    /// <summary>
    /// This visitor enforces the following expression for the Minimize/Maximize(source, name, expression):
    /// 
    /// objFunction :=  Sum(expression * solverVariable).
    /// 
    /// </summary>
    internal class ObjectiveFunctionEvalVisitor : EvalVisitor
    {
        private readonly List<Tuple<double, string>> _capturedTerms = new ();
        private readonly EvalVisitor _evalVisitor;
        private bool _sumFunctionCalled = false;

        public ObjectiveFunctionEvalVisitor(EvalVisitor visitor)
            : base(visitor.CultureInfo, new CancellationToken())
        {
            _evalVisitor = visitor;
        }

        public string ConditionalVariable { get; private set; } = string.Empty;

        public IEnumerable<Tuple<double, string>> Terms => _capturedTerms;
        
        public new void CheckCancel()
        {
            _evalVisitor.CheckCancel();
        }

        #region Overriden Visit methods
        public override async ValueTask<FormulaValue> Visit(ScopeAccessNode node, EvalVisitorContext context)
        {
            var currentVarName = string.Empty;
            double coefficient = 1;
            if (node.Value is ScopeAccessSymbol s1)
            {
                var scope = s1.Parent;

                var val = context.SymbolContext.GetScopeVar(scope, s1.Name);

                // TODO find a better way to identify when to capture a term as model variable
                if (val is UntypedObjectValue untypedObject &&
                    untypedObject.Impl is ISolverVariable solverVariable)
                {
                    currentVarName = solverVariable.GetVariableName();
                }
            }

            var value = await base.Visit(node, context);

            if (currentVarName != string.Empty)
            {
                if (value is NumberValue number)
                {
                    coefficient = number.Value;
                }

                _capturedTerms.Add(new Tuple<double, string>(coefficient, currentVarName));
            }

            return value;
        }

        public override async ValueTask<FormulaValue> Visit(CallNode node, EvalVisitorContext context)
        {
            var functionName = node.Function.Name;

            switch (functionName)
            {
                case "Sum":
                    return await VisitSumFunction(node, context);
                case "Table":
                case "Value":
                    return await base.Visit(node, context);
                default:
                    return await GetErrorValue(node, $"Function {functionName} is not supported.  The only functions supported are Table() and Sum().");
            }
        }

        private async ValueTask<FormulaValue> VisitSumFunction(CallNode node, EvalVisitorContext context)
        {
            if (_sumFunctionCalled)
            {
                return await GetErrorValue(node, $"Sum() is not supported in this context.");
            }

            try
            {
                _sumFunctionCalled = true;
                return await base.Visit(node, context);
            }
            finally
            {
                _sumFunctionCalled = false;
            }
        }

        public override async ValueTask<FormulaValue> Visit(BinaryOpNode node, EvalVisitorContext context)
        {            
            return await BinaryOpVisit(node, context);
        }

        public override ValueTask<FormulaValue> Visit(LazyEvalNode node, EvalVisitorContext context)
        {
            return base.Visit(node, context);
        }

        public override ValueTask<FormulaValue> Visit(ResolvedObjectNode node, EvalVisitorContext context)
        {
            return base.Visit(node, context);
        }

        #region overriden Visit methods not supposed to be called
        private Exception CreateException(IntermediateNode node)
        {
            return new InvalidOperationException($"Node {node.ToString()} is not supposed to be called");
        }

        public override ValueTask<FormulaValue> Visit(AggregateCoercionNode node, EvalVisitorContext context)
        {
            throw CreateException(node);
        }

        public override ValueTask<FormulaValue> Visit(BooleanLiteralNode node, EvalVisitorContext context)
        {
            throw CreateException(node);
        }

        public override ValueTask<FormulaValue> Visit(ChainingNode node, EvalVisitorContext context)
        {
            throw CreateException(node);
        }

        public override ValueTask<FormulaValue> Visit(ColorLiteralNode node, EvalVisitorContext context)
        {
            throw CreateException(node);
        }

        public override ValueTask<FormulaValue> Visit(ErrorNode node, EvalVisitorContext context)
        {
            throw CreateException(node);
        }

        public override ValueTask<FormulaValue> Visit(NumberLiteralNode node, EvalVisitorContext context)
        {
            throw CreateException(node);
        }

        public override ValueTask<FormulaValue> Visit(RecordFieldAccessNode node, EvalVisitorContext context)
        {
            throw CreateException(node);
        }

        public override ValueTask<FormulaValue> Visit(RecordNode node, EvalVisitorContext context)
        {
            throw CreateException(node);
        }

        public override ValueTask<FormulaValue> Visit(SingleColumnTableAccessNode node, EvalVisitorContext context)
        {
            throw CreateException(node);
        }

        public override ValueTask<FormulaValue> Visit(TextLiteralNode node, EvalVisitorContext context)
        {
            throw CreateException(node);
        }

        public override ValueTask<FormulaValue> Visit(UnaryOpNode node, EvalVisitorContext context)
        {
            throw CreateException(node);
        }
        #endregion

        #endregion

        private async ValueTask<FormulaValue> BinaryOpVisit(BinaryOpNode node, EvalVisitorContext context)
        {
            switch (node.Op)
            {
                case BinaryOpKind.DynamicGetField:
                    return await VisitDynamicsGetField(node, context);
                case BinaryOpKind.MulNumbers:
                    return await VisitMulNumbers(node, context);
                default:
                    return await GetErrorValue(node, $"The operator {node.Op} is not supported in this position.  Expected '*' or variable reference.");
            }
        }

        private async ValueTask<FormulaValue> VisitMulNumbers(BinaryOpNode node, EvalVisitorContext context)
        {
            Contract.Assert(node.Op == BinaryOpKind.MulNumbers);

            Tuple<double, string> newcapturedTerm = null;
            double newCoefficient = 0;

            // Accept any expression
            var arg1 = await node.Left.Accept(_evalVisitor, context);
            if (arg1 is NumberValue numberValue)
            {
                newCoefficient = numberValue.Value;
            }
            else
            {
                return await GetErrorValue(node, $"Expression must return a number. Expression={node.Left.ToString()}");
            }

            // Capture a variable
            var lastRightTerm = _capturedTerms.LastOrDefault();
            var arg2 = await node.Right.Accept(this, context);
            if (lastRightTerm != _capturedTerms.LastOrDefault())
            {
                newcapturedTerm = _capturedTerms.LastOrDefault();
            }
            else
            {
                return await GetErrorValue(node, $"Expression must reference a variable. Expression={node.Right.ToString()}");
            }

            _capturedTerms[_capturedTerms.Count - 1] = new Tuple<double, string>(newCoefficient, newcapturedTerm.Item2);

            var args = new FormulaValue[] { arg1, arg2 };

            return await VisitBinaryOpNode(node, context, args);
        }

        private async ValueTask<FormulaValue> VisitDynamicsGetField(BinaryOpNode node, EvalVisitorContext context)
        {
            Contract.Assert(node.Op == BinaryOpKind.DynamicGetField);

            var arg1 = await node.Left.Accept(_evalVisitor, context);
            var arg2 = await node.Right.Accept(_evalVisitor, context);
            var args = new FormulaValue[] { arg1, arg2 };

            if (arg1 is UntypedObjectValue cov && arg2 is StringValue sv)
            {
                if (cov.Impl.Type is ExternalType et &&
                    et.Kind == ExternalTypeKind.Object &&
                    cov.Impl.TryGetProperty(sv.Value, out var res) &&
                    res is IExternalObject externalObject &&
                    externalObject is ISolverVariable solverVariable)
                {
                    _capturedTerms.Add(new Tuple<double, string>(1, solverVariable.GetVariableName()));
                }
            }

            return await VisitBinaryOpNode(node, context, args);
        }

        private static async ValueTask<FormulaValue> GetErrorValue(IntermediateNode node, string msg)
        {
            return new ErrorValue(node.IRContext, new ExpressionError()
            {
                Message = msg,
                Span = node.IRContext.SourceContext,
                Kind = ErrorKind.InvalidFunctionUsage
            });
        }
    }
}
