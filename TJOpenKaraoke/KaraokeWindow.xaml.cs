using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Devices;
using Melanchall.DryWetMidi.Interaction;
using lzo.net;
using System.IO;
using TJKaraoke;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Threading;

namespace TJOpenKaraoke
{
    /// <summary>
    /// KaraokeWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class KaraokeWindow : Window
    {
        Queue<TickEvent> syncQue = new Queue<TickEvent>();
        MidiFile midiFileA = new MidiFile();
        MidiFile midiFileB = new MidiFile();
        Playback playbackA;
        Playback playbackB;
        OutputDevice outputDeviceA;
        OutputDevice outputDeviceB;
        TJN karaoke = null;
        Thread thread;
        LinearGradientBrush fill = new LinearGradientBrush();

        public KaraokeWindow(byte[] file, int country, int MidiOutputA, int MidiOutputB)
        {
            InitializeComponent();
            //https://social.msdn.microsoft.com/Forums/vstudio/en-US/2062f6ab-b4b5-487c-890a-9c3ffcdc1d2e/karaoke-text-effect-in-wpf?forum=wpf
            fill.StartPoint = new Point(0, 0.5);
            fill.EndPoint = new Point(1, 0.5);
            GradientStop gs1 = new GradientStop();
            GradientStop gs2 = new GradientStop();
            GradientStop gs3 = new GradientStop();
            GradientStop gs4 = new GradientStop();

            gs1.Offset = 0.0;
            gs2.Offset = 0.0;
            gs3.Offset = 0.0;
            gs4.Offset = 1.0;

            gs1.Color = Colors.Blue;
            gs2.Color = Colors.Blue;
            gs3.Color = Colors.Black;
            gs4.Color = Colors.Black;


            fill.GradientStops.Add(gs1);
            fill.GradientStops.Add(gs2);
            fill.GradientStops.Add(gs3);
            fill.GradientStops.Add(gs4);

            karaoke = new TJN(file, country);
            syncQue = new Queue<TickEvent>(karaoke.Lyrics.tickEvents);
            var midiFile = MidiFile.Read(new MemoryStream(karaoke.MidiData));
            IEnumerable<TrackChunk> tracks = midiFile.GetTrackChunks();
            foreach (var item in tracks)
            {
                foreach (var events in item.Events)
                {
                    if (events is PortPrefixEvent)
                    {
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
            outputDeviceA = OutputDevice.GetById(MidiOutputA);
            outputDeviceB = OutputDevice.GetById(MidiOutputB);
            playbackA = midiFileA.GetPlayback(outputDeviceA);
            playbackB = midiFileB.GetPlayback(outputDeviceB);
            playbackA.Start();
            playbackB.Start();
            thread = new Thread(() => { QueThread(playbackA); });
            thread.Start();
        }
        private void KWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            playbackA.Stop();
            playbackB.Stop();
            outputDeviceA.SendEvent(new StopEvent());
            outputDeviceB.SendEvent(new StopEvent());

            outputDeviceA.SendEvent(new NormalSysExEvent(new byte[] { 0x7E, 0x7F, 0x09, 0x00, 0xF7 }));
            outputDeviceB.SendEvent(new NormalSysExEvent(new byte[] { 0x7E, 0x7F, 0x09, 0x00, 0xF7 })); //GM Reset
            outputDeviceA.SendEvent(new NormalSysExEvent(new byte[] { 0x41, 0x10, 0x42, 0x12, 0x40, 0x00, 0x7F, 0x00, 0x41, 0xF7 }));
            outputDeviceB.SendEvent(new NormalSysExEvent(new byte[] { 0x41, 0x10, 0x42, 0x12, 0x40, 0x00, 0x7F, 0x00, 0x41, 0xF7 })); //GS Reset
            //https://www.recordingblogs.com/wiki/general-midi-system-enable-disable-message
            //http://odasan.s48.xrea.com/dtm/labo/gsreset.html
            //http://forums.rolandclan.com/viewtopic.php?f=27&t=30753

            outputDeviceA.Dispose();
            outputDeviceB.Dispose();
            playbackA.Dispose();
            playbackB.Dispose();
            thread.Abort();

        }
        private int line = 0;
        private int indexofline = 0;
        private LyricsTextBlock component = null;
        private void QueThread(Playback sender)
        {
            //KWindow.Dispatcher.Invoke(new Action(()=> { }));
            while (syncQue.Count != 0)
            {
                int time = (int)TimeConverter.ConvertFrom(sender.GetCurrentTime<ITimeSpan>(), sender.TempoMap);
                TickEvent peek = syncQue.Peek();
                if (peek.tick <= time)
                {
                    TickEvent tick = syncQue.Dequeue();
                    if (tick.cmd == 0x09) //가사 표출 시작
                    {
                        ShowNextLine();
                        ShowNextLine();
                    }
                    if (tick.cmd == 0x01)
                    {
                        if (line >= 2 && line < tick.lineNumber + 2)
                        {
                            KWindow.Dispatcher.Invoke(new Action(() =>
                            {
                                if (lastdisplayedline != -1)
                                {
                                    if (lastdisplayedline == 0)
                                    {
                                        dockpanel2.Children.Clear();//Dispose를 해줄 필요가 없다고??
                                    }
                                    if (lastdisplayedline == 1)
                                    {
                                        dockpanel.Children.Clear();//Dispose를 해줄 필요가 없다고??
                                    }
                                }
                            }));
                            ShowNextLine();
                        }
                    }

                }
                KWindow.Dispatcher.Invoke(new Action(() =>
                {
                    LyricsTextBlock temp = null;
                    foreach (var item in dockpanel.Children)
                    {
                        if (time >= ((LyricsTextBlock)item).starttick && time <= ((LyricsTextBlock)item).endtick)
                        {
                            temp = (LyricsTextBlock)item;
                        }
                    }
                    foreach (var item in dockpanel2.Children)
                    {
                        if (time >= ((LyricsTextBlock)item).starttick && time <= ((LyricsTextBlock)item).endtick)
                        {
                            temp = (LyricsTextBlock)item;
                        }
                    }
                    if (temp != null)
                    {

                        if (component != null && component != temp)
                        {
                            component.Foreground = Brushes.Blue;
                        }
                        component = temp;
                        fill.GradientStops[1].Offset = ((double)(time - component.starttick)) / ((double)(component.endtick - component.starttick));
                        fill.GradientStops[2].Offset = ((double)(time - component.starttick)) / ((double)(component.endtick - component.starttick));
                        component.Foreground = fill;

                    }
                }));
                Thread.Sleep(1);
            }
        }

        private int lastdisplayedline = -1;
        private void ShowNextLine()
        {
            List<TickEvent> lyricsList = new List<TickEvent>(karaoke.Lyrics.tickEvents.FindAll(q => (q.cmd == 0x01 || q.cmd == 0x02) && q.lineNumber == line));
            KWindow.Dispatcher.Invoke(new Action(() =>
            {
                foreach (var item in lyricsList)
                {
                    if (item.cmd == 0x01)
                    {
                        int i = item.str.IndexOf(" ");
                        LyricsTextBlock lyricsTextBlock;
                        if (i == 0)
                        {

                            lyricsTextBlock = new LyricsTextBlock()
                            {
                                Text = " ",
                                line = item.lineNumber,
                                indexofline = item.indexOfLine,
                                FontSize = 100,
                                FontWeight = FontWeights.Bold,
                                IsEnabled = true,
                                starttick = 0,
                                endtick = 0
                            };
                            if (lastdisplayedline == 0)
                                dockpanel2.Children.Add(lyricsTextBlock);
                            else
                                dockpanel.Children.Add(lyricsTextBlock);

                        }

                        lyricsTextBlock = new LyricsTextBlock()
                        {
                            Text = item.str.Replace(" ", ""),
                            line = item.lineNumber,
                            indexofline = item.indexOfLine,
                            FontSize = 100,
                            FontWeight = FontWeights.Bold,
                            IsEnabled = true,
                            starttick = item.tick,
                            endtick = lyricsList[(lyricsList.IndexOf(item) + 1)].tick
                        };
                        if (lastdisplayedline == 0)
                            dockpanel2.Children.Add(lyricsTextBlock);
                        else
                            dockpanel.Children.Add(lyricsTextBlock);


                        if (i > 0)
                        {

                            lyricsTextBlock = new LyricsTextBlock()
                            {
                                Text = " ",
                                line = item.lineNumber,
                                indexofline = item.indexOfLine,
                                FontSize = 100,
                                FontWeight = FontWeights.Bold,
                                IsEnabled = true,
                                starttick = 0,
                                endtick = 0
                            };
                            if (lastdisplayedline == 0)
                                dockpanel2.Children.Add(lyricsTextBlock);
                            else
                                dockpanel.Children.Add(lyricsTextBlock);
                        }
                    }


                }
                UpdateLayout();
            }));
            lastdisplayedline = (lastdisplayedline + 1) % 2;
            line++;
        }
    }
    class LyricsTextBlock : TextBlock
    {
        public int line;
        public int indexofline;
        public int starttick;
        public int endtick;
    }
}
