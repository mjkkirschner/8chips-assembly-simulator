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
        public ObservableCollection<short> mainMemory;
        public short pageRegister
        {
            get { return this.Registers[nameof(pageRegister)]; }
            set
            {
                if (value > 1 || value < 0)
                {
                    throw new Exception("page value outside valid range");
                }
                this.Registers[nameof(pageRegister)] = value;
            }
        }
        public short ARegister { get { return this.Registers[nameof(ARegister)]; } set { this.Registers[nameof(ARegister)] = value; } }
        public short BRegister { get { return this.Registers[nameof(BRegister)]; } set { this.Registers[nameof(BRegister)] = value; } }
        public short ProgramCounter { get { return this.Registers[nameof(ProgramCounter)]; } set { this.Registers[nameof(ProgramCounter)] = value; } }
        public short OutRegister { get { return this.Registers[nameof(OutRegister)]; } set { this.Registers[nameof(OutRegister)] = value; } }
        //0: AGB
        //1: AEB
        //2: ALB
        public short FlagsRegister { get { return this.Registers[nameof(FlagsRegister)]; } set { this.Registers[nameof(FlagsRegister)] = value; } }
        public short CommsControlRegister { get { return this.Registers[nameof(CommsControlRegister)]; } set { this.Registers[nameof(CommsControlRegister)] = value; } }
        public short CommsStatusRegister { get { return this.Registers[nameof(CommsStatusRegister)]; } set { this.Registers[nameof(CommsStatusRegister)] = value; } }
        public short CommsDataRegister { get { return this.Registers[nameof(CommsDataRegister)]; } set { this.Registers[nameof(CommsDataRegister)] = value; } }

        public long TotalInstructionCount = 0;
        public bool HALT = false;

        public Dictionary<string, short> Registers = new Dictionary<string, short>();
        public int instructionBundleSize { get; set; } = 100000;

        /// <summary>
        /// sets the simulator memory to the specified user code at the correct offset.
        /// </summary>
        /// <param name="binaryUserCode"></param>
        /// <returns></returns>
        public MemoryMapSegment setUserCode(short[] binaryUserCode)
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
            return new MemoryMapSegment(userCodeStartOffset, j, MemoryMap[MemoryMapKeys.user_code].PageLength);
        }

        public eightChipsSimulator(int wordWidth, int memoryLength)
        {
            this.wordWidth = wordWidth;
            this.mainMemory = new ObservableCollection<short>(Enumerable.Range(0, memoryLength).Select(x => (short)0));
            this.ARegister = 0;
            this.BRegister = 0;
            this.ProgramCounter = 0;
            this.OutRegister = 0;
            this.CommsControlRegister = 0;
            this.CommsStatusRegister = 0;
            this.CommsDataRegister = 0;
            this.pageRegister = 0;
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
                //TODO should program counter also use page so we can address all 64k of memory? or
                //should it be limited to first page?
                var currentInstruction = mainMemory[currentInstructionIndex];
                var operands = new List<short>();
                //TODO - currently we only ever have one operand so just get the next location in memory.
                operands.Add(mainMemory[currentInstructionIndex + 1]);
                var commandFunc = commandToInstructionHelper.map[(assembler.CommandType)currentInstruction];

                //simulate.
                ((Action<eightChipsSimulator, List<short>>)commandFunc)(this, operands);
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
                var operands = new List<short>();
                //TODO - currently we only ever have one operand so just get the next location in memory.
                operands.Add(mainMemory[currentInstructionIndex + 1]);
                var commandFunc = commandToInstructionHelper.map[(assembler.CommandType)currentInstruction];

                //simulate.
                ((Action<eightChipsSimulator, List<short>>)commandFunc)(this, operands);
            }
        }

        /// <summary>
        /// While the simulator runs, monitor a specific memory address has taken
        /// This function returns a handle that can be used to inspect the values this address has been assigned
        /// so far during the execution.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public MonitorHandle<short> monitor(short address, int page)
        {
            return new MonitorHandle<short>(address, page, this.mainMemory);
        }

        public void printMemory(int start, int end = short.MaxValue)
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
        public static void incrementCounter(eightChipsSimulator simulator, short num)
        {
            short incrementCounter = (short)(simulator.ProgramCounter + num);
            simulator.ProgramCounter = incrementCounter;
        }

        public static int calculateFinalMemoryAddressFromPageAndAddress(short operand, eightChipsSimulator simulator)
        {
            //we might add 32k and 32k and we need to return 64k (outside the range of a short)
            return operand + (simulator.pageRegister * short.MaxValue);
        }

        public static void doMath(CommandType command, List<short> operands, eightChipsSimulator simulator, Func<short, short, short> oper)
        {
            var operandAsInt = operands[0];
            var finalAddress = calculateFinalMemoryAddressFromPageAndAddress(operandAsInt, simulator);
            var dataToAdd = simulator.mainMemory[finalAddress];
            simulator.BRegister = dataToAdd;
            var a = simulator.ARegister;
            var b = simulator.BRegister;
            var result = oper(a, b);
            simulator.ARegister = result;
            Console.WriteLine($"{command}: performing operation on {a} from at A reg to {b} from B reg which was originally at memory address {finalAddress}");

            incrementCounter(simulator, 2);

        }

        public static Dictionary<CommandType, Delegate> map = new Dictionary<CommandType, Delegate>()
        {
            [assembler.CommandType.NOP] = new Action<eightChipsSimulator, List<short>>((simulator, operands) =>
              {
                  incrementCounter(simulator, 1);
              }),


            [assembler.CommandType.LOADA] = new Action<eightChipsSimulator, List<short>>((simulator, operands) =>
            { // when we get a load A command,
              // we go lookup the value at memory location operand[0] - then
              // store this in A register.
                var operandAsInt = operands[0];
                //calculate offset using current memory page value
                var finalAddress = calculateFinalMemoryAddressFromPageAndAddress(operandAsInt, simulator);
                simulator.ARegister = simulator.mainMemory[finalAddress];
                Console.WriteLine($"LOADA:loading into A Reg {simulator.mainMemory[finalAddress]} from at RAM[{finalAddress}]");
                incrementCounter(simulator, 2);

            }),
            [assembler.CommandType.OUTA] = new Action<eightChipsSimulator, List<short>>((simulator, operands) =>
           {
               simulator.OutRegister = simulator.ARegister;
               incrementCounter(simulator, 1);
           }),
            [assembler.CommandType.ADD] = new Action<eightChipsSimulator, List<short>>((simulator, operands) =>
           {
               doMath(assembler.CommandType.ADD, operands, simulator, (a, b) => { return (short)(a + b); });
           }),
            [assembler.CommandType.SUBTRACT] = new Action<eightChipsSimulator, List<short>>((simulator, operands) =>
           {
               doMath(assembler.CommandType.SUBTRACT, operands, simulator, (a, b) => { return (short)(a - b); });
           }),
            [assembler.CommandType.STOREA] = new Action<eightChipsSimulator, List<short>>((simulator, operands) =>
           {
               var operandAsInt = operands[0];
               var finalAddress = calculateFinalMemoryAddressFromPageAndAddress(operandAsInt, simulator);
               simulator.mainMemory[finalAddress] = simulator.ARegister;
               Console.WriteLine($"STOREA:storing {simulator.ARegister} from A register at RAM[{finalAddress}]");

               incrementCounter(simulator, 2);
           }),
            [assembler.CommandType.LOADAIMMEDIATE] = new Action<eightChipsSimulator, List<short>>((simulator, operands) =>
           {
               simulator.ARegister = operands[0];
               incrementCounter(simulator, 2);
           }),
            [assembler.CommandType.JUMP] = new Action<eightChipsSimulator, List<short>>((simulator, operands) =>
            {
                simulator.ProgramCounter = operands[0];
            }),
            [assembler.CommandType.JUMPIFEQUAL] = new Action<eightChipsSimulator, List<short>>((simulator, operands) =>
           {
               incrementCounter(simulator, 2);
               if (simulator.ARegister == simulator.BRegister)
               {
                   simulator.ProgramCounter = operands[0];
               }
           }),
            [assembler.CommandType.JUMPIFLESS] = new Action<eightChipsSimulator, List<short>>((simulator, operands) =>
           {
               incrementCounter(simulator, 2);
               if (simulator.ARegister < simulator.BRegister)
               {
                   simulator.ProgramCounter = operands[0];
               }
           }),
            [assembler.CommandType.JUMPIFGREATER] = new Action<eightChipsSimulator, List<short>>((simulator, operands) =>
           {
               incrementCounter(simulator, 2);
               if (simulator.ARegister > simulator.BRegister)
               {
                   simulator.ProgramCounter = operands[0];
               }
           }),
            [assembler.CommandType.LOADB] = new Action<eightChipsSimulator, List<short>>((simulator, operands) =>
            {
                var operandAsInt = operands[0];
                var finalAddress = calculateFinalMemoryAddressFromPageAndAddress(operandAsInt, simulator);
                simulator.BRegister = simulator.mainMemory[finalAddress];

                Console.WriteLine($"LOADB:loading into B Reg {simulator.mainMemory[finalAddress]} from at RAM[{finalAddress}]");
                incrementCounter(simulator, 2);

            }),
            [assembler.CommandType.LOADBIMMEDIATE] = new Action<eightChipsSimulator, List<short>>((simulator, operands) =>
          {
              simulator.BRegister = operands[0];
              incrementCounter(simulator, 2);
          }),
            [assembler.CommandType.STOREB] = new Action<eightChipsSimulator, List<short>>((simulator, operands) =>
             {
                 var operandAsInt = operands[0];
                 var finalAddress = calculateFinalMemoryAddressFromPageAndAddress(operandAsInt, simulator);

                 simulator.mainMemory[finalAddress] = simulator.BRegister;
                 Console.WriteLine($"STOREB:storing {simulator.BRegister} from B register at RAM[{finalAddress}]");

                 incrementCounter(simulator, 2);
             }),
            [assembler.CommandType.UPDATEFLAGS] = new Action<eightChipsSimulator, List<short>>((simulator, operands) =>
        {
            incrementCounter(simulator, 1);
            var a = simulator.ARegister;
            var b = simulator.BRegister;
            // TODO these values may no longer be accurate if this register is a short...
            // - we may want to keep this register a bit array - or an ushort... does not
            // really matter as long as we known the values which map to the bit mappings.
            //0000 0000 0000 0000
            //0000 0000 0000 0xxx.
            //which represent the following 3 flags.
            simulator.FlagsRegister = 0;
            if (a > b)
            {
                simulator.FlagsRegister = 1;
            }
            if (a == b)
            {
                simulator.FlagsRegister = (short)(simulator.FlagsRegister + 2);
            }
            if (a < b)
            {
                simulator.FlagsRegister = (short)(simulator.FlagsRegister + 4);
            }
        }),
            [assembler.CommandType.HALT] = new Action<eightChipsSimulator, List<short>>((simulator, operands) =>
        {
            simulator.HALT = true;
        }),
            [assembler.CommandType.LOADCONTROLIMMEDIATE] = new Action<eightChipsSimulator, List<short>>((simulator, operands) =>
           {
               incrementCounter(simulator, 2);
               simulator.CommsControlRegister = operands[0];
           }),
            [assembler.CommandType.STORECOMSTATUS] = new Action<eightChipsSimulator, List<short>>((simulator, operands) =>
           {
               incrementCounter(simulator, 2);
               var finalAddress = calculateFinalMemoryAddressFromPageAndAddress(operands[0], simulator);
               simulator.mainMemory[finalAddress] = simulator.CommsStatusRegister;
           }),
            [assembler.CommandType.STORECOMDATA] = new Action<eightChipsSimulator, List<short>>((simulator, operands) =>
          {
              incrementCounter(simulator, 2);
              var finalAddress = calculateFinalMemoryAddressFromPageAndAddress(operands[0], simulator);
              simulator.mainMemory[finalAddress] = simulator.CommsDataRegister;
          }),
            [assembler.CommandType.STOREAATPOINTER] = new Action<eightChipsSimulator, List<short>>((simulator, operands) =>
          {
              incrementCounter(simulator, 2);
              var finalAddress = calculateFinalMemoryAddressFromPageAndAddress(operands[0], simulator);
              var pointer = simulator.mainMemory[finalAddress];
              simulator.mainMemory[pointer] = simulator.ARegister;
          }),
            [assembler.CommandType.LOADAATPOINTER] = new Action<eightChipsSimulator, List<short>>((simulator, operands) =>
           {
               incrementCounter(simulator, 2);
               var finalAddress = calculateFinalMemoryAddressFromPageAndAddress(operands[0], simulator);
               var pointer = simulator.mainMemory[finalAddress];
               var finalData = simulator.mainMemory[pointer];
               simulator.ARegister = finalData;
           }),

            [assembler.CommandType.MULTIPLY] = new Action<eightChipsSimulator, List<short>>((simulator, operands) =>
           {
               doMath(assembler.CommandType.MULTIPLY, operands, simulator, (a, b) => { return (short)(a * b); });
           }),
            [assembler.CommandType.DIVIDE] = new Action<eightChipsSimulator, List<short>>((simulator, operands) =>
            {
                doMath(assembler.CommandType.DIVIDE, operands, simulator, (a, b) => { return (short)(a / b); });
            }),
            [assembler.CommandType.MODULO] = new Action<eightChipsSimulator, List<short>>((simulator, operands) =>
               {
                   doMath(assembler.CommandType.MODULO, operands, simulator, (a, b) => { return (short)(a % b); });
               }),

            [assembler.CommandType.AND] = new Action<eightChipsSimulator, List<short>>((simulator, operands) =>
       {
           doMath(assembler.CommandType.AND, operands, simulator, (a, b) => { return (short)(a & b); });

       }),
            [assembler.CommandType.OR] = new Action<eightChipsSimulator, List<short>>((simulator, operands) =>
            {
                doMath(assembler.CommandType.AND, operands, simulator, (a, b) => { return (short)(a | b); });

            }),
            [assembler.CommandType.NOT] = new Action<eightChipsSimulator, List<short>>((simulator, operands) =>
                   {

                       var a = simulator.ARegister;
                       var result = (short)(~a);
                       simulator.ARegister = result;

                       incrementCounter(simulator, 1);
                   }),
            [assembler.CommandType.LOADPAGEIMMEDIATE] = new Action<eightChipsSimulator, List<short>>((simulator, operands) =>
             {
                 simulator.pageRegister = operands[0];
                 incrementCounter(simulator, 2);
             }),
            [assembler.CommandType.LOADPAGE] = new Action<eightChipsSimulator, List<short>>((simulator, operands) =>
            {
                var operandAsInt = operands[0];
                 //calculate offset using current memory page value
                 var finalAddress = calculateFinalMemoryAddressFromPageAndAddress(operandAsInt, simulator);
                simulator.pageRegister = simulator.mainMemory[finalAddress];
                Console.WriteLine($"LOADPAGE:loading into PAGE Reg {simulator.mainMemory[finalAddress]} from at RAM[{finalAddress}]");
                incrementCounter(simulator, 2);
            })
        };

    }

}
