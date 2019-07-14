using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace vmtranslator
{

    public class vmtranslator
    {
        private string path;
        public vmtranslator(string path)
        {
            this.path = path;

        }

        public string[] TranslateToAssembly()
        {
            // get the file attributes for file or directory
            FileAttributes attr = File.GetAttributes(this.path);
            var codeWriter = new vmIL2ASMWriter();

            if (attr.HasFlag(FileAttributes.Directory))
            {
                throw new NotImplementedException();
            }
            else
            {
                var parser = new vmILParser(this.path, codeWriter);
                parser.Parse();
                Console.WriteLine("output is:");
                codeWriter.Output.ForEach(x => Console.Write(x));
                return codeWriter.Output.ToArray();
            }
        }

    }

    public class vmIL2ASMWriter
    {

        private const string stackPointer_symbol = "SP";
        private const string local_symbol = "LCL";
        private const string arg_symbol = "ARG";

        private const string this_symbol = "THIS";
        private const string that_symbol = "THAT";
        private const string temp_symbol = "TEMP";
        //TODO get rid of this, replace using assembler memory map.
        private int bootloaderOffset = 256;


        public List<string> Output = new List<string>();

        //setup our virtual registers and offset them using our bootloader offset...
        private void setupBaseRegisterSymbols()
        {
            this.Output.Add($"#define {stackPointer_symbol}  {0 + bootloaderOffset}");
            this.Output.Add($"#define {local_symbol}  {1 + bootloaderOffset}");
            this.Output.Add($"#define {arg_symbol}  {2 + bootloaderOffset}");
            this.Output.Add($"#define {this_symbol}  {3 + bootloaderOffset}");
            this.Output.Add($"#define {that_symbol}  {4 + bootloaderOffset}");
            this.Output.Add($"#define {temp_symbol}  {5 + bootloaderOffset}");


            //set the pointers to the right values
            //SP = 33040
            this.Output.Add($"{stackPointer_symbol} = {assembler.Assembler.MemoryMap[assembler.Assembler.MemoryMapKeys.stack].Item1}");
            //OTHER POINTERS NEED TO BE RESET EVERYTIME A VM FUNCTION IS ENTERED....

        }

        public vmIL2ASMWriter()
        {
            this.setupBaseRegisterSymbols();
        }

        private string[] generateDecrement(string symbol)
        {
            return new string[]{
                //load the symbol into A
                assembler.CommandType.LOADA.ToString(),
                symbol,
                //load 1 into b
                assembler.CommandType.LOADBIMMEDIATE.ToString(),
                "1",
                //store B at a location we can reference...
                assembler.CommandType.STOREB.ToString(),
                temp_symbol+ " + 1",
                //subtract the value at that mem address from A
                assembler.CommandType.SUBTRACT.ToString(),
                temp_symbol+ " + 1",
                //store result in original symbol
                assembler.CommandType.STOREA.ToString(),
                symbol

            };
        }

        private string[] generateIncrement(string symbol)
        {
            return new string[]{
                //load the symbol into A
                assembler.CommandType.LOADA.ToString(),
                symbol,
                //load 1 into b
                assembler.CommandType.LOADBIMMEDIATE.ToString(),
                "1",
                //store B at a location we can reference...
                assembler.CommandType.STOREB.ToString(),
                temp_symbol+" + 1",
                //subtract the value at that mem address from A
                assembler.CommandType.ADD.ToString(),
                temp_symbol+" + 1",
                //store result in original symbol
                assembler.CommandType.STOREA.ToString(),
                symbol

            };
        }
        private string[] generateMoveAtoTemp()
        {
            return new string[]{
                assembler.CommandType.STOREA.ToString(),
                //TODO should TEMP be a symbol like this or should we use the int instead directly?
                temp_symbol,
            };
        }

        /// <summary>
        /// decrements the SP and loads the value from SP into A register or B register
        /// </summary>
        /// <returns></returns>
        private string[] generatePOP()
        {
            var output = new List<string>();
            //first decrement the SP.
            output.AddRange(generateDecrement(stackPointer_symbol));
            //then load the value at SP into A register.
            throw new NotImplementedException();


        }

        private void handleArithmeticCommand(Tuple<vmILParser.vmCommmandType, object, string[]> instructionData)
        {
            //depending on the specific ALU command we need to generate the appropriate ASM command
            //there will be no operands for these commands - 
            //instead we will need to include pop twice to get the operands from the stack...
            //the stack lies from location 256 -2047 - we will need to reconcile this with our bootloader... 
            // and assembler offsets..

            //For now - lets just try offsetting everything by 256 - to make room for the bootloader...
            // so 256  - 256+15 = virtual registers
            // 256+16 - 256+255  = static variables
            // 256 + 256 - 256 + 2047 = THE STACK

            //we'll assume there is always a symbol called "stackpointer" which points to the last item in the stack.


            vmILParser.vmArithmetic_Logic_Instructions subCommand = (vmILParser.vmArithmetic_Logic_Instructions)instructionData.Item2;
            //ADD
            if (subCommand == vmILParser.vmArithmetic_Logic_Instructions.add)
            {
                this.Output.AddRange(generateDecrement(stackPointer_symbol));
                this.Output.Add(assembler.CommandType.LOADAATPOINTER.ToString());
                this.Output.Add(stackPointer_symbol);
                this.Output.AddRange(generateMoveAtoTemp());
                this.Output.AddRange(generateDecrement(stackPointer_symbol));

                this.Output.Add(assembler.CommandType.LOADAATPOINTER.ToString());
                this.Output.Add(stackPointer_symbol);
                this.Output.Add(assembler.CommandType.ADD.ToString());
                this.Output.Add(temp_symbol);
                //now store the result in SP
                this.Output.Add(assembler.CommandType.STOREAATPOINTER.ToString());
                this.Output.Add(stackPointer_symbol);
                //and increment
                this.Output.AddRange(generateIncrement(stackPointer_symbol));
            }

            else if (subCommand == vmILParser.vmArithmetic_Logic_Instructions.sub)
            {
                this.Output.AddRange(generateDecrement(stackPointer_symbol));
                this.Output.Add(assembler.CommandType.LOADAATPOINTER.ToString());
                this.Output.Add(stackPointer_symbol);
                this.Output.AddRange(generateMoveAtoTemp());

                this.Output.AddRange(generateDecrement(stackPointer_symbol));
                this.Output.Add(assembler.CommandType.LOADAATPOINTER.ToString());
                this.Output.Add(stackPointer_symbol);
                this.Output.Add(assembler.CommandType.SUBTRACT.ToString());
                this.Output.Add(temp_symbol);
                //now store the result in SP
                this.Output.Add(assembler.CommandType.STOREAATPOINTER.ToString());
                this.Output.Add(stackPointer_symbol);
                //and increment
                this.Output.AddRange(generateIncrement(stackPointer_symbol));
            }

            else if (subCommand == vmILParser.vmArithmetic_Logic_Instructions.eq)
            {
                var blockID = Guid.NewGuid().ToString("N");

                this.Output.AddRange(generateDecrement(stackPointer_symbol));
                this.Output.Add(assembler.CommandType.LOADAATPOINTER.ToString());
                this.Output.Add(stackPointer_symbol);
                this.Output.AddRange(generateMoveAtoTemp());

                this.Output.AddRange(generateDecrement(stackPointer_symbol));
                this.Output.Add(assembler.CommandType.LOADAATPOINTER.ToString());
                this.Output.Add(stackPointer_symbol);
                //load temp into B somehow
                this.Output.Add(assembler.CommandType.LOADB.ToString());
                this.Output.Add(temp_symbol);

                // update flags
                this.Output.Add(assembler.CommandType.UPDATEFLAGS.ToString());
                // we need to return different results to the stack depending on
                // if equal is true or not - can do this using jumps.
                this.Output.Add(assembler.CommandType.JUMPIFEQUAL.ToString());
                this.Output.Add($"EQ_TRUE_{blockID}");

                this.Output.Add(assembler.CommandType.LOADAIMMEDIATE.ToString());
                this.Output.Add("0");
                this.Output.Add(assembler.CommandType.JUMP.ToString());
                this.Output.Add($"EQ_STORE_STACK_{blockID}");

                this.Output.Add($"(EQ_TRUE_{blockID})");
                this.Output.Add(assembler.CommandType.LOADAIMMEDIATE.ToString());
                this.Output.Add("1");

                this.Output.Add($"(EQ_STORE_STACK_{blockID})");
                //now store the result in SP
                this.Output.Add(assembler.CommandType.STOREAATPOINTER.ToString());
                this.Output.Add(stackPointer_symbol);
                //and increment
                this.Output.AddRange(generateIncrement(stackPointer_symbol));
            }

            else if (subCommand == vmILParser.vmArithmetic_Logic_Instructions.gt)
            {
                var blockID = Guid.NewGuid().ToString("N");

                this.Output.AddRange(generateDecrement(stackPointer_symbol));
                this.Output.Add(assembler.CommandType.LOADAATPOINTER.ToString());
                this.Output.Add(stackPointer_symbol);
                this.Output.AddRange(generateMoveAtoTemp());

                this.Output.AddRange(generateDecrement(stackPointer_symbol));
                this.Output.Add(assembler.CommandType.LOADAATPOINTER.ToString());
                this.Output.Add(stackPointer_symbol);
                //load temp into B somehow
                this.Output.Add(assembler.CommandType.LOADB.ToString());
                this.Output.Add(temp_symbol);

                // update flags
                this.Output.Add(assembler.CommandType.UPDATEFLAGS.ToString());
                // we need to return different results to the stack depending on
                // if A>B or not - can do this using jumps.
                this.Output.Add(assembler.CommandType.JUMPIFGREATER.ToString());
                this.Output.Add($"GT_TRUE_{blockID}");

                this.Output.Add(assembler.CommandType.LOADAIMMEDIATE.ToString());
                this.Output.Add("0");
                this.Output.Add(assembler.CommandType.JUMP.ToString());
                this.Output.Add($"GT_STORE_STACK_{blockID}");

                this.Output.Add($"(GT_TRUE_{blockID})");
                this.Output.Add(assembler.CommandType.LOADAIMMEDIATE.ToString());
                this.Output.Add("1");

                this.Output.Add($"(GT_STORE_STACK_{blockID})");
                //now store the result in SP
                this.Output.Add(assembler.CommandType.STOREAATPOINTER.ToString());
                this.Output.Add(stackPointer_symbol);
                //and increment
                this.Output.AddRange(generateIncrement(stackPointer_symbol));
            }

            else if (subCommand == vmILParser.vmArithmetic_Logic_Instructions.lt)
            {
                var blockID = Guid.NewGuid().ToString("N");

                this.Output.AddRange(generateDecrement(stackPointer_symbol));
                this.Output.Add(assembler.CommandType.LOADAATPOINTER.ToString());
                this.Output.Add(stackPointer_symbol);
                this.Output.AddRange(generateMoveAtoTemp());

                this.Output.AddRange(generateDecrement(stackPointer_symbol));
                this.Output.Add(assembler.CommandType.LOADAATPOINTER.ToString());
                this.Output.Add(stackPointer_symbol);
                //load temp into B somehow
                this.Output.Add(assembler.CommandType.LOADB.ToString());
                this.Output.Add(temp_symbol);

                // update flags
                this.Output.Add(assembler.CommandType.UPDATEFLAGS.ToString());
                // we need to return different results to the stack depending on
                // if A>B or not - can do this using jumps.
                this.Output.Add(assembler.CommandType.JUMPIFLESS.ToString());
                this.Output.Add($"LT_TRUE_{blockID}");

                this.Output.Add(assembler.CommandType.LOADAIMMEDIATE.ToString());
                this.Output.Add("0");
                this.Output.Add(assembler.CommandType.JUMP.ToString());
                this.Output.Add($"LT_STORE_STACK_{blockID}");

                this.Output.Add($"(LT_TRUE_{blockID})");
                this.Output.Add(assembler.CommandType.LOADAIMMEDIATE.ToString());
                this.Output.Add("1");

                this.Output.Add($"(LT_STORE_STACK_{blockID})");
                //now store the result in SP
                this.Output.Add(assembler.CommandType.STOREAATPOINTER.ToString());
                this.Output.Add(stackPointer_symbol);
                //and increment
                this.Output.AddRange(generateIncrement(stackPointer_symbol));
            }

            else if (subCommand == vmILParser.vmArithmetic_Logic_Instructions.and)
            {
                this.Output.AddRange(generateDecrement(stackPointer_symbol));
                this.Output.Add(assembler.CommandType.LOADAATPOINTER.ToString());
                this.Output.Add(stackPointer_symbol);
                this.Output.AddRange(generateMoveAtoTemp());
                this.Output.AddRange(generateDecrement(stackPointer_symbol));

                this.Output.Add(assembler.CommandType.LOADAATPOINTER.ToString());
                this.Output.Add(stackPointer_symbol);
                this.Output.Add(assembler.CommandType.AND.ToString());
                this.Output.Add(temp_symbol);
                //now store the result in SP
                this.Output.Add(assembler.CommandType.STOREAATPOINTER.ToString());
                this.Output.Add(stackPointer_symbol);
                //and increment
                this.Output.AddRange(generateIncrement(stackPointer_symbol));
            }
            else if (subCommand == vmILParser.vmArithmetic_Logic_Instructions.or)
            {
                this.Output.AddRange(generateDecrement(stackPointer_symbol));
                this.Output.Add(assembler.CommandType.LOADAATPOINTER.ToString());
                this.Output.Add(stackPointer_symbol);
                this.Output.AddRange(generateMoveAtoTemp());
                this.Output.AddRange(generateDecrement(stackPointer_symbol));

                this.Output.Add(assembler.CommandType.LOADAATPOINTER.ToString());
                this.Output.Add(stackPointer_symbol);
                this.Output.Add(assembler.CommandType.OR.ToString());
                this.Output.Add(temp_symbol);
                //now store the result in SP
                this.Output.Add(assembler.CommandType.STOREAATPOINTER.ToString());
                this.Output.Add(stackPointer_symbol);
                //and increment
                this.Output.AddRange(generateIncrement(stackPointer_symbol));
            }

            else if (subCommand == vmILParser.vmArithmetic_Logic_Instructions.not)
            {
                this.Output.AddRange(generateDecrement(stackPointer_symbol));
                this.Output.Add(assembler.CommandType.LOADAATPOINTER.ToString());
                this.Output.Add(stackPointer_symbol);

                this.Output.Add(assembler.CommandType.NOT.ToString());

                //now store the result in SP
                this.Output.Add(assembler.CommandType.STOREAATPOINTER.ToString());
                this.Output.Add(stackPointer_symbol);
                //and increment
                this.Output.AddRange(generateIncrement(stackPointer_symbol));
            }

        }

        private void handlePushPop(Tuple<vmILParser.vmCommmandType, object, string[]> instructionData)
        {

            if (instructionData.Item1 == vmILParser.vmCommmandType.PUSH)
            {
                //for now we'll only get commands like
                //push constant 11
                var segment = Enum.Parse<vmILParser.vmMemSegments>(instructionData.Item3.FirstOrDefault());
                var indexORValue = instructionData.Item3.Skip(1).FirstOrDefault();
                if (segment == vmILParser.vmMemSegments.constant)
                {
                    this.Output.Add(assembler.CommandType.LOADAIMMEDIATE.ToString());
                    this.Output.Add(indexORValue);
                    this.Output.Add(assembler.CommandType.STOREAATPOINTER.ToString());
                    this.Output.Add(stackPointer_symbol);
                    this.Output.AddRange(generateIncrement(stackPointer_symbol));
                }


            }
            //TODO handle POP

        }


        public void handleCommand(Tuple<vmILParser.vmCommmandType, object, string[]> instructionData)
        {
            switch (instructionData.Item1)
            {
                case vmILParser.vmCommmandType.ARITHMETIC:
                    handleArithmeticCommand(instructionData);
                    break;

                case vmILParser.vmCommmandType.PUSH:
                    handlePushPop(instructionData);
                    break;

                default:
                    throw new Exception("unkown command");
            }

        }

    }

    public class vmILParser
    {
        private string filePath;
        private string[] allInputLines = new string[] { };
        private int currentLineIndex = -1;
        private string currentLine = string.Empty;
        private vmIL2ASMWriter writer;

        public vmILParser(string filePath, vmIL2ASMWriter writer)
        {
            this.filePath = filePath;
            this.writer = writer;
            var data = File.ReadAllText(this.filePath);
            this.allInputLines = data.Split(Environment.NewLine).Select(x => x.Trim()).ToArray();
        }


        public enum vmCommmandType
        {
            ARITHMETIC,
            PUSH,
            POP,
            LABEL,
            GOTO,
            IF,
            FUNCTION,
            RETURN,
            CALL,
            NULL,
        }

        public enum vmArithmetic_Logic_Instructions
        {
            add,
            sub,
            //TODO can't implement this until resolving unsigned vs signed internal number storage.
            neg,
            eq,
            gt,
            lt,
            and,
            or,
            not,

        }

        public enum vmMemoryAccess_instructions
        {
            push,
            pop,
        }

        public enum vmMemSegments
        {
            argument,
            local,
            _static,
            constant,
            _this,
            that,
            pointer,
            temp,

        }

        public bool HasMoreCommands()
        {
            return this.currentLineIndex < this.allInputLines.Length;
        }

        public bool Advance()
        {
            var increment = 1;
            this.currentLineIndex = this.currentLineIndex + increment;
            if (this.currentLineIndex < this.allInputLines.Count())
            {
                this.currentLine = this.allInputLines[this.currentLineIndex];
            }
            else
            {
                this.currentLine = null;
            }


            if (this.currentLine != null)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// This method returns the operands of the current instruction.
        /// This might be an empty list in the case of some instructions.
        /// </summary>
        /// <returns></returns>
        public string[] Operands()
        {
            var text = this.currentLine.Split(' ');
            return text.Skip(1).ToArray();
        }



        /// <summary>
        /// extract info about the current instruction - 
        /// </summary>
        /// <returns> returns a tuple containg the general VM command type,
        ///  the exact command, and the operands as an array of strings.</returns>
        public Tuple<vmCommmandType, object, string[]> getCurrentCommandType()
        {

            vmCommmandType cmdType = vmCommmandType.NULL;
            var firstItemInLine = this.currentLine.Split(" ").FirstOrDefault();
            object parsedEnum;

            //parse arithmetic
            var parseResult = System.Enum.TryParse(typeof(vmArithmetic_Logic_Instructions), firstItemInLine, out parsedEnum);
            if (parseResult && parsedEnum != null)
            {
                //it matched an ALU command.
                cmdType = vmCommmandType.ARITHMETIC;
                return new Tuple<vmCommmandType, object, string[]>(cmdType, parsedEnum, this.Operands());

            }
            parseResult = System.Enum.TryParse(typeof(vmMemoryAccess_instructions), firstItemInLine, out parsedEnum);
            if (parseResult && parsedEnum != null)
            {
                //it matched an MEM command.

                if (((vmMemoryAccess_instructions)parsedEnum) == vmMemoryAccess_instructions.pop)
                {
                    cmdType = vmCommmandType.POP;
                }
                else
                {
                    cmdType = vmCommmandType.PUSH;
                }
                return new Tuple<vmCommmandType, object, string[]>(cmdType, parsedEnum, this.Operands());

            }



            throw new Exception($"could not parse current line to command: {this.currentLine},{this.currentLineIndex} ");
        }

        public void Parse()
        {
            while (this.HasMoreCommands() && this.Advance())
            {
                this.writer.handleCommand(this.getCurrentCommandType());
            }
        }
    }

}
