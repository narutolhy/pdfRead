using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pdfRead.pdfObject {
    class PdfConsts {
        public const string PDF_EXTENSION = ".pdf";

        public const string LINE_REGEX = @"\((.+)\)";

        public const string MULTY_LINE_REGEX = @"\([^\)]+\)";

        public const string PDF_TRAILER = "trailer";

        public const string PDF_TYPE = "/Type";

        public const string PDF_SIZE = "/Size";

        public const string PDF_OBJECT = "obj";

        public const string PDF_END_OBJECT = "endobj";

        public const string PDF_BLEED_BOX = "/BleedBox";

        public const string PDF_MEDIA_BOX = "/MediaBox";

        public const string PDF_TRIM_BOX = "/TrimBox";

        public const string PDF_ART_BOX = "/ArtBox";

        public const string PDF_ARC = "/Artifact";

        public const string PDF_BEGIN_Marked_Content = "BDC";

        public const string PDF_BEGIN_TEXT_BLOCK = "BT";
        
        public const string PDF_TEXT_FONT = "Tf";

        public const string PDF_TEXT_POSITION = "Tm";

        public const string PDF_TEXT_PLAIN = "Tj";

        public const string PDF_TEXT_ASSEMBLY = "TJ";

        public const string PDF_END_TEXT_BLOCK = "ET";

        public const string PDF_IMAGE = "Do";

        public const string PDF_FLATE_DECODE = "Filter/FlateDecode";

        public const string PDF_STREAM_LENGTH = "/Length";

        public const string PDF_START_STREAM = "stream";

        public const string PDF_END_STREAM = "endstream";

        public const string PDF_VERSION = "%PDF-";

        public const int PDF_SPACE = 32;

        public const int PDF_PERSENT = 37;

        public const int PDF_OPEN_BRACKET = 40;

        public const int PDF_CLOSE_BRACKET = 41;

        public const int PDF_BACKSLASH = 47;

        public const int PDF_OPEN_TRIANGLE_BRACKET = 60;

        public const int PDF_CLOSE_TRIANGLE_BRACKET = 62;
        
        public const int PDF_OPEN_QUAD_BRACKET = 91;
        
        public const int PDF_CLOSE_QUAD_BRACKET = 93;

        public const int PDF_OPEN_FIGURE_BRACKET = 123;

        public const int PDF_CLOSE_FIGURE_BRACKET = 125;
    }
}
