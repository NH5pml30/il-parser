using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace IL
{
    public interface IEmittable
    {
        void Emit(ILGenerator generator);

        delegate void Emitter(ILGenerator generator);

        static Emitter EmitterFromOpCode(OpCode opCode)
        {
            return gen => gen.Emit(opCode);
        }
    }

    public interface IProgramElement : IEmittable
    {
    }

    public interface IBaseExpression : IProgramElement
    {
        Type GetResultType();
        static void CheckTypes(string funcName, string message, Type expected, Type got)
        {
            if (expected != got)
            {
                throw new TypeMismatchException(
                    funcName, message,
                    expected, got);
            }
        }

        object Evaluate();
    }

    public interface IBaseStatement : IProgramElement
    {
        public delegate IBaseStatement Factory(
            MarkedString source,
            Dictionary<string, Variable> vars,
            Dictionary<string, MethodOperation.Factory> funcs
            );

        bool CheckReturn();
    }
}
