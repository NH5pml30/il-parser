using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace IL
{
    public class ExpressionLexer : BaseLexer<State>
    {
        private static readonly (Token.Type type, State before)[] statesByType = new[]
        {
            (Token.Type.UnaryOperator,  State.Pre),
            (Token.Type.Name,           State.Pre),
            (Token.Type.Number,         State.Pre),
            (Token.Type.LeftPar,        State.Pre),

            (Token.Type.BinaryOperator, State.Post),
            (Token.Type.Comma,          State.Post),
            (Token.Type.RightPar,       State.Post),
        };

        public static readonly IMatchable[] matchables =
            new IMatchable[]
            {
                new MatchableRegex(new Regex(@"\G\+"), Token.Type.BinaryOperator,
                    BinaryOperation.GetFactory(BinaryOperation.Type.Add)),
                new MatchableRegex(new Regex(@"\G-"), Token.Type.BinaryOperator,
                    BinaryOperation.GetFactory(BinaryOperation.Type.Subtract)),
                new MatchableRegex(new Regex(@"\G/"), Token.Type.BinaryOperator,
                    BinaryOperation.GetFactory(BinaryOperation.Type.Divide)),
                new MatchableRegex(new Regex(@"\G\*"), Token.Type.BinaryOperator,
                    BinaryOperation.GetFactory(BinaryOperation.Type.Multiply)),
                new MatchableRegex(new Regex(@"\G&&"), Token.Type.BinaryOperator,
                    BinaryOperation.GetFactory(BinaryOperation.Type.LogicalAnd)),
                new MatchableRegex(new Regex(@"\G\|\|"), Token.Type.BinaryOperator,
                    BinaryOperation.GetFactory(BinaryOperation.Type.LogicalOr)),
                new MatchableRegex(new Regex(@"\G>="), Token.Type.BinaryOperator,
                    BinaryOperation.GetFactory(BinaryOperation.Type.GreaterEq)),
                new MatchableRegex(new Regex(@"\G<="), Token.Type.BinaryOperator,
                    BinaryOperation.GetFactory(BinaryOperation.Type.LowerEq)),
                new MatchableRegex(new Regex(@"\G>"), Token.Type.BinaryOperator,
                    BinaryOperation.GetFactory(BinaryOperation.Type.Greater)),
                new MatchableRegex(new Regex(@"\G<"), Token.Type.BinaryOperator,
                    BinaryOperation.GetFactory(BinaryOperation.Type.Lower)),
                new MatchableRegex(new Regex(@"\G=="), Token.Type.BinaryOperator,
                    BinaryOperation.GetFactory(BinaryOperation.Type.Eq)),
                new MatchableRegex(new Regex(@"\G!="), Token.Type.BinaryOperator,
                    BinaryOperation.GetFactory(BinaryOperation.Type.Neq)),
                new MatchableRegex(new Regex(@"\G\+"), Token.Type.UnaryOperator,
                    UnaryOperation.GetFactory(UnaryOperation.Type.Plus)),
                new MatchableRegex(new Regex(@"\G-"), Token.Type.UnaryOperator,
                    UnaryOperation.GetFactory(UnaryOperation.Type.Negate)),
                new MatchableRegex(new Regex(@"\G!"), Token.Type.UnaryOperator,
                    UnaryOperation.GetFactory(UnaryOperation.Type.LogicalNot)),
                new MatchableRegex(new Regex(@"\Gtrue"), Token.Type.Number,
                    str => new Token(Token.Type.Number, str, true)),
                new MatchableRegex(new Regex(@"\Gfalse"), Token.Type.Number,
                    str => new Token(Token.Type.Number, str, false)),
                matchableComma,
                matchableLeftPar,
                matchableRightPar,
                matchableName,
                new MatchableRegex(MarkedString.numberRegex, Token.Type.Number,
                    str => new Token(Token.Type.Number, str, long.Parse(str))),
            };

        public ExpressionLexer(MarkedString expr) :
            base(statesByType, matchables, State.Pre, State.End, expr) { }
    }
}
