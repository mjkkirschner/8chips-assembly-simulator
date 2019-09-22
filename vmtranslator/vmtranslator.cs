﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using static vmtranslator.vmILParser;

namespace vmtranslator
{

    public class vmtranslator
    {
        private string path;

        public core.Logger logger { get; }

        public vmIL2ASMWriter writer { get; }

        public vmtranslator(string path, core.Logger logger = null)
        {
            this.path = path;
            this.logger = logger ?? new core.Logger();
            this.writer = new vmIL2ASMWriter();
        }

        public vmtranslator(string path, vmIL2ASMWriter writer, core.Logger logger = null)
        {
            this.path = path;
            this.logger = logger ?? new core.Logger();
            this.writer = writer;
        }

        public string[] TranslateToAssembly()
        {
            // get the file attributes for file or directory
            FileAttributes attr = File.GetAttributes(this.path);
            var codeWriter = this.writer;
            codeWriter.entryPointFunctionCall();
            if (attr.HasFlag(FileAttributes.Directory))
            {
                var files = Directory.GetFiles(this.path);
                files.ToList().ForEach(filePath =>
                {
                    var parser = new vmILParser(filePath, codeWriter);
                    parser.Parse();
                    this.logger.log($"output of vm translation for file:{ filePath} is:");
                    codeWriter.Output.ForEach(x => this.logger.log(x));
                });
                return codeWriter.Output.ToArray();
            }
            else
            {
                var parser = new vmILParser(this.path, codeWriter);
                parser.Parse();
                this.logger.log("output of vm translation is:");
                codeWriter.Output.ForEach(x => this.logger.log(x));
                return codeWriter.Output.ToArray();
            }
        }

    }

    public class vmIL2ASMWriter
    {

        protected const string stackPointer_symbol = "SP";
        protected const string local_symbol = "LCL";
        protected const string arg_symbol = "ARG";

        protected const string this_symbol = "THIS";
        protected const string that_symbol = "THAT";
        protected const string temp_symbol = "TEMP";

        protected const string pointer_symbol = "POINTER";
        //TODO get rid of this, replace using assembler memory map.
        private int bootloaderOffset = 256;

        public Dictionary<vmMemSegments, string> vmSegmentToSymbolName = new Dictionary<vmMemSegments, string>(){
            {vmMemSegments.argument, arg_symbol},
            {vmMemSegments.local,local_symbol},
            {vmMemSegments._this,this_symbol},
            {vmMemSegments.that,that_symbol},
            {vmMemSegments.pointer,pointer_symbol}
        };

        public vmMemSegments parseVmSegment(string segmentSymbol)
        {
            //some conversions because of c# reserved keywords
            if (segmentSymbol.Trim().ToLower() == "static")
            {
                segmentSymbol = "_static";
            }
            if (segmentSymbol.Trim().ToLower() == "this")
            {
                segmentSymbol = "_this";
            }

            return Enum.Parse<vmILParser.vmMemSegments>(segmentSymbol);
        }

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
            this.Output.Add($"{stackPointer_symbol} = {assembler.Assembler.MemoryMap[assembler.Assembler.MemoryMapKeys.stack].AbsoluteStart}");
            this.Output.Add($"{pointer_symbol} = {assembler.Assembler.MemoryMap[assembler.Assembler.MemoryMapKeys.pointers_registers].AbsoluteStart + 3 }");
            //TODO for the sake of testing lets set some base addresses that are non zero.
            this.Output.Add($"{local_symbol} = {100}");
            this.Output.Add($"{arg_symbol} = {200}");

            this.Output.Add($"{this_symbol} = {pointer_symbol} + 0 ");
            this.Output.Add($"{that_symbol} = {pointer_symbol} + 1 ");
        }

