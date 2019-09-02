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
        // File name: projects/08/ProgramFlow/BasicLoop/BasicLoop.vm

        // Computes the sum 1 + 2 + ... + argument[0] and pushes the 
        // result onto the stack. Argument[0] is initialized by the test 
        // script before this code starts running.

        string basicLoopTest =
@"push constant 0    
pop local 0        // initialize sum = 0
label LOOP_START
push argument 0    
push local 0
add
pop local 0	   // sum = sum + counter
push argument 0
push constant 1
sub
pop argument 0     // counter--
push argument 0
if-goto LOOP_START // If counter > 0, goto LOOP_START
push local 0";



        [Test]
        public void basicLoop()
        {
            var path = Path.GetTempFileName();
            System.IO.File.WriteAllText(path, basicLoopTest);

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
            simulatorInstance.mainMemory[200] = 3;

            simulatorInstance.runSimulation();

            Assert.AreEqual(6, simulatorInstance.mainMemory[33040]);
        }
    }
}