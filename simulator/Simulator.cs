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

        public readonly int wordWidth = 16;
        public List<ushort> mainMemory;
        public ushort ARegister { get { return this.Registers[nameof(ARegister)]; } set { this.Registers[nameof(ARegister)] = value; } }
        public ushort BRegister { get { return this.Registers[nameof(BRegister)]; } set { this.Registers[nameof(BRegister)] = value; } }
        public ushort ProgramCounter { get { return this.Registers[nameof(ProgramCounter)]; } set { this.Registers[nameof(ProgramCounter)] = value; } }
        public ushort OutRegister { get { return this.Registers[nameof(OutRegister)]; } set { this.Registers[nameof(OutRegister)] = value; } }
        //0: AGB
        //1: AEB
        //2: ALB
        public ushort FlagsRegister { get { return this.Registers[nameof(FlagsRegister)]; } set { this.Registers[nameof(FlagsRegister)] = value; } }
        public ushort CommsControlRegister { get { return this.Registers[nameof(CommsControlRegister)]; } set { this.Registers[nameof(CommsControlRegister)] = value; } }
        public ushort CommsStatusRegister { get { return this.Registers[nameof(CommsStatusRegister)]; } set { this.Registers[nameof(CommsStatusRegister)] = value; } }
        public ushort CommsDataRegister { get { return this.Registers[nameof(CommsDataRegister)]; } set { this.Registers[nameof(CommsDataRegister)] = value; } }

        public long TotalInstructionCount = 0;
        public bool HALT = false;

        public Dictionary<string, ushort> Registers = new Dictionary<string, ushort>();
        public int instructionBundleSize { get; set; } = 100000;



        public simulator(int wordWidth, int memoryLength)
        {
            this.wordWidth = wordWidth;
            this.mainMemory = Enumerable.Range(0, memoryLength).Select(x => (ushort)0).ToList();
            this.ARegister = 0;
            this.BRegister = 0;
            this.ProgramCounter = 0;
            this.OutRegister = 0;
            this.CommsControlRegister = 0;
            this.CommsStatusRegister = 0;
            this.CommsDataRegister = 0;
        }
        //starts the computer.
        public void runSimulation()
        {
            var instructionCount = 0;

            while (!HALT)
            {
                instructionCount++;
                this.TotalInstructionCount++;
                if (instructionCount > this.instructionBundleSize)
                {
                    instructionCount = 0;
                    System.Threading.Thread.Sleep(100);
                }
                //fetch instruction from the program counter.
                var currentInstructionIndex = ProgramCounter;
                var currentInstruction = mainMemory[currentInstructionIndex];
                var operands = new List<ushort>();
                //TODO - currently we only ever have one operand so just get the next location in memory.
                operands.Add(mainMemory[currentInstructionIndex + 1]);
                var commandFunc = commandToInstructionHelper.map[(assembler.CommandType)currentInstruction];

                //simulate.
                ((Action<simulator, List<ushort>>)commandFunc)(this, operands);
            }

        }
    }
    static class commandToInstructionHelper
    {
        public static void incrementCounter(simulator simulator, ushort num)
        {
            ushort incrementCounter = (ushort)(simulator.ProgramCounter + num);
            simulator.ProgramCounter = incrementCounter;
        }

        public static Dictionary<CommandType, Delegate> map = new Dictionary<CommandType, Delegate>()
        {
            [assembler.CommandType.NOP] = new Action<simulator, List<ushort>>((simulator, operands) =>
              {
                  incrementCounter(simulator, 1);
              }),


            [assembler.CommandType.LOADA] = new Action<simulator, List<ushort>>((simulator, operands) =>
            { // when we get a load A command,
              // we go lookup the value at memory location operand[0] - then
              // store this in A register.
            var operandAsInt = operands[0];
                simulator.ARegister = simulator.mainMemory[operandAsInt];

                incrementCounter(simulator, 2);

            }),
            [assembler.CommandType.OUTA] = new Action<simulator, List<ushort>>((simulator, operands) =>
           {
               simulator.OutRegister = simulator.ARegister;
               incrementCounter(simulator, 1);
           }),
            [assembler.CommandType.ADD] = new Action<simulator, List<ushort>>((simulator, operands) =>
           {
               var operandAsInt = operands[0];
               var dataToAdd = simulator.mainMemory[operandAsInt];
               simulator.BRegister = dataToAdd;
               var a = simulator.ARegister;
               var b = simulator.BRegister;
               var result = (ushort)(a + b);
               simulator.ARegister = result;

               incrementCounter(simulator, 2);
           }),
            [assembler.CommandType.SUBTRACT] = new Action<simulator, List<ushort>>((simulator, operands) =>
           {
               var operandAsInt = operands[0];
               var dataToAdd = simulator.mainMemory[operandAsInt];
               simulator.BRegister = dataToAdd;
               var a = simulator.ARegister;
               var b = simulator.BRegister;
               var result = (ushort)(a - b);
               simulator.ARegister = result;

               incrementCounter(simulator, 2);
           }),
            [assembler.CommandType.STOREA] = new Action<simulator, List<ushort>>((simulator, operands) =>
           {
               var operandAsInt = operands[0];
               simulator.mainMemory[operandAsInt] = simulator.ARegister;

               incrementCounter(simulator, 2);
           }),
            [assembler.CommandType.LOADAIMMEDIATE] = new Action<simulator, List<ushort>>((simulator, operands) =>
           {
               simulator.ARegister = operands[0];
               incrementCounter(simulator, 2);
           }),
            [assembler.CommandType.JUMP] = new Action<simulator, List<ushort>>((simulator, operands) =>
            {
                simulator.ProgramCounter = operands[0];
            }),
            [assembler.CommandType.JUMPIFEQUAL] = new Action<simulator, List<ushort>>((simulator, operands) =>
           {
               incrementCounter(simulator, 2);
               if (simulator.ARegister == simulator.BRegister)
               {
                   simulator.ProgramCounter = operands[0];
               }
           }),
            [assembler.CommandType.JUMPIFLESS] = new Action<simulator, List<ushort>>((simulator, operands) =>
           {
               incrementCounter(simulator, 2);
               if (simulator.ARegister < simulator.BRegister)
               {
                   simulator.ProgramCounter = operands[0];
               }
           }),
            [assembler.CommandType.JUMPIFGREATER] = new Action<simulator, List<ushort>>((simulator, operands) =>
           {
               incrementCounter(simulator, 2);
               if (simulator.ARegister > simulator.BRegister)
               {
                   simulator.ProgramCounter = operands[0];
               }
           }),
            [assembler.CommandType.LOADB] = new Action<simulator, List<ushort>>((simulator, operands) =>
            {
                var operandAsInt = operands[0];
                simulator.BRegister = simulator.mainMemory[operandAsInt];

                incrementCounter(simulator, 2);

            }),
            [assembler.CommandType.LOADBIMMEDIATE] = new Action<simulator, List<ushort>>((simulator, operands) =>
          {
              simulator.BRegister = operands[0];
              incrementCounter(simulator, 2);
          }),
            [assembler.CommandType.STOREB] = new Action<simulator, List<ushort>>((simulator, operands) =>
             {
                 var operandAsInt = operands[0];
                 simulator.mainMemory[operandAsInt] = simulator.BRegister;

                 incrementCounter(simulator, 2);
             }),
            [assembler.CommandType.UPDATEFLAGS] = new Action<simulator, List<ushort>>((simulator, operands) =>
        {
            incrementCounter(simulator, 1);
            var a = simulator.ARegister;
            var b = simulator.BRegister;
            simulator.FlagsRegister = 0;
            if (a > b)
            {
                simulator.FlagsRegister = 1;
            }
            if (a == b)
            {
                simulator.FlagsRegister = (ushort)(simulator.FlagsRegister + 2);
            }
            if (a < b)
            {
                simulator.FlagsRegister = (ushort)(simulator.FlagsRegister + 4);
            }
        }),
            [assembler.CommandType.HALT] = new Action<simulator, List<ushort>>((simulator, operands) =>
        {
            simulator.HALT = true;
        }),
            [assembler.CommandType.LOADCONTROLIMMEDIATE] = new Action<simulator, List<ushort>>((simulator, operands) =>
           {
               incrementCounter(simulator, 2);
               simulator.CommsControlRegister = operands[0];
           }),
            [assembler.CommandType.STORECOMSTATUS] = new Action<simulator, List<ushort>>((simulator, operands) =>
           {
               incrementCounter(simulator, 2);
               simulator.mainMemory[operands[0]] = simulator.CommsStatusRegister;
           }),
            [assembler.CommandType.STORECOMDATA] = new Action<simulator, List<ushort>>((simulator, operands) =>
          {
              incrementCounter(simulator, 2);
              simulator.mainMemory[operands[0]] = simulator.CommsDataRegister;
          }),
            [assembler.CommandType.STOREAATPOINTER] = new Action<simulator, List<ushort>>((simulator, operands) =>
          {
              incrementCounter(simulator, 2);
              var pointer = simulator.mainMemory[operands[0]];
          //var finalAddress = simulator.mainMemory[pointer.ToNumeral()];
          simulator.mainMemory[pointer] = simulator.ARegister;
          }),
            [assembler.CommandType.LOADAATPOINTER] = new Action<simulator, List<ushort>>((simulator, operands) =>
           {
               incrementCounter(simulator, 2);
               var pointer = simulator.mainMemory[operands[0]];
               var finalData = simulator.mainMemory[pointer];
               simulator.ARegister = finalData;
           }),

            [assembler.CommandType.MULTIPLY] = new Action<simulator, List<ushort>>((simulator, operands) =>
           {
               var operandAsInt = operands[0];
               var dataToAdd = simulator.mainMemory[operandAsInt];
               simulator.BRegister = dataToAdd;
               var a = simulator.ARegister;
               var b = simulator.BRegister;
               var result = (ushort)(a * b);
               simulator.ARegister = result;

               incrementCounter(simulator, 2);
           }),
            [assembler.CommandType.DIVIDE] = new Action<simulator, List<ushort>>((simulator, operands) =>
            {
                var operandAsInt = operands[0];
                var dataToAdd = simulator.mainMemory[operandAsInt];
                simulator.BRegister = dataToAdd;
                var a = simulator.ARegister;
                var b = simulator.BRegister;
                var result = (ushort)(a / b);
                simulator.ARegister = result;

                incrementCounter(simulator, 2);
            }),
            [assembler.CommandType.MODULO] = new Action<simulator, List<ushort>>((simulator, operands) =>
               {
                   var operandAsInt = operands[0];
                   var dataToAdd = simulator.mainMemory[operandAsInt];
                   simulator.BRegister = dataToAdd;
                   var a = simulator.ARegister;
                   var b = simulator.BRegister;
                   var result = (ushort)(a % b);
                   simulator.ARegister = result;

                   incrementCounter(simulator, 2);
               }),


        };

    }

}
