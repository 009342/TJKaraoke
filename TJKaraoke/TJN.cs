using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace TJKaraoke
{
    public class TJN
    {
        //[DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        //static extern int memcmp(byte[] b1, byte[] b2, long count);
        static bool ByteArrayCompare(byte[] b1, byte[] b2)
        {
            return b1.Length == b2.Length && b1.SequenceEqual(b2);
        }//운영체제 의존성 코드 수정
        //private static byte[] magic = { 0x54, 0x41, 0x49, 0x4A, 0x49, 0x4E, 0x20, 0x4D, 0x45, 0x44, 0x49, 0x41, 0x20, 0x43, 0x4F, 0x2E, 0x2C, 0x4C, 0x54, 0x44,
        //    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        //매직이 다른 경우가 있기에 앞 16바이트만 검사
        private static byte[] magic = { 0x54, 0x41, 0x49, 0x4A, 0x49, 0x4E, 0x20, 0x4D, 0x45, 0x44, 0x49, 0x41, 0x20, 0x43, 0x4F, 0x2E };
        private static byte[] xcgmagic = { 0x58, 0x43, 0x47, 0x20, 0x31, 0x2e, 0x30, 0x1a }; //XCG 1.0 
        private static string regexString = @"^[a-zA-Z0-9@()\]\[/']+$";
        private static string[] specialChars = { "@", "(", ")", "っ", "ん", "ー", "ン", "ッ", "　", " ", "ョ", "ょ", "ゃ", "ャ", "ゅ", "ュ", "ぁ", "ァ", "ぃ", "ィ", "ぅ", "ゥ", "ぇ", "ェ", "ぉ", "ォ", "[", "]", "/", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
        private static string[] specialChars2 = { "]", "(" };
        //한 덩어리로 처리해야 할 특수한 문자들
        private static string[] specialChars3 = { "　", " " };
        //무조건 루프를 끊어주야 할 특수한 문자들

        private static char[] specialCharsc = { '@', ')', 'っ', 'ん', 'ー', 'ン', 'ッ', 'ョ', 'ょ', 'ゃ', 'ャ', 'ゅ', 'ュ', 'ぁ', 'ァ', 'ぃ', 'ィ', 'ぅ', 'ゥ', 'ぇ', 'ェ', 'ぉ', 'ォ', '[', ']', '/', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9'
        ,'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z'
        ,'a','b','c','d','e','f','g','h','i','j','k','l','m','n','o','p','q','r','s','t','u','v','w','x','y','z','\'','　', ' ' }; //(는 나오면 덩어리로 처리해줘야 하니 예외,
        private static char[] specialCharsc2 = { '　', ' ' }; //La la이런 경우에는 잘라주어야 겠지
        private static char[] specialCharsc3 = { '(' }; //전에 문자를 확인할 필요가 있을 경우, 전 문자가 ( 인 경우
        public string Path { get; private set; }
        public int Country { get; private set; }
        public int MidiSize { get; private set; }
        public int XcgSize { get; private set; }
        public int ChordSize { get; private set; }
        public byte[] MidiData { get; private set; }
        public byte[] XcgData { get; private set; }
        public byte[] ChordData { get; private set; }
        public short LyricsSize { get; private set; }
        public short SyncSize { get; private set; }
        public byte[] SyncData { get; private set; }
        public byte[] LyricsData { get; private set; }
        public SongInfo Lyrics { get; private set; }
        public Encoding Encoding { get; private set; }
        public TJN(string path, int country, bool isCompressed)
        {

            if (isCompressed)
            {
                throw new NotImplementedException("");
            }
            this.Country = country;
            this.Path = path;
            using (FileStream fstream = File.OpenRead(path))
            {
                byte[] buffer = new byte[0x10];
                fstream.Read(buffer, 0, 0x10);
                if (!ByteArrayCompare(magic, buffer))
                    throw new Exception("");
                fstream.Position += 0x10;
                buffer = new byte[4];

                fstream.Read(buffer, 0, 4);
                MidiSize = BitConverter.ToInt32(buffer, 0);
                fstream.Read(buffer, 0, 4);
                XcgSize = BitConverter.ToInt32(buffer, 0);
                fstream.Read(buffer, 0, 4);
                ChordSize = BitConverter.ToInt32(buffer, 0);
                fstream.Position += 8;
                MidiData = new byte[MidiSize];
                fstream.Read(MidiData, 0, MidiSize);
                XcgData = new byte[XcgSize];
                fstream.Read(XcgData, 0, XcgSize);
                ChordData = new byte[ChordSize];
                fstream.Read(ChordData, 0, ChordSize);

                using (Stream stream = new MemoryStream(XcgData))
                {
                    buffer = new byte[8];
                    stream.Read(buffer, (int)stream.Position, 8);
                    if (!ByteArrayCompare(buffer, xcgmagic))
                        throw new Exception();
                    buffer = new byte[2];
                    stream.Read(buffer, 0, 2);
                    LyricsSize = BitConverter.ToInt16(buffer, 0);
                    stream.Read(buffer, 0, 2);
                    SyncSize = BitConverter.ToInt16(buffer, 0);
                    LyricsData = new byte[LyricsSize];
                    stream.Read(LyricsData, 0, LyricsSize);
                    SyncData = new byte[SyncSize];
                    stream.Read(SyncData, 0, SyncSize);
                }

                switch (country)
                {
                    case 0:
                        Encoding = Encoding.GetEncoding("EUC-KR");
                        break;
                    case 1:
                        Encoding = Encoding.Default;
                        break;
                    case 3:
                        Encoding = Encoding.GetEncoding("Shift_JIS");
                        break;
                    default:
                        throw new NotImplementedException();
                }
                Lyrics = new SongInfo();
                Lyrics.tickEvents = new List<TickEvent>();
                var tickEvents = Lyrics.tickEvents;
                using (Stream syncStream = new MemoryStream(SyncData))
                {
                    using (Stream lyricsStream = new MemoryStream(LyricsData))
                    {
                        using (StreamReader streamReader = new StreamReader(lyricsStream, Encoding))
                        {
                            int gasaLine = 0;
                            int indexOfLine = 0;
                            Lyrics.kNumber = int.Parse(streamReader.ReadLine().Split('#')[1]);
                            streamReader.ReadLine();
                            Lyrics.title = streamReader.ReadLine();
                            Lyrics.subTitle = streamReader.ReadLine();
                            streamReader.ReadLine();
                            Lyrics.lyricist = streamReader.ReadLine();
                            Lyrics.composer = streamReader.ReadLine();
                            Lyrics.singer = streamReader.ReadLine();
                            buffer = new byte[4];
                            string line = streamReader.ReadLine();
                            bool pronContinue = false;
                            while (syncStream.Position != syncStream.Length)
                            {
                                string word = "";
                                string seperator = "";
                                TickEvent t = new TickEvent();
                                t.cmd = (byte)syncStream.ReadByte();
                                t.lineNumber = gasaLine;
                                t.indexOfLine = indexOfLine;
                                if (t.cmd == 0x01)
                                {
                                    while (line == "")
                                    {
                                        line = streamReader.ReadLine();
                                    }
                                    t.lineNumber = gasaLine;
                                    t.indexOfLine = indexOfLine++;
                                    if (word == "" && line.Contains(" "))
                                    {
                                        word = line.Split(new char[] { ' ' })[0];
                                        seperator = " ";
                                    }
                                    else if (word == "" && line.Contains("　"))
                                    {
                                        word = line.Split(new char[] { '　' })[0];
                                        seperator = "　";
                                    }
                                    else if (word == "")
                                    {
                                        word = line;
                                        seperator = "";
                                    }
                                    if (word.Contains("_")) //특수한 케이스,_는 제거해주어야 함
                                    {
                                        word = line.Split(new char[] { '_' })[0];
                                        seperator = "_";
                                    }
                                    if (word != "" && Regex.IsMatch(word, regexString)) //영어, 숫자 처리부
                                    {
                                        t.str = word + (seperator == "_" ? "" : seperator);
                                        line = line.Substring(t.str.Length + (seperator == "_" ? 1 : 0));
                                    }
                                    if (pronContinue) //요미가나의 연속성을 위해
                                    {
                                        t.PronGuide = tickEvents.Last((last) =>
                                        {
                                            return last.cmd == 0x01;
                                        }).PronGuide;
                                        if (line.Substring(1, 1) == "＞")
                                        {
                                            t.str = line.Substring(0, 1);
                                            line = line.Substring(2, line.Length - 2);
                                            pronContinue = false;
                                        }
                                    }
                                    while (true)
                                    {
                                        while (line.Length > 0 && specialChars.Contains(line.Substring(0, 1)))
                                        {

                                            t.str += line.Substring(0, 1);
                                            line = line.Substring(1);
                                        }
                                        if (line.Length > 0 && t.str.Length > 0 && specialChars2.Contains(t.str.Substring(t.str.Length - 1)) && line.Substring(0, 1) != "＜")
                                        {
                                            t.str += line.Substring(0, 1);
                                            line = line.Substring(1);
                                        }
                                        else break;
                                    }

                                    if (!(t.str.Length > 0 && specialChars3.Contains(t.str.Substring(t.str.Length - 1))) //asdf ＜発音＞과 같은 경우를 위해 
                                        && line.Length > 0 && line.Substring(0, 1) == "＜")
                                    {
                                        word = line.Substring(1).Split('＞')[0];
                                        if (Regex.IsMatch(word, regexString) || word.Length == 1)
                                        {
                                            t.str = word;
                                            line = line.Substring(word.Length + 2);
                                        }
                                        else
                                        {
                                            pronContinue = true;
                                            t.PronGuide = new PronGuide();
                                            line = line.Substring(1, line.Length - 1);
                                            t.str += line.Substring(0, 1);
                                            line = line.Substring(1, line.Length - 1);
                                        }
                                    }
                                    if (line.Length > 0) //다음 문자열 가져오고 대비
                                    {
                                        bool loop = true;
                                        while (loop)
                                        {

                                            if (line.Length > 0 && t.str == "")
                                            {
                                                t.str += line.Substring(0, 1);
                                                line = line.Substring(1);
                                            }
                                            if (line.Length > 0 && specialChars.Contains(line.Substring(0, 1)))
                                            {
                                                if (specialChars3.Contains(line.Substring(0, 1))) loop = false;
                                                t.str += line.Substring(0, 1);
                                                line = line.Substring(1, line.Length - 1);
                                            }
                                            else if (line.Length > 0 && line.Substring(0, 1) == "「")
                                            {
                                                if (t.PronGuide == null)
                                                    t.PronGuide = new PronGuide();
                                                t.PronGuide.Pron = line.Split('「')[1].Split('」')[0];
                                                line = line.Substring((t.PronGuide.Pron.Length + 2), line.Length - (t.PronGuide.Pron.Length + 2));
                                            }
                                            else
                                            {
                                                loop = false;
                                            }
                                        }

                                    }
                                    if (line == "")
                                    {
                                        gasaLine += 1;
                                        indexOfLine = 0;
                                    }
                                }
                                syncStream.Read(buffer, 0, 4);
                                t.tick = BitConverter.ToInt32(buffer, 0);
                                tickEvents.Add(t);
                            }

                        }

                    }
                }
                /*
                int part = 3;
                TickEvent temp = null;
                foreach (TickEvent item in Lyrics.tickEvents)
                {
                    if (temp == null)
                    {
                        item.part = 3;
                    }
                    if (item.str.Contains("[/"))
                    {
                        item.part = part;
                        part = 3;
                        item.str = Regex.Replace(item.str, @" ?\[.*?\]", string.Empty);
                    }
                    else if (item.str.Contains("["))
                    {
                        part = int.Parse(item.str.Split('[')[1].Split(']')[0]);
                        item.part = part;
                        item.str = Regex.Replace(item.str, @" ?\[.*?\]", string.Empty);
                    }
                    else
                    {
                        item.part = part;
                    }

                }
                                데이터의 일관성을 해칠 염려가 있을려나,
                */
            }
        }
    }
    public class SongInfo
    {
        public int kNumber;
        public string title;
        public string subTitle;
        public string composer;
        public string lyricist;
        public string singer;
        public List<TickEvent> tickEvents;
        public string HangulLyrics = null;
    }
    public class TickEvent
    {
        public int tick;
        public byte cmd;
        public string str = "";
        //public int part = 3;
        /*
         * 0 : 여자
         * 1 : 남자
         * 2 : 혼성
         * 3 : 기본
         */
        private PronGuide privPronGuide;
        public PronGuide PronGuide
        {
            get
            {
                return privPronGuide;
            }
            set
            {
                if (value != null)
                {
                    privPronGuide = value;
                    privPronGuide.Ref = new List<TickEvent>();
                    privPronGuide.Ref.Add(this);
                }
            }
        }
        public int lineNumber = 0;
        public int indexOfLine = 0;
    }
    public class PronGuide
    {
        public string Pron;
        public List<TickEvent> Ref;
    }
}