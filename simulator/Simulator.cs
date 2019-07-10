using System;
using System.Collections;
using System.Collections.Generic;
using assembler;
using System.Linq;
using System.Linq.Expressions;
using static assembler.Assembler;
using System.Collections.ObjectModel;

namespace simulator
{

    public class eightChipsSimulator
    {

        public readonly int wordWidth = 16;
        public ObservableCollection<ushort> mainMemory;
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

        /// <summary>
        /// sets the simulator memory to the specified user code at the correct offset.
        /// </summary>
        /// <param name="binaryUserCode"></param>
        /// <returns></returns>
        public Tuple<int, int> setUserCode(ushort[] binaryUserCode)
        {
            // start at the user code offset in the memory
            // and loop until we're out of values.\
            var userCodeStartOffset = MemoryMap[MemoryMapKeys.user_code].Item1;
            var j = userCodeStartOffset;
            for (var i = 0; i < binaryUserCode.Length; i++)
            {
                this.mainMemory[j] = binaryUserCode[i];
                j = j + 1;
            }
            //TODO check if we go out of range.
            return Tuple.Create(userCodeStartOffset, j);
        }

        public eightChipsSimulator(int wordWidth, int memoryLength)
        {
            this.wordWidth = wordWidth;
            this.mainMemory = new ObservableCollection<ushort>(Enumerable.Range(0, memoryLength).Select(x => (ushort)0));
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
                ((Action<eightChipsSimulator, List<ushort>>)commandFunc)(this, operands);
            }
        }

        public void runSimulation(int steps)
        {
            var instructionCount = 0;

            while (!HALT && steps < instructionCount)
            {
                instructionCount++;
                this.TotalInstructionCount++;

                //fetch instruction from the program counter.
                var currentInstructionIndex = ProgramCounter;
                var currentInstruction = mainMemory[currentInstructionIndex];
                var operands = new List<ushort>();
                //TODO - currently we only ever have one operand so just get the next location in memory.
                operands.Add(mainMemory[currentInstructionIndex + 1]);
                var commandFunc = commandToInstructionHelper.map[(assembler.CommandType)currentInstruction];

                //simulate.
                ((Action<eightChipsSimulator, List<ushort>>)commandFunc)(this, operands);
            }
        }

