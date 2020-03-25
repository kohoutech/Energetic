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

using OrigASM.Assemble;
using Origami.Asm32;

namespace OrigASM.Scan
{
    class Tokenizer
    {
        public iFeedback master;
        public Preprocessor prep;
        List<Fragment> frags;

        HashSet<string> pseudoList;
        InstructionList insnList;
        RegisterList regList;

        public Tokenizer(iFeedback _master, String filename)
        {
            master = _master;
            prep = new Preprocessor(master, filename);
            frags = new List<Fragment>();


            pseudoList = new HashSet<string>() { "DB", "DW", "DD", "DQ", "DT", "EQU" };
            insnList = new InstructionList();
            regList = new RegisterList();
        }

        //- fragment handling -------------------------------------------------

        public Fragment getNextFrag()
        {
            if (frags.Count != 0)
            {
                Fragment f = frags[frags.Count - 1];
                frags.RemoveAt(frags.Count - 1);
                return f;
            }
            Fragment frag = prep.getFrag();
            return frag;
        }

        public void putFragBack(Fragment frag)
        {
            frags.Add(frag);
        }

        //the number fragment we get from the scanner should be well-formed
        public Token ParseNumber(String numstr)
        {
            Token tok = null;
            try
            {
                if (numstr.Contains('.'))
                {
                    double dval = Convert.ToDouble(numstr);
                    tok = new Token(TokenType.FLOATCONST);
                    tok.floatval = dval;
                }
                else
                {
                    int bass = 10;
                    if (numstr.StartsWith("0x"))
                    {
                        bass = 16;
                    }
                    else if (numstr.StartsWith("0b"))
                    {
                        bass = 2;
                    }
                    else if (numstr.StartsWith("0"))
                    {
                        bass = 8;
                    }
                    int intval = Convert.ToInt32(numstr, bass);
                    tok = new Token(TokenType.INTCONST);
                    tok.intval = intval;
                }
            }
            catch (Exception e)
            {
                master.error("error parsing number str " + numstr + " : " + e.Message);
            }
            return tok;
        }

        public Token ParseString(string p)
        {
            return new Token(TokenType.STRINGCONST);
        }

        public Token ParseChar(string p)
        {
            return new Token(TokenType.CHARCONST);
        }
        
        //- token handling ----------------------------------------------------
        
        public Token getToken()
        {
            Token token = tokenizer();
            return token;
        }

