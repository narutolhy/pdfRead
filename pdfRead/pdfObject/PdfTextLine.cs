using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pdfRead.pdfObject {
    public class PdfTextLine {
        public string text;
        public int lineNum;
        public bool isTitle = false;
        public bool isOther = false;
        public double fontSize;
        public string fontName;

        public PdfTextLine(string text, int lineNum, bool isTitle, bool isOther, double fontSize, string fontName) {
            this.text = text;
            this.lineNum = lineNum;
            this.isTitle = isTitle;
            this.isOther = isOther;
            this.fontSize = fontSize;
            this.fontName = fontName;
        }
    }
}
