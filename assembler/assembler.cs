using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using assembler;
using System.Linq;
using core;
using System.Diagnostics;

namespace assembler
{



    public enum OutputFormat
    {
        hex,
        binary
    }
    public enum CommandType
    {
        NOP,
        LOADA,
        OUTA,
        ADD,
        SUBTRACT,
        STOREA,
        LOADAIMMEDIATE,
        JUMP,
        JUMPTOPOINTER,
        JUMPIFEQUAL,
        JUMPIFLESS,
        JUMPIFGREATER,
        LOADB,
        LOADBIMMEDIATE,
        STOREB,
        UPDATEFLAGS,
        HALT,
        LOADCONTROLIMMEDIATE,
        STORECOMSTATUS,
        STORECOMDATA,
        STOREAATPOINTER,
        LOADAATPOINTER,
        MULTIPLY,
        DIVIDE,
        MODULO,
        
        //adding logical ops
        //TODO need to add these to map of increments - .... should add a validator step.
        AND,
        OR,
        NOT,


        ASSEM_LABEL = -1,
        ASSEM_STORE_MACRO = -2,
        ASSEM_DEFINE = -3,
    }

    public struct MemoryMapSegment
    {
        public int AbsoluteStart { get; private set; }
        public int AbsoluteEnd { get; private set; }

        public MemoryMapSegment(int absStart, int absEnd)
        {
            this.AbsoluteStart = absStart;
            this.AbsoluteEnd = absEnd;
        }


    }
    public class Assembler
    {

        private string assemblyFilePath;

        public Logger logger { get; }

        public Dictionary<string, int> symbolTable = new Dictionary<string, int>();

        public static Dictionary<MemoryMapKeys, MemoryMapSegment> MemoryMap = new Dictionary<MemoryMapKeys, MemoryMapSegment>{
            {MemoryMapKeys.bootloader,new MemoryMapSegment(0,255)},
             {MemoryMapKeys.pointers_registers,new MemoryMapSegment(256,271)},
              {MemoryMapKeys.symbols,new MemoryMapSegment(272,527)},
                {MemoryMapKeys.user_code,new MemoryMapSegment(528,33039)},
                 {MemoryMapKeys.stack,new MemoryMapSegment(33040,34839)},
                   {MemoryMapKeys.heap,new MemoryMapSegment(34840,48839)},
                    {MemoryMapKeys.frame_buffer,new MemoryMapSegment(48840,65223)},

        };
        private int currentSymbolTableOffset = 0;

        public enum MemoryMapKeys
        {
            bootloader,
            pointers_registers,
            symbols,
            user_code,
            stack,
            heap,
            frame_buffer,
        }


        public Assembler(string filePath)
        {
            this.assemblyFilePath = filePath;
            this.logger = new core.Logger();
        }

        private IEnumerable<string> ExpandMacros(string assemblyFilePath)
        {

            logger.log("PHASE: EXPAND MACROS");

            var parser = new AssemblyParser(assemblyFilePath, null, this.logger);
            while (parser.HasMoreCommands() && parser.Advance())
            {

                //if the current command is a storage macro, we should do a conversion like:

                //symbol = 100
                //is transformed to:
                ////////////////
                //LOADAIMMEDIATE
                //100
                //STOREA
                //symbol
                ////////////////

                if (parser.CommandType() == CommandType.ASSEM_STORE_MACRO)
                {

                    parser.output.Add(nameof(CommandType.LOADAIMMEDIATE));
                    parser.output.Add(parser.Operands()[1]);
                    parser.output.Add(nameof(CommandType.STOREA));
                    parser.output.Add(parser.Operands()[0]);

                    logger.log("JUST EXPANDED A STORAGE MACRO");
                    logger.log(string.Join(",", parser.output.TakeLast(4)));



                }
                //all other commands are unchanged
                else
                {
                    parser.output.Add(parser.currentLine);
                    if (parser.HasOperands())
                    {
                        parser.output = parser.output.Concat(parser.Operands()).ToList();
                    }

                    logger.log("JUST EXPANDED NOTHING");
                    logger.log(string.Join(Environment.NewLine, parser.output.Last()));

                }


            }
            return parser.output;

        }

