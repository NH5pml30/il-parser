using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Reflection.Emit;

namespace IL
{
    public static class VarDefParser
    {
        public static VarDefStatement Parse(MarkedString source) =>
            new Parser(source).Parse();

        public static List<string> Keywords { get; } = new List<string>() { "var" };

        private class Parser : BaseParser<VarDefState, VarDefLexer>
        {
            public Parser(MarkedString source) :
                base(new VarDefLexer(source))
            {
            }

            public VarDefStatement Parse()
            {
                List<string> res = new List<string>();
                PeekToken();
                if (Test(Token.Type.Var, VarDefState.Pre))
                {
                    while (true)
                    {
                        Expect(Token.Type.Name, VarDefState.Post);
                        if (ProgramParser.IsKeyword(lastToken.str))
                        {
                            throw Error(
                                (at, message) => new UnsupportedIdentifierException(at, message),
                                lastToken.str
                                );
                        }
                        if (!res.Contains(lastToken.str))
                        { res.Add(lastToken.str); }
                        if (Test(Token.Type.Semicolon, VarDefState.End))
                        { break; }
                        Expect(Token.Type.Comma, VarDefState.Pre);
                    }
                    return new VarDefStatement(res);
                }
                return null;
            }
        }
    }

    public static class VarAssignParser
    {
        public static VarAssignStatement Parse(
            MarkedString source,
            Dictionary<string, Variable> vars,
            Dictionary<string, MethodOperation.Factory> funcs
            ) => new Parser(source, vars, funcs).Parse();

        private class Parser : BaseParser<VarAssignState, VarAssignLexer>
        {
            public Parser(
                MarkedString source,
                Dictionary<string, Variable> vars,
                Dictionary<string, MethodOperation.Factory> funcs
                ) :
                base(new VarAssignLexer(source, vars, funcs), vars, funcs)
            {
            }

            public VarAssignStatement Parse()
            {
                PeekToken();
                if (!Test(Token.Type.Name, VarAssignState.PreAssign))
                { return null; }
                string name = lastToken.str;
                if (!Test(Token.Type.Assign, VarAssignState.PostAssign))
                { return null; }
                Variable var = GetVar(name);
                Expect(Token.Type.Expression, VarAssignState.PostExpr);
                IBaseExpression expr = lastToken.GetData<IBaseExpression>();
                Expect(Token.Type.Semicolon, VarAssignState.End);
                return new VarAssignStatement(var, expr);
            }
        }
    }

    public static class ReturnParser
    {
        public static ReturnStatement Parse(
            MarkedString source,
            Dictionary<string, Variable> vars,
            Dictionary<string, MethodOperation.Factory> funcs
            ) => new Parser(source, vars, funcs).Parse();

        public static List<string> Keywords { get; } = new List<string>() { "return" };

        private class Parser : BaseParser<ReturnState, ReturnLexer>
        {
            public Parser(
                MarkedString source,
                Dictionary<string, Variable> vars,
                Dictionary<string, MethodOperation.Factory> funcs
                ) :
                base(new ReturnLexer(source, vars, funcs), vars, funcs)
            {
            }

            public ReturnStatement Parse()
            {
                PeekToken();
                if (!Test(Token.Type.Return, ReturnState.PreExpr))
                { return null; }
                Expect(Token.Type.Expression, ReturnState.PostExpr);
                IBaseExpression expr = lastToken.GetData<IBaseExpression>();
                Expect(Token.Type.Semicolon, ReturnState.End);
                return new ReturnStatement(expr);
            }
        }
    }

    public static class ExprOrBlockParser
    {
        public static ExprOrBlockStatement Parse(
            MarkedString source,
            Dictionary<string, Variable> vars,
            Dictionary<string, MethodOperation.Factory> funcs
            ) => new Parser(source, vars, funcs).Parse();

