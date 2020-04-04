using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using System.Linq.Expressions;

namespace IL
{
    public class OperationTypeDescriptor
    {
        public Type RetType
        {
            get;
        }
        public Type[] ArgTypes
        {
            get;
        }

        public OperationTypeDescriptor(Type retType, Type[] argTypes)
        {
            RetType = retType;
            ArgTypes = argTypes;
        }

        public OperationTypeDescriptor(Type retType) : this(retType, new Type[0])
        {
        }
    }
    public abstract class BaseOperation : IBaseExpression
    {
        protected readonly IEmittable.Emitter emitter;
        protected readonly IBaseExpression[] args;
        protected readonly OperationTypeDescriptor descriptor;
        protected readonly string name;
        protected readonly Delegate evaluator;

        public BaseOperation(
            string name,
            IEmittable.Emitter emitter,
            OperationTypeDescriptor descriptor,
            Delegate evaluator = null
            ) : this(name, emitter, descriptor, new IBaseExpression[0], evaluator)
        {
        }

        public BaseOperation(
            string name,
            IEmittable.Emitter emitter,
            OperationTypeDescriptor descriptor,
            IBaseExpression[] args,
            Delegate evaluator = null
            )
        {
            this.evaluator = evaluator;
            if (args.Length != descriptor.ArgTypes.Length)
            {
                throw new ArgumentsNumberMismatchException(
                    name,
                    descriptor.ArgTypes.Length, args.Length
                    );
            }
            if (args != null)
            {
                int i = 0;
                Array.ForEach(args,
                    arg => IBaseExpression.CheckTypes(
                        name, "operation argument #" + i,
                        descriptor.ArgTypes[i], arg.GetResultType()
                        )
                    );
            }
            this.name = name;
            this.descriptor = descriptor;
            this.emitter = emitter;
            this.args = args;
        }

        virtual public void Emit(ILGenerator generator)
        {
            if (args != null)
            {
                foreach (IBaseExpression arg in args)
                {
                    arg.Emit(generator);
                }
            }
            emitter(generator);
        }

        public Type GetResultType()
        {
            return descriptor.RetType;
        }

        virtual public object Evaluate()
        {
            if (evaluator != null)
            {
                bool success = true;
                object[] evalArgs = new object[descriptor.ArgTypes.Length];
                int i = 0;
                foreach (var arg in args)
                {
                    var res = arg.Evaluate();
                    if (res == null)
                    {
                        success = false;
                        break;
                    }
                    evalArgs[i++] = res;
                }
                if (success)
                { return evaluator.DynamicInvoke(evalArgs); }
            }
            return null;
        }
    }

    public class UnaryOperation : BaseOperation, IBaseExpression
    {
        public enum Type
        {
            Negate,
            Plus,
            LogicalNot
        }

        public static IEmittable.Emitter Type2Emitter(Type type)
        {
            return type switch
            {
                Type.Negate => IEmittable.EmitterFromOpCode(OpCodes.Neg),
                Type.Plus => gen => { },
                Type.LogicalNot => gen =>
                {
                    gen.Emit(OpCodes.Ldc_I4_1);
                    gen.Emit(OpCodes.Sub);
                },
                _ => throw new InvalidEnumArgumentException("Invalid type for unary operation")
            };
        }

        public static OperationTypeDescriptor Type2OperDescr(Type type)
        {
            return type switch
            {
                Type.Negate => GetDescriptor<long, long>(),
                Type.Plus => GetDescriptor<long, long>(),
                Type.LogicalNot => GetDescriptor<bool, bool>(),
                _ => throw new InvalidEnumArgumentException("Invalid type for unary operation")
            };
        }

        public static Delegate Type2Delegate(Type type)
        {
            return type switch
            {
                Type.Negate => (Func<long, long>)(x => -x),
                Type.Plus => (Func<long, long>)(x => x),
                Type.LogicalNot => (Func<bool, bool>)(x => !x),
                _ => throw new InvalidEnumArgumentException("Invalid type for unary operation")
            };
        }

