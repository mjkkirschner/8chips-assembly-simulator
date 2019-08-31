using System;
using System.Collections;
using System.Collections.Generic;
using assembler;
using System.Linq;
using System.Linq.Expressions;
using static assembler.Assembler;
using System.Collections.ObjectModel;
using core;

namespace simulator
{

    public class eightChipsSimulator
    {

        public readonly int wordWidth = 16;
        public ObservableCollection<int> mainMemory;
        public int ARegister { get { return this.Registers[nameof(ARegister)]; } set { this.Registers[nameof(ARegister)] = value; } }
        public int BRegister { get { return this.Registers[nameof(BRegister)]; } set { this.Registers[nameof(BRegister)] = value; } }
        public int ProgramCounter { get { return this.Registers[nameof(ProgramCounter)]; } set { this.Registers[nameof(ProgramCounter)] = value; } }
        public int OutRegister { get { return this.Registers[nameof(OutRegister)]; } set { this.Registers[nameof(OutRegister)] = value; } }
        //0: AGB
        //1: AEB
        //2: ALB
        public int FlagsRegister { get { return this.Registers[nameof(FlagsRegister)]; } set { this.Registers[nameof(FlagsRegister)] = value; } }
        public int CommsControlRegister { get { return this.Registers[nameof(CommsControlRegister)]; } set { this.Registers[nameof(CommsControlRegister)] = value; } }
        public int CommsStatusRegister { get { return this.Registers[nameof(CommsStatusRegister)]; } set { this.Registers[nameof(CommsStatusRegister)] = value; } }
        public int CommsDataRegister { get { return this.Registers[nameof(CommsDataRegister)]; } set { this.Registers[nameof(CommsDataRegister)] = value; } }

        public long TotalInstructionCount = 0;
        public bool HALT = false;

        public Dictionary<string, int> Registers = new Dictionary<string, int>();
        public int instructionBundleSize { get; set; } = 100000;

        public Logger logger { get; private set; } = new Logger();

        /// <summary>
        /// sets the simulator memory to the specified user code at the correct offset.
        /// </summary>
        /// <param name="binaryUserCode"></param>
        /// <returns></returns>
        public Tuple<int, int> setUserCode(int[] binaryUserCode)
        {
            // start at the user code offset in the memory
            // and loop until we're out of values.\
            var userCodeStartOffset = MemoryMap[MemoryMapKeys.user_code].AbsoluteStart;
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
            this.mainMemory = new ObservableCollection<int>(Enumerable.Range(0, memoryLength).Select(x => (int)0));
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
                var operands = new List<int>();
                //TODO - currently we only ever have one operand so just get the next location in memory.
                operands.Add(mainMemory[currentInstructionIndex + 1]);
                var commandFunc = commandToInstructionHelper.map[(assembler.CommandType)currentInstruction];

                //simulate.
                ((Action<eightChipsSimulator, List<int>>)commandFunc)(this, operands);
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
                var operands = new List<int>();
                //TODO - currently we only ever have one operand so just get the next location in memory.
                operands.Add(mainMemory[currentInstructionIndex + 1]);
                var commandFunc = commandToInstructionHelper.map[(assembler.CommandType)currentInstruction];

                //simulate.
                ((Action<eightChipsSimulator, List<int>>)commandFunc)(this, operands);
            }
        }

        /// <summary>
        /// While the simulator runs, monitor a specific memory address has taken
        /// This function returns a handle that can be used to inspect the values this address has been assigned
        /// so far during the execution.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public MonitorHandle<int> monitor(int address)
        {
            return new MonitorHandle<int>(address, this.mainMemory);
        }

