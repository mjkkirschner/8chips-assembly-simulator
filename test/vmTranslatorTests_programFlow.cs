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

        // This file is part of www.nand2tetris.org
        // and the book "The Elements of Computing Systems"
        // by Nisan and Schocken, MIT Press.
        // File name: projects/08/ProgramFlow/FibonacciSeries/FibonacciSeries.vm

        // Puts the first argument[0] elements of the Fibonacci series
        // in the memory, starting in the address given in argument[1].
        // Argument[0] and argument[1] are initialized by the test script 
        // before this code starts running.

        string fibTest =
        @"push argument 1
pop pointer 1           // that = argument[1]

push constant 0
pop that 0              // first element = 0
push constant 1
pop that 1              // second element = 1

push argument 0
push constant 2
sub
pop argument 0          // num_of_elements -= 2 (first 2 elements are set)

label MAIN_LOOP_START

push argument 0
if-goto COMPUTE_ELEMENT // if num_of_elements > 0, goto COMPUTE_ELEMENT
goto END_PROGRAM        // otherwise, goto END_PROGRAM

label COMPUTE_ELEMENT

push that 0
push that 1
add
pop that 2              // that[2] = that[0] + that[1]

push pointer 1
push constant 1
add
pop pointer 1           // that += 1

push argument 0
push constant 1
sub
pop argument 0          // num_of_elements--

goto MAIN_LOOP_START

label END_PROGRAM";


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
        [Test]
        public void fibSeries()
        {
            var path = Path.GetTempFileName();
            System.IO.File.WriteAllText(path, fibTest);

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
            simulatorInstance.mainMemory[200] = 7;
            simulatorInstance.mainMemory[201] = 3000;

            simulatorInstance.runSimulation();

            Assert.AreEqual(0, simulatorInstance.mainMemory[3000]);
            Assert.AreEqual(1, simulatorInstance.mainMemory[3001]);
            Assert.AreEqual(1, simulatorInstance.mainMemory[3002]);
            Assert.AreEqual(2, simulatorInstance.mainMemory[3003]);
            Assert.AreEqual(3, simulatorInstance.mainMemory[3004]);
            Assert.AreEqual(5, simulatorInstance.mainMemory[3005]);
            Assert.AreEqual(8, simulatorInstance.mainMemory[3006]);
        }
    }
}