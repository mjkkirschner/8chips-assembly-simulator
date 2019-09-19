using System;
using System.IO;
using NUnit.Framework;
using System.Linq;
using static assembler.Assembler;
using System.Collections.Generic;

namespace Tests.Memory
{



    public partial class VmTranslatorTests
    {

        // This file is part of www.nand2tetris.org
        // and the book "The Elements of Computing Systems"
        // by Nisan and Schocken, MIT Press.
        // File name: projects/08/FunctionCalls/SimpleFunction/SimpleFunction.vm

        // Performs a simple calculation and returns the result.

        string simpleFunctionDef =
        @"push constant 10
        push constant 5
        call SimpleFunction.test 2
        push constant 0 // nop
        function SimpleFunction.test 2
            push argument 1
            push argument 0
            add
        return";


        // This file is part of www.nand2tetris.org
        // and the book "The Elements of Computing Systems"
        // by Nisan and Schocken, MIT Press.
        // File name: projects/08/FunctionCalls/FibonacciElement/Sys.vm

        // Pushes n onto the stack and calls the Main.fibonacii function,
        // which computes the n'th element of the Fibonacci series.
        // The Sys.init function is called "automatically" by the 
        // bootstrap code.

        string sysvm =
                @"function Sys.init 0
push constant 4
call Main.fibonacci 1   // Compute the 4'th fibonacci element
label WHILE
goto WHILE";              // Loop infinitely



        // This file is part of www.nand2tetris.org
        // and the book "The Elements of Computing Systems"
        // by Nisan and Schocken, MIT Press.
        // File name: projects/08/FunctionCalls/FibonacciElement/Main.vm

        // Computes the n'th element of the Fibonacci series, recursively.
        // n is given in argument[0].  Called by the Sys.init function 
        // (part of the Sys.vm file), which also pushes the argument[0] 
        // parameter before this code starts running.

        string mainvm =
        @"function Main.fibonacci 0
push argument 0
push constant 2
lt                     // check if n < 2
if-goto IF_TRUE
goto IF_FALSE
label IF_TRUE          // if n<2, return n
push argument 0        
return
label IF_FALSE         // if n>=2, return fib(n-2)+fib(n-1)
push argument 0
push constant 2
sub
call Main.fibonacci 1  // compute fib(n-2)
push argument 0
push constant 1
sub
call Main.fibonacci 1  // compute fib(n-1)
add                    // return fib(n-1) + fib(n-2)
return";


        [Test]
        public void functionDefTest()
        {
            var path = Path.GetTempFileName();
            System.IO.File.WriteAllText(path, simpleFunctionDef);

            var translator = new vmtranslator.vmtranslator(path);
            var assembly = translator.TranslateToAssembly().ToList();
            assembly.Add(assembler.CommandType.HALT.ToString());

            System.IO.File.WriteAllLines(path, assembly);

            var assemblerInstance = new assembler.Assembler(path);
            var assembledResult = assemblerInstance.ConvertToBinary();

            var binaryProgram = assembledResult.Select(x => Convert.ToInt32(x, 16));

            var simulatorInstance = new simulator.eightChipsSimulator(16, (int)Math.Pow(2, 16));
            simulatorInstance.logger.enabled = true;
            simulatorInstance.setUserCode(binaryProgram.ToArray());
            simulatorInstance.ProgramCounter = (int)MemoryMap[MemoryMapKeys.user_code].AbsoluteStart;

            simulatorInstance.runSimulation();
            //simulatorInstance.printMemory(0);
            //need to check what the SP points to
            Assert.AreEqual(15, simulatorInstance.mainMemory[simulatorInstance.mainMemory[256] - 1]);
        }



        //[Test]
        public void multiVMFilesTest()
        {
            var path = Path.GetTempFileName();
            var path2 = Path.GetTempFileName();
            System.IO.File.WriteAllText(path, sysvm);
            System.IO.File.WriteAllText(path, mainvm);

            var translator = new vmtranslator.vmtranslator(path);
            var assembly = translator.TranslateToAssembly().ToList();
            assembly.Add(assembler.CommandType.HALT.ToString());

            System.IO.File.WriteAllLines(path, assembly);

            var assemblerInstance = new assembler.Assembler(path);
            var assembledResult = assemblerInstance.ConvertToBinary();

            var binaryProgram = assembledResult.Select(x => Convert.ToInt32(x, 16));

            var simulatorInstance = new simulator.eightChipsSimulator(16, (int)Math.Pow(2, 16));
            simulatorInstance.logger.enabled = true;
            simulatorInstance.setUserCode(binaryProgram.ToArray());
            simulatorInstance.ProgramCounter = (int)MemoryMap[MemoryMapKeys.user_code].AbsoluteStart;

            simulatorInstance.runSimulation();
            //simulatorInstance.printMemory(0);
            //need to check what the SP points to
            Assert.AreEqual(15, simulatorInstance.mainMemory[simulatorInstance.mainMemory[256] - 1]);
        }

    }
}