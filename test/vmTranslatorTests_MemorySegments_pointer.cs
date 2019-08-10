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

        //reference
        // This code is part of www.nand2tetris.org
        // and the book "The Elements of Computing Systems"
        // by Nisan and Schocken, MIT Press.
        // File name: projects/07/MemoryAccess/PointerTest/PointerTest.vm
        string pointerTest1 =
           @"push constant 3030
            pop pointer 0
            push constant 3040
            pop pointer 1
            push constant 32
            pop this 2
            push constant 46
            pop that 6
            push pointer 0
            push pointer 1
            add
            push this 2
            sub
            push that 6
            add";




        [Test]
        public void pointerSimple()
        {
            var path = Path.GetTempFileName();
            System.IO.File.WriteAllText(path, pointerTest1);

            var translator = new vmtranslator.vmtranslator(path);
            var assembly = translator.TranslateToAssembly().ToList();
            assembly.Add(assembler.CommandType.HALT.ToString());
            
            System.IO.File.WriteAllLines(path, assembly);

            var assemblerInstance = new assembler.Assembler(path);
            var assembledResult = assemblerInstance.ConvertToBinary();

            var binaryProgram = assembledResult.Select(x => Convert.ToUInt16(x, 16));

            var simulatorInstance = new simulator.eightChipsSimulator(16, (int)Math.Pow(2, 16));
            simulatorInstance.setUserCode(binaryProgram.ToArray());
            simulatorInstance.ProgramCounter = (ushort)MemoryMap[MemoryMapKeys.user_code].Item1;


            simulatorInstance.runSimulation();
            simulatorInstance.printMemory(0);

            Assert.AreEqual(6084, simulatorInstance.mainMemory[33040]);

            
            //pointers are set correctly
            Assert.AreEqual(3030, simulatorInstance.mainMemory[259]);
            Assert.AreEqual(3040, simulatorInstance.mainMemory[260]);
            //values at pointers are correct.
            Assert.AreEqual(32, simulatorInstance.mainMemory[3032]);
            Assert.AreEqual(46, simulatorInstance.mainMemory[3046]);

        }
    }
}