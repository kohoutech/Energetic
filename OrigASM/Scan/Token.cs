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

using Origami.Asm32;

namespace OrigASM.Scan
{
    public class Token
    {
        public TokenType type;

        public Register reg;
        public string strval;
        public int intval;
        public double floatval;

        public Token(TokenType _type)
        {
            type = _type;

            reg = null;
            strval = "";
            intval = 0;
            floatval = 0.0;
        }

        //these must be in the same order as the TokenType enum
        String[] spelling = new String[] { "ident", "insn", "pseudo", "directive", "register",
                                           "int const", "float const", "char const", "string const",
                                           "(", ")", "[", "]", "=", ".", ",", ":", "$", "$$",
                                           "+", "-", "*", "/", "//", "%", "%%", "<", "<<", ">", ">>", "^", "|", "&", "~", "!",
                                           "eoln", "eof", "error"};

        public override string ToString()
        {
            String spell = spelling[(int)type];
            switch (type)
            {
                case TokenType.IDENT:
                case TokenType.INSN:
                case TokenType.DIRECTIVE:
                case TokenType.STRINGCONST:
                    spell = spell + " (" + strval + ")";
                    break;

                case TokenType.REGISTER:
                    spell = spell + " (" + reg.ToString() + ")";
                    break;

                case TokenType.INTCONST:
                    spell = spell + " (" + intval + ")";
                    break;

                case TokenType.FLOATCONST:
                    spell = spell + " (" + floatval + ")";
                    break;

                case TokenType.CHARCONST:
                    spell = spell + " (" + (char)intval + ")";
                    break;

                default:
                    break;
            }
            return spell;
        }
    }

    //-------------------------------------------------------------------------

    public enum TokenType
    {
        IDENT,
        INSN,
        PSEUDO,
        DIRECTIVE,
        REGISTER,

        INTCONST,
        FLOATCONST,
        CHARCONST,
        STRINGCONST,

        //punctuation
        LPAREN,
        RPAREN,
        LBRACKET,          //[
        RBRACKET,          //]

        EQUAL,
        PERIOD,
        COMMA,
        COLON,
        DOLLAR,
        DBLDOLLAR,         //$$

        PLUS,
        MINUS,
        STAR,              //*
        SLASH,
        DBLSLASH,       
        PERCENT,
        DBLPERCENT,
        LESSTHAN,         //<
        DBLLESS,          //<<
        GTRTHAN,          //>
        DBLGTR,           //>>
        CARET,
        BAR,
        AMPERSAND,
        TILDE,
        EXCLAIM,           //!

        EOLN,
        EOF,

        ERROR              //any char we don't recognize
    }
}
