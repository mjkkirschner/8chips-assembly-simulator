using System;
using System.Collections;
using System.Collections.Generic;
using assembler;
using System.Linq;
using System.Linq.Expressions;

namespace simulator
{


    public class simulator
    {
        //TODO.... should we use bitArrays or just use numbers
        //so we can do math directly on this platform.
        public readonly int wordWidth = 16;
        public List<BitArray> mainMemory;
        public BitArray ARegister;
        public BitArray BRegister;
        public BitArray ProgramCounter;
        public BitArray OutRegister;
        //0: AGB
        //1: AEB
        //2: ALB
        public BitArray FlagsRegister = new BitArray(3);
        public BitArray CommsControlRegister;
        public BitArray CommsStatusRegister;
        public BitArray CommsDataRegister;

        public long TotalInstructionCount = 0;

        internal bool HALT = false;

        public simulator(int wordWidth, int memoryLength)
        {
            this.wordWidth = wordWidth;
            this.mainMemory = Enumerable.Range(0, memoryLength).Select(x => new BitArray(wordWidth, false)).ToList();
            this.ARegister = new BitArray(wordWidth, false);
            this.BRegister = new BitArray(wordWidth, false);
            this.ProgramCounter = new BitArray(wordWidth, false);
            this.OutRegister = new BitArray(wordWidth, false);
            this.CommsControlRegister = new BitArray(wordWidth, false);
            this.CommsStatusRegister = new BitArray(wordWidth, false);
            this.CommsDataRegister = new BitArray(wordWidth, false);
        }

        //starts the computer.
        public void runSimulation()
        {
            var instructionCount = 0;
            const int bundleSize = 10000;



            while (!HALT)
            {
                instructionCount++;
                this.TotalInstructionCount++;
                if (instructionCount == bundleSize)
                {
                    instructionCount = 0;
                    //System.Threading.Thread.Sleep(1);
                }
                //fetch instruction from the program counter.
                var currentInstructionIndex = ProgramCounter.ToNumeral();
                var currentInstruction = mainMemory[currentInstructionIndex].ToNumeral();
                var operands = new List<BitArray>();
                //TODO - currently we only ever have one operand.
                operands.Add(mainMemory[currentInstructionIndex + 1]);
                var commandFunc = commandToInstructionHelper.map[(assembler.CommandType)currentInstruction];

                //simulate.
                ((Action<simulator, List<BitArray>>)commandFunc)(this, operands);
            }

        }

    }
    static class commandToInstructionHelper
    {
        public static void incrementCounter(simulator simulator, int num)
        {
            var incrementCounter = simulator.ProgramCounter.ToNumeral() + num;
            simulator.ProgramCounter = incrementCounter.ToBinary();
        }