        private void AddLabelsToSymbolTable(IEnumerable<string> code)
        {

            logger.log("PHASE: FILL SYMBOL TABLE");
            logger.log($"INPUT CODE:---{string.Join(Environment.NewLine, code)}---");

            var outputLineCounter = 0;
            var parser = new AssemblyParser(null, code, this.logger);
            while (parser.HasMoreCommands() && parser.Advance())
            {

                //for all non label, non define commands increment the counter
                if (parser.CommandType() != CommandType.ASSEM_LABEL && parser.CommandType() != CommandType.ASSEM_DEFINE)
                {
                    var increment = parser.commandTypeToNumberOfLines[parser.CommandType()];
                    outputLineCounter = outputLineCounter + increment;

                    //variables will be handled in the next pass.
                }
                //if the current command is a label - don't increment our counter.
                //if we see a label, add a symbol for the address it points to.
                else if (parser.CommandType() == CommandType.ASSEM_LABEL)
                {
                    var memoryAddressInUserCodeSpace = outputLineCounter + MemoryMap[MemoryMapKeys.user_code].AbsoluteStart;

                    if (this.symbolTable.ContainsKey(parser.LabelText()))
                    {
                        throw new Exception($"Symboltable already contained this label {parser.LabelText()} at line {memoryAddressInUserCodeSpace}");
                    }

                    this.symbolTable[parser.LabelText()] = memoryAddressInUserCodeSpace;

                    logger.log($"adding symbol {parser.LabelText() } at line { memoryAddressInUserCodeSpace }");

                }
                else if (parser.CommandType() == CommandType.ASSEM_DEFINE)
                {
                    var define = parser.Operands().FirstOrDefault();
                    var address = parser.Operands().Skip(1).FirstOrDefault();
                    this.symbolTable[define] = int.Parse(address);

                    logger.log($"adding symbol {define } at line { address}");

                }
            }
        }
        private IEnumerable<string> ConvertOpCodes(IEnumerable<string> code)
        {

            logger.log("PHASE: CONVERT OPCODES TO OUTPUT FORMAT");

            var parser = new AssemblyParser(null, code, this.logger);
            var converter = new CodeConverter();

            while (parser.HasMoreCommands() && parser.Advance())
            {

                //dont do anything for labels or defines - we already made symbols for them
                //in the first pass.
                if (parser.CommandType() != CommandType.ASSEM_LABEL && parser.CommandType() != CommandType.ASSEM_DEFINE)
                {
                    //first convert the opcode.
                    parser.output.Add(converter.InstructionAsHexString(parser.CommandType()));

                    //then check if this opcode has symbols
                    if (parser.HasSymbols())
                    {
                        //get the symbol and lookup the memory address it points to - 
                        //store this as the next line in the output string array.

                        // always assume a default offset of addding 0.
                        int symbolOffset = 0;
                        var symbol = parser.Operands()[0];
                        if (parser.SymbolHasOffset())
                        {
                            symbolOffset = parser.symbolOffsetExpressionInfo().Item2;
                            symbol = parser.symbolOffsetExpressionInfo().Item1;
                        }

                        if (this.symbolTable.ContainsKey(symbol))
                        {
                            parser.output.Add(converter.NumberAsHexString(this.symbolTable[symbol] + symbolOffset));
                        }

                        //new symbol store it.
                        //increment the symbolTable offset so variables are stored at next free space at offset 255+500 ( so first one is at 755)
                        //this means programs have a max length currently of 500 lines - and can store 1000 - 755 symbols - 
                        //to increase this we just need to modify the bootloader - we have 64k address space to play with in the cpu.
                        //transfer time will just increase.
                        else
                        {
                            var symbolTableCurrentLocation = MemoryMap[MemoryMapKeys.symbols].AbsoluteStart + this.currentSymbolTableOffset;
                            if (symbolTableCurrentLocation > MemoryMap[MemoryMapKeys.symbols].AbsoluteEnd)
                            {
                                throw new Exception(" symbol table has more variables than allocated memory space for symbols");
                            }
                            logger.log($"adding symbol, {symbol} at line: {symbolTableCurrentLocation}");
                            this.symbolTable[symbol] = symbolTableCurrentLocation;
                            //increment the offset.
                            this.currentSymbolTableOffset = this.currentSymbolTableOffset + 1;
                            parser.output.Add(converter.NumberAsHexString(this.symbolTable[symbol]));
                        }
                    }
                    //if no symbols check if it has any operands
                    else if (parser.HasOperands())
                    {
                        var convertedOperandsToHex = parser.Operands().Select(x => converter.NumberAsHexString(int.Parse(x))).ToArray();
                        parser.output = parser.output.Concat(convertedOperandsToHex).ToList();
                    }
                }

            }
            return parser.output;
        }

        public IEnumerable<string> ConvertToBinary()
        {

            //phase 1: expand macro commands:
            var expandedCode = this.ExpandMacros(this.assemblyFilePath);
            //phase 2: build the symbol table for labels:
            this.AddLabelsToSymbolTable(expandedCode);
            //phase 3: do the actual conversion from command strings and dec numbers operands to hex.
            var outputLines = this.ConvertOpCodes(expandedCode);
            //verifySymboltable();
            return outputLines;
        }

