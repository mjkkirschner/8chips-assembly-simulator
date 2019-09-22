using System;
using System.IO;
using NUnit.Framework;
using System.Linq;
using static assembler.Assembler;
using System.Collections.Generic;
using vmtranslator;
using vmCommmandType = vmtranslator.vmILParser.vmCommandType;
using static vmtranslator.vmILParser;

namespace Tests.Memory
{

    public class testVMtoASMWriter : vmtranslator.vmIL2ASMWriter
    {

        private int[] argsToPush = null;
        public testVMtoASMWriter(int[] argsToPush) : base()
        {
            this.argsToPush = argsToPush;
        }

        protected override void entryPointFunctionCall()
        {
            //call sys.init and then call main.
            var callInit = new InstructionData(vmCommmandType.CALL, null, new string[] { "Sys.init", "0" }, "Sys.init", "Sys.init");
            handleFunctionCallingCommand(callInit);


            Output.Add(assembler.CommandType.HALT.ToString());


            var sysInitDefine = new InstructionData(vmCommmandType.FUNCTION, null, new string[] { "Sys.init", "0" }, "Sys.init", "Sys.init");
            handleFunctionCallingCommand(sysInitDefine);

            //for these tests we need to call our function with an argument.
            argsToPush.ToList().ForEach(arg =>
            {
                Output.AddRange(generatePushToStackFromSegment(vmMemSegments.constant, arg.ToString(), null));
            });


            //call the main.main func which is our real entry point.
            var callMain = new InstructionData(vmCommmandType.CALL, null, new string[] { "main.main", argsToPush.Length.ToString() }, "main.main", "main.main");
            handleFunctionCallingCommand(callMain);

            //TODO as part of any VM function we need to push some return value to the stack 
            //for now, lets just push the result of main so we can assert our test was successfull.

            Output.AddRange(generateDecrement(stackPointer_symbol, "1", false));
            Output.AddRange(generateMoveAtoTemp());
            Output.Add(assembler.CommandType.LOADAATPOINTER.ToString());
            Output.Add(temp_symbol);
            Output.Add(assembler.CommandType.STOREA.ToString());
            Output.Add(temp_symbol);
            Output.AddRange(this.generatePushToStackFromSymbol(temp_symbol, null));

            var returnFromSysInit = new InstructionData(vmCommmandType.RETURN, null, new string[] { }, "Sys.init", "Sys.init");
            handleFunctionCallingCommand(returnFromSysInit);

        }
    }



    public partial class VmTranslatorTests
    {

        string popLocalSimpleProgram =
           @"function main.main 2 
           push constant 2
         push constant 4
         push constant 10
         pop local 0
         pop local 0
         pop local 1
         return";




        [Test]
        public void popLocalSimple()
        {
            var path = Path.GetTempFileName();
            System.IO.File.WriteAllText(path, popLocalSimpleProgram);

            var translator = new vmtranslator.vmtranslator(path);
            var assembly = translator.TranslateToAssembly().ToList();
            assembly.Add(assembler.CommandType.HALT.ToString());

            System.IO.File.WriteAllLines(path, assembly);
            assembly.ForEach(x => Console.WriteLine(x));

            var assemblerInstance = new assembler.Assembler(path);
            var assembledResult = assemblerInstance.ConvertToBinary();

            var binaryProgram = assembledResult.Select(x => Convert.ToInt32(x, 16));

            var simulatorInstance = new simulator.eightChipsSimulator(16, (int)Math.Pow(2, 16));
            simulatorInstance.setUserCode(binaryProgram.ToArray());
            simulatorInstance.ProgramCounter = (int)MemoryMap[MemoryMapKeys.user_code].AbsoluteStart;

            //setup monitors:

            var stackHandle = simulatorInstance.monitor(33040 + 5);

            simulatorInstance.logger.enabled = true;
            simulatorInstance.runSimulation();
            var stackValues = stackHandle.getValues();


            simulatorInstance.printMemory(0);
            stackValues.ForEach(x => Console.WriteLine(x));
            Console.WriteLine("_____");

            Assert.IsTrue(stackValues.SequenceEqual(new List<int>() { 0, 725, 2, }));

        }

