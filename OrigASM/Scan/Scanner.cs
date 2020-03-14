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

        public Fragment getFrag()
        {
            Fragment frag = null;

            char ch = source[srcpos];
            while (true)
            {
                //if (isSpace(ch))
                //{
                //    skipWhitespace();
                //    frag = new Fragment(FragType.SPACE, (saveSpaces ? spstr.ToString() : " "));
                //    break;
                //}

                ////line comment
                //if (ch == '/' && (source[srcpos + 1] == '/'))
                //{
                //    skipLineComment();
                //    ch = ' ';                   //replace comment with single space
                //    continue;
                //}

                ////block comment
                //if (ch == '/' && (source[srcpos + 1] == '*'))
                //{
                //    skipBlockComment();
                //    ch = ' ';                   //replace comment with single space
                //    continue;
                //}

                //L is a special case since it can start long char constants or long string constants, as well as identifiers
                //if (ch == 'L')
                //{
                //    srcpos++;                     //skip initial 'L'
                //    if ((source[srcpos + 1]) == '\'')
                //    {
                //        string chstr = scanCharLiteral(true);
                //        frag = new Fragment(FragType.CHAR, chstr);
                //        break;
                //    }
                //    else if ((source[srcpos + 1]) == '"')
                //    {
                //        string sstr = scanString(true);
                //        frag = new Fragment(FragType.STRING, sstr);
                //        break;
                //    }
                //}

                //identifier
                //if (isAlpha(ch))
                //{
                //    string idstr = scanIdentifier();
                //    frag = new Fragment(FragType.WORD, idstr);
                //    break;
                //}

                //numeric constant
                //'.' can start a float const
                //if ((isDigit(ch)) || (ch == '.' && isDigit(source[srcpos + 1])))
                //{
                //    string numstr = scanNumber();
                //    frag = new Fragment(FragType.NUMBER, numstr);
                //    break;
                //}

                //char constant
                //if (ch == '\'')
                //{
                //    string chstr = scanCharLiteral(false);
                //    frag = new Fragment(FragType.CHAR, chstr);
                //    break;
                //}

                ////string constant
                //if (ch == '"')
                //{
                //    string sstr = scanString(false);
                //    frag = new Fragment(FragType.STRING, sstr);
                //    break;
                //}

                //end of line - does not include eolns in block comments or spliced lines
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