﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Linq;
using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Syntax;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class NamedFormulasTests
    {
        [Theory]
        [InlineData("x=1;y=2;")]
        public void NamedFormulaTest(string script)
        {
            var namedFormula = new NamedFormulas(script);
            Assert.Equal(script, namedFormula.Script);
        }

        [Theory]
        [InlineData("x=    \n\n\t2;colStyles=Table({\n\tkey:\"colorPrimary\", \n\tvalue:\"Red\"\n})", 2)]

        //[InlineData("x=158558 +\t                  \n\n289885;y=\n\n     \t2;", 2)]
        public void EnsureParsedTest(string script, int count)
        {
            var namedFormula = new NamedFormulas(script);
            var formulas = namedFormula.EnsureParsed();
            Assert.NotNull(formulas);
            Assert.Equal(formulas.Count(), count);
        }

        [Theory]
        [InlineData("x=")]
        public void EnsureParsedWithErrorsTest(string script)
        {
            var namedFormula = new NamedFormulas(script);
            var formulas = namedFormula.EnsureParsed();
            Assert.Empty(formulas);            
        }

        [Theory]
        [InlineData("x=")]
        public void GetParseErrorsTest(string script)
        {
            var namedFormula = new NamedFormulas(script);
            namedFormula.EnsureParsed();

            var errors = namedFormula.GetParseErrors();
            Assert.NotEmpty(errors);
        }

        [Theory]
        [InlineData("x=1;")]
        public void GetParseErrorsNoErrorsTest(string script)
        {
            var namedFormula = new NamedFormulas(script);
            namedFormula.EnsureParsed();

            var errors = namedFormula.GetParseErrors();
            Assert.Empty(errors);
        }

        [Theory]
        [InlineData("x=1;y=2;", "1", "2")]
        public void GetNamedFormulasTest(string script, string expectedX, string expectedY)
        {
            var namedFormula = new NamedFormulas(script);
            var formulas = namedFormula.EnsureParsed();
            formulas.OrderBy(formula => formula.formula.Script);

            Assert.NotNull(formulas);

            Assert.Equal(expectedX, formulas.ElementAt(0).formula.Script);
            Assert.Equal(expectedY, formulas.ElementAt(1).formula.Script);
        }
    }
}