        public void printMemory(int start, int end = ushort.MaxValue)
        {
            var columnsNum = 8;
            for (int i = start; i < end; i = i + columnsNum)
            {
                var line = string.Empty;
                for (var j = 0; j < columnsNum; j++)
                {
                    line = $"{line} | {this.mainMemory[i + j]}";
                }
                Console.WriteLine($"{i}: {line} ");
            }
        }
    }
    /// <summary>
    /// maps assembly instructions to functions that effect those commands on the simulator platform.
    /// </summary>
    static class commandToInstructionHelper
    {
        public static void incrementCounter(eightChipsSimulator simulator, int num)
        {
            int incrementCounter = (int)(simulator.ProgramCounter + num);
            simulator.ProgramCounter = incrementCounter;
        }

        public static void doMath(CommandType command, List<int> operands, eightChipsSimulator simulator, Func<int, int, int> oper)
        {
            var operandAsInt = operands[0];
            var finalAddress = operandAsInt;
            var dataToAdd = simulator.mainMemory[finalAddress];
            simulator.BRegister = dataToAdd;
            var a = simulator.ARegister;
            var b = simulator.BRegister;
            var result = oper(a, b);
            simulator.ARegister = result;
            simulator.logger.log($"{command}: performing operation on {a} from at A reg to {b} from B reg which was originally at memory address {finalAddress}");

            incrementCounter(simulator, 2);

        }


        public static Dictionary<CommandType, Delegate> map = new Dictionary<CommandType, Delegate>()
        {
            [assembler.CommandType.NOP] = new Action<eightChipsSimulator, List<int>>((simulator, operands) =>
              {
                  incrementCounter(simulator, 1);
              }),


            [assembler.CommandType.LOADA] = new Action<eightChipsSimulator, List<int>>((simulator, operands) =>
            { // when we get a load A command,
              // we go lookup the value at memory location operand[0] - then
              // store this in A register.
                var operandAsInt = operands[0];
                simulator.ARegister = simulator.mainMemory[operandAsInt];
                simulator.logger.log($"LOADA:loading into A Reg {simulator.mainMemory[operandAsInt]} from at RAM[{operandAsInt}]");
                incrementCounter(simulator, 2);

            }),
            [assembler.CommandType.OUTA] = new Action<eightChipsSimulator, List<int>>((simulator, operands) =>
           {
               simulator.OutRegister = simulator.ARegister;
               simulator.logger.log($"OUTA: copy A to OUT REG {simulator.OutRegister}");
               incrementCounter(simulator, 1);
           }),
            [assembler.CommandType.ADD] = new Action<eightChipsSimulator, List<int>>((simulator, operands) =>
           {
               doMath(assembler.CommandType.ADD, operands, simulator, (a, b) => { return (int)(a + b); });

           }),
            [assembler.CommandType.SUBTRACT] = new Action<eightChipsSimulator, List<int>>((simulator, operands) =>
           {
               doMath(assembler.CommandType.SUBTRACT, operands, simulator, (a, b) => { return (int)(a - b); });

           }),
            [assembler.CommandType.STOREA] = new Action<eightChipsSimulator, List<int>>((simulator, operands) =>
           {
               var operandAsInt = operands[0];
               simulator.mainMemory[operandAsInt] = simulator.ARegister;
               simulator.logger.log($"STOREA:storing {simulator.ARegister} from A register at RAM[{operandAsInt}]");

               incrementCounter(simulator, 2);
           }),
            [assembler.CommandType.LOADAIMMEDIATE] = new Action<eightChipsSimulator, List<int>>((simulator, operands) =>
           {
               simulator.ARegister = operands[0];
               simulator.logger.log($"LOADAIMMEDIATE: LOADING {simulator.ARegister} Into A register from RAM[{operands[0]}]");
               incrementCounter(simulator, 2);
           }),
            [assembler.CommandType.JUMP] = new Action<eightChipsSimulator, List<int>>((simulator, operands) =>
            {
                simulator.logger.log($"JUMP: JUMP TO {operands[0]}");
                simulator.ProgramCounter = operands[0];
            }),
            [assembler.CommandType.JUMPIFEQUAL] = new Action<eightChipsSimulator, List<int>>((simulator, operands) =>
           {
               incrementCounter(simulator, 2);
               if (simulator.ARegister == simulator.BRegister)
               {
                   simulator.logger.log($"JUMPIFEQUAL: JUMP TO {operands[0]} : JUMPING");
                   simulator.ProgramCounter = operands[0];
               }
               simulator.logger.log($"JUMPIFEQUAL: JUMP TO {operands[0]}: NOT JUMPING");
           }),
            [assembler.CommandType.JUMPIFLESS] = new Action<eightChipsSimulator, List<int>>((simulator, operands) =>
           {
               incrementCounter(simulator, 2);
               if (simulator.ARegister < simulator.BRegister)
               {
                   simulator.logger.log($"JUMPIFLESS: JUMP TO {operands[0]} : JUMPING");
                   simulator.ProgramCounter = operands[0];
               }
               simulator.logger.log($"JUMPIFLESS: JUMP TO {operands[0]} : NOT JUMPING");
           }),
            [assembler.CommandType.JUMPIFGREATER] = new Action<eightChipsSimulator, List<int>>((simulator, operands) =>
           {
               incrementCounter(simulator, 2);
               if (simulator.ARegister > simulator.BRegister)
               {
                   simulator.logger.log($"JUMPIFGREATER: JUMP TO {operands[0]} : JUMPING");
                   simulator.ProgramCounter = operands[0];
               }
               simulator.logger.log($"JUMPIFGREATER: JUMP TO {operands[0]} : NOT JUMPING");
           }),
            [assembler.CommandType.LOADB] = new Action<eightChipsSimulator, List<int>>((simulator, operands) =>
            {
                var operandAsInt = operands[0];
                var finalAddress = operandAsInt;
                simulator.BRegister = simulator.mainMemory[operandAsInt];
                simulator.logger.log($"LOADB:loading into B Reg {simulator.mainMemory[finalAddress]} from at RAM[{finalAddress}]");
                incrementCounter(simulator, 2);

            }),
            [assembler.CommandType.LOADBIMMEDIATE] = new Action<eightChipsSimulator, List<int>>((simulator, operands) =>
          {
              simulator.BRegister = operands[0];
              simulator.logger.log($"LOADBIMMEDIATE: LOADING {simulator.BRegister} Into B register from RAM[{operands[0]}]");
              incrementCounter(simulator, 2);
          }),
            [assembler.CommandType.STOREB] = new Action<eightChipsSimulator, List<int>>((simulator, operands) =>
             {
                 var operandAsInt = operands[0];
                 var finalAddress = operandAsInt;
                 simulator.mainMemory[operandAsInt] = simulator.BRegister;
                 simulator.logger.log($"STOREB:storing {simulator.BRegister} from B register at RAM[{finalAddress}]");
                 incrementCounter(simulator, 2);
             }),
            [assembler.CommandType.UPDATEFLAGS] = new Action<eightChipsSimulator, List<int>>((simulator, operands) =>
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
                simulator.FlagsRegister = (int)(simulator.FlagsRegister + 2);
            }
            if (a < b)
            {
                simulator.FlagsRegister = (int)(simulator.FlagsRegister + 4);
            }
        }),
            [assembler.CommandType.HALT] = new Action<eightChipsSimulator, List<int>>((simulator, operands) =>
        {
            simulator.HALT = true;
        }),
            [assembler.CommandType.LOADCONTROLIMMEDIATE] = new Action<eightChipsSimulator, List<int>>((simulator, operands) =>
           {
               incrementCounter(simulator, 2);
               simulator.CommsControlRegister = operands[0];
           }),
            [assembler.CommandType.STORECOMSTATUS] = new Action<eightChipsSimulator, List<int>>((simulator, operands) =>
           {
               incrementCounter(simulator, 2);
               simulator.mainMemory[operands[0]] = simulator.CommsStatusRegister;
           }),
            [assembler.CommandType.STORECOMDATA] = new Action<eightChipsSimulator, List<int>>((simulator, operands) =>
          {
              incrementCounter(simulator, 2);
              simulator.mainMemory[operands[0]] = simulator.CommsDataRegister;
          }),
            [assembler.CommandType.STOREAATPOINTER] = new Action<eightChipsSimulator, List<int>>((simulator, operands) =>
          {
              incrementCounter(simulator, 2);
              var pointer = simulator.mainMemory[operands[0]];
              //var finalAddress = simulator.mainMemory[pointer.ToNumeral()];
              simulator.mainMemory[pointer] = simulator.ARegister;
              simulator.logger.log($"STOREAATPOINTER: Storing {simulator.ARegister} at {operands[0]} which points to -> [{pointer}] ");
          }),
            [assembler.CommandType.LOADAATPOINTER] = new Action<eightChipsSimulator, List<int>>((simulator, operands) =>
           {
               incrementCounter(simulator, 2);
               var pointer = simulator.mainMemory[operands[0]];
               var finalData = simulator.mainMemory[pointer];
               simulator.ARegister = finalData;
               simulator.logger.log($"LOADAATPOINTER: LOADING {simulator.ARegister} Into A REG from {operands[0]} which points to -> [{pointer}] ");
           }),

            [assembler.CommandType.MULTIPLY] = new Action<eightChipsSimulator, List<int>>((simulator, operands) =>
           {
               doMath(assembler.CommandType.MULTIPLY, operands, simulator, (a, b) => { return (int)(a * b); });
           }),
            [assembler.CommandType.DIVIDE] = new Action<eightChipsSimulator, List<int>>((simulator, operands) =>
            {
                doMath(assembler.CommandType.DIVIDE, operands, simulator, (a, b) => { return (int)(a / b); });

            }),
            [assembler.CommandType.MODULO] = new Action<eightChipsSimulator, List<int>>((simulator, operands) =>
               {
                   doMath(assembler.CommandType.MODULO, operands, simulator, (a, b) => { return (int)(a % b); });
               }),

            [assembler.CommandType.AND] = new Action<eightChipsSimulator, List<int>>((simulator, operands) =>
       {
           doMath(assembler.CommandType.AND, operands, simulator, (a, b) => { return (int)(a & b); });
       }),
            [assembler.CommandType.OR] = new Action<eightChipsSimulator, List<int>>((simulator, operands) =>
            {
                doMath(assembler.CommandType.AND, operands, simulator, (a, b) => { return (int)(a | b); });
            }),

            [assembler.CommandType.NOT] = new Action<eightChipsSimulator, List<int>>((simulator, operands) =>
                   {

                       var a = simulator.ARegister;
                       var result = (int)(~a);
                       simulator.ARegister = result;
                       simulator.logger.log($"NOT: Binary not of A register {a} transformed to {result} ");

                       incrementCounter(simulator, 1);
                   }),

        };

    }

}
