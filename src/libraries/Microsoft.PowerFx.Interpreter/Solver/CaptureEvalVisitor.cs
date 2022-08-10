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
            _firstBinaryOpVisit = false;
        }

        public IEnumerable<Tuple<double, string>> Terms => _capturedTerms;
        
        public double Number => _rhs;
        
        public string Operator
        {
            get
            {
                switch (_binaryOp)
                {
                    case BinaryOpKind.EqNumbers:
                        return "=";
                    case BinaryOpKind.LtNumbers:
                        return "<";
                    case BinaryOpKind.GtNumbers:
                        return ">";
                    default:
                        return "???";
                }
            }
        }

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

                var val = context.SymbolContext.GetScopeVar(scope, "VarName");
                if (val is StringValue varName)
                {
                    currentVarName = varName.Value;
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
                    case BinaryOpKind.LtNumbers:
                    case BinaryOpKind.GtNumbers:
                        _binaryOp = node.Op;
                        break;
                    default:
                        // TODO: Error only supports '<', '=', '>'
                        result = false;
                        break;
                }

                var arg1 = await node.Left.Accept(this, context);

                var arg2 = await node.Right.Accept(this, context);
                if (arg1 is NumberValue numberValue1)
                {
                    _rhs = numberValue1.Value;
                    if (_binaryOp != BinaryOpKind.EqNumbers)
                    {
                        _binaryOp = BinaryOpKind.LtNumbers; //TODO
                    }
                }
                else if (arg2 is NumberValue numberValue2)
                {
                    _rhs = numberValue2.Value;
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