        public Token tokenizer()
        {
            Token tok = null;
            Fragment frag;
            Fragment nextfrag;

            while (true)
            {
                frag = getNextFrag();

                //ignore spaces
                if (frag.type == FragType.SPACE)
                {
                    continue;
                }

                //check if word is instruction, directive or identifier
                if (frag.type == FragType.WORD)
                {
                    String upstr = frag.str.ToUpper();
                    if (frag.str[0] == '.')
                    {
                        //either a directive or an identifier
                        string s = upstr.Substring(1);
                        if (Directive.names.Contains(s))
                        {
                            tok = new Token(TokenType.DIRECTIVE);
                            tok.strval = s;
                        }
                        else
                        {
                            tok = new Token(TokenType.IDENT);
                            tok.strval = frag.str;
                        }
                    }
                    else if (pseudoList.Contains(upstr))
                    {
                        tok = new Token(TokenType.PSEUDO);
                        tok.strval = upstr;
                    }
                    else if (insnList.names.Contains(upstr))
                    {
                        tok = new Token(TokenType.INSN);
                        tok.strval = upstr;
                    }
                    else if (regList.regs.ContainsKey(upstr))
                    {
                        tok = new Token(TokenType.REGISTER);
                        tok.reg = regList.regs[upstr];
                    }
                    else
                    {
                        tok = new Token(TokenType.IDENT);
                        tok.strval = frag.str;
                    }
                    break;
                }

                //convert number / string / char str into constant value
                if (frag.type == FragType.NUMBER)
                {
                    tok = ParseNumber(frag.str);
                    break;
                }

                if (frag.type == FragType.STRING)
                {
                    tok = ParseString(frag.str);
                    break;
                }

                if (frag.type == FragType.CHAR)
                {
                    tok = ParseChar(frag.str);
                    break;
                }

                //convert single punctuation chars into punctuation tokens, combining as necessary
                if (frag.type == FragType.PUNCT)
                {
                    char c = frag.str[0];
                    switch (c)
                    {
                        case '[':
                            tok = new Token(TokenType.LBRACKET);
                            break;
                        case ']':
                            tok = new Token(TokenType.RBRACKET);
                            break;
                        case '(':
                            tok = new Token(TokenType.LPAREN);
                            break;
                        case ')':
                            tok = new Token(TokenType.RPAREN);
                            break;

                        case '=':
                            tok = new Token(TokenType.EQUAL);
                            break;
                        case '.':
                            tok = new Token(TokenType.PERIOD);
                            break;
                        case ',':
                            tok = new Token(TokenType.COMMA);
                            break;
                        case ':':
                            tok = new Token(TokenType.COLON);
                            break;
                        case '$':
                            nextfrag = getNextFrag();
                            if ((nextfrag.type == FragType.PUNCT) && (nextfrag.str[0] == '$'))
                            {
                                tok = new Token(TokenType.DBLDOLLAR);
                            }
                            else
                            {
                                putFragBack(nextfrag);
                                tok = new Token(TokenType.DOLLAR);
                            }
                            break;

                        case '+':
                            tok = new Token(TokenType.PLUS);
                            break;
                        case '-':
                            tok = new Token(TokenType.MINUS);
                            break;
                        case '*':
                            tok = new Token(TokenType.STAR);
                            break;
                        case '/':
                            nextfrag = getNextFrag();
                            if ((nextfrag.type == FragType.PUNCT) && (nextfrag.str[0] == '/'))
                            {
                                tok = new Token(TokenType.DBLSLASH);
                            }
                            else
                            {
                                putFragBack(nextfrag);
                                tok = new Token(TokenType.SLASH);
                            }
                            break;

                        case '%':
                            nextfrag = getNextFrag();
                            if ((nextfrag.type == FragType.PUNCT) && (nextfrag.str[0] == '%'))
                            {
                                tok = new Token(TokenType.DBLPERCENT);
                            }
                            else
                            {
                                putFragBack(nextfrag);
                                tok = new Token(TokenType.PERCENT);
                            }
                            break;
                        case '<':
                            nextfrag = getNextFrag();
                            if ((nextfrag.type == FragType.PUNCT) && (nextfrag.str[0] == '<'))
                            {
                                tok = new Token(TokenType.DBLLESS);
                            }
                            else
                            {
                                putFragBack(nextfrag);
                                tok = new Token(TokenType.LESSTHAN);
                            }
                            break;
                        case '>':
                            nextfrag = getNextFrag();
                            if ((nextfrag.type == FragType.PUNCT) && (nextfrag.str[0] == '>'))
                            {
                                tok = new Token(TokenType.DBLGTR);
                            }
                            else
                            {
                                putFragBack(nextfrag);
                                tok = new Token(TokenType.GTRTHAN);
                            }
                            break;
                        case '^':
                            tok = new Token(TokenType.CARET);
                            break;
                        case '|':
                            tok = new Token(TokenType.BAR);
                            break;
                        case '&':
                            tok = new Token(TokenType.AMPERSAND);
                            break;
                        case '~':
                            tok = new Token(TokenType.TILDE);
                            break;
                        case '!':
                            tok = new Token(TokenType.EXCLAIM);
                            break;

                        default:
                            tok = new Token(TokenType.ERROR);
                            break;
                    }
                    break;
                }

                //eoln's are significant here
                if (frag.type == FragType.EOLN)
                {
                    tok = new Token(TokenType.EOLN);
                    break;
                }

                if (frag.type == FragType.EOF)
                {
                    tok = new Token(TokenType.EOF);
                    break;
                }
            }

            return tok;
        }
    }
    
    //-------------------------------------------------------------------------

    //all the instructions that assembler recognizes
    class InstructionList
    {
        public HashSet<String> names;

        public InstructionList()
        {
            names = new HashSet<string>();

            names.Add("AAA");
            names.Add("AAD");
            names.Add("AAM");
            names.Add("AAS");
            names.Add("ADC");
            names.Add("ADD");
            names.Add("AND");
            names.Add("CALL");
            names.Add("CBW");
            names.Add("CLC");
            names.Add("CLD");
            names.Add("CLI");
            names.Add("CMC");
            names.Add("CMP");
            names.Add("CMPS");
            names.Add("CMPSB");
            names.Add("CMPSW");
            names.Add("CWD");
            names.Add("DAA");
            names.Add("DAS");
            names.Add("DEC");
            names.Add("DIV");
            names.Add("ESC");
            names.Add("HLT");
            names.Add("IDIV");
            names.Add("IMUL");
            names.Add("IN");
            names.Add("INC");
            names.Add("INT");
            names.Add("INTO");
            names.Add("IRET");
            names.Add("JA");
            names.Add("JAE");
            names.Add("JB");
            names.Add("JBE");
            names.Add("JC");
            names.Add("JCXZ");
            names.Add("JE");
            names.Add("JG");
            names.Add("JGE");
            names.Add("JL");
            names.Add("JLE");
            names.Add("JMP");
            names.Add("JNA");
            names.Add("JNAE");
            names.Add("JNB");
            names.Add("JNBE");
            names.Add("JNC");
            names.Add("JNE");
            names.Add("JNG");
            names.Add("JNGE");
            names.Add("JNL");
            names.Add("JNLE");
            names.Add("JNO");
            names.Add("JNP");
            names.Add("JNS");
            names.Add("JNZ");
            names.Add("JO");
            names.Add("JP");
            names.Add("JPE");
            names.Add("JPO");
            names.Add("JS");
            names.Add("JZ");
            names.Add("LAHF");
            names.Add("LDS");
            names.Add("LEA");
            names.Add("LES");
            names.Add("LODS");
            names.Add("LODSB");
            names.Add("LODSW");
            names.Add("LOOP");
            names.Add("LOOPE");
            names.Add("LOOPEW");
            names.Add("LOOPNE");
            names.Add("LOOPNEW");
            names.Add("LOOPNZ");
            names.Add("LOOPNZW");
            names.Add("LOOPW");
            names.Add("LOOPZ");
            names.Add("LOOPZW");
            names.Add("MOV");
            names.Add("MOVS");
            names.Add("MOVSB");
            names.Add("MOVSW");
            names.Add("MUL");
            names.Add("NEG");
            names.Add("NOP");
            names.Add("NOT");
            names.Add("OR");
            names.Add("OUT");
            names.Add("POP");
            names.Add("POPF");
            names.Add("PUSH");
            names.Add("PUSHF");
            names.Add("RCL");
            names.Add("RCR");
            names.Add("RET");
            names.Add("RETF");
            names.Add("RETN");
            names.Add("ROL");
            names.Add("ROR");
            names.Add("SAHF");
            names.Add("SAL");
            names.Add("SAR");
            names.Add("SBB");
            names.Add("SCAS");
            names.Add("SCASB");
            names.Add("SCASW");
            names.Add("SHL");
            names.Add("SHR");
            names.Add("STC");
            names.Add("STD");
            names.Add("STI");
            names.Add("STOS");
            names.Add("STOSB");
            names.Add("STOSW");
            names.Add("SUB");
            names.Add("TEST");
            names.Add("WAIT");
            names.Add("XCHG");
            names.Add("XLAT");
            names.Add("XLATB");
            names.Add("XOR");
        }
    }

