using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TJKaraoke
{
    class TJN
    {
        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int memcmp(byte[] b1, byte[] b2, long count);
        static bool ByteArrayCompare(byte[] b1, byte[] b2)
        {
            return b1.Length == b2.Length && memcmp(b1, b2, b1.Length) == 0;
        }
        //private static byte[] magic = { 0x54, 0x41, 0x49, 0x4A, 0x49, 0x4E, 0x20, 0x4D, 0x45, 0x44, 0x49, 0x41, 0x20, 0x43, 0x4F, 0x2E, 0x2C, 0x4C, 0x54, 0x44,
        //    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        //매직이 다른 경우가 있기에 앞 16바이트만 검사
        private static byte[] magic = { 0x54, 0x41, 0x49, 0x4A, 0x49, 0x4E, 0x20, 0x4D, 0x45, 0x44, 0x49, 0x41, 0x20, 0x43, 0x4F, 0x2E };
        private static byte[] xcgmagic = { 0x58, 0x43, 0x47, 0x20, 0x31, 0x2e, 0x30, 0x1a }; //XCG 1.0 
        private static string[] specialCharacer = { "@", "(", ")", "っ", "ん", "ー", "ン", "ッ", "　", " ", "ョ", "ょ", "ゃ", "ャ", "ゅ", "ュ", "ぁ", "ァ", "ぃ", "ィ", "ぅ", "ゥ", "ぇ", "ェ", "ぉ", "ォ" };
        //한 덩어리로 처리해야 할 특수한 문자들
        public string path { get; private set; }
        public int country { get; private set; }
        public int midiSize { get; private set; }
        public int xcgSize { get; private set; }
        public int chordSize { get; private set; }
        public byte[] midi { get; private set; }
        public byte[] xcg { get; private set; }
        public byte[] chord { get; private set; }
        public short lyricsSize { get; private set; }
        public short syncSize { get; private set; }
        public byte[] syncData { get; private set; }
        public byte[] lyricsData { get; private set; }
        public SongInfo lyrics { get; private set; }
        public Encoding encoding { get; private set; }
        public TJN(string path, int country, bool isCompressed)
        {

            if (isCompressed)
            {
                throw new NotImplementedException("");
            }
            this.country = country;
            this.path = path;
            using (FileStream fstream = File.OpenRead(path))
            {
                byte[] buffer = new byte[0x10];
                fstream.Read(buffer, 0, 0x10);
                if (!ByteArrayCompare(magic, buffer))
                    throw new Exception("");
                fstream.Position += 0x10;
                buffer = new byte[4];

                fstream.Read(buffer, 0, 4);
                midiSize = BitConverter.ToInt32(buffer, 0);
                fstream.Read(buffer, 0, 4);
                xcgSize = BitConverter.ToInt32(buffer, 0);
                fstream.Read(buffer, 0, 4);
                chordSize = BitConverter.ToInt32(buffer, 0);
                fstream.Position += 8;
                midi = new byte[midiSize];
                fstream.Read(midi, 0, midiSize);
                xcg = new byte[xcgSize];
                fstream.Read(xcg, 0, xcgSize);
                chord = new byte[chordSize];
                fstream.Read(chord, 0, chordSize);

                using (Stream stream = new MemoryStream(xcg))
                {
                    buffer = new byte[8];
                    stream.Read(buffer, (int)stream.Position, 8);
                    if (!ByteArrayCompare(buffer, xcgmagic))
                        throw new Exception();
                    buffer = new byte[2];
                    stream.Read(buffer, 0, 2);
                    lyricsSize = BitConverter.ToInt16(buffer, 0);
                    stream.Read(buffer, 0, 2);
                    syncSize = BitConverter.ToInt16(buffer, 0);
                    lyricsData = new byte[lyricsSize];
                    stream.Read(lyricsData, 0, lyricsSize);
                    syncData = new byte[syncSize];
                    stream.Read(syncData, 0, syncSize);
                }

                switch (country)
                {
                    case 0:
                        encoding = Encoding.GetEncoding("EUC-KR");
                        break;
                    case 1:
                        encoding = Encoding.Default;
                        break;
                    case 3:
                        encoding = Encoding.GetEncoding("Shift_JIS");
                        break;
                    default:
                        throw new NotImplementedException();
                }
                lyrics = new SongInfo();
                lyrics.tickEvents = new List<TickEvent>();
                var tickEvents = lyrics.tickEvents;
                using (Stream syncStream = new MemoryStream(syncData))
                {
                    using (Stream lyricsStream = new MemoryStream(lyricsData))
                    {
                        using (StreamReader streamReader = new StreamReader(lyricsStream, encoding))
                        {
                            int gasaLine = 0;
                            int indexOfLine = 0;
                            lyrics.kNumber = int.Parse(streamReader.ReadLine().Split('#')[1]);
                            streamReader.ReadLine();
                            lyrics.name = streamReader.ReadLine();
                            streamReader.ReadLine();
                            streamReader.ReadLine();
                            lyrics.lyricist = streamReader.ReadLine();
                            lyrics.composer = streamReader.ReadLine();
                            lyrics.singer = streamReader.ReadLine();
                            buffer = new byte[4];
                            string line = streamReader.ReadLine();
                            bool pronContinue = false;
                            while (syncStream.Position != syncStream.Length)
                            {
                                string word = "";
                                string seperator = "";
                                TickEvent t = new TickEvent();
                                t.cmd = (byte)syncStream.ReadByte();
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
                                    if (word != "" && Regex.IsMatch(word, @"^[a-zA-Z0-9@()]+$")) //영어 처리부
                                    {
                                        t.str = word + (seperator == "_" ? "" : seperator);
                                        line = line.Substring(t.str.Length + (seperator == "_" ? 1 : 0));
                                    }
                                    else
                                    {


                                        do
                                        {
                                            t.str += line.Substring(0, 1);
                                            line = line.Substring(1, line.Length - 1);
                                            if (t.str.Substring(t.str.Length - 1) == "＜")
                                            {
                                                if (line.IndexOf('＞') > 1)
                                                {
                                                    pronContinue = true;
                                                    t.pronGuide = new PronGuide();
                                                    t.str = t.str.Substring(0, t.str.Length - 1);
                                                    t.str += line.Substring(0, 1);
                                                    line = line.Substring(1, line.Length - 1);
                                                }
                                                else
                                                {
                                                    t.str = line.Substring(0, 1);
                                                    line = line.Substring(2, line.Length - 2);
                                                }
                                            }
                                            else if (pronContinue)
                                            {
                                                t.pronGuide = tickEvents.Last((last) =>
                                                {
                                                    return last.cmd == 0x01;
                                                }).pronGuide;
                                                if (line.Substring(0, 1) == "＞")
                                                {
                                                    line = line.Substring(1, line.Length - 1);
                                                    pronContinue = false;
                                                }
                                            }
                                        } while (specialCharacer.Contains(t.str.Substring(t.str.Length - 1)));
                                        if (line.Length > 0)
                                        {
                                            bool loop = true;
                                            while (loop && line.Length > 0)
                                            {
                                                if (specialCharacer.Contains(line.Substring(0, 1)))
                                                {
                                                    t.str += line.Substring(0, 1);
                                                    line = line.Substring(1, line.Length - 1);
                                                }
                                                else if (line.Substring(0, 1) == "「")
                                                {
                                                    if (t.pronGuide == null)
                                                        t.pronGuide = new PronGuide();
                                                    t.pronGuide.Pron = line.Split('「')[1].Split('」')[0];
                                                    line = line.Substring((t.pronGuide.Pron.Length + 2), line.Length - (t.pronGuide.Pron.Length + 2));
                                                }
                                                else
                                                {
                                                    loop = false;
                                                }
                                            }

                                        }
                                    }
                                    if (line == "")
                                    {
                                        gasaLine += 1;
                                        indexOfLine = 0;
                                    }
                                }
                                else
                                {
                                    if (tickEvents.Exists((last) => { return last.cmd == 0x01; }))
                                    {
                                        t.lineNumber = tickEvents.Last((last) => { return last.cmd == 0x01; }).lineNumber;
                                    }
                                    else
                                    {
                                        t.lineNumber = gasaLine;
                                    }

                                }
                                syncStream.Read(buffer, 0, 4);
                                t.tick = BitConverter.ToInt32(buffer, 0);
                                tickEvents.Add(t);
                            }

                        }
                    }
                }
            }
        }
    }
    class SongInfo
    {
        public int kNumber;
        public string name;
        public string composer;
        public string lyricist;
        public string singer;
        public List<TickEvent> tickEvents;
    }
    class TickEvent
    {
        public int tick;
        public byte cmd;
        public string str = "";
        public PronGuide pronGuide;
        public int lineNumber = 0;
        public int indexOfLine = 0;
    }
    class PronGuide
    {
        public string Pron;
    }
}