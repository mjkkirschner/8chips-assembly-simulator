using NUnit.Framework;
using System.IO;
using System;

namespace Tests.Jack
{

    public class Coro_R_JackParserTests
    {

        [Test]
        public void simpleLetStatement()
        {
            var testClassCode =
            @"  class Main{
                function system.void main(){
                var x int;
                let x = 100;
                }
            }";
            var path = Path.GetTempFileName();
            File.WriteAllText(path, testClassCode);

            var scanner = new Scanner(path);
            var jackParser = new Parser(scanner);
            jackParser.Parse();
            Console.WriteLine(jackParser.root.ToString());
        }

    }


}