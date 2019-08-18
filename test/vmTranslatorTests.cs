using System;
using System.IO;
using NUnit.Framework;
using System.Linq;
using static assembler.Assembler;
using System.Collections.Generic;

namespace Tests
{



    public class VmTranslatorTests
    {

        string simeplAddTestProgram =
        @"push constant 7
         push constant 8
         add";

        string multiAddTestProgram =
      @"push constant 7
         push constant 8
         add
         push constant 1
         add
         push constant 100
         add
         push constant 100
         sub";

        string NegativeSubtractTestProgram =
               @"push constant 7
                push constant 8
                sub";
        string SubtractTestProgram =
     @"push constant 100
                push constant 90
                sub";

        string subAddTestProgram =
@"push constant 100
                push constant 90
                sub
                push constant 100
                add";

        string eqTestProgram1 =
            @"push constant 100
                push constant 90
            eq";

        string gtTestProgram1 =
        @"push constant 100
                push constant 90
                gt
                push constant 2
                gt";

        string eqTestProgram2 =
            @"push constant 100
                push constant 100
            eq";

        string ANDtestprogram1 =
      @"push constant 6
                push constant 5
            and";

        string ORtestprogram1 =
@"push constant 6
                push constant 5
            or";

        string NOTtestprogram1 =
 @"push constant 1
            not";

        // This code is part of www.nand2tetris.org
        // and the book "The Elements of Computing Systems"
        // by Nisan and Schocken, MIT Press.
        // File name: projects/07/StackArithmetic/StackTest/StackTest.vm

        // Executes a sequence of arithmetic and logical operations
        // on the stack. 
        string complexStack =
        @"push constant 17
push constant 17
eq
push constant 17
push constant 16
eq
push constant 16
push constant 17
eq
push constant 892
push constant 891
lt
push constant 891
push constant 892
lt
push constant 891
push constant 891
lt
push constant 32767
push constant 32766
gt
push constant 32766
push constant 32767
gt
push constant 32766
push constant 32766
gt
push constant 57
push constant 31
push constant 53
add
push constant 112
sub
neg
and
push constant 82
or
not";

                [Test]
        public void simpleAddTest()
        {
            var path = Path.GetTempFileName();
            System.IO.File.WriteAllText(path, simeplAddTestProgram);

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
            //TODO make this lookup the current SP location.
            Assert.AreEqual(simulatorInstance.mainMemory[33040], 15);

        }

        // TODO - this test fails because all values are stored internally as ushorts.
        // we might want to use short instead - this will support -32xxx - 32xxxx - but to index
        // into memory we'll need to use negative indices or offsets or something...
        [Test]
        public void subNegativeTest()
        {
            var path = Path.GetTempFileName();
            System.IO.File.WriteAllText(path, NegativeSubtractTestProgram);

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
            Assert.AreEqual(simulatorInstance.mainMemory[0], -1);

        }
        [Test]
        public void subTest()
        {
            var path = Path.GetTempFileName();
            System.IO.File.WriteAllText(path, SubtractTestProgram);

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

            //TODO make this lookup the current SP location.
            Assert.AreEqual(simulatorInstance.mainMemory[33040], 10);

        }

        [Test]
        public void MathTest()
        {
            var path = Path.GetTempFileName();
            System.IO.File.WriteAllText(path, subAddTestProgram);

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
            //TODO make this lookup the current SP location.
            Assert.AreEqual(simulatorInstance.mainMemory[33040], 110);

        }
        [Test]
        public void Math2Test()
        {
            var path = Path.GetTempFileName();
            System.IO.File.WriteAllText(path, multiAddTestProgram);

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

            var handle = simulatorInstance.monitor(33040);

            simulatorInstance.runSimulation();
            var values = handle.getValues();
            values.ForEach(x => Console.WriteLine(x));
            Assert.IsTrue(values.SequenceEqual(new List<ushort>() { 0, 7, 15, 16, 116, 16 }));

        }

        [Test]
        public void eqTest1False()
        {
            var path = Path.GetTempFileName();
            System.IO.File.WriteAllText(path, eqTestProgram1);

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
            //TODO make this lookup the current SP location.
            Assert.AreEqual(simulatorInstance.mainMemory[33040], 0);

        }

        [Test]
        public void eqTest2True()
        {
            var path = Path.GetTempFileName();
            System.IO.File.WriteAllText(path, eqTestProgram2);

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
            //TODO make this lookup the current SP location.
            Assert.AreEqual(simulatorInstance.mainMemory[33040], 1);

        }

        [Test]
        public void gtTest()
        {
            var path = Path.GetTempFileName();
            System.IO.File.WriteAllText(path, gtTestProgram1);

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

            //setup a monitor
            var handle = simulatorInstance.monitor(33040);

            simulatorInstance.runSimulation();
            var values = handle.getValues();
            values.ForEach(x => Console.WriteLine(x));
            Assert.IsTrue(values.SequenceEqual(new List<ushort>() { 0, 100, 1, 0 }));
        }

        [Test]
        public void ANDTest()
        {
            var path = Path.GetTempFileName();
            System.IO.File.WriteAllText(path, ANDtestprogram1);

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

            //setup a monitor
            var handle = simulatorInstance.monitor(33040);

            simulatorInstance.runSimulation();
            var values = handle.getValues();
            values.ForEach(x => Console.WriteLine(x));
            Assert.IsTrue(values.SequenceEqual(new List<ushort>() { 0, 6, 4 }));
        }

        [Test]
        public void ORTest()
        {
            var path = Path.GetTempFileName();
            System.IO.File.WriteAllText(path, ORtestprogram1);

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

            //setup a monitor
            var handle = simulatorInstance.monitor(33040);

            simulatorInstance.runSimulation();
            var values = handle.getValues();
            values.ForEach(x => Console.WriteLine(x));
            Assert.IsTrue(values.SequenceEqual(new List<ushort>() { 0, 6, 7 }));
        }

        [Test]
        public void NOTTest()
        {
            var path = Path.GetTempFileName();
            System.IO.File.WriteAllText(path, NOTtestprogram1);

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

            //setup a monitor
            var handle = simulatorInstance.monitor(33040);

            simulatorInstance.runSimulation();
            var values = handle.getValues();
            values.ForEach(x => Console.WriteLine(x));
            Assert.IsTrue(values.SequenceEqual(new List<ushort>() { 0, 1, 65534 }));
        }

        [Test]
        public void complexTest()
        {
            var path = Path.GetTempFileName();
            System.IO.File.WriteAllText(path, complexStack);

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

            //setup a monitor
            var handle = simulatorInstance.monitor(33040);

            simulatorInstance.runSimulation();
            var values = handle.getValues();
            values.ForEach(x => Console.WriteLine(x));
            Assert.IsTrue(values.SequenceEqual(new List<ushort>() { 0, 1, 91 }));
        }
    }
}