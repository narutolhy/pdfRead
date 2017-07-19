/////////////////////////////////////////////////////////////////////
//
//	PdfFileAnalyzer
//	PDF file analysis program
//
//	PdfDocument
//	The PdfDocument class is the top level class representing
//	the PDF file. The ReadPdfFile method is the entry point
//	to initiate PDF file analysis.
//
//	Granotech Limited
//	Author: Uzi Granot
//	Version: 1.0
//	Date: September 1, 2012
//	Copyright (C) 2012 Granotech Limited. All Rights Reserved
//
//	PdfFileAnalyzer application is a free software.
//	It is distributed under the Code Project Open License (CPOL).
//	The document PdfFileAnalyzerReadmeAndLicense.pdf contained within
//	the distribution specify the license agreement and other
//	conditions and notes. You must read this document and agree
//	with the conditions specified in order to use this software.
//
//	Version History:
//
//	Version 1.0 2012/09/01
//		Original revision
//	Version 1.1 2013/04/10
//		Allow program to be compiled in regions that define
//		decimal separator to be non period (comma)
//	Version 1.2 2014/03/10
//		Fix a problem related to PDF files with cross reference
//		stream.
//
/////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace pdfRead.pdfObject {
    ////////////////////////////////////////////////////////////////////
    // PDF Document
    ////////////////////////////////////////////////////////////////////

    public class PdfDocument {
        public String PdfFileName;
        public List<PdfIndirectObject> ObjectArray;
        public String ResultFolderName;
        public Int32 XRefStreamPosition;
        public Byte[] ObjectAnalyis;
        public PdfIndirectObject[] ContentsArray;
        public List<PdfIndirectObject> PageObjectArray;
        //public List<PdfIndirectObject> ImageObjectArray;

        private BinaryReader PdfFile;
        private PdfFileParser ParseFile;
        private Int32 RootObjectNumber;
        private Int32 StartPosition;

        ////////////////////////////////////////////////////////////////////
        // Constructor
        ////////////////////////////////////////////////////////////////////

        public PdfDocument() {
            ObjectArray = new List<PdfIndirectObject>();
            PageObjectArray = new List<PdfIndirectObject>();
            return;
        }

        ////////////////////////////////////////////////////////////////////
        // Get PDF file position
        ////////////////////////////////////////////////////////////////////

        public Int32 GetFilePosition() {
            return ((Int32)PdfFile.BaseStream.Position - StartPosition);
        }

        ////////////////////////////////////////////////////////////////////
        // Set PDF file position
        ////////////////////////////////////////////////////////////////////

        public void SetFilePosition
                (
                Int32 Position
                ) {
            PdfFile.BaseStream.Position = StartPosition + Position;
            return;
        }

        ////////////////////////////////////////////////////////////////////
        // Read first character
        ////////////////////////////////////////////////////////////////////

        public void ReadFirstChar() {
            ParseFile.ReadFirstChar();
            return;
        }

        ////////////////////////////////////////////////////////////////////
        // Read bytes
        ////////////////////////////////////////////////////////////////////

        public Byte[] ReadBytes
                (
                Int32 Length
                ) {
            return (PdfFile.ReadBytes(Length));
        }

        ////////////////////////////////////////////////////////////////////
        // Read next item
        ////////////////////////////////////////////////////////////////////

        public PdfBase ParseNextItem() {
            return (ParseFile.ParseNextItem());
        }

        ////////////////////////////////////////////////////////////////////
        // Read PDF file
        ////////////////////////////////////////////////////////////////////

        public Boolean ReadPdfFile
                (
                byte[] pdfbytes
                ) {
            try {

                MemoryStream memoryStream = new MemoryStream(pdfbytes);
                // open pdf file for reading
                PdfFile = new BinaryReader(memoryStream, Encoding.UTF8);

                // create parse file object
                ParseFile = new PdfFileParser(PdfFile);

                // validate file and read cross reference table
                ValidateFile();

                // read all objects without reading streams
                // skip trailer dictionaries and objects within streams
                foreach(PdfIndirectObject Obj in ObjectArray)
                    Obj.ReadObject();

                //process cross reference object streams
                foreach(PdfIndirectObject Obj in ObjectArray)
                    if(Obj.ObjectType == "/ObjStm") {
                        Obj.ReadStream();
                        Obj.ProcessObjectStream();
                    }

                //// dictionary analysis
                foreach(PdfIndirectObject Obj in ObjectArray) {
                    if(Obj.ObjectValue.IsStream)
                        SetObjectType(Obj.ObjectValue.StreamDictionary);
                    else if(Obj.ObjectValue.IsDictionary)
                        SetObjectType(Obj.ObjectValue.ToDictionary);
                }

                // read streams except object streams
                foreach(PdfIndirectObject Obj in ObjectArray)
                    if(Obj.ObjectValue.IsStream) {
                        Obj.ReadStream();
                    }


                // Document pages structure
                DocumentPagesStructure();

                // produce object analysis text file
                //ObjectAnalysisTextFile();
                // close file

                foreach(var obj in ObjectArray) {
                    if(obj.ObjectType == "/Page") {
                        PageObjectArray.Add(obj);
                    }
                }

                //foreach(var obj in ObjectArray) {
                //    if(obj.ObjectSubtype == "/Image") {
                //        ImageObjectArray.Add(obj);
                //    }
                //}

                PdfFile.Close();

                // successfule exit
                return (false);
            }

            // trap errors
            catch(Exception Ex) {
                // close file
                if(PdfFile != null)
                    PdfFile.Close();
                throw Ex;
                // error exit
                //String[] ExceptionStack = ExceptionReport.GetMessageAndStack(this, Ex);
                //MessageBox.Show(Form.ActiveForm, "PDF Document reading falied\n" + ExceptionStack[0] + "\n" + ExceptionStack[1],
                //    "PDFDocument Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                //return (true);
            }
        }

        ////////////////////////////////////////////////////////////////////
        // Find object by object number
        ////////////////////////////////////////////////////////////////////

        public PdfIndirectObject FindObject
                (
                Int32 ObjectNumber
                ) {
            // object position
            Int32 Index = ObjectArray.BinarySearch(new PdfIndirectObject(ObjectNumber));
            return (Index >= 0 ? ObjectArray[Index] : null);
        }

        ////////////////////////////////////////////////////////////////////
        // get stream length
        ////////////////////////////////////////////////////////////////////

        public Int32 GetStreamLength
                (
                PdfDict Dict
                ) {
            Int32 Length;

            // look for length entry in the dictionary
            PdfBase LenObj = Dict.GetValue("/Length");

            // length is a direct object
            if(LenObj.GetInteger(out Length))
                return (Length);

            // length is an indirect object
            if(LenObj.IsReference) {
                PdfIndirectObject LengthObj = FindObject(LenObj.ToObjectRefNo);
                if(LengthObj.ObjectValue.GetInteger(out Length)) {
                    // mark it as a stream length object
                    LengthObj.ObjectType = "/Length";

                    // exit with length
                    return (Length);
                }
            }

            // error
            throw new ApplicationException("Stream length is missing");
        }

        ////////////////////////////////////////////////////////////////////
        // Create object summary file
        ////////////////////////////////////////////////////////////////////

        private void ObjectAnalysisTextFile() {
            // write file name
            StringBuilder TextFile = new StringBuilder("PDF file name: " + PdfFileName + "\n");

            // output one object at a time
            foreach(PdfIndirectObject Obj in ObjectArray)
                TextFile.Append(Obj.ToString());

            // convert to string
            String Text = TextFile.ToString();

            // convert to bytes
            ObjectAnalyis = new Byte[Text.Length];
            for(Int32 Index = 0; Index < Text.Length; Index++)
                ObjectAnalyis[Index] = (Byte)Text[Index];

            // output file name
            String FileName = String.Format("{0}\\ObjectSummary.txt", ResultFolderName);

            // convert stream to binary writer
            using(StreamWriter OutputFile = new StreamWriter(FileName)) {
                // write result		
                OutputFile.Write(Text);

                // close file
                OutputFile.Close();
            }

            // successful exit
            return;
        }

        ////////////////////////////////////////////////////////////////Page type entry is missin////
        // Document pages structure
        ////////////////////////////////////////////////////////////////////

        private void DocumentPagesStructure() {
            // get root
            PdfIndirectObject Root = FindObject(RootObjectNumber);
            if(Root == null)
                throw new ApplicationException("Root object is missing");

            // get pages
            PdfIndirectObject Pages = FindObject(Root.ObjectValue.ToDictionary.GetValue("/Pages").ToObjectRefNo);
            if(Pages == null)
                throw new ApplicationException("Pages directory entry is missing");

            // get page count
            Int32 PageCount;
            if(!Pages.ObjectValue.ToDictionary.GetValue("/Count").GetInteger(out PageCount))
                throw new ApplicationException("Page count is missing in pages object");

            // get kids array
            PdfIndirectObject[] KidsArray = GetArrayOfObjects(Pages.ObjectValue.ToDictionary, "/Kids");
            if(KidsArray == null)
                throw new ApplicationException("Page kids is missing in pages object");

            // read page contents
            Int32 ResultCount = BuildPageContents(KidsArray);

            // test page count
            if(PageCount != ResultCount)
                throw new ApplicationException("Page count is not the same as pages processed");

            // exit
            return;
        }

        ////////////////////////////////////////////////////////////////////
        // Build page contents from contents objects
        ////////////////////////////////////////////////////////////////////

        private Int32 BuildPageContents
                (
                PdfIndirectObject[] KidsArray
                ) {
            // total leaf pages
            Int32 Total = 0;

            // loop for pages
            for(Int32 PagePtr = 0; PagePtr < KidsArray.Length; PagePtr++) {
                // this can be either a page or new pages object
                PdfIndirectObject Page = KidsArray[PagePtr];

                // we have a node of pages
                if(Page.ObjectType == "/Pages" || Page.ObjectType == "/Parent") {
                    // get page count
                    Int32 PageCount;
                    if(!Page.ObjectValue.ToDictionary.GetValue("/Count").GetInteger(out PageCount))
                        throw new ApplicationException("Page count is missing in pages object");

                    // get kids array
                    PdfIndirectObject[] GrandKidsArray = GetArrayOfObjects(Page.ObjectValue.ToDictionary, "/Kids");
                    if(GrandKidsArray == null)
                        throw new ApplicationException("Page kids is missing in pages object");

                    // read page contents
                    Int32 ResultCount = BuildPageContents(GrandKidsArray);

                    // test page count
                    if(PageCount != ResultCount)
                        throw new ApplicationException("Page count is not the same as pages processed");

                    // add to total
                    Total += ResultCount;
                    continue;
                }

                // verify page type
                if(Page.ObjectType != "/Page")
                    throw new ApplicationException("Page type entry is missing");

                // update total
                Total++;

                // contents can be reference or array of references or missing
                PdfIndirectObject[] ContentsArray = GetArrayOfObjects(Page.ObjectValue.ToDictionary, "/Contents");

                // blank page
                if(ContentsArray == null)
                    continue;

                Int32 Index;

                // if array is one element pointing to indirect object of contents objects
                if(ContentsArray.Length == 1 && ContentsArray[0].ObjectValue.IsArray) {
                    PdfBase[] RefArray = ContentsArray[0].ObjectValue.ToArray;
                    ContentsArray = new PdfIndirectObject[RefArray.Length];
                    for(Index = 0; Index < RefArray.Length; Index++) {
                        ContentsArray[Index] = FindObject(RefArray[Index].ToObjectRefNo);
                        if(ContentsArray[Index] == null)
                            break;
                    }
                    if(Index < RefArray.Length)
                        continue;
                }

                // loop for each contents object
                for(Index = 0; Index < ContentsArray.Length; Index++) {
                    // read the contents and append to page contents
                    Page.AddContentsToPage(ContentsArray[Index]);
                }

                // blank page
                if(Page.PageContents == null)
                    continue;

                // save contents to the disk
                //Page.SavePageContents();

                // save contents in a parsed way
                //Page.ParsePageContents();
            }

            // exit
            return (Total);
        }

        ////////////////////////////////////////////////////////////////////
        // Validate file
        ////////////////////////////////////////////////////////////////////

        private void ValidateFile() {
            // we do not want to deal with very long files
            if(PdfFile.BaseStream.Length > 0x40000000)
                throw new ApplicationException("File too big (Max allowed 1GB)");

            // file must have at least 32 byte
            if(PdfFile.BaseStream.Length < 32)
                throw new ApplicationException("File too small to be a PDF document");

            // get file signature at start of file the pdf revision number
            Int32 BufSize = PdfFile.BaseStream.Length > 1024 ? 1024 : (Int32)PdfFile.BaseStream.Length;
            Byte[] Buffer = new Byte[BufSize];
            PdfFile.Read(Buffer, 0, Buffer.Length);

            // skip white space
            Int32 Ptr = 0;
            while(PdfParser.IsWhiteSpace(Buffer[Ptr]))
                Ptr++;

            // save start of file
            StartPosition = Ptr;

            // validate signature 
            if(Buffer[Ptr + 0] != '%' || Buffer[Ptr + 1] != 'P' || Buffer[Ptr + 2] != 'D' ||
                Buffer[Ptr + 3] != 'F' || Buffer[Ptr + 4] != '-' || Buffer[Ptr + 5] != '1' ||
                Buffer[Ptr + 6] != '.' || (Buffer[Ptr + 7] < '0' && Buffer[Ptr + 7] > '7'))
                throw new ApplicationException("Invalid PDF file (bad signature: must be %PDF-1.x)");

            // get file signature at end of file %%EOF
            PdfFile.BaseStream.Position = PdfFile.BaseStream.Length - Buffer.Length;
            PdfFile.Read(Buffer, 0, Buffer.Length);

            // loop in case of extra text after the %%EOF
            Ptr = Buffer.Length - 1;
            for(;;) {
                // look for last F
                for(; Ptr > 32 && Buffer[Ptr] != 'F'; Ptr--)
                    ;
                if(Ptr == 32)
                    throw new ApplicationException("Invalid PDF file (Missing %%EOF at end of the file)");

                // match signature
                if((Buffer[Ptr - 5] == '\n' || Buffer[Ptr - 5] == '\r') && Buffer[Ptr - 4] == '%' &&
                    Buffer[Ptr - 3] == '%' && Buffer[Ptr - 2] == 'E' && Buffer[Ptr - 1] == 'O')
                    break;

                // move pointer back
                Ptr--;
            }

            // set pointer to one character before %%EOF
            Ptr -= 6;

            // remove leading white space (space and eol)
            while(PdfParser.IsWhiteSpace(Buffer[Ptr])) {
                Ptr--;
            }

            // get start of cross reference position
            Int32 XRefPos = 0;
            Int32 Power = 1;
            for(; Char.IsDigit((Char)Buffer[Ptr]); Ptr--) {
                XRefPos += Power * (Buffer[Ptr] - '0');
                Power *= 10;
            }

            // remove leading white space (space and eol)
            while(PdfParser.IsWhiteSpace(Buffer[Ptr])) {
                Ptr--;
            }

            // verify startxref 
            if(Buffer[Ptr - 8] != 's' || Buffer[Ptr - 7] != 't' || Buffer[Ptr - 6] != 'a' ||
                Buffer[Ptr - 5] != 'r' || Buffer[Ptr - 4] != 't' || Buffer[Ptr - 3] != 'x' ||
                Buffer[Ptr - 2] != 'r' || Buffer[Ptr - 1] != 'e' || Buffer[Ptr] != 'f')
                throw new ApplicationException("Invalid PDF file (Missing startxref at end of the file)");

            // set file position to cross reference table
            SetFilePosition(XRefPos);

            // read next character
            ParseFile.ReadFirstChar();

            // there are two possible cross reference cases xref table or xref stream
            // old style cross reference table
            if(ParseFile.ParseNextItem().ToKeyWord == KeyWord.Xref) {
                // restore file position to cross reference table
                SetFilePosition(XRefPos);

                // read all cross reference tables and create empty objects
                while(ReadXrefTable())
                    ;

                // test presence of cross reference stream
                if(XRefStreamPosition == 0)
                    return;

                // set file position to cross reference table
                XRefPos = XRefStreamPosition;
            }

            // set file position to cross reference table
            SetFilePosition(XRefPos);

            // create cross reference object
            while(ReadXRefStream())
                ;

            // exit
            return;
        }

        ////////////////////////////////////////////////////////////////////
        // read cross reference table
        ////////////////////////////////////////////////////////////////////

        private Boolean ReadXrefTable() {
            // create artificial object for the trailer dictionary
            Int32 ObjNo = ObjectArray.Count != 0 && ObjectArray[0].ObjectNo < 0 ? ObjectArray[0].ObjectNo - 1 : -1;

            // create object document
            PdfIndirectObject TrailerObj = new PdfIndirectObject(this, ObjNo, GetFilePosition());

            // object position
            Int32 TrailerIndex = ObjectArray.BinarySearch(TrailerObj);

            // insert object
            ObjectArray.Insert(~TrailerIndex, TrailerObj);

            // read next character
            ParseFile.ReadFirstChar();

            // validate xref
            if(ParseFile.ParseNextItem().ToKeyWord != KeyWord.Xref)
                throw new ApplicationException("Cross reference table in error (missing xref)");

            // loop for possible multiple blocks
            for(;;) {
                // get next object
                PdfBase Token = ParseFile.ParseNextItem();

                // test for trailer
                if(Token.ToKeyWord == KeyWord.Trailer)
                    break;

                // read first object number
                Int32 FirstObjectNo;
                if(!Token.GetInteger(out FirstObjectNo))
                    throw new ApplicationException("Cross reference table in error (missing first object number)");

                // read object count (can be zero)
                Int32 ObjectCount;
                if(!ParseFile.ParseNextItem().GetInteger(out ObjectCount))
                    throw new ApplicationException("Cross reference table in error (missing object count)");

                // loop for cross reference entries
                for(Int32 Index = 0; Index < ObjectCount; Index++) {
                    // object position in file
                    Int32 Position;
                    if(!ParseFile.ParseNextItem().GetInteger(out Position))
                        throw new ApplicationException("Cross reference table in error (object position)");

                    // generation must be zero
                    Int32 Generation;
                    if(!ParseFile.ParseNextItem().GetInteger(out Generation) || Generation != 0 && Generation != 65535)
                        throw new ApplicationException("No support for multi-generation PDF file");

                    // active or deleted
                    KeyWord EntryStatus = ParseFile.ParseNextItem().ToKeyWord;

                    // entry not in use
                    // NOTE: Position == 0 should not happen. However I found it in one file
                    if(EntryStatus == KeyWord.F || Position == 0)
                        continue;

                    // active
                    if(EntryStatus != KeyWord.N)
                        throw new ApplicationException("Cross reference table in error (missing n or f)");

                    // create object document
                    PdfIndirectObject Obj = new PdfIndirectObject(this, FirstObjectNo + Index, Position);

                    // object position
                    Int32 ObjIndex = ObjectArray.BinarySearch(Obj);

                    // insert object
                    // NOTE: in case of duplicate object numbers we keep the most recent one and ignore prior one
                    if(ObjIndex < 0)
                        ObjectArray.Insert(~ObjIndex, Obj);
                }
            }

            // read trailer dictionary
            PdfDict TrailerDict = ParseFile.ParseNextItem().ToDictionary;
            if(TrailerDict == null)
                throw new ApplicationException("Cross reference table in error (missing trailer dictionary)");

            // attach trailer dictionary to trailer dummy object
            TrailerObj.ObjectValue = TrailerDict;
            TrailerObj.ObjectType = "/Trailer";

            // search for /Encrypt
            if(!TrailerDict.GetValue("/Encrypt").IsEmpty)
                throw new ApplicationException("No support for encrypted file");

            // search for /Root
            Int32 Root = TrailerDict.GetValue("/Root").ToObjectRefNo;
            if(Root != 0)
                RootObjectNumber = Root;

            // search for /XRefStm
            Int32 XRefStmPos;
            if(TrailerDict.GetValue("/XRefStm").GetInteger(out XRefStmPos))
                XRefStreamPosition = XRefStmPos;

            // search for /Prev
            Int32 FilePos;
            if(TrailerDict.GetValue("/Prev").GetInteger(out FilePos)) {
                SetFilePosition(FilePos);
                return (true);
            }
            return (false);
        }

        ////////////////////////////////////////////////////////////////////
        // Read cross reference stream
        ////////////////////////////////////////////////////////////////////

        private Boolean ReadXRefStream() {
            // save file position
            Int32 XRefPos = GetFilePosition();

            // read next character
            ParseFile.ReadFirstChar();

            // token must be object number "nnn 0 obj"
            Int32 XRefObjNo = ParseFile.ParseNextItem().ToObjectNo;
            if(XRefObjNo == 0)
                throw new ApplicationException("Reading cross reference stream failed");

            // create cross reference object
            PdfIndirectObject XRefObj = new PdfIndirectObject(this, XRefObjNo, XRefPos);

            // object position
            Int32 XRefIndex = ObjectArray.BinarySearch(XRefObj);

            // there is no old style cross reference table
            if(XRefIndex < 0) {
                // insert object
                ObjectArray.Insert(~XRefIndex, XRefObj);
            }

            // we already have this object
            else {
                XRefObj = ObjectArray[XRefIndex];
            }

            // read this object
            XRefObj.ReadObject();
          
            // object must have a stream
            if(XRefObj.ObjectValue.StreamPosition == 0)
                throw new ApplicationException("Cross reference object must have a stream");

            // read the stream (note: the /Length must be direct value)
            XRefObj.ReadStream();
 
            // test for /Index entry
            PdfBase[] IndexArray = XRefObj.ObjectValue.StreamDictionary.GetValue("/Index").ToArray;
            if(IndexArray == null) {
                // there is no /Index entry, create artificial one
                IndexArray = new PdfBase[2];
                IndexArray[0] = new PdfInt(0);

                // get /Size
                Int32 Size;
                if(!XRefObj.ObjectValue.StreamDictionary.GetValue("/Size").GetInteger(out Size))
                    throw new ApplicationException("Cross reference object must have a /Size");
                IndexArray[1] = new PdfInt(Size);
            }

            // get W array
            PdfBase[] WArray = XRefObj.ObjectValue.StreamDictionary.GetValue("/W").ToArray;
            if(WArray == null || WArray.Length != 3)
                throw new ApplicationException("XRef object missing W array");

            // get the three widths
            Int32 Width1 = ((PdfInt)WArray[0]).IntValue;
            Int32 Width2 = ((PdfInt)WArray[1]).IntValue;
            Int32 Width3 = ((PdfInt)WArray[2]).IntValue;

            // contents stream pointer
            Int32 Ptr = 0;

            // loop for multiple index blocks
            for(Int32 Block = 0; Block < IndexArray.Length; Block += 2) {
                // first object number
                Int32 ObjectNo = ((PdfInt)IndexArray[Block]).IntValue;

                // read object count (can be zero)
                Int32 ObjectCount = ((PdfInt)IndexArray[Block + 1]).IntValue;

                // loop for cross reference entries
                for(Int32 Index = 0; Index < ObjectCount; Index++) {
                    Int32 ObjType = GetField(XRefObj.ObjectValue.StreamContents, Ptr, Width1);
                    Ptr += Width1;
                    Int32 Field2 = GetField(XRefObj.ObjectValue.StreamContents, Ptr, Width2);
                    Ptr += Width2;
                    Int32 Field3 = GetField(XRefObj.ObjectValue.StreamContents, Ptr, Width3);
                    Ptr += Width3;

                    switch(ObjType) {
                        // object is free (deleted) 
                        case 0:
                            // ignore object
                            break;

                        // object pointing to old style indirect object
                        case 1:
                            // field 3 is generation number
                            if(Field3 != 0)
                                throw new ApplicationException("No support for multi-generation PDF file");

                            // create object. Field2 is indirect object position within the file
                            PdfIndirectObject Obj1 = new PdfIndirectObject(this, ObjectNo, Field2);

                            // find object position in object array
                            Int32 ObjIndex1 = ObjectArray.BinarySearch(Obj1);

                            // if new object, add it to the list
                            // if not new, ignore it. We always take the first found object
                            if(ObjIndex1 < 0)
                                ObjectArray.Insert(~ObjIndex1, Obj1);
                            break;

                        // object pointing to new type of object
                        case 2:
                            // create new object. Field2 is parent number, Field3 is index number within parent
                            PdfIndirectObject Obj2 = new PdfIndirectObject(this, ObjectNo, Field2, Field3);

                            // object position
                            Int32 ObjIndex2 = ObjectArray.BinarySearch(Obj2);

                            // insert object
                            if(ObjIndex2 < 0)
                                ObjectArray.Insert(~ObjIndex2, Obj2);
                            break;

                        // error unknown object type
                        default:
                            throw new ApplicationException("Cross reference stream invalid object type (must be 0-2)");
                    }

                    // update object number
                    ObjectNo++;
                }
            }

            // search for /Encrypt
            //var test = XRefObj.ObjectValue.StreamDictionary.GetValue("/Encrypt");
            //if(!XRefObj.ObjectValue.StreamDictionary.GetValue("/Encrypt").IsEmpty)
            //    throw new ApplicationException("No support for encrypted file");

            // search for /Root
            if(RootObjectNumber == 0)
                RootObjectNumber = XRefObj.ObjectValue.StreamDictionary.GetValue("/Root").ToObjectRefNo;

            // search for /Prev
            Int32 FilePos;
            if(XRefObj.ObjectValue.StreamDictionary.GetValue("/Prev").GetInteger(out FilePos)) {
                // there is previous stream dictionary
                SetFilePosition(FilePos);
                return (true);
            }

            // no more stream dictionaries
            return (false);
        }

        ////////////////////////////////////////////////////////////////////
        // Get cross reference stream object field
        ////////////////////////////////////////////////////////////////////

        private Int32 GetField
                (
                Byte[] Contents,
                Int32 Pos,
                Int32 Len
                ) {
            Int32 Val = 0;
            for(; Len > 0; Pos++, Len--)
                Val = 256 * Val + Contents[Pos];
            return (Val);
        }

        ////////////////////////////////////////////////////////////////////
        // Set object type based on indirect reference
        ////////////////////////////////////////////////////////////////////

        private void SetObjectType
                (
                PdfDict Dict
                ) {
            // loop
            foreach(PdfPair Pair in Dict.DictValue) {
                // look for child dictionaries
                if(Pair.ObjValue.IsDictionary) {
                    SetObjectType(Pair.ObjValue.ToDictionary);
                    continue;
                }

                // look for pairs with indirect reference
                if(Pair.ObjValue.IsReference) {
                    PdfIndirectObject Obj = FindObject(Pair.ObjValue.ToObjectRefNo);
                    if(Obj != null && String.IsNullOrEmpty(Obj.ObjectType))
                        Obj.ObjectType = Pair.Key;
                    continue;
                }

                // look for arrays
                if(Pair.ObjValue.IsArray) {
                    // look for indirect references
                    foreach(PdfBase Item in Pair.ObjValue.ToArray) {
                        PdfIndirectObject Obj = FindObject(Item.ToObjectRefNo);
                        if(Obj != null && String.IsNullOrEmpty(Obj.ObjectType))
                            Obj.ObjectType = Pair.Key;
                    }
                    continue;
                }
            }

            // exit
            return;
        }

        ////////////////////////////////////////////////////////////////////
        // Find array of objects (Kids and Contents)
        ////////////////////////////////////////////////////////////////////

        private PdfIndirectObject[] GetArrayOfObjects
                (
                PdfDict Dict,
                String Key
                ) {
            // get dictionary pair
            PdfBase Token = Dict.GetValue(Key);

            // test for reference
            if(Token.IsReference) {
                PdfIndirectObject Obj = FindObject(Token.ToObjectRefNo);
                if(Obj != null)
                    return (new PdfIndirectObject[] { Obj });
            }

            // test for array
            else if(Token.IsArray) {
                PdfBase[] RefArray = Token.ToArray;
                PdfIndirectObject[] ObjArray = new PdfIndirectObject[RefArray.Length];
                for(Int32 Index = 0; Index < RefArray.Length; Index++) {
                    ObjArray[Index] = FindObject(RefArray[Index].ToObjectRefNo);
                    if(ObjArray[Index] == null)
                        return (null);
                }
                return (ObjArray);
            }

            // not found
            return (null);
        }

        public List<PdfTextObject> PageTextData(int pageNum) {
            List<PdfTextObject> textObjects = new List<PdfTextObject>();
            PdfIndirectObject page = PageObjectArray[pageNum];
            byte[] pageBytes = page.PageContents;
            string result = System.Text.Encoding.UTF8.GetString(pageBytes);
            int position = 0;
            byte[] data = PdfFunctions.GetTextBlock(pageBytes, position, out position);
            while(data != null) {
                textObjects.Add(new PdfTextObject(data));
                data = PdfFunctions.GetTextBlock(pageBytes, position, out position);
            }
            return textObjects;
        }
    }
}
