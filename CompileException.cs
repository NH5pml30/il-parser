using System;
using System.Collections.Generic;
using System.Text;

namespace IL
{
    public class CompileException : Exception
    {
        public string MessageEssence { get; }
        public CompileException(string message) :
            base("Error while compiling: " + message)
        { MessageEssence = message; }
        public CompileException(string message, Exception inner) :
            base("Error while compiling: " + message, inner)
        { MessageEssence = message; }
    }

    public class ParserException : CompileException
    {
        public delegate ParserException Factory(string at, string message);

        public ParserException(string at, string message) :
            base("cannot parse expression at " + at + ": " + message) { }
        public ParserException(string at, string message, Exception inner) :
            base("cannot parse expression at " + at + ": " + message, inner) { }
    }

    public class UnexpectedTokenException : ParserException
    {
        public UnexpectedTokenException(string at, string message) :
            base(at, "unexpected token: " + message) { }
    }

    public class UnrecognizedTokenException : ParserException
    {
        public UnrecognizedTokenException(string at, string message) :
            base(at, "unrecognized token: " + message) { }
    }

    public class UnrecognizedStatementException : ParserException
    {
        public UnrecognizedStatementException(string at, string message) :
            base(at, "unrecognized statement: " + message) { }
    }

    public class TypeMismatchException : CompileException
    {
        public Type ExpectedType { get; }
        public Type GotType { get; }
        public string FunctionName { get; }
        public TypeMismatchException(string funcName, string message, Type expected, Type got) :
            base(
                "type mismatch for " + funcName + (message == "" ? "" : "(" + message + ")") +
                ": expected '" + expected.Name +
                "', got '" + got.Name + "'"
                )
        {
            ExpectedType = expected;
            GotType = got;
            FunctionName = funcName;
        }
    }

    public class TypeMismatchParserException : ParserException
    {
        public Type ExpectedType { get; }
        public Type GotType { get; }
        public string FunctionName { get; }
        public TypeMismatchParserException(string at, TypeMismatchException other) :
            base(at, other.MessageEssence, other)
        {
            ExpectedType = other.ExpectedType;
            GotType = other.GotType;
            FunctionName = other.FunctionName;
        }
    }

    public class ArgumentsNumberMismatchException : CompileException
    {
        public int ExpectedArgumentsNumber { get; }
        public int GotArgumentsNumber { get; }
        public string FunctionName { get; }
        public ArgumentsNumberMismatchException(string funcName, int expected, int got) :
            base(
                "arguments number mismatch for " + funcName +
                ": expected '" + expected +
                "', got '" + got + "'"
                )
        {
            ExpectedArgumentsNumber = expected;
            GotArgumentsNumber = got;
            FunctionName = funcName;
        }
    }

    public class ArgumentsNumberMismatchParserException : ParserException
    {
        public int ExpectedArgumentsNumber { get; }
        public int GotArgumentsNumber { get; }
        public string FunctionName { get; }
        public ArgumentsNumberMismatchParserException(
            string at,
            ArgumentsNumberMismatchException other
            ) : base(at, other.MessageEssence, other)
        {
            ExpectedArgumentsNumber = other.ExpectedArgumentsNumber;
            GotArgumentsNumber = other.GotArgumentsNumber;
            FunctionName = other.FunctionName;
        }
    }

    public class UnsupportedIdentifierException : ParserException
    {
        public string Identifier { get; }
        public UnsupportedIdentifierException(string at, string identifier) :
            base(at, "unsupported identifier '" + identifier + "' (matches keyword)")
        {
            Identifier = identifier;
        }
    }
}
