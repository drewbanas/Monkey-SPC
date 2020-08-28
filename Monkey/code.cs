namespace code
{
    using System.Collections.Generic;

    using Instructions = System.Collections.Generic.List<System.Byte>;

    /* OpCode Operand Types
     * 
     * OpCode enum entries must be arranged by Operand Type
     * 
     * 0 operand
     * 1 operand,   1 byte
     * 1 operand,   2 bytes
     * 2 operands,  2 bytes,    1 byte OpClosure
     * 
     */

    public enum Opcode : byte
    {
        // operand defenitions are deduced from the OpCode values
        // OperandWidths = new int[] {}
        OpNull,
        OpPop,
        OpAdd,
        OpSub,
        OpMul,
        OpDiv, // language feature auto incrementing here??

        // new math operators: ^, %, <<, >>
        OpRaise,
        OpModulo,
        OpShiftLeft,
        OpShiftRight,

        OpTrue,
        OpFalse,
        OpEqual,
        OpNotEqual,
        OpGreaterThan,
        OpGreaterThanEqual, // new >=
        OpLessThan,         // AST left/right node swapping no longer available
        OpLessThanEqual,    // new <=
        OpMinus,
        OpBang,
        OpReturnValue,
        OpReturn,
        OpIndex,
        OpCurrentClosure,
        OpExit,              // new

        // OperandWidths = new int[] {1}
        OpGetLocal,
        OpSetLocal,
        OpCall,
        OpGetBuiltin,
        OpGetFree,

        //OperandWidths = new int[] {2}
        OpConstant,
        OpJumpNotTruthy,
        OpJump,
        OpGetGlobal,
        OpSetGlobal,
        OpArray,
        OpHash,

        // OperandWidths = new int[] {2, 1}
        OpClosure,
    }

    class code
    {
        /*
         * Originally, a dictionary/hashmap was used to store the operand widths and bytes
         * These info is now encoded on the arrangement of the OpCode's arrangements in the enum
         * This approach is similar to AZ Henley's Teeny Tiny Compiler
         */

        const byte oneByteOperand_Min = (byte)Opcode.OpGetLocal;
        const byte twoByteOperand_Min = (byte)Opcode.OpConstant;

        static byte[] zeroByteOperandWidths = new byte[] { };
        static byte[] oneByteOperandWidths = new byte[] { 1 };
        static byte[] twoByteOperandWidths = new byte[] { 2 };
        static byte[] opClosureOperandWidths = new byte[] { 2, 1 };

        static byte operandWidthBytes(Opcode op)
        {

            byte bOp = (byte)op;
            if (bOp < oneByteOperand_Min)
                return 0;
            if (bOp < twoByteOperand_Min)
                return 1;
            if (bOp < (byte)Opcode.OpClosure)
                return 2;

            // OpClosure:
            return 3;
        }

        static byte[] operandWidths(Opcode op)
        {
            byte bOp = (byte)op;

            if (bOp < oneByteOperand_Min)
                return zeroByteOperandWidths;
            if (bOp < twoByteOperand_Min)
                return oneByteOperandWidths;
            if (bOp < (byte)Opcode.OpClosure)
                return twoByteOperandWidths;

            return opClosureOperandWidths;
        }

        public static List<byte> Make(Opcode op, params int[] operands)
        {

            int instructionLen = 1 + operandWidthBytes(op);

            List<byte> instruction = new List<byte>(new byte[instructionLen]);
            instruction[0] = (byte)op;

            byte[] opWidths = operandWidths(op);

            int offset = 1;
            for (int i = 0; i < operands.Length; i++)
            {
                int o = operands[i];
                byte width = opWidths[i];
                switch (width)
                {
                    case 2:
                        ushort _o = (ushort)o;
                        instruction[offset] = (byte)((_o >> 8) & 0xff);
                        instruction[offset + 1] = (byte)(_o & 0xff);
                        break;
                    case 1:
                        instruction[offset] = (byte)o;
                        break;
                }
                offset += width;
            }

            return instruction;
        }


        public static byte ReadUint8(Instructions ins, int offset) { return (byte)ins[offset]; }

        public static ushort ReadUint16(Instructions ins, int offset)
        {
            return (ushort)((ins[offset] << 8) | ins[offset + 1]);
        }

    }
}
