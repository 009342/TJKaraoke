using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Melanchall.DryWetMidi.Devices;
using Melanchall.DryWetMidi.Core;
using System.IO;
using System.Threading;
using Melanchall.DryWetMidi.Interaction;
using System.Runtime.InteropServices;

namespace TJKaraoke
{
    class Program
    {
        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int memcmp(byte[] b1, byte[] b2, long count);
        static bool ByteArrayCompare(byte[] b1, byte[] b2)
        {
            return b1.Length == b2.Length && memcmp(b1, b2, b1.Length) == 0;
        }
        static Queue<TickEvent> syncQue = new Queue<TickEvent>();
        static void Main(string[] args)
        {
#if !DEBUG
            try
            {
                if (!(args.Length == 2 || args.Length == 3))
                {
                    Console.WriteLine("Usage : TJKaraoke.exe [File Name] [Country Code] [MIDI Device ID = 0 (Default)]");
                    Console.WriteLine();
                    Console.WriteLine("https://github.com/009342");
                    Console.WriteLine();
                    Console.WriteLine("OSS Notice");
                    Console.WriteLine("DryWetMIDI");
                    Console.WriteLine("https://github.com/melanchall/drywetmidi");
                    Console.WriteLine("Copyright (c) 2018 Maxim Dobroselsky");
                    Console.WriteLine("MIT License");

                }
                else
                {
                    TJN karaoke = new TJN(args[0], int.Parse(args[1]), false);
                    Console.OutputEncoding = karaoke.encoding;
                    syncQue = new Queue<TickEvent>(karaoke.lyrics.tickEvents);
                    var midiFile = MidiFile.Read(new MemoryStream(karaoke.midi));
                    int deviceId = int.Parse((args.Length == 3) ? args[2] : "0");
                    Console.WriteLine(karaoke.lyrics.kNumber);
                    Console.WriteLine(karaoke.lyrics.name);
                    Console.WriteLine(karaoke.lyrics.lyricist);
                    Console.WriteLine(karaoke.lyrics.composer);
                    Console.WriteLine(karaoke.lyrics.singer);
                    var outputDevice = OutputDevice.GetById(deviceId);
                    var playback = midiFile.GetPlayback(outputDevice);
                    playback.Finished += Playback_Finished;
                    Thread thread = new Thread(() => { Playback(playback); });
                    playback.Start();
                    thread.Start();
                    Thread.Sleep(-1);
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.ToString());
            }

#else
            TJN karaoke = new TJN(args[0], int.Parse(args[1]), false);
            Console.OutputEncoding = karaoke.encoding;
            syncQue = new Queue<TickEvent>(karaoke.lyrics.tickEvents);
            foreach (var item in syncQue)
            {
                Console.WriteLine("{0} {1} {2} {3} {4}  {5}", item.tick, item.cmd, item.lineNumber, item.indexOfLine, item.str, item.pronGuide != null ? item.pronGuide.Pron : "");
            }
            var midiFile = MidiFile.Read(new MemoryStream(karaoke.midi));
            int deviceId = int.Parse((args.Length == 3) ? args[2] : "0");
            Console.WriteLine(karaoke.lyrics.kNumber);
            Console.WriteLine(karaoke.lyrics.name);
            Console.WriteLine(karaoke.lyrics.lyricist);
            Console.WriteLine(karaoke.lyrics.composer);
            Console.WriteLine(karaoke.lyrics.singer);
            var outputDevice = OutputDevice.GetById(deviceId);
            var playback = midiFile.GetPlayback(outputDevice);
            playback.Finished += Playback_Finished;
            Thread thread = new Thread(() => { Playback(playback); });
            playback.Start();
            thread.Start();
            Thread.Sleep(-1);
#endif

        }

        private static void Playback_Finished(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private static void Playback(object sender)
        {
            int line = 0;
            while (syncQue.Count != 0)
            {
                int time = (int)TimeConverter.ConvertFrom(((Playback)sender).GetCurrentTime<ITimeSpan>(), ((Playback)sender).TempoMap);
                TickEvent peek = syncQue.Peek();
                if (peek.tick <= time)
                {
                    TickEvent tick = syncQue.Dequeue();
                    if (tick.cmd == 0x01)
                    {
                        if (line != tick.lineNumber)
                        {
                            line = tick.lineNumber;
                            Console.WriteLine();
                        }
                        Console.Write(tick.str);
                    }
                }
                Thread.Sleep(1);
            }

        }
    }
}