        string pushAndPop_Local =
               @"function main.main 2 
               push constant 2
         push constant 4
         pop local 0
         pop local 1
         push local 0
         push local 1
         add
         push local 0
         add
         return";

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

            var binaryProgram = assembledResult.Select(x => Convert.ToInt32(x, 16));

            var simulatorInstance = new simulator.eightChipsSimulator(16, (int)Math.Pow(2, 16));
            simulatorInstance.setUserCode(binaryProgram.ToArray());
            simulatorInstance.ProgramCounter = (int)MemoryMap[MemoryMapKeys.user_code].AbsoluteStart;

            var stackHandle = simulatorInstance.monitor(33040 + 5);


            simulatorInstance.runSimulation();
            var stackValues = stackHandle.getValues();

            stackValues.ForEach(x => Console.WriteLine(x));
            Console.WriteLine("_____");


            Assert.IsTrue(stackValues.SequenceEqual(new List<int>() { 0, 725, 10 }));
        }


        string pushAndPop_LocalAndArgument =
               @"function main.main 2
               push constant 2
         push constant 4
         pop local 0
         pop local 1
         push local 0
         push local 1
         add
         push local 0
         add
         pop argument 0
         push argument 0
         return";

        [Test]
        public void PushPopLocalArgument()
        {
            var path = Path.GetTempFileName();
            System.IO.File.WriteAllText(path, pushAndPop_LocalAndArgument);

            var writer = new testVMtoASMWriter(new int[] { 2000 });
            var translator = new vmtranslator.vmtranslator(path, writer);
            var assembly = translator.TranslateToAssembly().ToList();
            assembly.Add(assembler.CommandType.HALT.ToString());

            System.IO.File.WriteAllLines(path, assembly);

            var assemblerInstance = new assembler.Assembler(path);
            var assembledResult = assemblerInstance.ConvertToBinary();

            var binaryProgram = assembledResult.Select(x => Convert.ToInt32(x, 16));

            var simulatorInstance = new simulator.eightChipsSimulator(16, (int)Math.Pow(2, 16));
            simulatorInstance.setUserCode(binaryProgram.ToArray());
            simulatorInstance.ProgramCounter = (int)MemoryMap[MemoryMapKeys.user_code].AbsoluteStart;

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

            Assert.IsTrue(stackValues.SequenceEqual(new List<int>() { 0, 638, 10 }));
        }

        string pushAndPop_Argument =
       @"function main.main 0
        push constant 2
         push constant 4
         pop argument 0
         pop argument 1
         push argument 0
         push argument 1
         add
         push argument 0
         add
         return";

        [Test]
        public void PushPopArgument()
        {
            var path = Path.GetTempFileName();
            System.IO.File.WriteAllText(path, pushAndPop_Argument);

            var writer = new testVMtoASMWriter(new int[] { 2000, 2000 });
            var translator = new vmtranslator.vmtranslator(path, writer);
            var assembly = translator.TranslateToAssembly().ToList();
            //assembly.ForEach(x => Console.WriteLine(x));
            assembly.Add(assembler.CommandType.HALT.ToString());

            System.IO.File.WriteAllLines(path, assembly);

            var assemblerInstance = new assembler.Assembler(path);
            var assembledResult = assemblerInstance.ConvertToBinary();

            var binaryProgram = assembledResult.Select(x => Convert.ToInt32(x, 16));

            var simulatorInstance = new simulator.eightChipsSimulator(16, (int)Math.Pow(2, 16));
            simulatorInstance.setUserCode(binaryProgram.ToArray());
            simulatorInstance.ProgramCounter = (int)MemoryMap[MemoryMapKeys.user_code].AbsoluteStart;


            var stackHandle = simulatorInstance.monitor(33040);
            var argument0 = simulatorInstance.monitor(200);
            var argument1 = simulatorInstance.monitor(201);

            //simulatorInstance.logger.enabled = true;

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

            var sp = simulatorInstance.mainMemory[simulatorInstance.mainMemory[256] - 1];
            Assert.AreEqual(10, sp);

            Assert.IsTrue(stackValues.SequenceEqual(new List<int>() { 0, 638, 10 }));



        }
    }
}