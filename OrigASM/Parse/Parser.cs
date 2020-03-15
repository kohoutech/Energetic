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

        public Parser(iFeedback _master)
        {
            master = _master;
        }

        public List<Instruction> parseFile(string _filename)
        {
            filename = _filename;
            prep = new Tokenizer(master, filename);

            Token token = prep.getToken();
            while (token.type != TokenType.EOF)
            {
                token = prep.getToken();
                Console.WriteLine(token.ToString());
            }

            List<Instruction> insns = new List<Instruction>();
            return insns;
        }
    }
}

//Console.WriteLine("There's no sun in the shadow of the wizard");