using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace IL
{
    public class VarDefStatement : IBaseStatement
    {
        public readonly List<string> names;

        public VarDefStatement(List<string> names) =>
            this.names = names;

        public void Emit(ILGenerator generator) =>
            names.ForEach(name => generator.DeclareLocal(typeof(long)));

        public bool CheckReturn() => false;
    }

    public class VarAssignStatement : IBaseStatement
    {
        private readonly Variable lhs;
        private readonly IBaseExpression rhs;
        public VarAssignStatement(Variable lhs, IBaseExpression rhs)
        {
            IBaseExpression.CheckTypes(
                "assignment to " + lhs.ToString(), "",
                typeof(long), rhs.GetResultType()
                );
            this.lhs = lhs;
            this.rhs = rhs;
        }

        public void Emit(ILGenerator generator)
        {
            rhs.Emit(generator);
            lhs.EmitStore(generator);
        }

        public bool CheckReturn() => false;
    }

    public class ReturnStatement : IBaseStatement
    {
        private readonly IBaseExpression expr;

        public ReturnStatement(IBaseExpression expr)
        {
            IBaseExpression.CheckTypes(
                "return statement", "",
                typeof(long), expr.GetResultType()
                );
            this.expr = expr;
        }

        public void Emit(ILGenerator generator)
        {
            expr.Emit(generator);
            generator.Emit(OpCodes.Ret);
        }

        public bool CheckReturn() => true;
    }

    public class ExpressionStatement : IBaseStatement
    {
        private readonly IBaseExpression expr;

        public ExpressionStatement(IBaseExpression expr) => this.expr = expr;

        public void Emit(ILGenerator generator)
        {
            if (expr != null)
            {
                expr.Emit(generator);
                if (expr.GetResultType() != typeof(void))
                { generator.Emit(OpCodes.Pop); }
            }
        }

        public bool CheckReturn() => false;
    }

    public class ExprOrBlockStatement : IBaseStatement
    {
        private readonly List<IBaseStatement> blockables;

        public ExprOrBlockStatement(List<IBaseStatement> blockables) =>
            this.blockables = blockables;

        public void Emit(ILGenerator generator) =>
            blockables.ForEach(x => x.Emit(generator));

        public bool CheckReturn()
        {
            bool res = false;
            foreach (var stt in blockables)
            {
                if (stt.CheckReturn())
                { res = true; break; }
            }
            return res;
        }
    }

    public class IfStatement : IBaseStatement
    {
        private readonly IBaseExpression condition;
        private readonly IBaseStatement ifBranch, elseBranch;
        private readonly bool? precalc;

        public IfStatement(
            IBaseExpression condition,
            IBaseStatement ifBranch,
            IBaseStatement elseBranch = null
            )
        {
            IBaseExpression.CheckTypes(
                "if condition", "",
                typeof(bool), condition.GetResultType()
                );
            this.condition = condition;
            precalc = (bool?)condition.Evaluate();
            this.ifBranch = ifBranch;
            this.elseBranch = elseBranch;
        }

        public void Emit(ILGenerator generator)
        {
            if (precalc.HasValue)
            {
                // precalculated
                if (precalc.Value)
                { ifBranch.Emit(generator); }
                else if (elseBranch != null)
                { elseBranch.Emit(generator); }
            }
            else
            {
                // general case
                var endif = generator.DefineLabel();
                condition.Emit(generator);
                generator.Emit(OpCodes.Brfalse, endif);
                ifBranch.Emit(generator);
                if (elseBranch == null)
                { generator.MarkLabel(endif); }
                else
                {
                    var endelse = generator.DefineLabel();
                    generator.Emit(OpCodes.Br, endelse);
                    generator.MarkLabel(endif);
                    elseBranch.Emit(generator);
                    generator.MarkLabel(endelse);
                }
            }
        }

        public bool CheckReturn()
        {
            if (precalc.HasValue)
            {
                // precalculated
                if (precalc.Value)
                { return ifBranch.CheckReturn(); }
                else if (elseBranch != null)
                { return elseBranch.CheckReturn(); }
                else
                { return false; }
            }
            // general case
            return ifBranch.CheckReturn() && elseBranch != null && elseBranch.CheckReturn();
        }
    }
}
