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

        string popLocalSimpleProgram =
           @"push constant 2
         push constant 4
         push constant 10
         pop local 0
         pop local 0
         pop local 1";




        [Test]
        public void popLocalSimple()
        {
            var path = Path.GetTempFileName();
            System.IO.File.WriteAllText(path, popLocalSimpleProgram);

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

            //setup monitors:
            var stackHandle = simulatorInstance.monitor(33040);
            var local0 = simulatorInstance.monitor(100);
            var local1 = simulatorInstance.monitor(101);

            simulatorInstance.runSimulation();
            var stackValues = stackHandle.getValues();
            var localValues0 = local0.getValues();
            var localValues1 = local1.getValues();
            //simulatorInstance.printMemory(0);
            stackValues.ForEach(x => Console.WriteLine(x));
            Console.WriteLine("_____");
            localValues0.ForEach(x => Console.WriteLine(x));
            Console.WriteLine("_____");
            localValues1.ForEach(x => Console.WriteLine(x));
            Console.WriteLine("_____");


            Assert.IsTrue(stackValues.SequenceEqual(new List<ushort>() { 0, 2, }));
            Assert.IsTrue(localValues0.SequenceEqual(new List<ushort>() { 0, 10, 4 }));
            Assert.IsTrue(localValues1.SequenceEqual(new List<ushort>() { 0, 2, }));
        }

        string pushAndPop_Local =
               @"push constant 2
         push constant 4
         pop local 0
         pop local 1
         push local 0
         push local 1
         add
         push local 0
         add";

        [Test]
        public void PushPopLocal()
        {
            var path = Path.GetTempFileName();
            System.IO.File.WriteAllText(path, pushAndPop_Local);

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

            var stackHandle = simulatorInstance.monitor(33040);
            var local0 = simulatorInstance.monitor(100);
            var local1 = simulatorInstance.monitor(101);

            simulatorInstance.runSimulation();
            var stackValues = stackHandle.getValues();
            var localValues0 = local0.getValues();
            var localValues1 = local1.getValues();
            stackValues.ForEach(x => Console.WriteLine(x));
            Console.WriteLine("_____");
            localValues0.ForEach(x => Console.WriteLine(x));
            Console.WriteLine("_____");
            localValues1.ForEach(x => Console.WriteLine(x));
            Console.WriteLine("_____");
            
            Assert.IsTrue(stackValues.SequenceEqual(new List<ushort>() { 0, 2, 4, 6, 10 }));
        }

        //TODO these argument tests are flawed -
        //currently argument and local point to the same baseaddress...
        
        string pushAndPop_LocalAndArgument =
               @"push constant 2
         push constant 4
         pop local 0
         pop local 1
         push local 0
         push local 1
         add
         push local 0
         add
         pop argument 0
         push argument 0";

        [Test]
        public void PushPopLocalArgument()
        {
            var path = Path.GetTempFileName();
            System.IO.File.WriteAllText(path, pushAndPop_LocalAndArgument);

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

            var stackHandle = simulatorInstance.monitor(33040);
           var local0 = simulatorInstance.monitor(100);
            var local1 = simulatorInstance.monitor(101);

            simulatorInstance.runSimulation();
            var stackValues = stackHandle.getValues();
            var localValues0 = local0.getValues();
            var localValues1 = local1.getValues();
            //simulatorInstance.printMemory(0);
            stackValues.ForEach(x => Console.WriteLine(x));
            Console.WriteLine("_____");
            localValues0.ForEach(x => Console.WriteLine(x));
            Console.WriteLine("_____");
            localValues1.ForEach(x => Console.WriteLine(x));
            Console.WriteLine("_____");

            Assert.IsTrue(stackValues.SequenceEqual(new List<ushort>() { 0, 2, 4, 6, 10, 10 }));
        }

        string pushAndPop_Argument =
       @"push constant 2
         push constant 4
         pop argument 0
         pop argument 1
         push argument 0
         push argument 1
         add
         push argument 0
         add";

        [Test]
        public void PushPopArgument()
        {
            var path = Path.GetTempFileName();
            System.IO.File.WriteAllText(path, pushAndPop_Argument);

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

            var stackHandle = simulatorInstance.monitor(33040);
            var argument0 = simulatorInstance.monitor(200);
            var argument1 = simulatorInstance.monitor(201);

            simulatorInstance.runSimulation();
            var stackValues = stackHandle.getValues();
            var argument0values = argument0.getValues();
            var argument1values = argument1.getValues();
            //simulatorInstance.printMemory(0);
            stackValues.ForEach(x => Console.WriteLine(x));
            Console.WriteLine("_____");
            argument0values.ForEach(x => Console.WriteLine(x));
            Console.WriteLine("_____");
            argument1values.ForEach(x => Console.WriteLine(x));
            Console.WriteLine("_____");

            Assert.IsTrue(stackValues.SequenceEqual(new List<ushort>() { 0, 2, 4, 6, 10 }));
        }
    }
}