        /// <summary>
        /// Override this method to control what code is called at bootup. Can be used to set arguments correctly during tests.
        /// </summary>
        internal protected virtual void entryPointFunctionCall()
        {

            //call sys.init and then call main.
            var callInit = new InstructionData(vmCommandType.CALL, null, new string[] { "Sys.init", "0" }, "Sys.init", "Sys.init");
            handleFunctionCallingCommand(callInit);


            Output.Add(assembler.CommandType.HALT.ToString());


            var sysInitDefine = new InstructionData(vmCommandType.FUNCTION, null, new string[] { "Sys.init", "0" }, "Sys.init", "Sys.init");
            handleFunctionCallingCommand(sysInitDefine);

            //call the main.main func which is our real entry point.
            var callMain = new InstructionData(vmCommandType.CALL, null, new string[] { "main.main", "0" }, "main.main", "main.main");
            handleFunctionCallingCommand(callMain);

            //TODO as part of any VM function we need to push some return value to the stack 
            //for now, lets just push the result of main so we can assert our test was successfull.

            Output.AddRange(generateDecrement(stackPointer_symbol, "1", false));
            Output.AddRange(generateMoveAtoTemp());
            Output.Add(assembler.CommandType.LOADAATPOINTER.ToString());
            Output.Add(temp_symbol);
            Output.Add(assembler.CommandType.STOREA.ToString());
            Output.Add(temp_symbol);
            Output.AddRange(this.generatePushToStackFromSymbol(temp_symbol, null));

            var returnFromSysInit = new InstructionData(vmCommandType.RETURN, null, new string[] { }, "Sys.init", "Sys.init");
            handleFunctionCallingCommand(returnFromSysInit);
        }

        public vmIL2ASMWriter()
        {
            this.setupBaseRegisterSymbols();
        }

        protected string[] generateDecrement(string symbol, string offset = "1", bool updateSymbol = true)
        {
            int result;
            if (!int.TryParse(offset, out result))
            {
                throw new Exception("offset was not a valid int");
            }

            var output = new List<string>{
                //load the symbol into A
                assembler.CommandType.LOADA.ToString(),
                symbol,
                //load 1 into b
                assembler.CommandType.LOADBIMMEDIATE.ToString(),
                offset,
                //store B at a location we can reference...
                assembler.CommandType.STOREB.ToString(),
                temp_symbol+" + 1",
                //subtract the value at that mem address from A
                assembler.CommandType.SUBTRACT.ToString(),
                temp_symbol+" + 1",
            };

            if (updateSymbol)
            {
                //store result in original symbol
                output.Add(assembler.CommandType.STOREA.ToString());
                output.Add(symbol);
            }
            return output.ToArray();
        }

        protected string[] generateIncrement(string symbol, string offset = "1", bool updateSymbol = true)
        {
            int result;
            if (!int.TryParse(offset, out result))
            {
                throw new Exception("offset was not a valid int");
            }

            var output = new List<string>{
                //load the symbol into A
                assembler.CommandType.LOADA.ToString(),
                symbol,
                //load 1 into b
                assembler.CommandType.LOADBIMMEDIATE.ToString(),
                offset,
                //store B at a location we can reference...
                assembler.CommandType.STOREB.ToString(),
                temp_symbol+" + 1",
                //subtract the value at that mem address from A
                assembler.CommandType.ADD.ToString(),
                temp_symbol+" + 1",
            };

            if (updateSymbol)
            {
                //store result in original symbol
                output.Add(assembler.CommandType.STOREA.ToString());
                output.Add(symbol);
            }
            return output.ToArray();

        }
        protected string[] generateMoveAtoTemp()
        {
            return new string[]{
                assembler.CommandType.STOREA.ToString(),
                //TODO should TEMP be a symbol like this or should we use the int instead directly?
                temp_symbol,
            };
        }

