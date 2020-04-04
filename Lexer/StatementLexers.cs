using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace IL
{
    public class BaseStatementLexer<StateT> : BaseLexer<StateT> where StateT : Enum
    {
        protected delegate MatchableElement MatchableElementFactory(
            Dictionary<string, Variable> vars,
            Dictionary<string, MethodOperation.Factory> funcs
            );

        protected static MatchableElementFactory
            matchableExpr = (vars, funcs) =>
                new MatchableElement(Token.Type.Expression,
                    source => ExpressionParser.Parse(source, vars, funcs), "expression"),
            matchableVarDef = (vars, funcs) =>
                new MatchableElement(Token.Type.VarDefStatement,
                    VarDefParser.Parse, "variable definition statement"),
            matchableVarAssign = (vars, funcs) =>
                new MatchableElement(Token.Type.VarAssignStatement,
                    source => VarAssignParser.Parse(source, vars, funcs),
                        "variable assignment statement"),
            matchableReturnStt = (vars, funcs) =>
                new MatchableElement(Token.Type.ReturnStatement,
                    source => ReturnParser.Parse(source, vars, funcs),
                        "return statement"),
            matchableBlock = (vars, funcs) =>
                new MatchableElement(Token.Type.ExprOrBlockStatement,
                    source => ExprOrBlockParser.Parse(source, vars, funcs),
                        "block statement"),
            matchableIf = (vars, funcs) =>
                new MatchableElement(Token.Type.IfStatement,
                    source => IfParser.Parse(source, vars, funcs),
                        "if statement"),
            matchableStt = (vars, funcs) =>
                new MatchableElement(Token.Type.Statement,
                    source => StatementParser.Parse(source, vars, funcs),
                        "statement");


        protected static IMatchable[] AddExpressionMatch(
            IMatchable[] matchables,
            Dictionary<string, Variable> vars,
            Dictionary<string, MethodOperation.Factory> funcs,
            MatchableElementFactory[] factories
            )
        {
            var res = new List<IMatchable>(matchables);
            Array.ForEach(factories, factory => res.Add(factory(vars, funcs)));
            return res.ToArray();
        }

        protected BaseStatementLexer(
            (Token.Type type, StateT before)[] statesForTypes,
            IMatchable[] matchables,
            StateT begin, StateT end,
            MarkedString source,
            Dictionary<string, Variable> vars,
            Dictionary<string, MethodOperation.Factory> funcs,
            MatchableElementFactory[] factories
            ) :
            base(
                statesForTypes,
                AddExpressionMatch(matchables, vars, funcs, factories),
                begin, end, source
                )
        { }

        protected BaseStatementLexer(
            (Token.Type type, StateT before)[] statesForTypes,
            IMatchable[] matchables,
            StateT begin, StateT end,
            MarkedString source
            ) :
            base(statesForTypes, matchables, begin, end, source)
        { }
    }


    public enum VarDefState
    {
        Begin, Pre, Post, End
    }

    public class VarDefLexer : BaseLexer<VarDefState>
    {
        private static readonly (Token.Type type, VarDefState before)[]
            statesByType = new[]
            {
                (Token.Type.Var,       VarDefState.Begin),
                (Token.Type.Name,      VarDefState.Pre),
                (Token.Type.Comma,     VarDefState.Post),
                (Token.Type.Semicolon, VarDefState.Post),
            };

        private static readonly IMatchable[] matchables =
            new IMatchable[]
            {
                new MatchableRegex(new Regex(@"\Gvar\b"), Token.Type.Var,
                    str => new Token(Token.Type.Var, str)),
                matchableComma,
                matchableName,
                matchableSemicolon,
            };

        public VarDefLexer(MarkedString expr) :
            base(
                statesByType,
                matchables,
                VarDefState.Begin,
                VarDefState.End,
                expr
                )
        { }
    }

    public enum VarAssignState
    {
        Begin, PreAssign, PostAssign, PostExpr, End
    }

    public class VarAssignLexer : BaseStatementLexer<VarAssignState>
    {
        private static readonly (Token.Type type, VarAssignState before)[]
            statesByType = new[]
            {
                (Token.Type.Name,       VarAssignState.Begin),
                (Token.Type.Assign,     VarAssignState.PreAssign),
                (Token.Type.Expression, VarAssignState.PostAssign),
                (Token.Type.Semicolon,  VarAssignState.PostExpr),
            };

        private static readonly IMatchable[] matchables =
            new IMatchable[]
            {
                matchableName,
                new MatchableRegex(new Regex(@"\G="), Token.Type.Assign,
                    str => new Token(Token.Type.Assign, str)),
                matchableSemicolon,
            };

        public VarAssignLexer(
            MarkedString expr,
            Dictionary<string, Variable> vars,
            Dictionary<string, MethodOperation.Factory> funcs
            ) :
            base(
                statesByType,
                matchables,
                VarAssignState.Begin,
                VarAssignState.End,
                expr, vars, funcs,
                new[] { matchableExpr }
                )
        { }
    }

    public enum ReturnState
    {
        Begin, PreExpr, PostExpr, End
    }

    public class ReturnLexer : BaseStatementLexer<ReturnState>
    {
        private static readonly (Token.Type type, ReturnState before)[]
            statesByType = new[]
            {
                (Token.Type.Return,     ReturnState.Begin),
                (Token.Type.Expression, ReturnState.PreExpr),
                (Token.Type.Semicolon,  ReturnState.PostExpr),
            };

        private static readonly IMatchable[] matchables =
            new IMatchable[]
            {
                new MatchableRegex(new Regex(@"\Greturn\b"), Token.Type.Return,
                    str => new Token(Token.Type.Return, str)),
                matchableSemicolon,
            };

        public ReturnLexer(
            MarkedString expr,
            Dictionary<string, Variable> vars,
            Dictionary<string, MethodOperation.Factory> funcs
            ) :
            base(
                statesByType,
                matchables,
                ReturnState.Begin,
                ReturnState.End,
                expr, vars, funcs,
                new[] { matchableExpr }
                )
        { }
    }

    public enum ExprOrBlockState
    {
        Begin, InBlock, InExpr, End
    }

    public class ExprOrBlockLexer : BaseStatementLexer<ExprOrBlockState>
    {
        private static readonly (Token.Type type, ExprOrBlockState before)[]
            statesByType = new[]
            {
                (Token.Type.LeftBrace,  ExprOrBlockState.Begin),
                (Token.Type.Expression, ExprOrBlockState.Begin),
                (Token.Type.Semicolon,  ExprOrBlockState.Begin),
                (Token.Type.Semicolon,  ExprOrBlockState.InExpr),
                (Token.Type.Statement,  ExprOrBlockState.InBlock),
                (Token.Type.RightBrace, ExprOrBlockState.InBlock),
            };

        private static readonly IMatchable[] matchables =
            new IMatchable[]
            {
                matchableSemicolon,
                new MatchableRegex(new Regex(@"\G{"), Token.Type.LeftBrace,
                    str => new Token(Token.Type.LeftBrace, str)),
                new MatchableRegex(new Regex(@"\G}"), Token.Type.RightBrace,
                    str => new Token(Token.Type.RightBrace, str)),
            };

        public ExprOrBlockLexer(
            MarkedString expr,
            Dictionary<string, Variable> vars,
            Dictionary<string, MethodOperation.Factory> funcs
            ) :
            base(
                statesByType,
                matchables,
                ExprOrBlockState.Begin,
                ExprOrBlockState.End,
                expr, vars, funcs,
                new[] { matchableExpr, matchableStt }
                )
        { }
    }

    public enum IfState
    {
        Begin, PreLeftPar, PreExpr, PostExpr, PreBlock, PostBlock, PostElse, End
    }

    public class IfLexer : BaseStatementLexer<IfState>
    {
        private static readonly (Token.Type type, IfState before)[]
            statesByType = new[]
            {
                (Token.Type.If,         IfState.Begin),
                (Token.Type.LeftPar,    IfState.PreLeftPar),
                (Token.Type.Expression, IfState.PreExpr),
                (Token.Type.RightPar,   IfState.PostExpr),
                (Token.Type.Statement,  IfState.PreBlock),
                (Token.Type.Else,       IfState.PostBlock),
                (Token.Type.Statement,  IfState.PostElse),
            };

        private static readonly IMatchable[] matchables =
            new IMatchable[]
            {
                matchableLeftPar,
                matchableRightPar,
                new MatchableRegex(new Regex(@"\Gif\b"),
                    Token.Type.If, str => new Token(Token.Type.If, str)),
                new MatchableRegex(new Regex(@"\Gelse\b"),
                    Token.Type.Else, str => new Token(Token.Type.Else, str)),
            };

        public IfLexer(
            MarkedString expr,
            Dictionary<string, Variable> vars,
            Dictionary<string, MethodOperation.Factory> funcs
            ) :
            base(
                statesByType,
                matchables,
                IfState.Begin,
                IfState.End,
                expr, vars, funcs,
                new[] { matchableExpr, matchableStt }
                )
        { }
    }

    public enum StatementState
    {
        Begin, End
    }

    public class StatementLexer : BaseStatementLexer<StatementState>
    {
        private static readonly (Token.Type type, StatementState before)[]
            statesByType = new[]
            {
                (Token.Type.VarAssignStatement,   StatementState.Begin),
                (Token.Type.IfStatement,          StatementState.Begin),
                (Token.Type.ReturnStatement,      StatementState.Begin),
                (Token.Type.ExprOrBlockStatement, StatementState.Begin),
            };

        private static readonly IMatchable[] matchables =
            new IMatchable[]
            {
            };

        public StatementLexer(
            MarkedString expr,
            Dictionary<string, Variable> vars,
            Dictionary<string, MethodOperation.Factory> funcs
            ) :
            base(
                statesByType,
                matchables,
                StatementState.Begin,
                StatementState.End,
                expr, vars, funcs,
                new[] { matchableVarAssign, matchableReturnStt, matchableIf, matchableBlock }
                )
        { }
    }

    public enum ProgramState
    {
        Begin, AfterDef, End
    }

    public class ProgramLexer : BaseStatementLexer<ProgramState>
    {
        private static readonly (Token.Type type, ProgramState before)[]
            statesByType = new[]
            {
                (Token.Type.VarDefStatement, ProgramState.Begin),
                (Token.Type.Statement,       ProgramState.Begin),
                (Token.Type.Statement,       ProgramState.AfterDef),
            };

        private static readonly IMatchable[] matchables =
            new IMatchable[]
            {
            };

        public ProgramLexer(
            MarkedString expr,
            Dictionary<string, Variable> vars,
            Dictionary<string, MethodOperation.Factory> funcs
            ) :
            base(
                statesByType,
                matchables,
                ProgramState.Begin,
                ProgramState.End,
                expr, vars, funcs,
                new[] { matchableVarDef, matchableStt }
                )
        { }
    }
}
