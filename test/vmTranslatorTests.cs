using System;
using System.IO;
using NUnit.Framework;
using System.Linq;
using static assembler.Assembler;

namespace Tests
{



    public class VmTranslatorTests
    {

        string simeplAddTestProgram =
        @"push constant 7
         push constant 8
         add";

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
        string eqTestProgram2 =
            @"push constant 100
                push constant 100
            eq";


        [Test]
        public void simpleAddTest()
        {
            var path = Path.GetTempFileName();
            System.IO.File.WriteAllText(path, simeplAddTestProgram);

            var translator = new vmtranslator.vmtranslator(path);
            var assembly = translator.TranslateToAssembly().ToList();
            assembly.Add(assembler.CommandType.HALT.ToString());
            assembly.ToList().ForEach(x => Console.WriteLine(x));
            System.IO.File.WriteAllLines(path, assembly);

            Assert.AreEqual("TEMP + 1", assembly[16]);

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
            assembly.ToList().ForEach(x => Console.WriteLine(x));
            System.IO.File.WriteAllLines(path, assembly);

            Assert.AreEqual("TEMP + 1", assembly[16]);

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
            assembly.ToList().ForEach(x => Console.WriteLine(x));
            System.IO.File.WriteAllLines(path, assembly);

            Assert.AreEqual("TEMP + 1", assembly[16]);

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
            assembly.ToList().ForEach(x => Console.WriteLine(x));
            System.IO.File.WriteAllLines(path, assembly);

            Assert.AreEqual("TEMP + 1", assembly[16]);

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
        public void eqTest1False()
        {
            var path = Path.GetTempFileName();
            System.IO.File.WriteAllText(path, eqTestProgram1);

            var translator = new vmtranslator.vmtranslator(path);
            var assembly = translator.TranslateToAssembly().ToList();
            assembly.Add(assembler.CommandType.HALT.ToString());
            assembly.ToList().ForEach(x => Console.WriteLine(x));
            System.IO.File.WriteAllLines(path, assembly);

            Assert.AreEqual("TEMP + 1", assembly[16]);

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
            assembly.ToList().ForEach(x => Console.WriteLine(x));
            System.IO.File.WriteAllLines(path, assembly);

            Assert.AreEqual("TEMP + 1", assembly[16]);

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
    }
}