        protected string[] generatePopFromStackToSegment(vmILParser.vmMemSegments segment, string IndexOperand, InstructionData currentVMinstruction)
        {
            var output = new List<String>();

            if (segment == vmMemSegments._static)
            {
                output.AddRange(generateDecrement(stackPointer_symbol));
                output.Add(assembler.CommandType.LOADAATPOINTER.ToString());
                output.Add(stackPointer_symbol);
                output.Add(assembler.CommandType.STOREA.ToString());
                output.Add($"STATIC{currentVMinstruction.VMFilePath}.{currentVMinstruction.VMFunction}.{IndexOperand}");
            }
            else
            {
                //offset the base address of the symbol by the index
                output.AddRange(generateIncrement(this.vmSegmentToSymbolName[segment], IndexOperand, updateSymbol: false));
                output.AddRange(generateMoveAtoTemp());

                output.AddRange(generateDecrement(stackPointer_symbol));
                output.Add(assembler.CommandType.LOADAATPOINTER.ToString());
                output.Add(stackPointer_symbol);

                output.Add(assembler.CommandType.STOREAATPOINTER.ToString());
                output.Add(temp_symbol);
            }
            return output.ToArray();
        }

        protected string[] generatePushToStackFromSymbol(string symbol, InstructionData currentVMinstruction)
        {
            var output = new List<string>();
            output.Add(assembler.CommandType.LOADA.ToString());
            output.Add(symbol);
            output.Add(assembler.CommandType.STOREAATPOINTER.ToString());
            output.Add(stackPointer_symbol);
            output.AddRange(generateIncrement(stackPointer_symbol));
            return output.ToArray();
        }
        protected string[] generatePushToStackFromSymbolImmediate(string symbol, InstructionData currentVMinstruction)
        {
            var output = new List<string>();
            output.Add(assembler.CommandType.LOADAIMMEDIATE.ToString());
            output.Add(symbol);
            output.Add(assembler.CommandType.STOREAATPOINTER.ToString());
            output.Add(stackPointer_symbol);
            output.AddRange(generateIncrement(stackPointer_symbol));
            return output.ToArray();
        }

        protected string[] generatePopFromStackToSymbol(string symbol, InstructionData currentVMinstruction)
        {
            var output = new List<string>();
            output.AddRange(generateDecrement(stackPointer_symbol));
            output.Add(assembler.CommandType.LOADAATPOINTER.ToString());
            output.Add(stackPointer_symbol);
            //TODO...treat symbol as pointer as argument to this func?
            output.Add(assembler.CommandType.STOREAATPOINTER.ToString());
            output.Add(symbol);
            return output.ToArray();
        }

