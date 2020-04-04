using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace IL
{
    public enum State
    {
        Pre, Post, End
    }

    public class Token
    {
        public enum Type
        {
            Name,
            Number,

            UnaryOperator,
            BinaryOperator,
            Comma,
            LeftPar,
            RightPar,
            LeftBrace,
            RightBrace,
            Assign,
            Semicolon,

            Var,
            Return,
            If,
            Else,

            Expression,

            VarDefStatement,
            VarAssignStatement,
            ReturnStatement,
            IfStatement,
            ExprOrBlockStatement,

            Statement,
        }

        public delegate Token Factory(string str);

        public readonly Type type;
        public readonly string str;
        public readonly object data;

        public Token(Type type, string str, object data = null)
        {
            this.type = type;
            this.str = str;
            this.data = data;
        }

        public T GetData<T>()
        {
            if (data is T dataT)
            {
                return dataT;
            }
            return default;
        }

        public bool IsOfType(System.Type type)
        {
            return data.GetType().IsSubclassOf(type) || data.GetType() == type;
        }
    }

    public class BaseLexer<StateT> where StateT : Enum
    {
        protected static readonly IMatchable
            matchableComma = new MatchableRegex(new Regex(@"\G,"), Token.Type.Comma,
               str => new Token(Token.Type.Comma, str)),
            matchableName = new MatchableRegex(MarkedString.nameRegex, Token.Type.Name,
               str => new Token(Token.Type.Name, str)),
            matchableLeftPar = new MatchableRegex(new Regex(@"\G\("), Token.Type.LeftPar,
                str => new Token(Token.Type.LeftPar, str)),
            matchableRightPar = new MatchableRegex(new Regex(@"\G\)"), Token.Type.RightPar,
                str => new Token(Token.Type.RightPar, str)),
            matchableSemicolon = new MatchableRegex(MarkedString.semicolonRegex,
                Token.Type.Semicolon, str => new Token(Token.Type.Semicolon, str));


        private StateT state;
        private readonly StateT endState;
        private readonly Dictionary<StateT, List<IMatchable>> matchablesByState =
            new Dictionary<StateT, List<IMatchable>>();

        public MarkedString Source
        {
            get;
        }

        public BaseLexer(
            (Token.Type type, StateT before)[] statesForTypes,
            IMatchable[] matchables,
            StateT begin, StateT end,
            MarkedString source
            )
        {
            Dictionary<Token.Type, List<StateT>> edgeBegins =
                new Dictionary<Token.Type, List<StateT>>();
            Array.ForEach(statesForTypes, ((Token.Type type, StateT before) tuple) =>
            {
                if (!edgeBegins.ContainsKey(tuple.type))
                { edgeBegins.Add(tuple.type, new List<StateT>()); }
                edgeBegins[tuple.type].Add(tuple.before);
            });
            foreach (var matchable in matchables)
            {
                foreach (var before in edgeBegins[matchable.TokenType])
                {
                    if (!matchablesByState.ContainsKey(before))
                    { matchablesByState.Add(before, new List<IMatchable>()); }
                    matchablesByState[before].Add(matchable);
                }
            }

            state = begin;
            endState = end;
            Source = source;
        }

        public void SetState(StateT newState)
        {
            state = newState;
            RevertPeek();
        }

        private Token cachedToken;

        private void CacheToken()
        {
            if (state.Equals(endState) || cachedToken != null)
            { return; }

            Source.SkipWhitespaces();
            IMatchable match = null;
            int l = -1;
            foreach (var matchable in matchablesByState[state])
            {
                match = matchable;
                l = match.Match(Source);
                if (l >= 0)
                { break; }
            }
            if (l < 0)
            { cachedToken = null; }
            else
            {
                cachedToken = match.CreateLastMatch();
                Source.Skip(l);
            }
        }

        public Token PeekNextToken()
        {
            CommitPeek();
            CacheToken();
            return cachedToken;
        }

        public void CommitPeek()
        {
            Source.CommitCachedPos();
            cachedToken = null;
        }
        public void RevertPeek()
        {
            Source.RevertCachedPos();
            cachedToken = null;
        }

        public ParserException GetLastTokenError(ListTokenTester tester)
        {
            string message = "caused by:\n";
            bool foundToken = false;
            matchablesByState[state].ForEach(matchable =>
            {
                if (tester.Test(matchable.TokenType))
                {
                    message += matchable.LastError + "\n";
                    foundToken = true;
                }
            });
            return Source.Error(
                foundToken ?
                    (ParserException.Factory)((at, message) =>
                        new UnrecognizedTokenException(at, message)) :
                    (at, message) => new UnexpectedTokenException(at, message),
                message
                );
        }
    }
}
