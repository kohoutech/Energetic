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
using Origami.Win32;

namespace OrigASM.Assemble
{
    class Assembler
    {
        public iFeedback master;
        Assembly assembly;
        Win32Coff objfile;

        public Assembler(iFeedback _master)
        {
            master = _master;

            objfile = null;
        }

        public Win32Coff assemble(Assembly _assembly)
        {
            assembly = _assembly;
            objfile = new Win32Coff();

            foreach (Instruction insn in assembly.insns)
            {
                if (insn is Directive)
                {
                    handleDirective((Directive)insn);
                }
                else if (insn is PseudoOp)
                {
                    handlePseudoOp((PseudoOp)insn);
                }
                else
                {
                    handleInstruction(insn);
                }
            }

            return objfile;
        }

        //- directives --------------------------------------------------------

        public void handleDirective(Directive directive)
        {
        }

        //- instructions ------------------------------------------------------

        public void handlePseudoOp(PseudoOp pseudo)
        {
        }

        public void handleInstruction(Instruction insn)
        {
        }
    }
}
