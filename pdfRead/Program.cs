using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using pdfRead.pdfObject;

namespace pdfRead {
    class Program {
        static void Main(string[] args) {
            string filePath = @"C:\Users\t-holu\Documents\Visual Studio 2015\Projects\ConsoleApplication1\ConsoleApplication1\data\effect\0010S000001nGJTQA2.pdf";
            byte[] pdfbytes = FileToByteArray(filePath);
            MemoryStream memoryStream = new MemoryStream(pdfbytes);
            BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8);
            PdfFileParser ps = new PdfFileParser(binaryReader);
            PdfByteArrayParser pbas = new PdfByteArrayParser(pdfbytes, true);
            PdfBase test = pbas.ParseNextItem();
            Console.WriteLine(pdfbytes.Length);
            PdfDocument doc = new PdfDocument();
            doc.ReadPdfFile(pdfbytes);
            StringBuilder sb = new StringBuilder();
            var objarray = doc.ObjectArray;
            //foreach(var obj in doc.ObjectArray) {
            //    if(obj.ObjectType == "/Page") {

            //        byte[] pageBytes = obj.PageContents;
            //        string result = System.Text.Encoding.UTF8.GetString(pageBytes);
            //        sb.Append(result + "\n");
            //        sb.Append("split" + "\n");
            //    }

            //}
            //File.WriteAllText("test.txt", sb.ToString());

            //var text = doc.PageTextData(0);
            //int lineN = 0;
            //double height = 0;
            //List<PdfTextLine> lines = new List<PdfTextLine>();
            //StringBuilder textSb = new StringBuilder();
            //for(int i = 0; i < text.Count; i++) {
            //    if(text[i].TextHeight != height) {
            //        height = text[i].TextHeight;
            //        string line = textSb.ToString();
            //        if(line.Trim().Length > 0) {
            //            PdfTextLine textLine = new PdfTextLine(textSb.ToString(), lineN, false, false, text[i].FontSize, text[i].FontName);
            //            lines.Add(textLine);
            //        }
            //        textSb.Clear();
            //        foreach(var str in text[i].TextLines)
            //            textSb.Append(str);
            //        lineN++;
            //    } else {
            //        foreach(var str in text[i].TextLines)
            //            textSb.Append(str);
            //    }
            //}
            //string lastLine = textSb.ToString();
            //if(lastLine.Trim().Length > 0) {
            //    PdfTextLine textLine = new PdfTextLine(textSb.ToString(), lineN, false, false, text[text.Count - 1].FontSize, text[text.Count - 1].FontName);
            //    lines.Add(textLine);
            //}
            //textSb.Clear();

            //foreach(var obj in lines) {
            //    sb.Append(obj.text + "\n");
            //}
            //File.WriteAllText("testLine.txt", sb.ToString());

            var lines = doc.PageTextLine(0);
            foreach(var obj in lines) {
                if(obj.isOther)
                    Console.WriteLine(obj.text);
                sb.Append(obj.text + "\n");
                
            }
            File.WriteAllText("testMergeLine.txt", sb.ToString());


            //foreach(var obj in text) {
            //    Console.WriteLine(obj.TextHeight);
            //    foreach(string line in obj.TextLines)
            //        sb.Append(line);
            //}
            //File.WriteAllText("test.txt", sb.ToString());
            //var obj = doc.ObjectArray[5];
            //byte[] pageBytes = obj.ObjectValue.StreamContents;
            //string result = System.Text.Encoding.UTF8.GetString(pageBytes);
            //Console.WriteLine(result);
            Console.ReadKey();
        }
        public static byte[] GetPdfByte(string fileName) {
            string pdfText = string.Empty;
            byte[] pdfBytes = FileToByteArray(fileName);
            return pdfBytes;
        }

        public static byte[] FileToByteArray(string fileName) {
            byte[] buff = null;
            FileStream fs = new FileStream(fileName,
                                           FileMode.Open,
                                           FileAccess.Read);
            BinaryReader br = new BinaryReader(fs);
            long numBytes = new FileInfo(fileName).Length;
            buff = br.ReadBytes((int)numBytes);

            return buff;
        }
    }

}
