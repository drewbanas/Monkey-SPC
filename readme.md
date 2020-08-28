# Monkey-SPC
A monkey without a tree. A single pass compiler variation of the [Monkey Language](https://monkeylang.org/)’s [virtual machine](https://compilerbook.com/). Implemented in C#.

## Usage:
`monkey.exe [ scriptFile.monk ] [ -b ]`
- Arguments are optional and REPL mode is activated when they are not provided
- The __-b__ switch enables benchmarking when running script files. The virtual machine's run time will be reported.

## Minor language features added:
- Comments: single line (`// `) and multi-line (`/*  */`)
- Additional binary/infix operators
  - “... than and equal” comparisons: `>=`, `<=`
  - Bit shifts: `>>`, `<<`
  - Power and modulo, `^`, `%` 
- “`exit`” command for terminating the REPL or script
- Numbers within identifier names
- Additional error handling 
  - Returns outside functions
  - Ignoring empty code

## Noted implementation changes
- The parser is merged into the compiler
- The AST nodes are gone
- Where avoidable, hashmaps/dictionaries are not used for looking up:
  - Keywords
  - Pratt parse functions
  - Opcode operand counts and bit widths
- Syntax tree based optimization techniques are no longer applicable, so an opcode for less than has been added.
- Go “`const`” enums that originally contained string members are replaced with numeric enums
