using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;

namespace IL
{
    public static class ExpressionParser
    {
        public static IBaseExpression Parse(
            MarkedString expr,
            Dictionary<string, Variable> vars,
            Dictionary<string, MethodOperation.Factory> funcs
            )
        {
            return new Parser(expr, vars, funcs).Parse();
        }

        public static List<string> Keywords { get; } = new List<string>() { "true", "false" };

        private class Parser : BaseParser<State, ExpressionLexer>
        {
            public Parser(
                MarkedString source,
                Dictionary<string, Variable> vars,
                Dictionary<string, MethodOperation.Factory> funcs
                ) :
                base(new ExpressionLexer(source), vars, funcs)
            {
            }

            public IBaseExpression Parse()
            {
                PeekToken();
                IBaseExpression res;
                try
                {
                    res = ParseSubexpression(int.MaxValue);
                    RetrieveString();
                }
                catch (TypeMismatchException e)
                {
                    throw new TypeMismatchParserException(SourceAt, e);
                }
                catch (ArgumentsNumberMismatchException e)
                {
                    throw new ArgumentsNumberMismatchParserException(SourceAt, e);
                }
                return res;
            }

            private IBaseExpression ParseSubexpression(int lastPriority)
            {
                IBaseExpression left = null;
                if (Test(Token.Type.LeftPar, State.Pre))
                {
                    left = ParseSubexpression(int.MaxValue);
                    Expect(Token.Type.RightPar, State.Post);
                }
                else if (Test(Token.Type.Name, State.Pre))
                {
                    string name = lastToken.str;
                    // try read left_par
                    if (Test(Token.Type.LeftPar, State.Post))
                    {
                        // success - function call
                        List<IBaseExpression> args = new List<IBaseExpression>();
                        // try read right_par right now
                        if (!Test(Token.Type.RightPar, State.Post))
                        {
                            // some arguments, read them
                            RePeekToken(State.Pre);
                            while (true)
                            {
                                args.Add(ParseSubexpression(int.MaxValue));
                                if (Test(Token.Type.RightPar, State.Post))
                                { break; }
                                Expect(Token.Type.Comma, State.Pre);
                            }
                        }
                        left = GetFunc(name)(args.ToArray());
                    }
                    else
                    {
                        // otherwise - variable name
                        RePeekToken(State.Post);
                        left = GetVar(lastToken.str);
                    }
                }
                else if (Test(Token.Type.UnaryOperator, State.Pre))
                {
                    var oper = lastToken.GetData<UnaryOperation.Type>();
                    left = new UnaryOperation(
                        oper,
                        ParseSubexpression(oper.GetPriority())
                        );
                }
                else
                {
                    Expect(Token.Type.Number, State.Post);
                    if (lastToken.IsOfType(typeof(long)))
                    { left = new Const(lastToken.GetData<long>()); }
                    else
                    { left = new Const(lastToken.GetData<bool>()); }
                }

                while (true)
                {
                    if (!TestNoShift(Token.Type.BinaryOperator))
                    { break; }

                    (BinaryOperation.Factory factory, BinaryOperation.Type oper) =
                        token.GetData<KeyValuePair<BinaryOperation.Factory, BinaryOperation.Type>>();
                    if (oper.GetPriority() >= lastPriority)
                    {
                        break;
                    }
                    PeekToken(State.Pre);
                    left = factory(left, ParseSubexpression(oper.GetPriority()));
                }

                return left;
            }
        }
    }
}
