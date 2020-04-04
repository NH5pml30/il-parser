using System;
using System.Collections.Generic;

namespace IL
{
    public class BaseParser<StateT, LexerT>
        where StateT : Enum
        where LexerT : BaseLexer<StateT>
    {
        protected readonly Dictionary<string, Variable> vars;
        protected readonly Dictionary<string, MethodOperation.Factory> funcs;

        protected readonly LexerT lexer;
        protected Token token, lastToken;

        protected BaseParser(
            LexerT lexer,
            Dictionary<string, Variable> vars = null,
            Dictionary<string, MethodOperation.Factory> funcs = null
            )
        {
            this.vars = vars;
            this.funcs = funcs;
            this.lexer = lexer;
        }

        protected void SetState(StateT after)
        {
            token = null;
            lexer.SetState(after);
        }

        protected void RePeekToken()
        {
            lexer.RevertPeek();
            token = lexer.PeekNextToken();
        }
        protected void RePeekToken(StateT before)
        {
            SetState(before);
            token = lexer.PeekNextToken();
        }
        protected void PeekToken()
        {
            lastToken = token;
            lexer.CommitPeek();
            RePeekToken();
        }
        protected void PeekToken(StateT before)
        {
            lastToken = token;
            lexer.CommitPeek();
            RePeekToken(before);
        }

        protected bool HasToken()
        {
            return token != null;
        }

        protected bool TestNoShift(ListTokenTester tester) =>
            HasToken() && tester.Test(token.type);

        protected bool TestNoShift(Token.Type expected) =>
            TestNoShift(new ListTokenTester(expected));

        protected bool Test(ListTokenTester tester, StateT after)
        {
            if (TestNoShift(tester))
            {
                PeekToken(after);
                return true;
            }
            return false;
        }
        protected bool Test(Token.Type expected, StateT after) =>
            Test(new ListTokenTester(expected), after);

        protected void ExpectNoShift(ListTokenTester tester)
        {
            if (!TestNoShift(tester))
            { throw lexer.GetLastTokenError(tester); }
        }
        protected void ExpectNoShift(Token.Type expected) =>
            ExpectNoShift(new ListTokenTester(expected));

        protected void Expect(ListTokenTester tester, StateT after)
        {
            ExpectNoShift(tester);
            PeekToken(after);
        }
        protected void Expect(Token.Type expected, StateT after) =>
            Expect(new ListTokenTester(expected), after);

        protected void RetrieveString()
        {
            token = null;
            lexer.RevertPeek();
        }

        protected void CommitPeek()
        {
            lastToken = token;
            token = null;
            lexer.CommitPeek();
        }

        protected Variable GetVar(string name)
        {
            if (!vars.ContainsKey(name))
            {
                if (ProgramParser.IsKeyword(name))
                {
                    throw Error(
                        (at, message) => new UnexpectedTokenException(at, message),
                        "keyword '" + name + "', expected variable name"
                        );
                }
                throw Error(
                    (at, message) => new UnrecognizedTokenException(at, message),
                    "cannot find variable '" + name
                    );
            }
            return vars[name];
        }

        protected MethodOperation.Factory GetFunc(string name)
        {
            if (!funcs.ContainsKey(name))
            {
                if (ProgramParser.IsKeyword(name))
                {
                    throw Error(
                        (at, message) => new UnexpectedTokenException(at, message),
                        "keyword '" + name + "', expected function name"
                        );
                }
                throw Error(
                    (at, message) => new UnrecognizedTokenException(at, message),
                    "cannot find function '" + name + "' to call"
                    );
            }
            return funcs[name];
        }

        protected ParserException Error(ParserException.Factory factory, string message) =>
            lexer.Source.Error(factory, message);

        protected string SourceAt
        {
            get => lexer.Source.At;
        }
    }
}