        /// <summary>
        /// While the simulator runs, monitor a specific memory address has taken
        /// This function returns a handle that can be used to inspect the values this address has been assigned
        /// so far during the execution.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public MonitorHandle<ushort> monitor(ushort address)
        {
            return new MonitorHandle<ushort>(address, this.mainMemory);
        }
    }
    static class commandToInstructionHelper
    {
        public static void incrementCounter(eightChipsSimulator simulator, ushort num)
        {
            ushort incrementCounter = (ushort)(simulator.ProgramCounter + num);
            simulator.ProgramCounter = incrementCounter;
        }

        public static Dictionary<CommandType, Delegate> map = new Dictionary<CommandType, Delegate>()
        {
            [assembler.CommandType.NOP] = new Action<eightChipsSimulator, List<ushort>>((simulator, operands) =>
              {
                  incrementCounter(simulator, 1);
              }),


            [assembler.CommandType.LOADA] = new Action<eightChipsSimulator, List<ushort>>((simulator, operands) =>
            { // when we get a load A command,
              // we go lookup the value at memory location operand[0] - then
              // store this in A register.
                var operandAsInt = operands[0];
                simulator.ARegister = simulator.mainMemory[operandAsInt];

                incrementCounter(simulator, 2);

            }),
            [assembler.CommandType.OUTA] = new Action<eightChipsSimulator, List<ushort>>((simulator, operands) =>
           {
               simulator.OutRegister = simulator.ARegister;
               incrementCounter(simulator, 1);
           }),
            [assembler.CommandType.ADD] = new Action<eightChipsSimulator, List<ushort>>((simulator, operands) =>
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
            [assembler.CommandType.SUBTRACT] = new Action<eightChipsSimulator, List<ushort>>((simulator, operands) =>
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
            [assembler.CommandType.STOREA] = new Action<eightChipsSimulator, List<ushort>>((simulator, operands) =>
           {
               var operandAsInt = operands[0];
               simulator.mainMemory[operandAsInt] = simulator.ARegister;

               incrementCounter(simulator, 2);
           }),
            [assembler.CommandType.LOADAIMMEDIATE] = new Action<eightChipsSimulator, List<ushort>>((simulator, operands) =>
           {
               simulator.ARegister = operands[0];
               incrementCounter(simulator, 2);
           }),
            [assembler.CommandType.JUMP] = new Action<eightChipsSimulator, List<ushort>>((simulator, operands) =>
            {
                simulator.ProgramCounter = operands[0];
            }),
            [assembler.CommandType.JUMPIFEQUAL] = new Action<eightChipsSimulator, List<ushort>>((simulator, operands) =>
           {
               incrementCounter(simulator, 2);
               if (simulator.ARegister == simulator.BRegister)
               {
                   simulator.ProgramCounter = operands[0];
               }
           }),
            [assembler.CommandType.JUMPIFLESS] = new Action<eightChipsSimulator, List<ushort>>((simulator, operands) =>
           {
               incrementCounter(simulator, 2);
               if (simulator.ARegister < simulator.BRegister)
               {
                   simulator.ProgramCounter = operands[0];
               }
           }),
            [assembler.CommandType.JUMPIFGREATER] = new Action<eightChipsSimulator, List<ushort>>((simulator, operands) =>
           {
               incrementCounter(simulator, 2);
               if (simulator.ARegister > simulator.BRegister)
               {
                   simulator.ProgramCounter = operands[0];
               }
           }),
            [assembler.CommandType.LOADB] = new Action<eightChipsSimulator, List<ushort>>((simulator, operands) =>
            {
                var operandAsInt = operands[0];
                simulator.BRegister = simulator.mainMemory[operandAsInt];

                incrementCounter(simulator, 2);

            }),
            [assembler.CommandType.LOADBIMMEDIATE] = new Action<eightChipsSimulator, List<ushort>>((simulator, operands) =>
          {
              simulator.BRegister = operands[0];
              incrementCounter(simulator, 2);
          }),
            [assembler.CommandType.STOREB] = new Action<eightChipsSimulator, List<ushort>>((simulator, operands) =>
             {
                 var operandAsInt = operands[0];
                 simulator.mainMemory[operandAsInt] = simulator.BRegister;

                 incrementCounter(simulator, 2);
             }),
            [assembler.CommandType.UPDATEFLAGS] = new Action<eightChipsSimulator, List<ushort>>((simulator, operands) =>
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
            [assembler.CommandType.HALT] = new Action<eightChipsSimulator, List<ushort>>((simulator, operands) =>
        {
            simulator.HALT = true;
        }),
            [assembler.CommandType.LOADCONTROLIMMEDIATE] = new Action<eightChipsSimulator, List<ushort>>((simulator, operands) =>
           {
               incrementCounter(simulator, 2);
               simulator.CommsControlRegister = operands[0];
           }),
            [assembler.CommandType.STORECOMSTATUS] = new Action<eightChipsSimulator, List<ushort>>((simulator, operands) =>
           {
               incrementCounter(simulator, 2);
               simulator.mainMemory[operands[0]] = simulator.CommsStatusRegister;
           }),
            [assembler.CommandType.STORECOMDATA] = new Action<eightChipsSimulator, List<ushort>>((simulator, operands) =>
          {
              incrementCounter(simulator, 2);
              simulator.mainMemory[operands[0]] = simulator.CommsDataRegister;
          }),
            [assembler.CommandType.STOREAATPOINTER] = new Action<eightChipsSimulator, List<ushort>>((simulator, operands) =>
          {
              incrementCounter(simulator, 2);
              var pointer = simulator.mainMemory[operands[0]];
              //var finalAddress = simulator.mainMemory[pointer.ToNumeral()];
              simulator.mainMemory[pointer] = simulator.ARegister;
          }),
            [assembler.CommandType.LOADAATPOINTER] = new Action<eightChipsSimulator, List<ushort>>((simulator, operands) =>
           {
               incrementCounter(simulator, 2);
               var pointer = simulator.mainMemory[operands[0]];
               var finalData = simulator.mainMemory[pointer];
               simulator.ARegister = finalData;
           }),

            [assembler.CommandType.MULTIPLY] = new Action<eightChipsSimulator, List<ushort>>((simulator, operands) =>
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
            [assembler.CommandType.DIVIDE] = new Action<eightChipsSimulator, List<ushort>>((simulator, operands) =>
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
            [assembler.CommandType.MODULO] = new Action<eightChipsSimulator, List<ushort>>((simulator, operands) =>
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