        private void verifySymboltable()
        {
            var matchingValues = this.symbolTable.Where(i => symbolTable.Any(t => t.Key != i.Key && t.Value == i.Value)).ToDictionary(i => i.Key, i => i.Value);
            if (matchingValues.Keys.Any())
            {
                throw new Exception("symbol table had symbols that pointed to same memory address");
            }
        }

        /// call this method to expand any macros (labels etc)
        /// and return the input assembly code as an IEnumerable of strings
        public IEnumerable<string> ExpandMacros()
        {
            return this.ExpandMacros(this.assemblyFilePath);
        }

    }

    class CodeConverter
    {
        public string InstructionAsBinaryString(CommandType command)
        {
            //given a command which maps directly to an enum number
            //lets convert it to a 16bit binary string.
            return Convert.ToString((int)command, 2).PadLeft(16, '0');
        }
        public string InstructionAsHexString(CommandType command)
        {
            //given a command which maps directly to an enum number
            //lets convert it to a 16bit binary string.
            return "0x" + Convert.ToString((int)command, 16).PadLeft(4, '0');
        }

        public string NumberAsHexString(int operand)
        {
            return "0x" + Convert.ToString(operand, 16).PadLeft(4, '0');
        }
    }

    class AssemblyParser
    {
        public List<String> output = new List<string>();
        public int currentLineIndex = -1;
        public string currentLine = string.Empty;
        public string[] allInputLines = new string[] { };

        public Dictionary<CommandType, int> commandTypeToNumberOfLines = new Dictionary<CommandType, int>();

        public Logger logger { get; }

        public AssemblyParser(string assemblyFilePath, IEnumerable<String> code = null, core.Logger logger = null)
        {
            if (code == null)
            {
                var data = File.ReadAllText(assemblyFilePath);
                this.allInputLines = data.Split(Environment.NewLine).Where(line => line != string.Empty && !(line.StartsWith("//"))).Select(x => x.Trim()).ToArray();
            }
            else
            {
                this.allInputLines = code.ToArray();
            }
            if (logger == null)
            {
                this.logger = new Logger();
            }
            else
            {
                this.logger = logger;
            }

            this.SetInitialMaps();
        }

        private void SetInitialMaps()
        {
            this.commandTypeToNumberOfLines = new Dictionary<CommandType, int>()
            {
                //set how many lines of code each instruction is
                [assembler.CommandType.NOP] = 1,
                [assembler.CommandType.LOADA] = 2,
                [assembler.CommandType.OUTA] = 1,
                [assembler.CommandType.ADD] = 2,
                [assembler.CommandType.SUBTRACT] = 2,
                [assembler.CommandType.STOREA] = 2,
                [assembler.CommandType.LOADAIMMEDIATE] = 2,
                [assembler.CommandType.JUMP] = 2,
                 [assembler.CommandType.JUMPTOPOINTER] = 2,
                [assembler.CommandType.JUMPIFEQUAL] = 2,
                [assembler.CommandType.JUMPIFLESS] = 2,
                [assembler.CommandType.JUMPIFGREATER] = 2,
                [assembler.CommandType.LOADB] = 2,
                [assembler.CommandType.LOADBIMMEDIATE] = 2,
                [assembler.CommandType.STOREB] = 2,
                [assembler.CommandType.UPDATEFLAGS] = 1,
                [assembler.CommandType.HALT] = 1,
                [assembler.CommandType.LOADCONTROLIMMEDIATE] = 2,
                [assembler.CommandType.STORECOMSTATUS] = 2,
                [assembler.CommandType.STORECOMDATA] = 2,
                [assembler.CommandType.STOREAATPOINTER] = 2,

                [assembler.CommandType.ASSEM_LABEL] = 1,
                [assembler.CommandType.ASSEM_STORE_MACRO] = 1,
                [assembler.CommandType.ASSEM_DEFINE] = 1,

                [assembler.CommandType.LOADAATPOINTER] = 2,
                [assembler.CommandType.MULTIPLY] = 2,
                [assembler.CommandType.DIVIDE] = 2,
                [assembler.CommandType.MODULO] = 2,
                [assembler.CommandType.AND] = 2,
                [assembler.CommandType.OR] = 2,
                [assembler.CommandType.NOT] = 1,
            };
            if (this.commandTypeToNumberOfLines.Count < Enum.GetValues(typeof(assembler.CommandType)).Length)
            {
                throw new Exception("missing increment in the commandTypeToNumberOfLines map ");
            }
        }

