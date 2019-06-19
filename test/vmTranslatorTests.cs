using System;
using System.IO;
using NUnit.Framework;
using System.Linq;


namespace Tests
{



    public class VmTranslatorTests
    {

        string simeplAddTestProgram =
        @"push constant 7
         push constant 8
         add";

        [Test]
        public void simpleAddTest()
        {
            var path = Path.GetTempFileName();
            System.IO.File.WriteAllText(path, simeplAddTestProgram);
            var translator = new vmtranslator.vmtranslator(path);
            var assembly = translator.TranslateToAssembly().ToList();
            assembly.Add (assembler.CommandType.HALT.ToString());
            assembly.ToList().ForEach(x => Console.WriteLine(x));
            System.IO.File.WriteAllLines(path, assembly);

            var assemblerInstance = new assembler.Assembler(path);
            var assembledResult = assemblerInstance.ConvertToBinary();

            var binaryProgram = assembledResult.Select(x => Convert.ToUInt16(x, 16));

            var simulatorInstance = new simulator.simulator(16, (int)Math.Pow(2, 16));
            simulatorInstance.mainMemory.InsertRange(255, binaryProgram.ToList());

            simulatorInstance.ProgramCounter = 255;
            simulatorInstance.runSimulation();
            Assert.AreEqual(simulatorInstance.mainMemory[256], 15);

        }
    }
}