using System;
using System.Collections.Generic;
using System.Text;

namespace IL
{
    public class ListTokenTester
    {
        public Token.Type[] TestedTypes
        {
            get;
        }

        public ListTokenTester(Token.Type[] acceptableTokens)
        {
            TestedTypes = acceptableTokens;
        }
        public ListTokenTester(Token.Type acceptableToken) : this(new[] { acceptableToken }) { }

        public ListTokenTester(ListTokenTester other, Token.Type[] addTokens)
        {
            var list = new List<Token.Type>(other.TestedTypes);
            list.AddRange(addTokens);
            TestedTypes = list.ToArray();
        }

        public ListTokenTester(ListTokenTester other, Token.Type addToken) :
            this(other, new[] { addToken })
        { }

        public bool Test(Token.Type type)
        {
            bool res = false;
            foreach (Token.Type token in TestedTypes)
            {
                res |= token == type;
            }
            return res;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("[");
            for (int i = 0; i < TestedTypes.Length; i++)
            {
                sb.Append(TestedTypes[i].ToString());
                if (i != TestedTypes.Length - 1)
                {
                    sb.Append(", ");
                }
            }
            return sb.Append("]").ToString();
        }
    }
}
