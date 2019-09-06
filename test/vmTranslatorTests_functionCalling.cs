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

    }
}