        public static OperationTypeDescriptor GetDescriptor<Out, Arg>()
        {
            return new OperationTypeDescriptor(typeof(Out), new[] { typeof(Arg) });
        }

        public static Token.Factory GetFactory(Type type)
        {
            return str => new Token(Token.Type.UnaryOperator, str, type);
        }

        public UnaryOperation(Type type, IBaseExpression arg) :
            base(
                "operator " + type.ToString(),
                Type2Emitter(type),
                Type2OperDescr(type),
                new IBaseExpression[] { arg },
                Type2Delegate(type)
                )
        {
        }
    }

    public class BinaryOperation : BaseOperation, IBaseExpression
    {
        public enum Type
        {
            Add,
            Subtract,
            Multiply,
            Divide,
            LogicalAnd,
            LogicalOr,
            Lower,
            Greater,
            LowerEq,
            GreaterEq,
            Eq,
            Neq
        }

        public static IEmittable.Emitter Type2Emitter(Type type)
        {
            return type switch
            {
                Type.Add => IEmittable.EmitterFromOpCode(OpCodes.Add),
                Type.Subtract => IEmittable.EmitterFromOpCode(OpCodes.Sub),
                Type.Multiply => IEmittable.EmitterFromOpCode(OpCodes.Mul),
                Type.Divide => IEmittable.EmitterFromOpCode(OpCodes.Div),
                Type.LogicalAnd => IEmittable.EmitterFromOpCode(OpCodes.And),
                Type.LogicalOr => IEmittable.EmitterFromOpCode(OpCodes.Or),
                Type.Greater => IEmittable.EmitterFromOpCode(OpCodes.Cgt),
                Type.Lower => IEmittable.EmitterFromOpCode(OpCodes.Clt),
                Type.Eq => IEmittable.EmitterFromOpCode(OpCodes.Ceq),
                Type.GreaterEq => gen =>
                {
                    gen.Emit(OpCodes.Clt);
                    gen.Emit(OpCodes.Ldc_I4_1);
                    gen.Emit(OpCodes.Sub);
                },
                Type.LowerEq => gen =>
                {
                    gen.Emit(OpCodes.Cgt);
                    gen.Emit(OpCodes.Ldc_I4_1);
                    gen.Emit(OpCodes.Sub);
                },
                Type.Neq => gen =>
                {
                    gen.Emit(OpCodes.Ceq);
                    gen.Emit(OpCodes.Ldc_I4_1);
                    gen.Emit(OpCodes.Sub);
                },
                _ => throw new InvalidEnumArgumentException("Invalid type for binary operation")
            };
        }

        public static OperationTypeDescriptor Type2OperDescr(Type type)
        {
            return type switch
            {
                Type.Add => GetDescriptor<long, long, long>(),
                Type.Subtract => GetDescriptor<long, long, long>(),
                Type.Multiply => GetDescriptor<long, long, long>(),
                Type.Divide => GetDescriptor<long, long, long>(),
                Type.Lower => GetDescriptor<bool, long, long>(),
                Type.Greater => GetDescriptor<bool, long, long>(),
                Type.LowerEq => GetDescriptor<bool, long, long>(),
                Type.GreaterEq => GetDescriptor<bool, long, long>(),
                Type.Eq => GetDescriptor<bool, long, long>(),
                Type.Neq => GetDescriptor<bool, long, long>(),
                Type.LogicalOr => GetDescriptor<bool, bool, bool>(),
                Type.LogicalAnd => GetDescriptor<bool, bool, bool>(),
                _ => throw new InvalidEnumArgumentException("Invalid type for binary operation")
            };
        }