        public static Dictionary<CommandType, Delegate> map = new Dictionary<CommandType, Delegate>()
        {
            [assembler.CommandType.NOP] = new Action<simulator, List<BitArray>>((simulator, operands) =>
              {
                  incrementCounter(simulator, 1);
              }),


            [assembler.CommandType.LOADA] = new Action<simulator, List<BitArray>>((simulator, operands) =>
            { // when we get a load A command,
              // we go lookup the value at memory location operand[0] - then
              // store this in A register.
                var operandAsInt = operands[0].ToNumeral();
                simulator.ARegister = simulator.mainMemory[operandAsInt];

                incrementCounter(simulator, 2);

            }),
            [assembler.CommandType.OUTA] = new Action<simulator, List<BitArray>>((simulator, operands) =>
           {
               simulator.OutRegister = simulator.ARegister;
               incrementCounter(simulator, 1);
           }),
            [assembler.CommandType.ADD] = new Action<simulator, List<BitArray>>((simulator, operands) =>
           {
               var operandAsInt = operands[0].ToNumeral();
               var dataToAdd = simulator.mainMemory[operandAsInt];
               simulator.BRegister = dataToAdd;
               var a = simulator.ARegister.ToNumeral();
               var b = simulator.BRegister.ToNumeral();
               var result = a + b;
               simulator.ARegister = result.ToBinary();

               incrementCounter(simulator, 2);
           }),
            [assembler.CommandType.SUBTRACT] = new Action<simulator, List<BitArray>>((simulator, operands) =>
           {
               var operandAsInt = operands[0].ToNumeral();
               var dataToAdd = simulator.mainMemory[operandAsInt];
               simulator.BRegister = dataToAdd;
               var a = simulator.ARegister.ToNumeral();
               var b = simulator.BRegister.ToNumeral();
               var result = a - b;
               simulator.ARegister = result.ToBinary();

               incrementCounter(simulator, 2);
           }),
            [assembler.CommandType.STOREA] = new Action<simulator, List<BitArray>>((simulator, operands) =>
           {
               var operandAsInt = operands[0].ToNumeral();
               simulator.mainMemory[operandAsInt] = simulator.ARegister;

               incrementCounter(simulator, 2);
           }),
            [assembler.CommandType.LOADAIMMEDIATE] = new Action<simulator, List<BitArray>>((simulator, operands) =>
           {
               simulator.ARegister = operands[0];
               incrementCounter(simulator, 2);
           }),
            [assembler.CommandType.JUMP] = new Action<simulator, List<BitArray>>((simulator, operands) =>
            {
                simulator.ProgramCounter = operands[0];
            }),
            [assembler.CommandType.JUMPIFEQUAL] = new Action<simulator, List<BitArray>>((simulator, operands) =>
           {
               incrementCounter(simulator, 2);
               if (simulator.ARegister.isEquivalent(simulator.BRegister))
               {
                   simulator.ProgramCounter = operands[0];
               }
           }),
            [assembler.CommandType.JUMPIFLESS] = new Action<simulator, List<BitArray>>((simulator, operands) =>
           {
               incrementCounter(simulator, 2);
               if (simulator.ARegister.ToNumeral() < (simulator.BRegister.ToNumeral()))
               {
                   simulator.ProgramCounter = operands[0];
               }
           }),
            [assembler.CommandType.JUMPIFGREATER] = new Action<simulator, List<BitArray>>((simulator, operands) =>
           {
               incrementCounter(simulator, 2);
               if (simulator.ARegister.ToNumeral() > (simulator.BRegister.ToNumeral()))
               {
                   simulator.ProgramCounter = operands[0];
               }
           }),
            [assembler.CommandType.LOADB] = new Action<simulator, List<BitArray>>((simulator, operands) =>
            {
                var operandAsInt = operands[0].ToNumeral();
                simulator.BRegister = simulator.mainMemory[operandAsInt];

                incrementCounter(simulator, 2);

            }),
            [assembler.CommandType.LOADBIMMEDIATE] = new Action<simulator, List<BitArray>>((simulator, operands) =>
          {
              simulator.BRegister = operands[0];
              incrementCounter(simulator, 2);
          }),
            [assembler.CommandType.STOREB] = new Action<simulator, List<BitArray>>((simulator, operands) =>
             {
                 var operandAsInt = operands[0].ToNumeral();
                 simulator.mainMemory[operandAsInt] = simulator.BRegister;

                 incrementCounter(simulator, 2);
             }),
            [assembler.CommandType.UPDATEFLAGS] = new Action<simulator, List<BitArray>>((simulator, operands) =>
        {
            incrementCounter(simulator, 1);
            var a = simulator.ARegister.ToNumeral();
            var b = simulator.BRegister.ToNumeral();
            simulator.FlagsRegister.SetAll(false);
            if (a > b)
            {
                simulator.FlagsRegister.Set(0, true);
            }
            if (a == b)
            {
                simulator.FlagsRegister.Set(1, true);
            }
            if (a < b)
            {
                simulator.FlagsRegister.Set(2, true);
            }
        }),
            [assembler.CommandType.HALT] = new Action<simulator, List<BitArray>>((simulator, operands) =>
        {
            simulator.HALT = true;
        }),
            [assembler.CommandType.LOADCONTROLIMMEDIATE] = new Action<simulator, List<BitArray>>((simulator, operands) =>
           {
               incrementCounter(simulator, 2);
               simulator.CommsControlRegister = operands[0];
           }),
            [assembler.CommandType.STORECOMSTATUS] = new Action<simulator, List<BitArray>>((simulator, operands) =>
           {
               incrementCounter(simulator, 2);
               simulator.mainMemory[operands[0].ToNumeral()] = simulator.CommsStatusRegister;
           }),
            [assembler.CommandType.STORECOMDATA] = new Action<simulator, List<BitArray>>((simulator, operands) =>
          {
              incrementCounter(simulator, 2);
              simulator.mainMemory[operands[0].ToNumeral()] = simulator.CommsDataRegister;
          }),
            [assembler.CommandType.STOREAATPOINTER] = new Action<simulator, List<BitArray>>((simulator, operands) =>
          {
              incrementCounter(simulator, 2);
              var pointer = simulator.mainMemory[operands[0].ToNumeral()];
              //var finalAddress = simulator.mainMemory[pointer.ToNumeral()];
              simulator.mainMemory[pointer.ToNumeral()] = simulator.ARegister;
          }),
            [assembler.CommandType.LOADAATPOINTER] = new Action<simulator, List<BitArray>>((simulator, operands) =>
           {
               incrementCounter(simulator, 2);
               var pointer = simulator.mainMemory[operands[0].ToNumeral()];
               var finalData = simulator.mainMemory[pointer.ToNumeral()];
               simulator.ARegister = finalData;
           }),

            [assembler.CommandType.MULTIPLY] = new Action<simulator, List<BitArray>>((simulator, operands) =>
           {
               var operandAsInt = operands[0].ToNumeral();
               var dataToAdd = simulator.mainMemory[operandAsInt];
               simulator.BRegister = dataToAdd;
               var a = simulator.ARegister.ToNumeral();
               var b = simulator.BRegister.ToNumeral();
               var result = a * b;
               simulator.ARegister = result.ToBinary();

               incrementCounter(simulator, 2);
           }),
            [assembler.CommandType.DIVIDE] = new Action<simulator, List<BitArray>>((simulator, operands) =>
            {
                var operandAsInt = operands[0].ToNumeral();
                var dataToAdd = simulator.mainMemory[operandAsInt];
                simulator.BRegister = dataToAdd;
                var a = simulator.ARegister.ToNumeral();
                var b = simulator.BRegister.ToNumeral();
                var result = a / b;
                simulator.ARegister = result.ToBinary();

                incrementCounter(simulator, 2);
            }),
            [assembler.CommandType.MODULO] = new Action<simulator, List<BitArray>>((simulator, operands) =>
               {
                   var operandAsInt = operands[0].ToNumeral();
                   var dataToAdd = simulator.mainMemory[operandAsInt];
                   simulator.BRegister = dataToAdd;
                   var a = simulator.ARegister.ToNumeral();
                   var b = simulator.BRegister.ToNumeral();
                   var result = a % b;
                   simulator.ARegister = result.ToBinary();

                   incrementCounter(simulator, 2);
               }),


        };

    }


}
