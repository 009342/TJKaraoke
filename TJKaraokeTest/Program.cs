using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Devices;
using Melanchall.DryWetMidi.Interaction;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TJKaraoke;

namespace TJKaraokeTest
{
    class Program
    {
        static Queue<TickEvent> syncQue = new Queue<TickEvent>();
        static void Main(string[] args)
        {
#if !DEBUG
            try
            {
                if (!(args.Length == 2 || args.Length == 3))
                {
                    Console.WriteLine("Usage : TJKaraoke.exe [File Name] [Country Code] [MIDI Device ID = 0 (Default)] [MIDI Device ID = 1 (Default)]");
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
            Console.OutputEncoding = karaoke.Encoding;
            syncQue = new Queue<TickEvent>(karaoke.Lyrics.tickEvents);
            foreach (var item in syncQue)
            {
                Console.WriteLine("{0} {1} {2} {3} {4} {5}", item.tick, item.cmd, item.lineNumber, item.indexOfLine, item.str, item.PronGuide != null ? item.PronGuide.Pron : "");
            }
            var midiFile = MidiFile.Read(new MemoryStream(karaoke.MidiData));
            var midiFileA = new MidiFile();
            var midiFileB = new MidiFile();
            IEnumerable<TrackChunk> tracks = midiFile.GetTrackChunks();
            foreach (var item in tracks)
            {
                foreach (var events in item.Events)
                {

                    if (events is SequenceTrackNameEvent)
                    {
                        Console.WriteLine("{0} : {1}", item.ChunkId, ((SequenceTrackNameEvent)events).Text);
                        
                    }
                }
            }
            foreach (var item in tracks)
            {
                
                foreach (var events in item.Events)
                {
                    if (events is PortPrefixEvent)
                    {
                        //Console.WriteLine("{0} : {1}", item.ChunkId, ((SequenceTrackNameEvent)events).Text);
                        if (((PortPrefixEvent)events).Port == 0)
                            midiFileA.Chunks.Add(item.Clone());
                        else if (((PortPrefixEvent)events).Port == 1)
                            midiFileB.Chunks.Add(item.Clone());
                        break;
                    }
                }
            }

            midiFileA.ReplaceTempoMap(midiFile.GetTempoMap());
            midiFileB.ReplaceTempoMap(midiFile.GetTempoMap());
            

            Console.WriteLine(karaoke.Lyrics.kNumber);
            Console.WriteLine(karaoke.Lyrics.title);
            Console.WriteLine(karaoke.Lyrics.subTitle);
            Console.WriteLine(karaoke.Lyrics.lyricist);
            Console.WriteLine(karaoke.Lyrics.composer);
            Console.WriteLine(karaoke.Lyrics.singer);

            var playbackA = midiFileA.GetPlayback(OutputDevice.GetById(args.Length >= 3 ? int.Parse(args[2]) : 0));
            var playbackB = midiFileB.GetPlayback(OutputDevice.GetById(args.Length >= 4 ? int.Parse(args[3]) : 0));

            playbackA.Finished += Playback_Finished;
            Thread thread = new Thread(() => { Playback(playbackA); });

            playbackA.Start();
            playbackB.Start();
            thread.Start();
            Console.ReadLine();
            Environment.Exit(0);
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