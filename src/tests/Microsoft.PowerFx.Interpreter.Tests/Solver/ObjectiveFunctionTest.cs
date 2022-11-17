// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Interpreter.Solver;
using Microsoft.PowerFx.Types;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests.Solver
{
    public class ObjectiveFunctionTest : PowerFxTest
    {
        private const string ColumnName = "Value";

        public ObjectiveFunctionTest()
        {
        }

        [Theory]
        [InlineData(
            "Minimize(Table1, Concatenate(\"name\", Text(Value)), Value(Value) )", 
            new string[] { "Minimize[name2]:1*intVar2" })]
        [InlineData(
            "Maximize(Table1, Concatenate(\"name\", Text(Value(Value))), Value(Value) )", 
            new string[] { "Maximize[name2]:1*intVar2" })]
        [InlineData(
            "Maximize(Table({a:0}), \"name\", Sum(Table1, Value(Value)))", 
            new string[] { "Maximize[name]:1*intVar0 + 1*intVar1 + 1*intVar2" })]
        public void AddObjectiveFunction(string script, string[] expected)
        {
            var engine = new RecalcEngine();
            var solver = new SolverEngine();

            var obj1 = FormulaValue.New(new SolverObjectVariable<int>(0, "intVar0"));
            var obj2 = FormulaValue.New(new SolverObjectVariable<int>(1, "intVar1"));
            var obj3 = FormulaValue.New(new SolverObjectVariable<int>(2, "intVar2"));
            engine.UpdateVariable("intVar0", obj1);
            engine.UpdateVariable("intVar1", obj2);
            engine.UpdateVariable("intVar2", obj3);

            var record1 = FormulaValue.NewRecordFromFields(new NamedValue(ColumnName, obj1));
            var record2 = FormulaValue.NewRecordFromFields(new NamedValue(ColumnName, obj2));
            var record3 = FormulaValue.NewRecordFromFields(new NamedValue(ColumnName, obj3));
            var records = new List<RecordValue>() { record1, record2, record3 };

            engine.UpdateVariable("Table1", FormulaValue.NewTable(RecordType.Empty().Add(ColumnName, FormulaType.UntypedObject), records));

            var values = new SymbolValues();
            values.AddService<ISolver>(solver);

            var result = engine.EvalAsync(script, CancellationToken.None, runtimeConfig: values).Result;
            Assert.True(result is not ErrorValue, "result must not be an error");
            solver.AssertCommandsReceived(expected);
        }
    }
}
