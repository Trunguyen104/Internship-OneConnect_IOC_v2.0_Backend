using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Domain.Enums
{
    public enum FileType
    {
        // Documents
        PDF = 1,
        DOCX = 2,
        PPTX = 3,

        // Archives
        ZIP = 4,
        RAR = 5,

        // Images
        JPG = 6,
        PNG = 7,

        // External links (Docs, Figma, ...)
        LINK = 8
    }
}