        private class Parser : BaseParser<ExprOrBlockState, ExprOrBlockLexer>
        {
            public Parser(
                MarkedString source,
                Dictionary<string, Variable> vars,
                Dictionary<string, MethodOperation.Factory> funcs
                ) :
                base(new ExprOrBlockLexer(source, vars, funcs), vars, funcs)
            {
            }

            public ExprOrBlockStatement Parse()
            {
                PeekToken();

                if (!Test(Token.Type.LeftBrace, ExprOrBlockState.InBlock))
                {
                    if (Test(Token.Type.Semicolon, ExprOrBlockState.End))
                    {
                        return new ExprOrBlockStatement(
                            new List<IBaseStatement>() { new ExpressionStatement(null) }
                            );
                    }
                    Expect(Token.Type.Expression, ExprOrBlockState.InExpr);
                    var res = lastToken.GetData<IBaseExpression>();
                    Expect(Token.Type.Semicolon, ExprOrBlockState.End);
                    return new ExprOrBlockStatement(new List<IBaseStatement>()
                        { new ExpressionStatement(res) });
                }

                List<IBaseStatement> contents = new List<IBaseStatement>();
                while (HasToken() && !TestNoShift(Token.Type.RightBrace))
                {
                    Expect(Token.Type.Statement, ExprOrBlockState.InBlock);
                    contents.Add(lastToken.GetData<IBaseStatement>());
                }
                Expect(Token.Type.RightBrace, ExprOrBlockState.End);
                return new ExprOrBlockStatement(contents);
            }
        }
    }

    public static class IfParser
    {
        public static IfStatement Parse(
            MarkedString source,
            Dictionary<string, Variable> vars,
            Dictionary<string, MethodOperation.Factory> funcs
            ) => new Parser(source, vars, funcs).Parse();

        public static List<string> Keywords { get; } = new List<string>(){ "if", "else" };

        private class Parser : BaseParser<IfState, IfLexer>
        {
            public Parser(
                MarkedString source,
                Dictionary<string, Variable> vars,
                Dictionary<string, MethodOperation.Factory> funcs
                ) :
                base(new IfLexer(source, vars, funcs), vars, funcs)
            {
            }

            public IfStatement Parse()
            {
                PeekToken();

                if (!Test(Token.Type.If, IfState.PreLeftPar) ||
                    !Test(Token.Type.LeftPar, IfState.PreExpr))
                { return null; }
                PeekToken(IfState.PostExpr);
                var cond = lastToken.GetData<IBaseExpression>();

                Expect(Token.Type.RightPar, IfState.PreBlock);
                PeekToken(IfState.PostBlock);
                IBaseStatement
                    ifBlock = lastToken.GetData<IBaseStatement>(),
                    elseBlock = null;

                if (Test(Token.Type.Else, IfState.PostElse))
                {
                    CommitPeek();
                    elseBlock = lastToken.GetData<IBaseStatement>();
                }
                else
                { SetState(IfState.End); }

                return new IfStatement(cond, ifBlock, elseBlock);
            }
        }
    }

    public static class StatementParser
    {
        public static IBaseStatement Parse(
            MarkedString source,
            Dictionary<string, Variable> vars,
            Dictionary<string, MethodOperation.Factory> funcs
            ) => new Parser(source, vars, funcs).Parse();

        private class Parser : BaseParser<StatementState, StatementLexer>
        {
            public Parser(
                MarkedString source,
                Dictionary<string, Variable> vars,
                Dictionary<string, MethodOperation.Factory> funcs
                ) :
                base(new StatementLexer(source, vars, funcs), vars, funcs)
            {
            }

            public IBaseStatement Parse()
            {
                PeekToken();
                if (!HasToken())
                { return null; }

                CommitPeek();
                SetState(StatementState.End);
                return lastToken.GetData<IBaseStatement>();
            }
        }
    }

