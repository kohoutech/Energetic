/* ----------------------------------------------------------------------------
OrigASM - Origami Assembler
Copyright (C) 2007-2020  George E Greaney

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
----------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OrigASM.Scan;
using Origami.Asm32;

namespace OrigASM.Parse
{
    class Parser
    {
        public iFeedback master;
        public Tokenizer prep;

        public String filename;

        public Dictionary<String, Symbol> symbolTable;
        public List<Symbol> labels;

        public Parser(iFeedback _master)
        {
            master = _master;

            symbolTable = new Dictionary<string, Symbol>();
            labels = new List<Symbol>();
        }

        //find symbol in sym tbl or create new symbol & add it in if not there yet
        public Symbol getSymbol(string symId)
        {
            Symbol sym = null;
            if (symbolTable.ContainsKey(symId))
            {
                sym = symbolTable[symId];
            }
            else
            {
                sym = new Symbol(symId);
                symbolTable.Add(symId, sym);
            }
            return sym;
        }

        //- parsing methods ---------------------------------------------------

        public Assembly parseFile(string _filename)
        {
            filename = _filename;
            prep = new Tokenizer(master, filename);
            Assembly assembly = new Assembly();
            //List<Instruction> insns = new List<Instruction>();

            Token token = prep.getToken();
            while (token.type != TokenType.EOF)
            {
                Instruction insn = null;
                if (token.type == TokenType.DIRECTIVE)
                {
                    insn = handleDirective(token);
                }
                else
                {
                    //starting label
                    if (token.type == TokenType.IDENT)
                    {
                        Symbol label = getSymbol(token.strval);
                        labels.Add(label);

                        //skip trailing colon, if present
                        token = prep.getToken();
                        if (token.type == TokenType.COLON)
                        {
                            token = prep.getToken();
                        }
                    }

                    if (token.type == TokenType.PSEUDO)
                    {
                        insn = handlePseudoOp(token);
                    }
                    else if (token.type == TokenType.INSN)
                    {
                        insn = handleInstruction(token);
                    }
                }
                if (insn != null)
                {
                    while (labels.Count > 0)
                    {
                        Symbol label = labels[0];
                        label.def = insn;
                        labels.RemoveAt(0);

                    }
                    assembly.AddInsn(insn);
                }

                token = prep.getToken();
                Console.WriteLine(token.ToString());
            }

            return assembly;
        }

        //- operands ----------------------------------------------------------

        public Operand getOperand()
        {
            Operand op = null;
            Token token = prep.getToken();
            switch (token.type)
            {
                case TokenType.EOLN:            //no op, return null
                    break;

                case TokenType.INTCONST:
                    op = new IntConst(token.intval);
                    break;

                case TokenType.IDENT:
                    op = getSymbol(token.strval);
                    break;

                case TokenType.REGISTER:
                    op = token.reg;
                    break;

                case TokenType.LBRACKET:
                    Token lhs = prep.getToken();        //assume lhs is a register for now
                    Token sign = prep.getToken();
                    Token val = prep.getToken();
                    token = prep.getToken();            //skip closing bracket
                    Register reg = lhs.reg;
                    uint immval = (uint)val.intval;
                    if (sign.type == TokenType.MINUS)
                    {
                        immval = 0x100 - immval;
                    }
                    Immediate imm = new Immediate(immval, OPSIZE.Byte);
                    op = new Memory(reg, imm);                    
                    break;
            }
            return op;
        }

        public List<Operand> getOperands()
        {
            List<Operand> ops = new List<Operand>();
            Operand op = getOperand();
            if (op != null)
            {
                ops.Add(op);
                Token token = prep.getToken();
                while (token.type == TokenType.COMMA)
                {
                    op = getOperand();
                    ops.Add(op);
                    token = prep.getToken();
                }
            }
            return ops;
        }

        //- directives --------------------------------------------------------

        public Directive handleDirective(Token token)
        {
            Directive direct = null;
            switch (token.strval)
            {
                case "SECTION":
                    token = prep.getToken();
                    direct = new SectionDir(token.strval);
                    break;

                case "PUBLIC":
                    token = prep.getToken();
                    Symbol gsym = getSymbol(token.strval);
                    direct = new PublicDir(gsym);
                    break;
            }

            //skip any trailing junk & goto eoln
            while (token.type != TokenType.EOLN)
            {
                token = prep.getToken();
            }
            return direct;
        }

        //- instructions ------------------------------------------------------

        public PseudoOp handlePseudoOp(Token token)
        {
            PseudoOp pseudo = null;
            switch (token.strval)
            {
                case "DB":
                case "DW": 
                case "DD":
                case "DQ": 
                case "DT":
                    char ch = token.strval[1];
                    int size = (ch == 'B') ? 1 : (ch == 'W') ? 2 : (ch == 'D') ? 4 : (ch == 'Q') ? 8 : 10;
                    Operand val = getOperand();
                    pseudo = new DataDefinition(size, val);
                    break;
            }

            //skip any trailing junk & goto eoln
            while (token.type != TokenType.EOLN)
            {
                token = prep.getToken();
            }
            return pseudo;
        }

        public Instruction handleInstruction(Token token)
        {
            Instruction insn = null;
            List<Operand> opList = getOperands();

            switch (token.strval)
            {
                case "ADD":
                    insn = new Add(opList[0], opList[1], false);
                    break;

                case "SUB":
                    insn = new Subtract(opList[0], opList[1], false);
                    break;

                case "MOV":
                    insn = new Move(opList[0], opList[1]);
                    break;

                case "POP":
                    insn = new Pop(opList[0]);
                    break;

                case "PUSH":
                    insn = new Push(opList[0]);
                    break;

                case "RET":
                    insn = new Return(false);
                    break;
            }

            return insn;
        }
    }
}

//Console.WriteLine("There's no sun in the shadow of the wizard");