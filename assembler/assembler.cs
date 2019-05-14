﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using assembler;
using System.Linq;

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

        ASSEM_LABEL = -1,
        ASSEM_STORE_MACRO = -2,

    }
    public class Assembler
    {


        private int bootLoaderOffset = 255;
        private int symbolTableOffset = 500;

        private string assemblyFilePath;
        public Dictionary<string, int> symbolTable = new Dictionary<string, int>();

        public Assembler(string filePath)
        {
            this.assemblyFilePath = filePath;
        }

        private IEnumerable<string> ExpandMacros(string assemblyFilePath)
        {
            Console.WriteLine("PHASE: EXPAND MACROS");
            var parser = new AssemblyParser(assemblyFilePath);
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
                    Console.WriteLine("JUST EXPANDED A STORAGE MACRO");
                    Console.WriteLine(string.Join(",", parser.output));

                }
                //all other commands are unchanged
                else
                {
                    parser.output.Add(parser.currentLine);
                    if (parser.HasOperands())
                    {
                        parser.output = parser.output.Concat(parser.Operands()).ToList();
                    }
                    Console.WriteLine("JUST EXPANDED NOTHING");
                    Console.WriteLine(string.Join(Environment.NewLine, parser.output));
                }


            }
            return parser.output;

        }

        private void AddLabelsToSymbolTable(IEnumerable<string> code)
        {
            Console.WriteLine("PHASE: FILL SYMBOL TABLE");
            Console.WriteLine($"INPUT CODE:aaa{string.Join(Environment.NewLine, code)}aaa");

            var outputLineCounter = 0;
            var parser = new AssemblyParser(null, code);
            while (parser.HasMoreCommands() && parser.Advance())
            {

                //if the current command is a label - don't increment our counter.
                if (parser.CommandType() != CommandType.ASSEM_LABEL)
                {
                    var increment = parser.commandTypeToNumberOfLines[parser.CommandType()];
                    outputLineCounter = outputLineCounter + increment;

                    //variables will be handled in the next pass.
                }
                //if we see a label, add a symbol for the address it points to.
                else
                {
                    this.symbolTable[parser.LabelText()] = outputLineCounter + this.bootLoaderOffset;
                    Console.WriteLine($"adding symbol {parser.LabelText() } at line {  outputLineCounter + this.bootLoaderOffset}");
                }
            }
        }
        private IEnumerable<string> ConvertOpCodes(IEnumerable<string> code)
        {
            Console.WriteLine("PHASE: CONVERT OPCODES TO OUTPUT FORMAT");

            var parser = new AssemblyParser(null, code);
            var converter = new CodeConverter();

            while (parser.HasMoreCommands() && parser.Advance())
            {

                //dont do anything for labels - we already made symbols for them
                //in the first pass.
                if (parser.CommandType() != CommandType.ASSEM_LABEL)
                {
                    //first convert the opcode.
                    parser.output.Add(converter.InstructionAsHexString(parser.CommandType()));

                    //then check if this opcode has symbols
                    if (parser.HasSymbols())
                    {
                        //get the symbol and lookup the memory address it points to - 
                        //store this as the next line in the output string array.

                        //TODO for now there is only ever 1;
                        var symbol = parser.Operands()[0];
                        if (this.symbolTable.ContainsKey(symbol))
                        {
                            parser.output.Add(converter.NumberAsHexString(this.symbolTable[symbol]));
                        }

                        //new symbol store it.
                        //increment the symbolTable offset so variables are stored at next free space at offset 255+500 ( so first one is at 755)
                        //this means programs have a max length currently of 500 lines - and can store 1000 - 755 symbols - 
                        //to increase this we just need to modify the bootloader - we have 64k address space to play with in the cpu.
                        //transfer time will just increase.
                        else
                        {
                            Console.WriteLine($"adding symbol, {symbol} at line: {this.bootLoaderOffset + this.symbolTableOffset}");
                            this.symbolTable[symbol] = this.bootLoaderOffset + this.symbolTableOffset;
                            this.symbolTableOffset = this.symbolTableOffset + 1;
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
            return outputLines;
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

        public AssemblyParser(string assemblyFilePath, IEnumerable<String> code = null)
        {
            if (code == null)
            {
                var data = File.ReadAllText(assemblyFilePath);
                this.allInputLines = data.Split(Environment.NewLine).Where(line => line != string.Empty && !(line.StartsWith("//"))).ToArray();
            }
            else
            {
                this.allInputLines = code.ToArray();
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
                [assembler.CommandType.LOADAATPOINTER] = 2,
                [assembler.CommandType.MULTIPLY] = 2,
                [assembler.CommandType.DIVIDE] = 2,
                [assembler.CommandType.MODULO] = 2,
            };
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
                Console.WriteLine($"increment is {increment} for commandType: {this.CommandType()} : {this.currentLine}");
            }

            this.currentLineIndex = this.currentLineIndex + increment;
            Console.WriteLine("debugging");
            Console.WriteLine(this.currentLineIndex);
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
            Console.WriteLine(string.Join(Environment.NewLine, this.output));
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