        protected string[] generatePushToStackFromSegment(vmILParser.vmMemSegments segment, string index, InstructionData currentVMinstruction)
        {
            //if we are writing from local to stack - we need to read from memory at the base address
            //pointed to by the local symbol which is stored at LCL
            //flow is LoadAFromPointer LCL - lets assume LCL holds the number 100 - which is the base address for the LCL segment
            //then increment A by index
            //store this in temp
            var output = new List<String>();
            //add LCL and index and store in A

            //static is different from other segments
            //we need to read a value from the symbol VMFILE.VMFUNCTION.INDEX
            if (segment == vmMemSegments._static)
            {
                output.Add(assembler.CommandType.LOADA.ToString());
                output.Add($"STATIC{currentVMinstruction.VMFilePath}.{currentVMinstruction.VMFunction}.{index}");
            }
            //if constant, then just load the constant specified into A.
            else if (segment == vmMemSegments.constant)
            {
                output.Add(assembler.CommandType.LOADAIMMEDIATE.ToString());
                output.Add(index);
            }
            else
            {
                output.AddRange(generateIncrement(vmSegmentToSymbolName[segment], index, updateSymbol: false));
                output.Add(assembler.CommandType.STOREA.ToString());
                output.Add(temp_symbol + " + 2");

                output.Add(assembler.CommandType.LOADAATPOINTER.ToString());
                output.Add(temp_symbol + " + 2");
            }
            //store it on the stack, and increment SP
            output.Add(assembler.CommandType.STOREAATPOINTER.ToString());
            output.Add(stackPointer_symbol);
            output.AddRange(generateIncrement(stackPointer_symbol));
            return output.ToArray();
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

        private void handleArithmeticCommand(InstructionData instructionData)
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


            vmILParser.vmArithmetic_Logic_Instructions subCommand = (vmILParser.vmArithmetic_Logic_Instructions)instructionData.CommmandObject;
            //ADD
            this.Output.Add("//handleArithmeticCommand");
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

            else if (subCommand == vmILParser.vmArithmetic_Logic_Instructions.neg)
            {
                this.Output.AddRange(generateDecrement(stackPointer_symbol));
                this.Output.Add(assembler.CommandType.LOADAATPOINTER.ToString());
                this.Output.Add(stackPointer_symbol);
                this.Output.AddRange(generateMoveAtoTemp());

                this.Output.Add(assembler.CommandType.LOADAIMMEDIATE.ToString());
                this.Output.Add("-1");

                this.Output.Add(assembler.CommandType.MULTIPLY.ToString());
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

        private void handlePushPop(InstructionData instructionData)
        {
            ////////////////////////////////////////////////////////////////////////////////////
            /////PUSH - write to stack
            /// ///////////////////////////////////////////////////////////////////////////////

            if (instructionData.CommandType == vmILParser.vmCommandType.PUSH)
            {
                this.Output.Add("//handle PUSH");
                //memory instructions look like
                //pop segment index
                var segment = parseVmSegment(instructionData.Operands.FirstOrDefault());
                var indexORValue = instructionData.Operands.Skip(1).FirstOrDefault();
                if (segment == vmILParser.vmMemSegments.constant)
                {
                    this.Output.Add(assembler.CommandType.LOADAIMMEDIATE.ToString());
                    this.Output.Add(indexORValue);
                    this.Output.Add(assembler.CommandType.STOREAATPOINTER.ToString());
                    this.Output.Add(stackPointer_symbol);
                    this.Output.AddRange(generateIncrement(stackPointer_symbol));
                }
                //segments
                else if (segment == vmILParser.vmMemSegments.local)
                {
                    this.Output.AddRange(generatePushToStackFromSegment(vmMemSegments.local, indexORValue, instructionData));

                }

                else if (segment == vmILParser.vmMemSegments.argument)
                {
                    this.Output.AddRange(generatePushToStackFromSegment(vmMemSegments.argument, indexORValue, instructionData));
                }

                else if (segment == vmILParser.vmMemSegments.pointer)
                {
                    this.Output.AddRange(generatePushToStackFromSegment(vmMemSegments.pointer, indexORValue, instructionData));
                }

                else if (segment == vmILParser.vmMemSegments._this)
                {
                    this.Output.AddRange(generatePushToStackFromSegment(vmMemSegments._this, indexORValue, instructionData));
                }

                else if (segment == vmILParser.vmMemSegments.that)
                {
                    this.Output.AddRange(generatePushToStackFromSegment(vmMemSegments.that, indexORValue, instructionData));

                }
                else if (segment == vmILParser.vmMemSegments._static)
                {
                    this.Output.AddRange(generatePushToStackFromSegment(vmMemSegments._static, indexORValue, instructionData));
                }


                ////////////////////////////////////////////////////////////////////////////////////
                /////POP - write to memory
                /// ///////////////////////////////////////////////////////////////////////////////

            }

            else if (instructionData.CommandType == vmILParser.vmCommandType.POP)
            {
                this.Output.Add("//handle POP");
                var segment = parseVmSegment(instructionData.Operands.FirstOrDefault());
                var indexORValue = instructionData.Operands.Skip(1).FirstOrDefault();

                if (segment == vmILParser.vmMemSegments.constant)
                {
                    throw new Exception("cannot write to constant segment ");

                }
                else if (segment == vmILParser.vmMemSegments.local)
                {
                    this.Output.AddRange(generatePopFromStackToSegment(vmMemSegments.local, indexORValue, instructionData));
                }
                else if (segment == vmILParser.vmMemSegments.argument)
                {
                    this.Output.AddRange(generatePopFromStackToSegment(vmMemSegments.argument, indexORValue, instructionData));
                }
                else if (segment == vmILParser.vmMemSegments.pointer)
                {
                    this.Output.AddRange(generatePopFromStackToSegment(vmMemSegments.pointer, indexORValue, instructionData));
                }
                else if (segment == vmILParser.vmMemSegments._this)
                {
                    this.Output.AddRange(generatePopFromStackToSegment(vmMemSegments._this, indexORValue, instructionData));
                }
                else if (segment == vmILParser.vmMemSegments.that)
                {
                    this.Output.AddRange(generatePopFromStackToSegment(vmMemSegments.that, indexORValue, instructionData));
                }
                else if (segment == vmILParser.vmMemSegments._static)
                {
                    this.Output.AddRange(generatePopFromStackToSegment(vmMemSegments._static, indexORValue, instructionData));
                }

            }

        }

        protected void handleFunctionCallingCommand(InstructionData instructionData)
        {

            //function funcName 5 //number of arguments to get from stack.
            if (instructionData.CommandType == vmCommandType.FUNCTION)
            {
                this.Output.Add("//handle FUNCTION DEF");
                var funcName = instructionData.Operands[0];
                var argNum = instructionData.Operands[1];

                //label
                Output.Add($"({funcName})");
                //allocate some memory for local arguments
                for (var i = 0; i < int.Parse(argNum); i++)
                {
                    this.Output.Add(assembler.CommandType.LOADAIMMEDIATE.ToString());
                    this.Output.Add("0");
                    this.Output.Add(assembler.CommandType.STOREAATPOINTER.ToString());
                    this.Output.Add(stackPointer_symbol);
                    this.Output.AddRange(generateIncrement(stackPointer_symbol));
                }
            }

            if (instructionData.CommandType == vmCommandType.CALL)
            {
                this.Output.Add($"//handle CALL {instructionData.Operands[0]}");
                var funcName = instructionData.Operands[0];
                var argNum = instructionData.Operands[1];
                var returnaddress = "RETURN" + Guid.NewGuid().ToString("N");

                //push return address
                Output.AddRange(generatePushToStackFromSymbolImmediate(returnaddress, instructionData));
                //push LCL
                Output.AddRange(generatePushToStackFromSymbol(local_symbol, instructionData));
                //push ARG
                Output.AddRange(generatePushToStackFromSymbol(arg_symbol, instructionData));
                //push THIS
                Output.AddRange(generatePushToStackFromSymbol(this_symbol, instructionData));
                //push THAT
                Output.AddRange(generatePushToStackFromSymbol(that_symbol, instructionData));

                //set ARG to SP-n-5
                //first calculate the new address and store in A.
                Output.AddRange(generateDecrement(stackPointer_symbol, (int.Parse(argNum) + 5).ToString(), false));
                //then store it in ARG.
                Output.Add(assembler.CommandType.STOREA.ToString());
                Output.Add(arg_symbol);

                //set LCL to new SP.
                Output.Add(assembler.CommandType.LOADA.ToString());
                Output.Add(stackPointer_symbol);
                Output.Add(assembler.CommandType.STOREA.ToString());
                Output.Add(local_symbol);
                //execute function.
                Output.Add(assembler.CommandType.JUMP.ToString());
                Output.Add(funcName);
                //return label
                Output.Add($"({returnaddress})");

            }

            if (instructionData.CommandType == vmCommandType.RETURN)
            {
                Output.Add("//handle RETURN");

                Output.Add(assembler.CommandType.LOADA.ToString());
                Output.Add(local_symbol);
                Output.Add(assembler.CommandType.STOREA.ToString());
                Output.Add("FRAME");


                //frame currently points to somewhere on the stack -
                //lets get the real value at the frame pointer and save to ret.
                Output.AddRange(generateDecrement("FRAME", "5", false));
                Output.AddRange(generateMoveAtoTemp());
                Output.Add(assembler.CommandType.LOADAATPOINTER.ToString());
                Output.Add(temp_symbol);
                //A should now contain a real return address.
                Output.Add(assembler.CommandType.STOREA.ToString());
                Output.Add("RET");

                Output.AddRange(generatePopFromStackToSymbol(arg_symbol, instructionData));

                Output.AddRange(generateIncrement(arg_symbol, "1", false));
                Output.Add(assembler.CommandType.STOREA.ToString());
                Output.Add(stackPointer_symbol);

                Output.AddRange(generateDecrement("FRAME", (1).ToString(), false));
                Output.AddRange(generateMoveAtoTemp());
                Output.Add(assembler.CommandType.LOADAATPOINTER.ToString());
                Output.Add(temp_symbol);
                Output.Add(assembler.CommandType.STOREA.ToString());
                Output.Add(that_symbol);

                Output.AddRange(generateDecrement("FRAME", (2).ToString(), false));
                Output.AddRange(generateMoveAtoTemp());
                Output.Add(assembler.CommandType.LOADAATPOINTER.ToString());
                Output.Add(temp_symbol);
                Output.Add(assembler.CommandType.STOREA.ToString());
                Output.Add(this_symbol);

                Output.AddRange(generateDecrement("FRAME", (3).ToString(), false));
                Output.AddRange(generateMoveAtoTemp());
                Output.Add(assembler.CommandType.LOADAATPOINTER.ToString());
                Output.Add(temp_symbol);
                Output.Add(assembler.CommandType.STOREA.ToString());
                Output.Add(arg_symbol);

                Output.AddRange(generateDecrement("FRAME", (4).ToString(), false));
                Output.AddRange(generateMoveAtoTemp());
                Output.Add(assembler.CommandType.LOADAATPOINTER.ToString());
                Output.Add(temp_symbol);
                Output.Add(assembler.CommandType.STOREA.ToString());
                Output.Add(local_symbol);


                Output.Add(assembler.CommandType.JUMPTOPOINTER.ToString());
                Output.Add("RET");


            }
        }

        private void handleControlFlow(InstructionData instructionData)
        {
            if (instructionData.CommandType == vmCommandType.LABEL)
            {
                this.Output.Add("//handle VM LABEL");
                var labelString = instructionData.Operands.FirstOrDefault();
                this.Output.Add($"({labelString})");
            }
            if (instructionData.CommandType == vmCommandType.GOTO)
            {
                this.Output.Add("//handle GOTO");
                var labelToJumpTo = instructionData.Operands.FirstOrDefault();
                this.Output.Add(assembler.CommandType.JUMP.ToString());
                this.Output.Add(labelToJumpTo);
            }
            if (instructionData.CommandType == vmCommandType.IF)
            {
                this.Output.Add("//handle IF GOTO");
                var blockID = Guid.NewGuid().ToString("N");

                var labelToJumpTo = instructionData.Operands.FirstOrDefault();
                // we only do the jump if whatever is at the top of the stack is not equal to 0.
                // so load a with stack value, load b with 0, then do a jumpIfNotEqual
                this.Output.AddRange(generateDecrement(stackPointer_symbol));
                this.Output.Add(assembler.CommandType.LOADAATPOINTER.ToString());
                this.Output.Add(stackPointer_symbol);
                this.Output.Add(assembler.CommandType.LOADBIMMEDIATE.ToString());
                this.Output.Add("0");
                this.Output.Add(assembler.CommandType.UPDATEFLAGS.ToString());

                this.Output.Add(assembler.CommandType.JUMPIFEQUAL.ToString());
                this.Output.Add($"FALSE{blockID}");

                this.Output.Add(assembler.CommandType.JUMP.ToString());
                this.Output.Add(labelToJumpTo);
                this.Output.Add($"(FALSE{blockID})");
            }
        }

        public void handleCommand(InstructionData instructionData)
        {
            switch (instructionData.CommandType)
            {
                case vmILParser.vmCommandType.ARITHMETIC:
                    handleArithmeticCommand(instructionData);
                    break;

                case vmILParser.vmCommandType.PUSH:
                    handlePushPop(instructionData);
                    break;

                case vmILParser.vmCommandType.POP:
                    handlePushPop(instructionData);
                    break;

                case vmILParser.vmCommandType.LABEL:
                    {
                        handleControlFlow(instructionData);
                        break;
                    }
                case vmILParser.vmCommandType.IF:
                    {
                        handleControlFlow(instructionData);
                        break;
                    }
                case vmILParser.vmCommandType.GOTO:
                    {
                        handleControlFlow(instructionData);
                        break;
                    }

                case vmILParser.vmCommandType.CALL:
                    handleFunctionCallingCommand(instructionData);
                    break;
                case vmILParser.vmCommandType.RETURN:
                    handleFunctionCallingCommand(instructionData);
                    break;
                case vmILParser.vmCommandType.FUNCTION:
                    handleFunctionCallingCommand(instructionData);
                    break;

                default:
                    throw new Exception($"unkown command:{instructionData.CommandType}");
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
        private string currentVMFunction = null;

        public vmILParser(string filePath, vmIL2ASMWriter writer)
        {
            this.filePath = filePath;
            this.writer = writer;
            var data = File.ReadAllText(this.filePath);
            //get all lines
            var allLines = data.Split(Environment.NewLine).Select(x => x.Trim());
            var withoutFullLineComments = allLines.Where(x => !x.StartsWith(@"//"));
            //each line might also contain a comment somewhere in it - lets remove those as well.
            var noComments = withoutFullLineComments.Select(x => x.Split(@"//").FirstOrDefault());
            var noEmptyLines = noComments.Where(x => !(string.IsNullOrEmpty(x)));
            this.allInputLines = noEmptyLines.ToArray();
        }


        public enum vmCommandType
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
        /// retruns info about the current VM instruction including where it was parsed from.
        /// </summary>
        /// <returns></returns>
        public InstructionData getCurrentCommandType()
        {

            vmCommandType cmdType = vmCommandType.NULL;
            var firstItemInLine = this.currentLine.Split(" ").FirstOrDefault();
            object parsedEnum;

            //parse arithmetic
            var parseResult = System.Enum.TryParse(typeof(vmArithmetic_Logic_Instructions), firstItemInLine, out parsedEnum);
            if (parseResult && parsedEnum != null)
            {
                //it matched an ALU command.
                cmdType = vmCommandType.ARITHMETIC;
                return new InstructionData(cmdType, parsedEnum, this.Operands(), this.filePath, this.currentVMFunction);

            }
            parseResult = System.Enum.TryParse(typeof(vmMemoryAccess_instructions), firstItemInLine, out parsedEnum);
            if (parseResult && parsedEnum != null)
            {
                //it matched an MEM command.

                if (((vmMemoryAccess_instructions)parsedEnum) == vmMemoryAccess_instructions.pop)
                {
                    cmdType = vmCommandType.POP;
                }
                else
                {
                    cmdType = vmCommandType.PUSH;
                }
                return new InstructionData(cmdType, parsedEnum, this.Operands(), this.filePath, this.currentVMFunction);

            }

            //not memory access or arithmetic, lets try other command types

            if (firstItemInLine.ToLower().StartsWith("if-goto"))
            {
                cmdType = vmCommandType.IF;
                return new InstructionData(cmdType, null, this.Operands(), this.filePath, this.currentVMFunction);
            }
            if (firstItemInLine.ToLower().StartsWith("label"))
            {
                cmdType = vmCommandType.LABEL;
                return new InstructionData(cmdType, null, this.Operands(), this.filePath, this.currentVMFunction);
            }
            if (firstItemInLine.ToLower().StartsWith("goto"))
            {
                cmdType = vmCommandType.GOTO;
                return new InstructionData(cmdType, null, this.Operands(), this.filePath, this.currentVMFunction);
            }

            if (firstItemInLine.ToLower().StartsWith("function"))
            {
                cmdType = vmCommandType.FUNCTION;
                this.currentVMFunction = this.Operands().First();
                return new InstructionData(cmdType, null, this.Operands(), this.filePath, this.currentVMFunction);
            }

            if (firstItemInLine.ToLower().StartsWith("return"))
            {
                cmdType = vmCommandType.RETURN;
                return new InstructionData(cmdType, null, this.Operands(), this.filePath, this.currentVMFunction);
            }

            if (firstItemInLine.ToLower().StartsWith("call"))
            {
                cmdType = vmCommandType.CALL;
                return new InstructionData(cmdType, null, this.Operands(), this.filePath, this.currentVMFunction);
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
