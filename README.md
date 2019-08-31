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

 Values in this simulator (*on this branch anyway*) are internally stored as 32bit signed ints.

 Originally this design supported only 16 bit unsigned numbers, and word width was 16 bits, but memory is cheap in a simulator... It's unclear if when synthesized, if this design (64k x 32bit memory) will fit in bitram on reasonable cost FPGAs.

`ram[0]: LOADA`

`ram[1]: 10`

valid instructions for the assembler:

- NOP : spins for an instruction cycle - this simulator does not emulate clock cycles or execute microcode so this doesn't do much.

- LOADA : Loads value at operand into A register. 1 operand.
- OUTA : Loads the output register with the value in the A register.
- ADD : ADD Adds the current value in the A register to the value stored at the memory location at operand. 1 operand.
- SUBTRACT :
- STOREA :
- LOADAIMMEDIATE :
- JUMP :
- JUMPIFEQUAL :
- JUMPIFLESS :
- JUMPIFGREATER :
- LOADB :
- LOADBIMMEDIATE :
- STOREB :
- UPDATEFLAGS :
- HALT :
- LOADCONTROLIMMEDIATE :
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

### vm language docs: