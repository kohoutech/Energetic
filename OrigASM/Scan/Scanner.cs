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
using System.IO;

namespace OrigASM.Scan
{
    class Scanner
    {
        public iFeedback master;
        
        public string filename;
        String source;
        int srcpos;


        public Scanner(iFeedback _master, String _filename)
        {
            master = _master;
            filename = _filename;

            try
            {
                source = File.ReadAllText(filename);        //read entire file as single string
                transformSource();

                srcpos = 0;
            }
            catch (Exception e)
            {
                master.fatal("error reading source file " + filename + " : " + e.Message);
            }
        }

        public void transformSource()
        {
            if (!source.EndsWith("\n"))
            {
                source = source + '\n';                 //add eoln at end of file if not already there
            }
            source = source + '\0';                     //mark end of file
        }

        //- skipping whitespace & comments  -----------------------------------
        
        public bool isSpace(char ch)
        {
            return (ch == ' ' || ch == '\t' || ch == '\v' || ch == '\f');
        }

        public void skipWhitespace()
        {
            char ch = source[++srcpos];
            while (isSpace(ch))
            {
                ch = source[++srcpos];
            };
        }

        //skips comments chars & eoln, leaves source pos at start of next line or at eof char
        public void skipLineComment()
        {
            char ch = source[++srcpos];
            while (ch != '\n' && ch != '\0')
            {
                ch = source[++srcpos];
            }
            if (ch == '\n')
            {
                srcpos++;               //skip eoln char
            }
        }

        //should we allow block comments /* ... */?
        //convenient to comment out large blocks of text, but goes against the "one instruction per line" assembler style
        //maybe in a future version?

        //- source scanning ------------------------------------------------

        public bool startsIdent(char ch)
        {
            return ((ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z') || ch == '_' || ch == '.');
        }

        public bool isIdentChar(char ch)
        {
            return ((ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z') || ch == '_' || isDigit(ch));
        }

        public string scanIdentifier()
        {
            String idstr = "" + source[srcpos];         //store first char

            char ch = source[++srcpos];
            while (isIdentChar(ch))
            {
                idstr = idstr + ch;       
                ch = source[++srcpos];
            }
            return idstr;
        }

        public bool isDigit(char ch)
        {
            return (ch >= '0' && ch <= '9');
        }

        //just doing integers for now
        //numberic formats:
        //ddd   - decimal int (not starting with '0')
        //0xddd - hexidecimal int
        //0ddd  - octal int
        //0bddd - binary int
        public string scanNumber()     
        {
            int bass = 10;              //default number base ("base" is a reserved C# word, hence "bass")
            char ch = source[srcpos];
            String numstr = "" + ch;

            if (ch == '0')             //set base
            {
                if ((source[srcpos + 1] == 'X' || source[srcpos + 1] == 'x'))
                {
                    bass = 16;
                    numstr += source[++srcpos];
                    srcpos++;
                }
                else if ((source[srcpos + 1] == 'B' || source[srcpos + 1] == 'b'))
                {
                    bass = 2;
                    numstr += source[++srcpos];
                    srcpos++;
                }
                else
                {
                    bass = 8;
                }
            }
            ch = source[++srcpos];
            while (((bass == 10) && (ch >= '0' && ch <= '9')) ||
                   ((bass == 2)  && (ch == '0' || ch == '1')) ||
                   ((bass == 8)  && (ch >= '0' && ch <= '7')) ||
                   ((bass == 16) && ((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'F') || (ch >= 'a' && ch <= 'f'))))
            {
                numstr = numstr + ch;
                ch = source[++srcpos];
            }

            return numstr;
        }

        public string scanCharConst()
        {
            string cstr = "";
            char ch = source[++srcpos];
            while ((ch != '\'') && (ch != '\n') && (ch != '\0'))
            {
                if ((ch == '\\') && (source[srcpos + 1] == '\''))
                {
                    cstr = cstr + "\\\'";
                    srcpos++;                    //skip over escaped single quotes
                }
                else
                {
                    cstr = cstr + ch;
                }
                ch = source[++srcpos];
            }
            if (ch == '\'')         //add the closing quote if not at eoln or eof
            {
                cstr = cstr + '\'';
                srcpos++;
            }
            return cstr;
        }

        public string scanStringConst()
        {
            string sstr = "";
            char ch = source[++srcpos];
            while ((ch != '\"') && (ch != '\n') && (ch != '\0'))
            {
                if ((ch == '\\') && (source[srcpos + 1] == '\"'))
                {
                    sstr = sstr + "\\\"";
                    srcpos++;                    //skip over escaped double quotes
                }
                else
                {
                    sstr = sstr + ch;
                }
                ch = source[++srcpos];
            }
            if (ch == '\"')                     //skip the closing quote if not at eoln or eof
            {
                sstr = sstr + '\"';
                srcpos++;
            }
            return sstr;
        }

        //- main scanning method ----------------------------------------------

        public Fragment getFrag()
        {
            Fragment frag = null;

            char ch = source[srcpos];
                //whitespace
                if (isSpace(ch))
                {
                    skipWhitespace();
                    frag = new Fragment(FragType.SPACE, " ");
                }

                //line comment - effectively the end of line
                else if (ch == ';')
                {
                    skipLineComment();
                    frag = new Fragment(FragType.EOLN, "<eoln>");
                }

                //identifier
                else if (startsIdent(ch))
                {
                    string idstr = scanIdentifier();
                    frag = new Fragment(FragType.WORD, idstr);
                }

                //numeric constant
                else if (isDigit(ch))
                {
                    string numstr = scanNumber();
                    frag = new Fragment(FragType.NUMBER, numstr);
                }

                //char constant
                else if (ch == '\'')
                {
                    string chstr = scanCharConst();
                    frag = new Fragment(FragType.CHAR, chstr);
                }

                //string constant
                else if (ch == '"')
                {
                    string sstr = scanStringConst();
                    frag = new Fragment(FragType.STRING, sstr);
                }

                //end of line - does not include eolns in spliced lines
                else if ((ch == '\n') || (ch == '\r' && (source[srcpos + 1] == '\n')))
                {
                    frag = new Fragment(FragType.EOLN, "<eoln>");
                    if (ch == '\r')
                    {
                        srcpos++;
                    }
                    srcpos++;
                }

                //end of file - check if this isn't a stray 0x0 char in file, if so pass it on as punctuation
                else if ((ch == '\0') && (srcpos == (source.Length - 1)))
                {
                    frag = new Fragment(FragType.EOF, "<eof>");
                }

                //anything else is punctuation
                else
                {
                    frag = new Fragment(FragType.PUNCT, "" + ch);
                    srcpos++;
                }            

            return frag;
        }
    }

    public enum FragType
    {
        WORD,
        NUMBER,
        STRING,
        CHAR,
        PUNCT,
        SPACE,
        EOLN,
        EOF
    }

    public class Fragment
    {
        public String str;
        public FragType type;

        public Fragment(FragType _type, String _str)
        {
            str = _str;
            type = _type;
        }

        public override string ToString()
        {
            return str;
        }
    }

}

//Console.WriteLine("There's no sun in the shadow of the wizard");