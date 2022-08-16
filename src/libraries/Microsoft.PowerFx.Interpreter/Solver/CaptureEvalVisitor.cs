// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.IR.Symbols;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Interpreter.Solver
{
    internal class CaptureEvalVisitor : EvalVisitor
    {
        private readonly List<Tuple<double, string>> _capturedTerms = new ();
        private readonly EvalVisitor _evalVisitor;
        private BinaryOpKind _binaryOp = BinaryOpKind.Invalid;
        private double _rhs = 0;
        private bool _firstBinaryOpVisit;

        public CaptureEvalVisitor(EvalVisitor visitor)
            : base(visitor.CultureInfo, new CancellationToken())
        {
            _evalVisitor = visitor;
            Reset();
        }

        public void Reset()
        {
            _firstBinaryOpVisit = true;
        }

        public IEnumerable<Tuple<double, string>> Terms => _capturedTerms;
        
        public double Number => _rhs;

        public ConstraintOperator Operator => _binaryOp switch
        {
            BinaryOpKind.EqNumbers => ConstraintOperator.Equal,
            BinaryOpKind.LeqNumbers => ConstraintOperator.LessEqual,
            BinaryOpKind.GeqNumbers => ConstraintOperator.GreaterEqual,
            _ => throw new InvalidOperationException($"Binary operator not supported = {_binaryOp}"),
        };

        public new void CheckCancel()
        {
            _evalVisitor.CheckCancel();
        }

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
                    currentVarName = solverVariable.Name;
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

        public override async ValueTask<FormulaValue> Visit(BinaryOpNode node, EvalVisitorContext context)
        {
            // Only first time visiting BinaryOpNode will enter the block
            if (_firstBinaryOpVisit)
            {
                _firstBinaryOpVisit = false;
                var result = true;
                switch (node.Op)
                {
                    case BinaryOpKind.EqNumbers:
                    case BinaryOpKind.LeqNumbers:
                    case BinaryOpKind.GeqNumbers:
                        _binaryOp = node.Op;
                        break;
                    default:
                        return new ErrorValue(node.IRContext, new ExpressionError()
                        {
                            Message = $"The only support operators are '<=', '=', '=>'.  {node.Op} is not supported",
                            Span = node.IRContext.SourceContext,
                            Kind = ErrorKind.InvalidFunctionUsage
                        });
                }

                var initialCount = _capturedTerms.Count;
                var arg1 = await node.Left.Accept(this, context);
                var arg1TermsCount = _capturedTerms.Count;

                var arg2 = await node.Right.Accept(this, context);
                var arg2TermsCount = _capturedTerms.Count - arg1TermsCount;
                arg1TermsCount -= initialCount;

                if (_capturedTerms.Count - initialCount == 0)
                {
                    return new ErrorValue(node.IRContext, new ExpressionError()
                    {
                        Message = "No variables were found in the expression.",
                        Span = node.IRContext.SourceContext,
                        Kind = ErrorKind.InvalidFunctionUsage
                    });
                }

                var lhs = arg1;
                var rhs = arg2;

                if (arg2TermsCount != 0)
                {
                    lhs = arg2;
                    rhs = arg1;
                    if (_binaryOp != BinaryOpKind.EqNumbers)
                    {
                        _binaryOp = (_binaryOp == BinaryOpKind.LtNumbers) ? BinaryOpKind.GtNumbers : BinaryOpKind.LtNumbers;
                    }
                }

                if (rhs is NumberValue number)
                {
                    _rhs = number.Value;
                }

                return FormulaValue.New(result);
            }
            else
            {
                return await base.Visit(node, context);
            }
        }

        public override async ValueTask<FormulaValue> Visit(RecordFieldAccessNode node, EvalVisitorContext context)
        {
            return await base.Visit(node, context);
        }

        public override async ValueTask<FormulaValue> Visit(SingleColumnTableAccessNode node, EvalVisitorContext context)
        {
            return await base.Visit(node, context);
        }
    }
}
