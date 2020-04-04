using System;
using System.Text.RegularExpressions;
using System.Linq;

namespace IL
{
    public class MarkedString
    {
        private readonly string data;
        private int pos = 0, cachedPos = 0;
        private static readonly Regex wsRegex = new Regex(@"\G\s+");

        public static Regex
            nameRegex = new Regex(@"\G[a-zA-Z_][a-zA-Z_0-9]*"),
            numberRegex = new Regex(@"\G-?[0-9]+"),
            semicolonRegex = new Regex(@"\G;"),
            emptyMatchRegex = new Regex(@"\G");

        public int Line
        {
            get;
            private set;
        }
        public int CharInLine
        {
            get;
            private set;
        }
        public int CachedLine
        {
            get;
            private set;
        }
        public int CachedCharInLine
        {
            get;
            private set;
        }

        private void FillCachedPoses(string added)
        {
            int addedLines = added.Count(ch => ch == '\n');
            CachedLine += addedLines;
            if (addedLines > 0)
            {
                CachedCharInLine = 0;
            }
            CachedCharInLine += added.Aggregate(
                0,
                (fromLastLS, ch) => ch == '\n' ? 0 : fromLastLS + 1
                );
            cachedPos += added.Length;
        }

        public MarkedString(string data)
        {
            this.data = data;
        }

        public void RevertToCopy(MarkedString other)
        {
            pos = other.pos;
            cachedPos = other.cachedPos;
            Line = other.Line;
            CachedLine = other.CachedLine;
            CharInLine = other.CharInLine;
            CachedCharInLine = other.CachedCharInLine;
        }

        public Match MatchOne(Regex regex)
        { return regex.Match(data, cachedPos); }

        public void CommitCachedPos()
        {
            pos = cachedPos;
            Line = CachedLine;
            CharInLine = CachedCharInLine;
        }

        public void RevertCachedPos()
        {
            cachedPos = pos;
            CachedLine = Line;
            CachedCharInLine = CharInLine;
        }

        public void SkipWhitespaces()
        {
            FillCachedPoses(wsRegex.Match(data, cachedPos).Value);
        }

        public void Skip(int x)
        {
            FillCachedPoses(data.Substring(cachedPos, x));
        }

        public bool CommitAndCheckIsEnd()
        {
            SkipWhitespaces();
            CommitCachedPos();
            return pos == data.Length;
        }

        public MarkedString ShallowCopy()
        {
            return (MarkedString)MemberwiseClone();
        }

        public string At
        {
            get => CachedLine + ":" + CachedCharInLine;
        }

        public int AbsoluteAt
        {
            get => cachedPos;
        }

        public ParserException Error(ParserException.Factory factory, string message)
        {
            return factory(At, message);
        }
    }
}