        public bool Advance()
        {
            //for some commands we need to advance 2 or more lines.
            //if those commands take operands.
            //consult the map to determine how many lines to advance.
            var increment = 1;
            //we need to increment based on the last command.
            if (this.currentLine != null && this.currentLine != string.Empty)
            {
                increment = this.commandTypeToNumberOfLines[this.CommandType()];
                logger.log($"increment is {increment} for commandType: {this.CommandType()} : {this.currentLine}");
            }

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

        public bool HasMoreCommands()
        {
            return this.currentLineIndex < this.allInputLines.Count();
        }

        public CommandType CommandType()
        {

            object parsedEnum;
            var parseResult = System.Enum.TryParse(typeof(CommandType), this.currentLine, out parsedEnum);
            if (parseResult && parsedEnum != null)
            {
                return (CommandType)parsedEnum;
            }

            else if (this.currentLine.StartsWith('(') && this.currentLine.EndsWith(')'))
            {
                return assembler.CommandType.ASSEM_LABEL;
            }
            else if (this.currentLine.Contains('='))
            {
                return assembler.CommandType.ASSEM_STORE_MACRO;
            }
            else if (this.currentLine.ToLower().Contains("#define"))
            {
                return assembler.CommandType.ASSEM_DEFINE;
            }

            throw new Exception($"could not parse current line to command: {this.currentLine},{this.currentLineIndex} ");
        }
        public bool HasSymbols()
        {
            //return true that the current command has symbols if the command has operands and the operand is a alphabetic string.
            //we only need to check the first character because symbols in assembly always look like
            // increment = 1
            if (this.commandTypeToNumberOfLines[this.CommandType()] > 1 &&
                Char.IsLetter(this.allInputLines[this.currentLineIndex + 1][0]))
            {
                return true;
            }
            return false;
        }

        public bool SymbolHasOffset()
        {
            var symbolExpression = this.allInputLines[this.currentLineIndex + 1];
            if ((symbolExpression.Contains("+")) || (symbolExpression.Contains("-")))
            {
                return true;
            }
            return false;
        }

        public Tuple<string, int> symbolOffsetExpressionInfo()
        {
            // symbol + 100
            var symbolExpression = this.allInputLines[this.currentLineIndex + 1];
            var ops = symbolExpression.Split(" ", StringSplitOptions.RemoveEmptyEntries).ToArray();

            if (ops[1] == "+")
            {
                return Tuple.Create(ops[0], int.Parse(ops[2]));
            }
            else if (ops[0] == "-")
            {
                return Tuple.Create(ops[0], int.Parse(ops[2]) * -1);
            }
            throw new Exception("unkown offset type");
        }

        public bool HasOperands()
        {
            if (this.commandTypeToNumberOfLines[this.CommandType()] > 1
                || this.CommandType() == assembler.CommandType.ASSEM_STORE_MACRO)
            {
                return true;
            }
            return false;
        }
        public string[] Operands()
        {
            if (this.commandTypeToNumberOfLines[this.CommandType()] > 1)
            {
                return new string[] { this.allInputLines[this.currentLineIndex + 1] };
            }
            else if (this.CommandType() == assembler.CommandType.ASSEM_STORE_MACRO)
            {
                return this.currentLine.Split('=').Select(x => x.Trim()).ToArray();
            }
            else if (this.CommandType() == assembler.CommandType.ASSEM_DEFINE)
            {
                return this.currentLine.Split(" ", StringSplitOptions.RemoveEmptyEntries).Skip(1).Select(x => x.Trim()).ToArray();
            }

            throw new Exception(this.CommandType().ToString() + "does not have an operand");
        }

        public string LabelText()
        {
            if (this.CommandType() == assembler.CommandType.ASSEM_LABEL)
            {
                return this.currentLine.Replace("(", "").Replace(")", "");
            }
            else
            {
                throw new Exception("not a label");
            }
        }

        public void Parse(OutputFormat format)
        {
            var converter = new CodeConverter();
            while (this.HasMoreCommands() && this.Advance())
            {
                if (format == OutputFormat.hex)
                {
                    this.output.Add(converter.InstructionAsHexString(this.CommandType()));
                }
                else
                {
                    this.output.Add(converter.InstructionAsBinaryString(this.CommandType()));
                }
                // TODO do any conversion we need to do on the operand - 
                // the operand might be hex, decimal, or binary... convert it.
                this.output = this.output.Concat(this.Operands()).ToList();
            }
            logger.log("*********parsed output*********");
            logger.log(string.Join(Environment.NewLine, this.output));
        }

    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }



    }
}