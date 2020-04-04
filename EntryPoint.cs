﻿using System;

namespace IL
{
    public class EntryPoint
    {
        public static class Context
        {
            public static long FooMethod(long a, long b) => a + b;
            public static long BarMethod() => 0;
            public static void DoNothing() { }
            public static long zero = 100;

            public static void PrintLine(long x) { Console.WriteLine(x); }
            public static void PrintLineb(bool x) { Console.WriteLine(x); }
            public static bool SetZero()
            {
                zero = 0;
                return true;
            }
        }

        delegate long CompileResult(long x, long y, long z);

        CompileResult Compile(string expression)
        {
            return ProgramParser.Parse<CompileResult>(expression, typeof(Context));
        }

        void Run()
        {
            var result = Compile(@"
                var zero, return0, afas;
                zero = 03948319;
                {}{}{}
                zero = 5 * FooMethod(FooMethod(FooMethod(0, 1), 2), 3);
                PrintLine(x);
                return0 = 324;
                PrintLine(zero);
                if (true || x < 0) {
                    if (false) return 0;
                    if (FooMethod(FooMethod(FooMethod(0, 1), 2), 3) > 0) {
                        if (x >= 2) {
                            PrintLine(5066355);
                        }
                        else {
                            PrintLine(4411083);
                        }
                        {
                            return+++---(-------100);
                        }
                    } else {;}
                    PrintLineb(!!!!!!!(return0 == 0));
                    {}
                    return 1;
                }
            ");
            Console.WriteLine(result.Invoke(1, 1, 1));
            Console.WriteLine(result.Invoke(2, 2, 2));
            Console.WriteLine(result.Invoke(2, 3, 4));
            Console.WriteLine(result.Invoke(1, 0, 2));
        }

        static void Main(string[] args)
        {
            new EntryPoint().Run();
        }
    }
}