    public static class ProgramParser
    {
        private static readonly List<Func<List<string>>> keywordsGetters =
            new List<Func<List<string>>>()
            {
                () => ExpressionParser.Keywords,
                () => VarDefParser.Keywords,
                () => ReturnParser.Keywords,
                () => IfParser.Keywords
            };
        private static readonly List<string> keywords = new List<string>();

        public static bool IsKeyword(string identifier) => keywords.Contains(identifier);

        static ProgramParser()
        {
            keywordsGetters.ForEach(getter => keywords.AddRange(getter()));
        }

        public static DelegateT Parse<DelegateT>(
            string sourceStr,
            Type context
            ) where DelegateT : Delegate
        {
            return new Parser<DelegateT>(
                sourceStr, context,
                new Dictionary<string, Variable>(),
                new Dictionary<string, MethodOperation.Factory>()
                ).Parse();
        }

        private class Parser<DelegateT> : BaseParser<ProgramState, ProgramLexer>
            where DelegateT : Delegate
        {
            private static readonly Dictionary<string, Variable> argVars =
                new Dictionary<string, Variable>();

            static Parser()
            {
                MethodInfo delegateInfo = typeof(DelegateT).GetMethod("Invoke");
                if (delegateInfo.ReturnType != typeof(long))
                {
                    throw new TypeMismatchException(
                        "<EntryPoint>", "return type",
                        typeof(long),
                        delegateInfo.ReturnType
                        );
                }
                int i = 0;
                Array.ForEach(
                    delegateInfo.GetParameters(),
                    parInfo =>
                    {
                        if (parInfo.ParameterType != typeof(long))
                        {
                            throw new TypeMismatchException(
                                "<EntryPoint>", "argument #" + i,
                                typeof(long),
                                parInfo.ParameterType
                                );
                        }
                        argVars[parInfo.Name] = new Variable(parInfo.Name, i++, false);
                    }
                    );
            }

            public Parser(
                string sourceStr,
                Type context,
                Dictionary<string, Variable> emptyVars,
                Dictionary<string, MethodOperation.Factory> emptyFuncs
                ) :
                base(new ProgramLexer(
                    new MarkedString(sourceStr),
                    emptyVars,
                    emptyFuncs
                    ), emptyVars, emptyFuncs)
            {
                foreach (var entry in argVars)
                { vars.Add(entry.Key, entry.Value); }

                Array.ForEach(
                    context.GetFields(
                        BindingFlags.Static |
                        BindingFlags.Public
                        ),
                    field => vars[field.Name] = new Variable(field)
                    );

                Array.ForEach(
                    context.GetMethods(
                        BindingFlags.Static |
                        BindingFlags.Public
                        ),
                    method => funcs[method.Name] = args => new MethodOperation(method, args)
                    );
            }

            public DelegateT Parse()
            {
                var method = new DynamicMethod(
                    "Evaluate",
                    typeof(long),
                    new Type[] { typeof(long), typeof(long), typeof(long) }
                );
                var generator = method.GetILGenerator();


                PeekToken();
                if (TestNoShift(Token.Type.VarDefStatement))
                {
                    VarDefStatement defStt = token.GetData<VarDefStatement>();
                    defStt.Emit(generator);
                    int i = 0;
                    defStt.names.ForEach(name => vars[name] = new Variable(name, i++, true));
                    PeekToken(ProgramState.AfterDef);
                }

                IBaseStatement stt = null;
                bool returns = false;
                while (HasToken())
                {
                    stt = token.GetData<IBaseStatement>();
                    returns |= stt.CheckReturn();
                    stt.Emit(generator);
                    if (lexer.Source.IsEnd())
                    { break; }
                    PeekToken(ProgramState.AfterDef);
                }
                if (!returns)
                {
                    throw new CompileException(
                        "end of function is reachable without any return statement"
                        );
                }
                if (!(stt is ReturnStatement))
                {
                    new ReturnStatement(new Const(0)).Emit(generator); // load dummy to not get error
                }
                return (DelegateT)method.CreateDelegate(typeof(DelegateT));
            }
        }
    }
}