        public static Delegate Type2Delegate(Type type)
        {
            return type switch
            {
                Type.Add => (Func<long, long, long>)((x, y) => x + y),
                Type.Subtract => (Func<long, long, long>)((x, y) => x - y),
                Type.Multiply => (Func<long, long, long>)((x, y) => x * y),
                Type.Divide => (Func<long, long, long>)((x, y) => x / y),
                Type.Lower => (Func<long, long, bool>)((x, y) => x < y),
                Type.Greater => (Func<long, long, bool>)((x, y) => x > y),
                Type.LowerEq => (Func<long, long, bool>)((x, y) => x <= y),
                Type.GreaterEq => (Func<long, long, bool>)((x, y) => x >= y),
                Type.Eq => (Func<long, long, bool>)((x, y) => x == y),
                Type.Neq => (Func<long, long, bool>)((x, y) => x != y),
                Type.LogicalOr => (Func<bool, bool, bool>)((x, y) => x || y),
                Type.LogicalAnd => (Func<bool, bool, bool>)((x, y) => x && y),
                _ => throw new InvalidEnumArgumentException("Invalid type for binary operation")
            };
        }

        public static OperationTypeDescriptor GetDescriptor<Out, Left, Right>()
        {
            return new OperationTypeDescriptor(typeof(Out), new[] { typeof(Left), typeof(Right) });
        }

        public delegate BinaryOperation Factory(IBaseExpression left, IBaseExpression right);

        public static Token.Factory GetFactory(Type type)
        {
            return type switch
            {
                Type.LogicalAnd => str => new Token(
                    Token.Type.BinaryOperator, str,
                    new KeyValuePair<Factory, Type>(
                        (left, right) => new LogicalAnd(left, right), type
                        )
                    ),
                Type.LogicalOr => str => new Token(
                    Token.Type.BinaryOperator, str,
                    new KeyValuePair<Factory, Type>(
                        (left, right) => new LogicalOr(left, right), type
                        )
                    ),
                _ => str => new Token(
                    Token.Type.BinaryOperator, str,
                    new KeyValuePair<Factory, Type>(
                        (left, right) => new BinaryOperation(type, left, right), type
                        )
                    )
            };
        }

        public BinaryOperation(Type type, IBaseExpression left, IBaseExpression right) :
            base(
                "operator " + type.ToString(),
                Type2Emitter(type),
                Type2OperDescr(type),
                new IBaseExpression[] { left, right },
                Type2Delegate(type)
                )
        {
        }
    }

    public class LogicalOr : BinaryOperation
    {
        public LogicalOr(IBaseExpression left, IBaseExpression right) :
            base(Type.LogicalOr, left, right)
        {
        }

        public override void Emit(ILGenerator generator)
        {
            args[0].Emit(generator);
            var endFastRet = generator.DefineLabel();
            generator.Emit(OpCodes.Brfalse, endFastRet);
            generator.Emit(OpCodes.Ldc_I4_1);
            var endOp = generator.DefineLabel();
            generator.Emit(OpCodes.Br, endOp);
            generator.MarkLabel(endFastRet);

            args[1].Emit(generator);
            generator.MarkLabel(endOp);
        }

        public override object Evaluate()
        {
            bool?
                left = (bool?)args[0].Evaluate(),
                right = (bool?)args[1].Evaluate();

            if (left.HasValue && right.HasValue)
            { return left.Value || right.Value; }

            if (left.HasValue && left.Value ||
                right.HasValue && right.Value)
            { return true; }

            return null;
        }
    }

    public class LogicalAnd : BinaryOperation
    {
        public LogicalAnd(IBaseExpression left, IBaseExpression right) :
            base(Type.LogicalAnd, left, right)
        {
        }

        public override void Emit(ILGenerator generator)
        {
            args[0].Emit(generator);
            var endFastRet = generator.DefineLabel();
            generator.Emit(OpCodes.Brtrue, endFastRet);
            generator.Emit(OpCodes.Ldc_I4_0);
            var endOp = generator.DefineLabel();
            generator.Emit(OpCodes.Br, endOp);
            generator.MarkLabel(endFastRet);

            args[1].Emit(generator);
            generator.MarkLabel(endOp);
        }

