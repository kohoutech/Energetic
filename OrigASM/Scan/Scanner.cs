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
        public string filename;
        String source;
        int srcpos;


        public Scanner(String _filename)
        {
            filename = _filename;

            try
            {
                source = File.ReadAllText(filename);        //read entire file as single string
            }
            catch (Exception e)
            {
                //parser.fatal("error reading source file " + filename + " : " + e.Message);
            }
        }

        public bool isSpace(char ch)
        {
            return (ch == ' ' || ch == '\t' || ch == '\v' || ch == '\f' || ch == '\r');
        }

        public void skipWhitespace()
        {
            bool done = false;
            char ch = source[srcpos];
            while (!done)
            {
                //skip any whitespace
                if ((ch == ' ') || (ch == '\t') || (ch == '\f') || (ch == '\v') || (ch == '\r'))
                {
                    ch = source[++srcpos];
                    continue;
                }

                //skip any following comments, if we found a comment, then we're not done yet
                if (ch == ';')
                {
                    skipComment();
                    ch = source[++srcpos];
                    continue;
                }

                //if we've gotten to here, then we not at a space or comment & we're done
                done = true;
            };
        }

        public void skipComment()
        {
            char ch = source[++srcpos];
            while (ch != '\n' && ch != '\0')
            {
                ch = source[++srcpos];
            }
        }

        public bool isAlpha(char ch)
        {
            return ((ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z') || ch == '_');
        }

        public bool isAlphaNum(char ch)
        {
            return isAlpha(ch) || isDigit(ch);
        }

        public string scanIdentifier()
        {
            String idstr = "";
            char ch = source[srcpos];
            while (isAlphaNum(ch))
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

        public string scanNumber()     
        {
            int bass = 10;              //default number base
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
                else
                {
                    bass = 8;
                }
            }
            ch = source[++srcpos];
            while (((bass == 10) && (ch >= '0' && ch <= '9')) ||
                    ((bass == 8) && (ch >= '0' && ch <= '7')) ||
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

        public Fragment getFrag()
        {
            Fragment frag = null;

            char ch = source[srcpos];
            while (true)
            {
                if (isSpace(ch))
                {
                    skipWhitespace();
                    frag = new Fragment(FragType.SPACE, " ");
                    break;
                }

                //line comment
                if (ch == ';')
                {
                    skipComment();
                    ch = ' ';                   //replace comment with single space
                    continue;
                }

                //identifier
                if (isAlpha(ch))
                {
                    string idstr = scanIdentifier();
                    frag = new Fragment(FragType.WORD, idstr);
                    break;
                }

                //numeric constant
                if (isDigit(ch))
                {
                    string numstr = scanNumber();
                    frag = new Fragment(FragType.NUMBER, numstr);
                    break;
                }

                //char constant
                if (ch == '\'')
                {
                    string chstr = scanCharConst();
                    frag = new Fragment(FragType.CHAR, chstr);
                    break;
                }

                //string constant
                if (ch == '"')
                {
                    string sstr = scanStringConst();
                    frag = new Fragment(FragType.STRING, sstr);
                    break;
                }

                //end of line - does not include eolns in spliced lines
                if (ch == '\n')
                {
                    frag = new Fragment(FragType.EOLN, "<eoln>");
                    srcpos++;
                    break;
                }

                //end of file - check if this isn't a stray 0x0 char in file, if so pass it on as punctuation
                if ((ch == '\0') && (srcpos == (source.Length - 1)))
                {
                    frag = new Fragment(FragType.EOF, "<eof>");
                    break;
                }

                //anything else is punctuation
                frag = new Fragment(FragType.PUNCT, "" + ch);
                srcpos++;
                break;
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