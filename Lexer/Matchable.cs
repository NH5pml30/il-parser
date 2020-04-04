using System;
using System.Text.RegularExpressions;

namespace IL
{
    public interface IMatchable
    {
        int Match(MarkedString source);

        string LastError { get; }
        Token.Type TokenType { get; }

        Token CreateLastMatch();
    }

    public class MatchableRegex : IMatchable
    {
        private readonly Regex regex;
        private string lastMatch = "";
        private readonly Token.Factory factory;

        public string LastError { get; private set; } = "";
        public Token.Type TokenType { get; }

        public MatchableRegex(Regex regex, Token.Type type, Token.Factory factory)
        {
            TokenType = type;
            this.regex = regex;
            this.factory = factory;
        }

        public int Match(MarkedString source)
        {
            var res = source.MatchOne(regex);
            if (!res.Success)
            { LastError = "no match for '" + regex.ToString() + "' regex"; }
            else
            { LastError = ""; }
            lastMatch = res.Value;
            return res.Success ? res.Length : -1;
        }

        public Token CreateLastMatch() => factory(lastMatch);
    }

    public class MatchableElement : IMatchable
    {
        private readonly ElementFactory factory;
        private IProgramElement lastMatch = null;
        private readonly string name;

        public string LastError { get; private set; } = "";
        public Token.Type TokenType { get; }

        public delegate IProgramElement ElementFactory(
            MarkedString source
            );

        public MatchableElement(
            Token.Type type,
            ElementFactory factory,
            string name
            )
        {
            TokenType = type;
            this.factory = factory;
            this.name = name;
        }

        public int Match(MarkedString source)
        {
            MarkedString saved = source.ShallowCopy();

            int last = source.AbsoluteAt;
            lastMatch = factory(source);
            if (lastMatch == null)
            {
                source.RevertToCopy(saved);
                LastError = "cannot parse " + name;
                return -1;
            }
            int res = source.AbsoluteAt - last;
            source.RevertToCopy(saved);
            return res;
        }

        public Token CreateLastMatch() => new Token(TokenType, "", lastMatch);
    }
}