        public override object Evaluate()
        {
            bool?
                left = (bool?)args[0].Evaluate(),
                right = (bool?)args[1].Evaluate();

            if (left.HasValue && right.HasValue)
            { return left.Value && right.Value; }

            if (left.HasValue && !left.Value ||
                right.HasValue && !right.Value)
            { return false; }

            return null;
        }
    }

    public static class OperationTypeExtension
    {
        public static int GetPriority(this BinaryOperation.Type type)
        {
            return type switch
            {
                BinaryOperation.Type.LogicalOr => 6,
                BinaryOperation.Type.LogicalAnd => 5,
                BinaryOperation.Type.Eq => 4,
                BinaryOperation.Type.Neq => 4,
                BinaryOperation.Type.Lower => 3,
                BinaryOperation.Type.LowerEq => 3,
                BinaryOperation.Type.Greater => 3,
                BinaryOperation.Type.GreaterEq => 3,
                BinaryOperation.Type.Add => 2,
                BinaryOperation.Type.Subtract => 2,
                BinaryOperation.Type.Divide => 1,
                BinaryOperation.Type.Multiply => 1,
                _ => throw new InvalidEnumArgumentException("Unknown binary operation type")
            };
        }

        public static int GetPriority(this UnaryOperation.Type type)
        {
            return type switch
            {
                UnaryOperation.Type.Negate => 0,
                UnaryOperation.Type.Plus => 0,
                UnaryOperation.Type.LogicalNot => 0,
                _ => throw new InvalidEnumArgumentException("Unknown unary operation type")
            };
        }
    }

    public class MethodOperation : BaseOperation, IBaseExpression
    {
        public delegate MethodOperation Factory(IBaseExpression[] args);

        public MethodOperation(MethodInfo methodInfo, IBaseExpression[] args) :
            base(
                methodInfo.Name,
                gen => gen.EmitCall(OpCodes.Call, methodInfo, null),
                new OperationTypeDescriptor(
                    methodInfo.ReturnType,
                    Array.ConvertAll(methodInfo.GetParameters(), pInfo => pInfo.ParameterType)
                    ),
                args // No precalc because of possible side-effects
                )
        {
        }
    }

    public class Const : BaseOperation, IBaseExpression
    {
        private readonly object str;
        public Const(long value) :
            base(
                "constant",
                gen => gen.Emit(OpCodes.Ldc_I8, value),
                new OperationTypeDescriptor(typeof(long)),
                (Func<long>)(() => value)
                )
        {
            str = value;
        }

        public Const(bool value) :
            base(
                "constant",
                value ?
                    (IEmittable.Emitter)(gen => gen.Emit(OpCodes.Ldc_I4_1)) :
                    gen => gen.Emit(OpCodes.Ldc_I4_0),
                new OperationTypeDescriptor(typeof(bool)),
                (Func<bool>)(() => value)
                )
        {
            str = value;
        }

        public override string ToString()
        {
            return str.ToString();
        }
    }

    public class Variable : BaseOperation, IBaseExpression
    {
        private delegate void EmitWrite(ILGenerator generator);
        private readonly EmitWrite emitStore;

        public void EmitStore(ILGenerator generator)
        {
            emitStore(generator);
        }

        public Variable(FieldInfo fieldInfo) :
            base(
                "field '" + fieldInfo.Name + "'",
                gen => gen.Emit(OpCodes.Ldsfld, fieldInfo),
                new OperationTypeDescriptor(typeof(long))
                )
        {
            emitStore = gen => gen.Emit(OpCodes.Stsfld, fieldInfo);
        }

        public Variable(string name, int varIndex, bool isLocal) :
            base(
                "variable '" + name + "'",
                isLocal ?
                    (IEmittable.Emitter)(gen => gen.Emit(OpCodes.Ldloc, varIndex)) :
                    gen => gen.Emit(OpCodes.Ldarg, varIndex),
                new OperationTypeDescriptor(typeof(long))
                )
        {
            emitStore = gen => gen.Emit(isLocal ? OpCodes.Stloc : OpCodes.Starg, varIndex);
        }

        public override string ToString()
        {
            return name;
        }
    }
}