    //all the registers that assembler recognizes
    class RegisterList
    {
        public Dictionary<string, Register> regs;

        public RegisterList()
        {
            regs = new Dictionary<string, Register>();

            regs.Add("AL", Register8.AL);
            regs.Add("AH", Register8.AH);
            regs.Add("BL", Register8.BL);
            regs.Add("BH", Register8.BH);
            regs.Add("CL", Register8.CL);
            regs.Add("CH", Register8.CH);
            regs.Add("DL", Register8.DL);
            regs.Add("DH", Register8.DH);

            regs.Add("AX", Register16.AX);
            regs.Add("BX", Register16.BX);
            regs.Add("CX", Register16.CX);
            regs.Add("DX", Register16.DX);
            regs.Add("SP", Register16.SP);
            regs.Add("BP", Register16.BP);
            regs.Add("SI", Register16.SI);
            regs.Add("DI", Register16.DI);

            regs.Add("EAX", Register32.EAX);
            regs.Add("EBX", Register32.EBX);
            regs.Add("ECX", Register32.ECX);
            regs.Add("EDX", Register32.EDX);
            regs.Add("ESP", Register32.ESP);
            regs.Add("EBP", Register32.EBP);
            regs.Add("ESI", Register32.ESI);
            regs.Add("EDI", Register32.EDI);

            regs.Add("ST0", Register87.ST0);
            regs.Add("ST1", Register87.ST1);
            regs.Add("ST2", Register87.ST2);
            regs.Add("ST3", Register87.ST3);
            regs.Add("ST4", Register87.ST4);
            regs.Add("ST5", Register87.ST5);
            regs.Add("ST6", Register87.ST6);
            regs.Add("ST7", Register87.ST7);

            regs.Add("MM0", RegisterMM.MM0);
            regs.Add("MM1", RegisterMM.MM1);
            regs.Add("MM2", RegisterMM.MM2);
            regs.Add("MM3", RegisterMM.MM3);
            regs.Add("MM4", RegisterMM.MM4);
            regs.Add("MM5", RegisterMM.MM5);
            regs.Add("MM6", RegisterMM.MM6);
            regs.Add("MM7", RegisterMM.MM7);

            regs.Add("XMM0", RegisterXMM.XMM0);
            regs.Add("XMM1", RegisterXMM.XMM1);
            regs.Add("XMM2", RegisterXMM.XMM2);
            regs.Add("XMM3", RegisterXMM.XMM3);
            regs.Add("XMM4", RegisterXMM.XMM4);
            regs.Add("XMM5", RegisterXMM.XMM5);
            regs.Add("XMM6", RegisterXMM.XMM6);
            regs.Add("XMM7", RegisterXMM.XMM7);

            regs.Add("CR0", RegisterCR.CR0);
            regs.Add("CR1", RegisterCR.CR1);
            regs.Add("CR2", RegisterCR.CR2);
            regs.Add("CR3", RegisterCR.CR3);
            regs.Add("CR4", RegisterCR.CR4);
            regs.Add("CR5", RegisterCR.CR5);
            regs.Add("CR6", RegisterCR.CR6);
            regs.Add("CR7", RegisterCR.CR7);

            regs.Add("DR0", RegisterDR.DR0);
            regs.Add("DR1", RegisterDR.DR1);
            regs.Add("DR2", RegisterDR.DR2);
            regs.Add("DR3", RegisterDR.DR3);
            regs.Add("DR4", RegisterDR.DR4);
            regs.Add("DR5", RegisterDR.DR5);
            regs.Add("DR6", RegisterDR.DR6);
            regs.Add("DR7", RegisterDR.DR7);
        }
    }

}

//Console.WriteLine("There's no sun in the shadow of the wizard");