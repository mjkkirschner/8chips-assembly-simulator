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
        // This code is part of www.nand2tetris.org
        // and the book "The Elements of Computing Systems"
        // by Nisan and Schocken, MIT Press.
        // File name: projects/07/MemoryAccess/StaticTest/StaticTest.vm
        string staticTest1 =
      @"push constant 111
push constant 333
push constant 888
pop static 8
pop static 3
pop static 1
push static 3
push static 1
sub
push static 8
add";




        [Test]
        public void staticSimple()
        {
            var path = Path.GetTempFileName();
            System.IO.File.WriteAllText(path, staticTest1);

            var translator = new vmtranslator.vmtranslator(path);
            var assembly = translator.TranslateToAssembly().ToList();
            assembly.Add(assembler.CommandType.HALT.ToString());

            System.IO.File.WriteAllLines(path, assembly);

            var assemblerInstance = new assembler.Assembler(path);
            var assembledResult = assemblerInstance.ConvertToBinary();

            var binaryProgram = assembledResult.Select(x => Convert.ToInt32(x, 16));

            var simulatorInstance = new simulator.eightChipsSimulator(16, (int)Math.Pow(2, 16));
            simulatorInstance.setUserCode(binaryProgram.ToArray());
            simulatorInstance.ProgramCounter = (int)MemoryMap[MemoryMapKeys.user_code].AbsoluteStart;


            simulatorInstance.runSimulation();
            //simulatorInstance.printMemory(0);

            Assert.AreEqual(1110, simulatorInstance.mainMemory[33040]);


            //pointers are set correctly
            Assert.AreEqual(3030, simulatorInstance.mainMemory[259]);
            Assert.AreEqual(3040, simulatorInstance.mainMemory[260]);
            //values at pointers are correct.
            Assert.AreEqual(32, simulatorInstance.mainMemory[3032]);
            Assert.AreEqual(46, simulatorInstance.mainMemory[3046]);

        }
    }
}