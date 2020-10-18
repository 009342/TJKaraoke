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
        static bool ByteArrayCompare(byte[] b1, byte[] b2)
        {
            return b1.Length == b2.Length && b1.SequenceEqual(b2);
        }
        //private static byte[] magic = { 0x54, 0x41, 0x49, 0x4A, 0x49, 0x4E, 0x20, 0x4D, 0x45, 0x44, 0x49, 0x41, 0x20, 0x43, 0x4F, 0x2E, 0x2C, 0x4C, 0x54, 0x44,
        //    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        //매직이 다른 경우가 있기에 앞 16바이트만 검사
        private static byte[] magic = { 0x54, 0x41, 0x49, 0x4A, 0x49, 0x4E, 0x20, 0x4D, 0x45, 0x44, 0x49, 0x41, 0x20, 0x43, 0x4F, 0x2E };
        private static byte[] xcgmagic = { 0x58, 0x43, 0x47, 0x20, 0x31, 0x2e, 0x30, 0x1a }; //XCG 1.0 

        private static char[] specialCharsc = { '@', ')', 'っ', 'ん', 'ー', 'ン', 'ッ', 'ョ', 'ょ', 'ゃ', 'ャ', 'ゅ', 'ュ', 'ぁ', 'ァ', 'ぃ', 'ィ', 'ぅ', 'ゥ', 'ぇ', 'ェ', 'ぉ', 'ォ', '[', ']', '/', '　', ' ' }; //(는 나오면 덩어리로 처리해줘야 하니 예외,

        private static char[] specialCharsc2 = { '　', ' ' }; //La la이런 경우에는 잘라주어야 겠지
        private static char[] specialCharsc3 = { '(' }; //전에 문자를 확인할 필요가 있을 경우, 전 문자가 ( 인 경우
        private static char[] specialCharsc4 = {'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z'
        ,'a','b','c','d','e','f','g','h','i','j','k','l','m','n','o','p','q','r','s','t','u','v','w','x','y','z','\''}; //알파벳 예외처리 ばABC같은 경우에는 별도로
        private static char[] specialCharsc5 = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
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
                        Encoding = Encoding.GetEncoding("ks_c_5601-1987");
                        break;
                    case 1:
                    case 4:
                    case 6:
                    case 7:
                    case 0x14:
                        Encoding = Encoding.GetEncoding("EUC-KR");
                        break;
                    case 2:
                        //Encoding = Encoding.GetEncoding("CP936");
                        Encoding = Encoding.GetEncoding("EUC-CN");
                        break;
                    case 3:
                        Encoding = Encoding.GetEncoding("Shift_JIS");
                        break;
                    case 5:
                        Encoding = Encoding.GetEncoding("windows-1258");
                        break;
                    case 8:
                        Encoding = Encoding.GetEncoding("Windows-1252");
                        break;
                    case 9:
                        Encoding = Encoding.GetEncoding("windows-874");
                        break;
                    case 10:
                        Encoding = Encoding.GetEncoding("windows-1251");
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
                        string line = "";
                        Queue<char> lyricsQue = new Queue<char>();
                        Queue<TickEvent> lyricsEventsQueue = new Queue<TickEvent>();
                        using (StreamReader streamReader = new StreamReader(lyricsStream, Encoding))
                        {
                            Lyrics.kNumber = int.Parse(streamReader.ReadLine().Split('#')[1]);
                            streamReader.ReadLine();
                            Lyrics.title = streamReader.ReadLine();
                            Lyrics.subTitle = streamReader.ReadLine();
                            streamReader.ReadLine();
                            Lyrics.lyricist = streamReader.ReadLine();
                            Lyrics.composer = streamReader.ReadLine();
                            Lyrics.singer = streamReader.ReadLine();
                            streamReader.ReadLine();
                            while (true)
                            {
                                line = streamReader.ReadLine();
                                if (line == "HANGUL" || streamReader.EndOfStream)
                                {
                                    break;
                                }
                                foreach (char item in line)
                                {
                                    lyricsQue.Enqueue(item);
                                }
                                lyricsQue.Enqueue('\n');
                            }
                        }
                        int gasaline = 0;
                        int gasaindex = 0;
                        int mode = 0;
                        bool cut = true; //연속을 자를 것인가, 처음에 영어가 나올 경우를 대비에서 true로 설정
                        PronGuide pronGuide = null;
                        char prev = '\r';
                        while (lyricsQue.Count != 0)
                        {
                            char c = lyricsQue.Dequeue();
                            if (c == '\n')
                            {
                                cut = true;
                                gasaline++;
                                gasaindex = 0;
                                mode = 0;
                            }
                            else if (c == '_') //_는 무조건 끊어주야 하기에 cut=true
                            {
                                cut = true;
                            }
                            else if (c == '＜')
                            {
                                cut = true;
                                mode = 1;
                                pronGuide = new PronGuide();
                            }
                            else if (c == '＞')
                            {
                                mode = 0;
                                cut = false;
                                pronGuide = null;
                            }
                            else if (c == '「')
                            {
                                mode = 2;
                            }
                            else if (c == '」')
                            {
                                mode = 0;
                                pronGuide = null;
                            }
                            else if (specialCharsc3.Contains(prev)) //전 문자가 (인 경우에는 무조건 붙여줘야 함
                            {
                                lyricsEventsQueue.LastOrDefault().str += c.ToString();
                                prev = c;
                            }
                            else if (mode == 2) //발음기호 추가해줌
                            {
                                if (lyricsEventsQueue.LastOrDefault().PronGuide == null)
                                {
                                    lyricsEventsQueue.LastOrDefault().PronGuide = new PronGuide();
                                }
                                lyricsEventsQueue.LastOrDefault().PronGuide.Pron += c.ToString();
                            }
                            else if (!cut && specialCharsc.Contains(c))
                            {
                                lyricsEventsQueue.LastOrDefault().str += c.ToString();
                                if (specialCharsc2.Contains(c)) //띄어쓰기인 경우에는 뒤에 영어가 나와도 잘라줌
                                {
                                    cut = true;
                                }
                                prev = c;
                            }
                            else if (!cut && specialCharsc4.Contains(prev) && specialCharsc4.Contains(c))
                            {
                                lyricsEventsQueue.LastOrDefault().str += c.ToString();
                                if (specialCharsc2.Contains(c)) //띄어쓰기인 경우에는 뒤에 영어가 나와도 잘라줌
                                {
                                    cut = true;
                                }
                                prev = c;
                            }
                            else if (!cut && specialCharsc5.Contains(prev) && specialCharsc5.Contains(c))
                            {
                                lyricsEventsQueue.LastOrDefault().str += c.ToString();
                                if (specialCharsc2.Contains(c)) //띄어쓰기인 경우에는 뒤에 영어가 나와도 잘라줌
                                {
                                    cut = true;
                                }
                                prev = c;
                            }
                            else
                            {
                                TickEvent tickEvent = new TickEvent();
                                tickEvent.str = c.ToString();
                                tickEvent.indexOfLine = gasaindex++;
                                tickEvent.lineNumber = gasaline;
                                tickEvent.PronGuide = pronGuide; //모드가 1인 경우에는 같은pronguide, 0일 경우에는 null이 들어감
                                lyricsEventsQueue.Enqueue(tickEvent);
                                cut = false;
                                prev = c;
                            }
                        }
                        /*foreach (var item in lyricsEventsQueue)
                        {
                            Console.WriteLine(item.str);
                        }*/
                        buffer = new byte[4];
                        while (syncStream.Position != syncStream.Length)
                        {
                            byte temp = (byte)syncStream.ReadByte();
                            if (temp == 0x01)
                            {
                                syncStream.Read(buffer, 0, 4);
                                TickEvent t = lyricsEventsQueue.Dequeue();
                                Console.WriteLine(t.str);
                                t.tick = BitConverter.ToInt32(buffer, 0);
                                t.cmd = temp;
                                tickEvents.Add(t);

                            }
                            else
                            {
                                syncStream.Read(buffer, 0, 4);
                                TickEvent t = new TickEvent();
                                t.tick = BitConverter.ToInt32(buffer, 0);
                                t.cmd = temp;
                                t.lineNumber = tickEvents.LastOrDefault() == null ? 0 : tickEvents.LastOrDefault().lineNumber;
                                tickEvents.Add(t);
                            }
                        }

                    }
                }
                using (Stream lyricsStream = new MemoryStream(LyricsData))
                {
                    using (StreamReader streamReader = new StreamReader(lyricsStream, Encoding.GetEncoding("ks_c_5601-1987")))
                    {
                        while (streamReader.ReadLine() != "HANGUL")
                        {
                        }
                        while(!streamReader.EndOfStream)
                        {
                            Lyrics.HangulLyrics = streamReader.ReadToEnd();
                        }
                    }
                }
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