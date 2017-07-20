using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;


namespace pdfRead.pdfObject {
    public class PdfTextObject {
        private static byte[] _ObjectBytes;

        public string FontName {
            get; private set;
        }

        public double TextHeight {
            get; private set;
        }

        public double FontSize {
            get; private set;
        }

        public List<string> TextLines = new List<string>();

        public PdfTextObject(byte[] inBytes) {
            _ObjectBytes = inBytes;
            //var str = PdfFunctions.ConvertByteToString(_ObjectBytes);
            Parse();
        }



        private void Parse() {
            int position;
            var font = PdfFunctions.GetTextAttribute(_ObjectBytes, 0, PdfConsts.PDF_TEXT_FONT, out position);
            if(!String.IsNullOrEmpty(font))
                FillTextParameters(font);
            position = 0;
            var height = PdfFunctions.GetTextAttribute(_ObjectBytes, 0, PdfConsts.PDF_TEXT_POSITION, out position);
            if(!String.IsNullOrEmpty(height))
                GetTextHeight(height);
            string text;
            position = 0;
            do {
                text = PdfFunctions.GetTextAttribute(_ObjectBytes, position, PdfConsts.PDF_TEXT_PLAIN, out position);
                if(!String.IsNullOrEmpty(text))
                    AddPlainText(text);
            }
            while(!String.IsNullOrEmpty(text));
            position = 0;
            do {
                text = PdfFunctions.GetTextAttribute(_ObjectBytes, position, PdfConsts.PDF_TEXT_ASSEMBLY, out position);
                if(!String.IsNullOrEmpty(text))
                    AddAssemblyText(text);
            }
            while(!String.IsNullOrEmpty(text));

        }

        private void AddPlainText(string value) {
            var startPos = value.IndexOf("(", StringComparison.Ordinal);
            if(startPos == -1)
                AddHexaDecimalPlainText(value);
            else
                AddLiteralPlainText(value, startPos);
        }

        private void AddHexaDecimalPlainText(string value) {
            var startPos = value.IndexOf("<", StringComparison.Ordinal);
            if(startPos == -1)
                return;
            var endPos = value.IndexOf(">", StringComparison.Ordinal);
            var res = endPos == -1 ? value.Substring(startPos) : value.Substring(startPos + 1, endPos - startPos - 1);
            var i = 0;
            var current = "";
            var outValue = "";
            while(i < res.Length) {
                current = res.Substring(i, 2);
                outValue += (char)Int32.Parse(current, System.Globalization.NumberStyles.HexNumber);
                i += 2;
            }
            outValue = PdfFunctions.AnsiToUnicode(outValue);
        }

        private void AddAssemblyText(string value) {
            var textValues = Regex.Matches(value, PdfConsts.MULTY_LINE_REGEX);
            var outValue = textValues.Cast<Match>().Aggregate("", (current, textValue) => current + Regex.Replace(textValue.Value, PdfConsts.LINE_REGEX, "$1"));
            if(!String.IsNullOrEmpty(outValue))
                TextLines.Add(outValue);
        }

        private void AddLiteralPlainText(string value, int startPos) {
            var endPos = value.IndexOf(")", StringComparison.Ordinal);
            var res = endPos == -1 ? value.Substring(startPos) : value.Substring(startPos + 1, endPos - startPos - 1);
            TextLines.Add(res);
        }

        private void FillTextParameters(string value) {
            if(String.IsNullOrEmpty(value))
                return;
            var startPos = value.IndexOf("/", StringComparison.Ordinal);
            var endPos = value.IndexOf(" ", StringComparison.Ordinal);
            FontName = value.Substring(startPos + 1, endPos - startPos);
            FontSize = Convert.ToDouble(value.Substring(endPos + 1));
        }

        private void GetTextHeight(string height) {
            height = height.Trim();
            if(String.IsNullOrEmpty(height))
                return;
            int index = height.LastIndexOf(' ');
            if(index == -1)
                return;
            TextHeight = Convert.ToDouble(height.Substring(index + 1));
            return;
        }
    }
}
