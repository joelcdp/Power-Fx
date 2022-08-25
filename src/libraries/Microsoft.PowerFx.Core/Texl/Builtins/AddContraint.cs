// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    //  AddConstraints(source, cond:b, cond:b, ...) : b
    internal sealed class AddConstraintFunction : FunctionWithTableInput
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => true;

        public AddConstraintFunction()
            : base("AddConstraints", TexlStrings.AboutAddConstraint, FunctionCategories.Logical, DType.Boolean, 0x6, 3, int.MaxValue, DType.EmptyTable, DType.String)
        {
            ScopeInfo = new FunctionScopeInfo(this);
        }

        public override bool IsLazyEvalParam(int index)
        {
            return index > 0;
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            // Enumerate just the base overloads (the first 3 possibilities).
            yield return new[] { TexlStrings.LogicalFuncParam };
            yield return new[] { TexlStrings.LogicalFuncParam, TexlStrings.LogicalFuncParam };
            yield return new[] { TexlStrings.LogicalFuncParam, TexlStrings.LogicalFuncParam, TexlStrings.LogicalFuncParam };
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 2)
            {
                return GetGenericSignatures(arity, TexlStrings.LogicalFuncParam);
            }

            return base.GetSignatures(arity);
        }

        public override bool CheckInvocation(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var fArgsValid = CheckInvocation(args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);

            if (argTypes[2].IsRecord)
            {
                returnType = argTypes[2].ToTable();
            }
            else if (argTypes[2].IsPrimitive || argTypes[2].IsTable)
            {
                returnType = DType.CreateTable(new TypedName(argTypes[2], ColumnName_Value));
            }
            else
            {
                returnType = DType.Error;
                fArgsValid = false;
            }

            return fArgsValid;
        }
    }
}
