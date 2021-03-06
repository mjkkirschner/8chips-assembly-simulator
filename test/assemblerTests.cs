using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Tests
{
    public class AssemblerTests
    {

        string defineProgram =
@"#define bincrement 16
increment = 1
(START)
LOADAIMMEDIATE
0
(ADD_1)
ADD
increment
OUTA
LOADBIMMEDIATE
bincrement
UPDATEFLAGS
JUMPIFLESS
ADD_1
JUMP
START";


        string testCountProgram =
@"increment = 1
(START)
LOADAIMMEDIATE
0
(ADD_1)
ADD
increment
OUTA
LOADBIMMEDIATE
16
UPDATEFLAGS
JUMPIFLESS
ADD_1
JUMP
START";

        string testProgramWithSymbol_Offset =
        @"increment =1
        ADD
        increment + 2";

        string testVGAOutputProgram =
        @"increment = 1
(START)
LOADAIMMEDIATE
40000
(ADD_1)
ADD
increment
OUTA
STOREA
pixelindex
LOADAATPOINTER
pixelindex
LOADBIMMEDIATE
0
UPDATEFLAGS
JUMPIFEQUAL
COLORWHITE

(COLORBLACK)
LOADAIMMEDIATE
0
STOREAATPOINTER
pixelindex
JUMP
DONECHECK

(COLORWHITE)
LOADAIMMEDIATE
65000
STOREAATPOINTER
pixelindex

//a comment

(DONECHECK)
LOADBIMMEDIATE
62000
UPDATEFLAGS
JUMPIFLESS
ADD_1
JUMP
START";


        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void symbolOffsets()
        {
            var path = Path.GetTempFileName();
            System.IO.File.WriteAllText(path, testProgramWithSymbol_Offset);
            var assembler = new assembler.Assembler(path);
            var assembledResult = assembler.ConvertToBinary();
            CollectionAssert.AreEqual(assembledResult, new List<string>{
            "0x0006", //loadAimmediate
            "0x0001",//1
            "0x0005", //Store A
            "0x0110",//next location in variable space
            "0x0003",
            "0x0112" //symbol + 2
        }.Select(x => x.ToLower()));
        }

        [Test]
        public void assembleMacroAndSymbols()
        {
            var path = Path.GetTempFileName();
            System.IO.File.WriteAllText(path, testCountProgram);
            var assembler = new assembler.Assembler(path);
            var assembledResult = assembler.ConvertToBinary();
            CollectionAssert.AreEqual(assembledResult, new List<string>{
            "0x0006", //loadAimmediate
            "0x0001",//1
            "0x0005", //Store A
            "0x0110",//next location in variable space
            "0x0006",//loadAImmediate
            "0x0000",//0
            "0x0003",//add
            "0x0110",//refer to increment symbol
            "0x0002",// outA
            "0x000D",//loadBImmediate
            "0x0010", //16
            "0x000F", //update flags for jump
            "0x000A", //jump if less (A<B)
            "0x0216", //ADD_1 label location in hex
            "0x0007", //JUMP
            "0x0214", //START label location})
        }.Select(x => x.ToLower()));
        }

        [Test]
        public void assemblerShouldConvertDirectMemoryAddressesCorrectly()
        {
            var path = Path.GetTempFileName();
            System.IO.File.WriteAllText(path, testVGAOutputProgram);
            var assembler = new assembler.Assembler(path);
            var assembledResult = assembler.ConvertToBinary();
            CollectionAssert.AreEqual(assembledResult, new List<string>{
            "0x0006", //loadAimmediate
            "0x0001",//1
            "0x0005", //Store A
            "0x0110",//next location in variable space,
            "0x0006",//loadAImmediate
            "0x9C40",//40000
            "0x0003",//add
            "0x0110",//increment memory location,
            "0x0002",// outA
            "0x0005", //Store A,
            "0x0111", // pixel index next var space
            "0x0015", //load A from pointer
            "0x0111", // pixel index next var space
            "0x000D",//loadBImmediate
            "0x0000", //0
            "0x000F", //update flags for jump
            "0x0009", //jump if equal
            "0x0228", //address 24 color white
            "0x0006",//loadAImmediate
            "0x0000",//0
            "0x0014", //store a at pointer,
            "0x0111", // pixel index next var space
            "0x0007", //JUMP
            "0x022c", //done check (28)
            "0x0006",//loadAImmediate
            "0xFDE8",//65000
            "0x0014", //store a at pointer,
            "0x0111", // pixel index next var space,
            "0x000D", //loadbimm
            "0xF230", //62000,
            "0x000F", //update flags for jump
            "0x000A", //jump if less (A<B)
            "0x0216", //add 1 (6)
            "0x0007", //JUMP
            "0x0214" // START (4)
        }.Select(x => x.ToLower()));
        }

        [Test]
        public void assembleMacroAndSymbolsAndDefine()
        {
            var path = Path.GetTempFileName();
            System.IO.File.WriteAllText(path, defineProgram);
            var assembler = new assembler.Assembler(path);
            var assembledResult = assembler.ConvertToBinary();
            CollectionAssert.AreEqual(assembledResult, new List<string>{
            "0x0006", //loadAimmediate
            "0x0001",//1
            "0x0005", //Store A
            "0x0110",//next location in variable space
            "0x0006",//loadAImmediate
            "0x0000",//0
            "0x0003",//add
            "0x0110",// refer to increment symbol
            "0x0002",// outA
            "0x000D",//loadBImmediate
            "0x0010", //16
            "0x000F", //update flags for jump
            "0x000A", //jump if less (A<B)
            "0x0216", //ADD_1 label location in hex
            "0x0007", //JUMP
            "0x0214", //START label location})
        }.Select(x => x.ToLower()));
        }

    }
}