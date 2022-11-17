// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Microsoft.PowerFx.Interpreter.Solver;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    internal class SolverObjectVariable<T> : ExternalObject<T>, ISolverObjectVariable
    {
        private static object objectNextIndex = new BigInteger();

        private string _variableName = null;

        public SolverObjectVariable(T value)
            : this(value, null)
        {
        }

        public SolverObjectVariable(T value, string variableName)
            : base(value)
        {
            _variableName = variableName;
        }

        public T Value { get; set; }

        public string GetFieldName()
        {
            return nameof(Value);
        }

        public string GetVariableName()
        {
            if (_variableName == null) 
            {
                lock (objectNextIndex)
                {
                    _variableName = $"object{objectNextIndex}";
                    objectNextIndex = ((BigInteger)objectNextIndex) + 1;
                }
            }

            return _variableName;
        }
    }

    public class ExternalObject<T> : IExternalObject
    {
        private readonly T _value;
        private readonly FormulaType _formulaType;
        private readonly Array _array;
        private readonly bool _valueBool;
        private readonly double _valueDouble;

        public ExternalObject(T value)
        {
            _value = value;

            if (_value is ValueType)
            {
                switch (_value)
                {
                    case int _:
                    case short _:
                    case long _:
                    case float _:
                    case double _:
                        _formulaType = FormulaType.Number;
                        _valueDouble = Convert.ToDouble(_value);
                        break;
                    case bool _:
                        _formulaType = FormulaType.Boolean;
                        _valueBool = Convert.ToBoolean(_value);
                        break;
                }
            }
            else if (_value is Array)
            {
                _formulaType = ExternalType.ArrayType;
                _array = _value as Array;
            }
            else if (_value is string)
            {
                _formulaType = FormulaType.String;
            }
            else
            {
                _formulaType = ExternalType.ObjectType;
            }
        }

        public IUntypedObject this[int index] => new SolverObjectVariable<object>(_array.GetValue(index));

        public FormulaType Type => _formulaType;

        public int GetArrayLength()
        {
            if (_formulaType == ExternalType.ArrayType)
            {
                return _array.Length;
            }

            throw new InvalidOperationException();
        }

        public bool GetBoolean()
        {
            if (_formulaType == FormulaType.Boolean)
            {
                return _valueBool;
            }

            throw new InvalidOperationException();
        }

        public double GetDouble()
        {
            if (_formulaType == FormulaType.Number)
            {
                return _valueDouble;
            }

            throw new InvalidOperationException();
        }

        public string GetString()
        {
            if (_formulaType == FormulaType.String)
            {
                return _value as string;
            }

            throw new InvalidOperationException();
        }

        public virtual bool TryGetProperty(string name, out IUntypedObject result)
        {
            var properties = _value.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            var property = properties.FirstOrDefault(p => p.Name.Equals(name));
            if (property == null)
            {
                result = null;
                return false;
            }

            var propertyValue = property.GetValue(_value);
            
            // Find if the property being retrieved is the field mapped to a solver variable
            if (_value is ISolverObjectVariable solverVariable &&
                solverVariable.GetFieldName().Equals(name, StringComparison.InvariantCultureIgnoreCase))
            {
                result = new SolverObjectVariable<object>(propertyValue, solverVariable.GetVariableName());
            }
            else
            {
                result = new ExternalObject<object>(propertyValue);
            }

            return true;
        }

        public override string ToString()
        {
            return $"{_formulaType}({_value})";
        }

        public object ToObject()
        {
            return _value;
        }
    }
}
