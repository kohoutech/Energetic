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
        Section curSection;

        public Assembler(iFeedback _master)
        {
            master = _master;

            objfile = null;
            curSection = null;
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

            finishUp();

            return objfile;
        }

        //- directives --------------------------------------------------------

        public void handleDirective(Directive directive)
        {
            switch (directive.type)
            {
                case DirectiveType.PUBLIC:
                    Symbol sym = ((PublicDir)directive).sym;
                    sym.type = Symbol.SymType.PUBLIC;
                    break;

                case DirectiveType.SECTION:

                    String secName = ((SectionDir)directive).name;
                    Section sec = objfile.findSection(secName);
                    if (sec == null)
                    {
                        sec = objfile.addSection(secName);
                    }
                    curSection = sec;
                    break;

                default:
                    break;
            }
        }

        //- instructions ------------------------------------------------------

        public void handlePseudoOp(PseudoOp pseudo)
        {
        }

        public void handleInstruction(Instruction insn)
        {
            curSection.addData(insn.getBytes());
        }

        public void finishUp()
        {
            List<Symbol> syms = assembly.getSymbols();
            foreach (Symbol sym in syms)
            {
                switch (sym.type)
                {
                    case Symbol.SymType.PUBLIC:
                        break;

                    default:
                        break;
                }
            }
        }
    }
}
