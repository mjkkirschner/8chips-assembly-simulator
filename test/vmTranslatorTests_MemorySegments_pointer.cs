using System;
using System.IO;
using NUnit.Framework;
using System.Linq;
using static assembler.Assembler;
using System.Collections.Generic;
using simulator;

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
           @"function main.main 0
           push constant 3030
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
            add
            return";




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

            var binaryProgram = assembledResult.Select(x => Convert.ToInt32(x, 16));

            var simulatorInstance = new simulator.eightChipsSimulator(16, (int)Math.Pow(2, 16));
            simulatorInstance.setUserCode(binaryProgram.ToArray());
            simulatorInstance.ProgramCounter = (int)MemoryMap[MemoryMapKeys.user_code].AbsoluteStart;

            var pt1Monitor = new MonitorHandle<int>(3032, simulatorInstance.mainMemory);
            var pt2Monitor = new MonitorHandle<int>(3046, simulatorInstance.mainMemory);
            var thismon = new MonitorHandle<int>(259, simulatorInstance.mainMemory);
            var thatmon = new MonitorHandle<int>(260, simulatorInstance.mainMemory);

            simulatorInstance.runSimulation();
            //simulatorInstance.printMemory(0);

            var sp = simulatorInstance.mainMemory[simulatorInstance.mainMemory[256] - 1];
            //simulatorInstance.printMemory(0);
            Assert.AreEqual(6084, sp);

            var pt1values = pt1Monitor.getValues();
            var pt2values = pt2Monitor.getValues();
            var thisvalues = thismon.getValues();
            var thatvalues = thatmon.getValues();

            new List<List<int>>() { pt1values, pt2values, thisvalues, thatvalues }.ForEach(x =>
            {
                x.ForEach(y => Console.WriteLine(y));
                Console.WriteLine("---------------");
            });

            //pointers are set correctly
            //TODO I am not sure 100% sure these 27x values are correct.
            Assert.IsTrue(new int[] { 0, 272, 3030, 272, 272 }.SequenceEqual(thisvalues));
            Assert.IsTrue(new int[] { 0, 273, 3040, 273, 273 }.SequenceEqual(thatvalues));
            //Assert.AreEqual(3030, simulatorInstance.mainMemory[259]);
            //Assert.AreEqual(3040, simulatorInstance.mainMemory[260]);


            //values at pointers are correct.
            //TODO not sure how to check this or what the correct value should be because we set the
            //pointers back when returning - we may want to use monitors for these mem locations instead.
            Assert.IsTrue(new int[] { 0, 32 }.SequenceEqual(pt1values));
            Assert.IsTrue(new int[] { 0, 46 }.SequenceEqual(pt2values));
        }
    }
}