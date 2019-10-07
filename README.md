# 8chips-assembly-simulator

roughly following along with / inspired by:
* https://www.nand2tetris.org
* [Digital Computer Electronics](https://books.google.com/books/about/Digital_Computer_Electronics.html?id=1QaMPwAACAAJ&source=kp_book_description
)


Contains:
* An assembler for a byte code language
* A simulator for a processor that runs the byte code language - with a very simple instruction set.
* A UI for the simulator using imgui.net
* A vm translator/implementation - which transpiles vm intermediate language into assembly.
* some tests.

A register level simulator and verilog implementation of this cpu exits here:
https://github.com/mjkkirschner/8chipsimulator

![alt text](https://github.com/mjkkirschner/8chips-assembly-simulator/blob/master/images/simulator.png)



### assembly language docs:

Instructions are 32 bit values. Operands are stored at the next location in memory. For example to load the a register with the value 10.

`ram[0]: LOADA`

`ram[1]: 10`

 Values in this simulator (*on this branch anyway*) are internally stored as 32bit signed ints.

 Originally this design supported only 16 bit unsigned numbers, and word width was 16 bits, but memory is cheap in a simulator... It's unclear if when synthesized, if this design (64k x 32bit memory) will fit in bitram on reasonable cost FPGAs.



valid instructions for the assembler:

- NOP : spins for an instruction cycle - this simulator does not emulate clock cycles or execute microcode so this doesn't do much.

- LOADA : Loads value at operand into A register. 1 operand.
- OUTA : Loads the output register with the value in the A register.
- ADD : ADD Adds the current value in the A register to the value stored at the memory location at operand. 1 operand.
- SUBTRACT :
- STOREA :
- LOADAIMMEDIATE :
- JUMP :
- JUMPTOPOINTER :
- JUMPIFEQUAL :
- JUMPIFLESS :
- JUMPIFGREATER :
- LOADB :
- LOADBIMMEDIATE :
- STOREB :
- UPDATEFLAGS : Updates the flags register which has bitflags for `A>B, A==B, A<B` - on real hardware this should be called before any jumps are made. Not required in this simulator.
- HALT :
- LOADCONTROLIMMEDIATE : Not implemented in simulator.
- STORECOMSTATUS : Not implemented in simulator.
- STORECOMDATA : Not implemented in simulator.
- STOREAATPOINTER : 
- LOADAATPOINTER : 
- MULTIPLY :
- DIVIDE :
- MODULO :
- AND :
- OR :
- NOT :

#### assembler features:
* Labels - labels take the form 
(SOMELABEL) and designate a memory location. They can be used for jumps
```
(START)
JUMP
START
```
* Symbols - symbols are alphaNumeric strings starting with a letter character and containing no spaces. These map to a memory location.

```
LOADA
someMemLocationSymbol
```
has the effect of loading whatever value is at someMemLocationSymbol into A

symbols can also be used with offsets.
```
LOADA
someMemLocationSymbol + 10
```
has the effect of loading whatever value is at someMemLocationSymbol offset by 10 memory addresses into A


you can also store a value at a symbol -
this will be expanded to a simple macro of assembly code that will store the value at the memory location which the symbol points to.

```
someMemLocation = 10
LOADA
someMemLocationSymbol
```
has the effect of loading 10 into A.


* Defines - defines add a symbol to the symbol table - but you can control the address. Think of it like a label for any memory address. Perhaps these should be called something else ;).

These have a form like:

`#define importantSymbol 100`

will create a symbol in the assembler symbol table at memory address 100
you can then use it like any other symbol.

### vm stack language docs:
- PUSH  `<segment> <index>`
- POP  `<segment> <index>`
- LABEL `<labelName>`
- GOTO `<labelName>`
- IF-GOTO `<labelName>`
- FUNCTION `<funcName> <numberOfLocals>`
- CALL `<funcName> <numberOfArgsPushedToStack>`
- RETURN
- add
- sub
- eq
- lt
- or
- and
- not
- neg