using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZLibNet;

namespace pdfRead.pdfObject {
    class PdfFunctions {

        private const string BEGIN_TEXT_BLOCK = "BT\r\n";

        private const string END_TEXT_BLOCK = "ET\r\n";

        private const string PLAIN_TEXT_BLOCK = "Tj\r\n";

        private const string TEXT_BLOCK = "TJ\r\n";

        public static string ConvertByteToString(byte[] inArray) {
            return inArray.Aggregate("", (current, element) => current + (char)element);
        }

        public static int GetPosition(byte[] buffer, int startIndex, string value) {
            var inBytes = new byte[value.Length];

            for(var i = 0; i < value.Length; i++)
                inBytes[i] = (byte)value[i];
            return GetPosition(buffer, startIndex, inBytes);
        }

        public static int GetPosition(byte[] buffer, int startIndex, byte[] inBytes) {
            for(int i = startIndex; i < buffer.Length; i++) {
                if(buffer[i] == inBytes[0]) {
                    var counter = 1;
                    for(int j = 1; j < inBytes.Length; j++) {
                        if(j + i < buffer.Length)
                            if(buffer[i + j] == inBytes[j])
                                counter++;
                    }
                    if(counter == inBytes.Length)
                        return i + inBytes.Length - 1;
                }
            }
            return -1;
        }

        public static string GetAttribute(byte[] inBytes, int startIndex, string name) {
            var position = GetPosition(inBytes, startIndex, name);
            if(position == -1)
                return "";
            position++;
            var current = (char)inBytes[position];
            var value = "";
            while(current != PdfConsts.PDF_BACKSLASH && current != PdfConsts.PDF_CLOSE_TRIANGLE_BRACKET) {
                value += current;
                position++;
                current = (char)inBytes[position];
            }
            return value;
        }

        public static string GetTextAttribute(byte[] inBytes, int startIndex, string name, out int position) {
            position = startIndex;
            var pos = GetPosition(inBytes, startIndex, name);
            if(pos == -1)
                return "";
            var beginIndex = pos - name.Length;
            var endIndex = beginIndex;
            var current = (char)inBytes[beginIndex];
            var value = "";
            while(current != 10) {
                endIndex--;
                current = (char)inBytes[endIndex];
            }
            for(int i = endIndex; i <= beginIndex; i++)
                value += (char)inBytes[i];
            position = beginIndex + name.Length;
            return value;
        }

        public static byte[] GetObjectData(byte[] buffer, int startIndex, string objName) {
            var name = new byte[objName.Length + 1];
            name[0] = 10;
            for(int i = 0; i < objName.Length; i++)
                name[i + 1] = (byte)objName[i];
            var position = GetPosition(buffer, startIndex, name);
            if(position == -1) {
                name[0] = 13;
                position = GetPosition(buffer, startIndex, name);
                if(position == -1)
                    return null;
            }

            var endPosition = GetPosition(buffer, position, PdfConsts.PDF_END_OBJECT) - PdfConsts.PDF_END_OBJECT.Length;
            var result = new byte[endPosition - position + 1];
            var counter = 0;
            for(var i = position; i <= endPosition; i++) {
                result[counter] = buffer[i];
                counter++;
            }
            return result;
        }


        public static byte[] Decompress(byte[] input) {
            try {
                return ZLibCompressor.DeCompress(input);
            } catch(Exception) {
                throw new ArgumentException("DeCopress Error!");
            }

        }

        public static byte[] Compress(byte[] data) {
            return ZLibCompressor.Compress(data);
        }

        private static void EraseArray(byte[] inArray) {
            for(int i = 0; i < inArray.Length; i++) {
                var beginText = GetPosition(inArray, i, BEGIN_TEXT_BLOCK);
                if(beginText == -1)
                    break;
                beginText += 1;
                var endText = GetPosition(inArray, beginText, END_TEXT_BLOCK);
                if(endText == -1)
                    break;
                endText -= 2;
                for(int j = beginText; j < endText; j++) {
                    var pos = GetPosition(inArray, j, PLAIN_TEXT_BLOCK);
                    if(pos != -1) {

                        for(int k = pos - 1; k > 0; k--) {
                            if(inArray[k] == 10)
                                break;
                            inArray[k] = 0;
                        }
                        j = pos;
                    }
                }
                for(int j = beginText; j < endText; j++) {
                    var pos = GetPosition(inArray, j, TEXT_BLOCK);
                    if(pos != -1) {

                        for(int k = pos - 1; k > 0; k--) {
                            if(inArray[k] == 10)
                                break;
                            inArray[k] = 0;
                        }
                        j = pos;
                    }
                }
                i = endText + 1;
            }
        }

        public static byte[] GetEncodedTextBlock(byte[] inArray, int startIndex, out int position) {
            position = startIndex;
            var startPos = GetPosition(inArray, startIndex, BEGIN_TEXT_BLOCK);
            if(startPos == -1)
                return null;
            startPos++;
            var endPos = GetPosition(inArray, startPos, END_TEXT_BLOCK);
            if(endPos == -1)
                return null;
            position = endPos;
            endPos -= 2;
            var outArray = new byte[endPos - startPos];
            var counter = 0;
            for(int i = startPos; i < endPos; i++) {
                outArray[counter] = inArray[i];
                counter++;
            }
            return outArray;
        }

        public static byte[] GetTextBlock(byte[] inArray, int startIndex, out int position) {
            position = startIndex;
            var startPos = GetPosition(inArray, startIndex, BEGIN_TEXT_BLOCK);
            if(startPos == -1)
                return null;
            startPos++;
            var endPos = GetPosition(inArray, startPos, END_TEXT_BLOCK);
            if(endPos == -1)
                return null;
            position = endPos;
            endPos -= 2;
            var outArray = new byte[endPos - startPos];
            var counter = 0;
            for(int i = startPos; i < endPos; i++) {
                outArray[counter] = inArray[i];
                counter++;
            }
            return outArray;
        }

        public static string AnsiToUnicode(byte[] inArray) {
            var byteArray = System.Text.Encoding.Convert(System.Text.Encoding.GetEncoding(1251), System.Text.Encoding.Unicode, inArray);
            return System.Text.Encoding.Unicode.GetString(byteArray);
        }

        public static string AnsiToUnicode(string text) {
            var buffer = new byte[text.Length];
            int i = 0;
            foreach(var element in text) {
                buffer[i] = (byte)element;
                i++;
            }
            return AnsiToUnicode(buffer);
        }

    }
}
