/* ----------------------------------------------------------------------------
Origami Win32 Library
Copyright (C) 1998-2019  George E Greaney

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

//https://en.wikibooks.org/wiki/X86_Disassembly/Windows_Executable_Files

namespace Origami.Win32
{
    public class Section
    {

        //section header fields
        public int secNum;
        public String name;

        public uint memloc;                 //section addr in memory
        public uint memsize;                //section size in memory
        public uint fileloc;                //section addr in file
        public uint filesize;               //section size in file

        public List<CoffRelocation> relocations;
        public List<CoffLineNumber> linenumbers;                //line num data is deprecated

        //flag fields
        public bool hasCode;
        public bool hasInitializedData;
        public bool hasUninitializedData;
        public bool hasInfo;
        public bool isRemoveable;
        public bool hasComdat;
        public bool resetSpecExcept;
        public bool hasGlobalPtrData;
        public bool hasExtendRelocs;
        public bool isDiscardable;
        public bool notCached;
        public bool notPaged;
        public bool isShared;
        public bool isExecutable;
        public bool isReadable;
        public bool isWritable;

        public int dataAlignment;

        public byte[] data;

        //new section cons
        public Section(String _name)
        {
            secNum = 0;
            name = _name;

            memsize = 0;
            memloc = 0;
            filesize = 0;
            fileloc = 0;

            relocations = new List<CoffRelocation>();
            linenumbers = new List<CoffLineNumber>();

            hasCode = false;
            hasInitializedData = false;
            hasUninitializedData = false;
            hasInfo = false;
            isRemoveable = false;
            hasComdat = false;
            resetSpecExcept = false;
            hasGlobalPtrData = false;
            hasExtendRelocs = false;
            isExecutable = false;
            isReadable = false;
            isWritable = false;
            isShared = false;
            isDiscardable = false;
            notCached = false;
            notPaged = false;

            dataAlignment = 1;

            data = new byte[0];            
        }

//- reading in ----------------------------------------------------------------

        public static Section loadSection(SourceFile source)
        {

            String name = source.getAsciiString(8);
            Section section = new Section(name);

            section.memsize = source.getFour();
            section.memloc = source.getFour();
            section.filesize = source.getFour();
            section.fileloc = source.getFour();

            section.relocations = CoffRelocation.load(source);
            section.linenumbers = CoffLineNumber.load(source);

            uint flags = source.getFour();
            section.hasCode = (flags & 0x20) != 0;
            section.hasInitializedData = (flags & 0x40) != 0;
            section.hasUninitializedData = (flags & 0x80) != 0;
            section.hasInfo = (flags & 0x200) != 0;
            section.isRemoveable = (flags & 0x800) != 0;
            section.hasComdat = (flags & 0x1000) != 0;
            section.resetSpecExcept = (flags & 0x4000) != 0;
            section.hasGlobalPtrData = (flags & 0x8000) != 0;
            section.hasExtendRelocs = (flags & 0x02000000) != 0;
            section.isDiscardable = (flags & 0x02000000) != 0;
            section.notCached = (flags & 0x04000000) != 0;
            section.notPaged = (flags & 0x08000000) != 0;
            section.isShared = (flags & 0x10000000) != 0;
            section.isExecutable = (flags & 0x20000000) != 0;
            section.isReadable = (flags & 0x40000000) != 0;
            section.isWritable = (flags & 0x80000000) != 0;

            section.dataAlignment = (int)((flags >> 5) % 0x10);

            //load section data - read in all the bytes that will be loaded into mem (memsize)
            //and skip the remaining section bytes (filesize) to pad out the data to a file boundary
            section.data = source.getRange(section.fileloc, section.memsize);          

            return section;
        }

//- writing out ---------------------------------------------------------------

        public void writeSectionTblEntry(OutputFile outfile)
        {
            outfile.putFixedString(name, 8);

            outfile.putFour(memsize);
            outfile.putFour(memloc);
            outfile.putFour(filesize);
            outfile.putFour(fileloc);

            CoffRelocation.write(outfile);
            CoffLineNumber.write(outfile);

            uint flags = 0;
            if (hasCode) flags += 0x20;
            if (hasInitializedData) flags += 0x40;
            if (hasUninitializedData) flags += 0x80;
            if (hasInfo) flags += 0x200;
            if (isRemoveable) flags += 0x800;
            if (hasComdat) flags += 0x1000;
            if (resetSpecExcept) flags += 0x4000;
            if (hasGlobalPtrData) flags += 0x8000;
            if (hasExtendRelocs) flags += 0x02000000;
            if (isDiscardable) flags += 0x02000000;
            if (notCached) flags += 0x04000000;
            if (notPaged) flags += 0x08000000;
            if (isShared) flags += 0x10000000;
            if (isExecutable) flags += 0x20000000;
            if (isReadable) flags += 0x40000000;
            if (isWritable) flags += 0x80000000;
            flags += (uint)(dataAlignment << 5);
        }

        public void writeSectionData(OutputFile outfile)
        {
            outfile.putRange(data);
        }
    }

//-----------------------------------------------------------------------------

    public class CoffRelocation
    {
        public enum Reloctype
        {
            ABSOLUTE = 0x00,
            DIR32 = 0x06,
            DIR32NB = 0x07,
            SECTION = 0x0a,
            SECREL = 0x0b,
            TOKEN = 0x0c,
            SECREL7 = 0x0d,
            REL32 = 0x14
        }

        public uint address;
        public uint symTblIdx;
        public Reloctype type;

        public CoffRelocation(uint _addr, uint _idx, Reloctype _type)
        {
            address = _addr;
            symTblIdx = _idx;
            type = _type;
        }

        internal void writeToFile(OutputFile outfile)
        {
            outfile.putFour(address);
            outfile.putFour(symTblIdx);
            outfile.putTwo((uint)type);            
        }

        public static List<CoffRelocation> load(SourceFile source)
        {
            throw new NotImplementedException();
        }

        public static void write(OutputFile outfile)
        {
            throw new NotImplementedException();
        }
    }

    public class CoffLineNumber
    {
        public static List<CoffLineNumber> load(SourceFile source)
        {
            throw new NotImplementedException();
        }

        public static void write(OutputFile outfile)
        {
            throw new NotImplementedException();
        }
    }
}
