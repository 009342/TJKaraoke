using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Common;
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
            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }
        bool started = false;
        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            //KWindow.Dispatcher.Invoke(new Action(()=> { }));
            if (syncQue.Count != 0)
            {
                int time = (int)TimeConverter.ConvertFrom(playbackA.GetCurrentTime<ITimeSpan>(), playbackA.TempoMap);
                TickEvent peek = syncQue.Peek();
                if (peek.tick <= time)
                {
                    TickEvent tick = syncQue.Dequeue();
                    if (!started && tick.cmd == 0x09) //가사 표출 시작
                    {
                        ShowLine(0);
                        ShowLine(1);
                        started = true;
                    }
                    if (tick.cmd == 0x0a)
                    {
                        ShowLine(syncQue.Peek().lineNumber);
                        ShowLine(syncQue.Peek().lineNumber + 1);
                    }
                    if (tick.cmd == 0x01 && tick.lineNumber > 0)
                    {
                        if (tick.indexOfLine == 0)
                        {
                            ShowLine(tick.lineNumber + 1);
                        }
                    }

                }
                if (component == null || !(component.starttick <= time && time <= component.endtick))
                {
                    if (component != null)
                    {
                        component.Foreground = Brushes.Blue;
                    }
                    component = null;
                    foreach (var stackitem in dockpanel.Children)
                    {
                        foreach (var item in ((DockPanel)((Grid)stackitem).Children[1]).Children)
                        {
                            if (time >= ((LyricsTextBlock)item).starttick && time <= ((LyricsTextBlock)item).endtick)
                            {
                                component = (LyricsTextBlock)item;
                            }
                        }
                    }
                    foreach (var stackitem in dockpanel2.Children)
                    {
                        foreach (var item in ((DockPanel)((Grid)stackitem).Children[1]).Children)
                        {
                            if (time >= ((LyricsTextBlock)item).starttick && time <= ((LyricsTextBlock)item).endtick)
                            {
                                component = (LyricsTextBlock)item;
                            }
                        }
                    }
                }
                if (component != null)
                {
                    KWindow.Title = string.Format("{4} {0} {1}-{2} {3}", time, component.starttick, component.endtick, (time - component.starttick) / ((double)(component.endtick - component.starttick)), component.Text);
                    fill.GradientStops[1].Offset = (time - component.starttick) / ((double)(component.endtick - component.starttick));
                    fill.GradientStops[2].Offset = (time - component.starttick) / ((double)(component.endtick - component.starttick));

                    component.Foreground = fill;
                }

            }
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
            GC.Collect();
        }
        private LyricsTextBlock component = null;
        private void ShowLine(int line)
        {
            List<TickEvent> lyricsList = new List<TickEvent>(karaoke.Lyrics.tickEvents.FindAll(q => (q.cmd == 0x01 || q.cmd == 0x02) && q.lineNumber == line));

            KWindow.Dispatcher.Invoke(new Action(() =>
            {
                if ((line + 1) % 2 == 0)
                {
                    dockpanel2.Children.Clear();//Dispose를 해줄 필요가 없다고??
                }
                if ((line + 1) % 2 == 1)
                {
                    dockpanel.Children.Clear();//Dispose를 해줄 필요가 없다고??
                }
                var temp = lyricsList.FindAll(q => q.cmd == 0x01);
                for (int j = 0; j < temp.Count;)
                {
                    var item = temp[j];
                    if (j == temp.Count) item.str += " ";
                    Grid grid = new Grid();
                    grid.ShowGridLines = true;
                    grid.RowDefinitions.Add(new RowDefinition());
                    grid.RowDefinitions.Add(new RowDefinition());
                    grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                    grid.ColumnDefinitions.Add(new ColumnDefinition());



                    DockPanel yomiPanel = new DockPanel() { Height = 40, HorizontalAlignment = HorizontalAlignment.Center };
                    DockPanel mojiPanel = new DockPanel();


                    Grid.SetRow(yomiPanel, 0);
                    Grid.SetRow(mojiPanel, 1);
                    Grid.SetColumn(yomiPanel, 0);
                    Grid.SetColumn(mojiPanel, 0);
                    grid.Children.Add(yomiPanel);
                    grid.Children.Add(mojiPanel);

                    int t = 1;
                    if (item.PronGuide != null)
                    {
                        TextBlock yomigana = new TextBlock() { Text = item.PronGuide.Pron, FontSize = 30 };
                        t = item.PronGuide.Ref.Count;
                        yomiPanel.Children.Add(yomigana);
                    }
                    while (t-- > 0)
                    {
                        int i = item.str.IndexOf(" ");
                        LyricsTextBlock lyricsTextBlock;
                        if (i == 0)
                        {
                            lyricsTextBlock = new LyricsTextBlock()
                            {
                                FontFamily = new FontFamily("MS Mincho"),
                                Text = " ",
                                line = item.lineNumber,
                                indexofline = item.indexOfLine,
                                FontSize = 100,
                                FontWeight = FontWeights.Bold,
                                IsEnabled = true,
                                starttick = 0,
                                endtick = 0
                            };
                            mojiPanel.Children.Add(lyricsTextBlock);

                        }
                        lyricsTextBlock = new LyricsTextBlock()
                        {
                            FontFamily = new FontFamily("MS Mincho"),
                            Text = item.str.Replace(" ", ""),
                            line = item.lineNumber,
                            indexofline = item.indexOfLine,
                            FontSize = 100,
                            FontWeight = FontWeights.Bold,
                            IsEnabled = true,
                            starttick = item.tick,
                            endtick = lyricsList[(lyricsList.IndexOf(item) + 1)].tick
                        };
                        mojiPanel.Children.Add(lyricsTextBlock);
                        if (i > 0)
                        {

                            lyricsTextBlock = new LyricsTextBlock()
                            {
                                FontFamily = new FontFamily("MS Mincho"),
                                Text = " ",
                                line = item.lineNumber,
                                indexofline = item.indexOfLine,
                                FontSize = 100,
                                FontWeight = FontWeights.Bold,
                                IsEnabled = true,
                                starttick = 0,
                                endtick = 0
                            };
                            mojiPanel.Children.Add(lyricsTextBlock);
                        }
                        if (t > 0)
                        {
                            j++;
                            item = temp[j];
                        }

                    }
                    if (item.lineNumber % 2 == 1)
                        dockpanel2.Children.Add(grid);
                    else
                        dockpanel.Children.Add(grid);

                    j++;
                }
                UpdateLayout();
            }));
        }

        private void KWindow_KeyDown(object sender, KeyEventArgs e)
        {
            int time = (int)TimeConverter.ConvertFrom(playbackA.GetCurrentTime<ITimeSpan>(), playbackA.TempoMap);
            if (e.Key == Key.Right)
            {
                TickEvent tc = karaoke.Lyrics.tickEvents.Find(q => (q.cmd == 0x01) && q.tick >= time);
                tc = karaoke.Lyrics.tickEvents.Find(q => (q.cmd == 0x01) && q.lineNumber == tc.lineNumber + 1);
                if (tc != null)
                {
                    playbackA.Stop();
                    playbackB.Stop();
                    for (int i = 0; i < 16; i++)
                    {
                        outputDeviceA.SendEvent(new ControlChangeEvent(ControlName.AllNotesOff.AsSevenBitNumber(), (SevenBitNumber)0) { Channel = (FourBitNumber)i });
                        outputDeviceB.SendEvent(new ControlChangeEvent(ControlName.AllNotesOff.AsSevenBitNumber(), (SevenBitNumber)0) { Channel = (FourBitNumber)i });
                        outputDeviceA.SendEvent(new ControlChangeEvent(ControlName.AllSoundOff.AsSevenBitNumber(), (SevenBitNumber)0) { Channel = (FourBitNumber)i });
                        outputDeviceB.SendEvent(new ControlChangeEvent(ControlName.AllSoundOff.AsSevenBitNumber(), (SevenBitNumber)0) { Channel = (FourBitNumber)i });
                    }
                    syncQue = new Queue<TickEvent>(karaoke.Lyrics.tickEvents.FindAll(q => q.lineNumber >= tc.lineNumber));
                    ShowLine(tc.lineNumber);
                    ShowLine(tc.lineNumber + 1);
                    playbackA.MoveToTime(TimeConverter.ConvertTo(tc.tick, TimeSpanType.Midi, playbackA.TempoMap));
                    playbackB.MoveToTime(TimeConverter.ConvertTo(tc.tick, TimeSpanType.Midi, playbackB.TempoMap));
                    playbackA.Start();
                    playbackB.Start();
                }
            }
            if (e.Key == Key.Left)
            {
                TickEvent tc = karaoke.Lyrics.tickEvents.Find(q => (q.cmd == 0x01) && q.tick >= time);
                tc = karaoke.Lyrics.tickEvents.Find(q => (q.cmd == 0x01) && q.lineNumber == tc.lineNumber - 1);
                if (tc != null)
                {
                    playbackA.Stop();
                    playbackB.Stop();
                    for (int i = 0; i < 16; i++)
                    {
                        outputDeviceA.SendEvent(new ControlChangeEvent(ControlName.AllNotesOff.AsSevenBitNumber(), (SevenBitNumber)0) { Channel = (FourBitNumber)i });
                        outputDeviceB.SendEvent(new ControlChangeEvent(ControlName.AllNotesOff.AsSevenBitNumber(), (SevenBitNumber)0) { Channel = (FourBitNumber)i });
                        outputDeviceA.SendEvent(new ControlChangeEvent(ControlName.AllSoundOff.AsSevenBitNumber(), (SevenBitNumber)0) { Channel = (FourBitNumber)i });
                        outputDeviceB.SendEvent(new ControlChangeEvent(ControlName.AllSoundOff.AsSevenBitNumber(), (SevenBitNumber)0) { Channel = (FourBitNumber)i });
                    }
                    syncQue = new Queue<TickEvent>(karaoke.Lyrics.tickEvents.FindAll(q => q.lineNumber >= tc.lineNumber));
                    ShowLine(tc.lineNumber);
                    ShowLine(tc.lineNumber + 1);
                    playbackA.MoveToTime(TimeConverter.ConvertTo(tc.tick, TimeSpanType.Midi, playbackA.TempoMap));
                    playbackB.MoveToTime(TimeConverter.ConvertTo(tc.tick, TimeSpanType.Midi, playbackB.TempoMap));
                    playbackA.Start();
                    playbackB.Start();
                }
            }
            if (e.Key == Key.Down)
            {
                playbackA.Stop();
                playbackB.Stop();
                for (int i = 0; i < 16; i++)
                {
                    outputDeviceA.SendEvent(new ControlChangeEvent(ControlName.AllNotesOff.AsSevenBitNumber(), (SevenBitNumber)0) { Channel = (FourBitNumber)i });
                    outputDeviceB.SendEvent(new ControlChangeEvent(ControlName.AllNotesOff.AsSevenBitNumber(), (SevenBitNumber)0) { Channel = (FourBitNumber)i });
                    outputDeviceA.SendEvent(new ControlChangeEvent(ControlName.AllSoundOff.AsSevenBitNumber(), (SevenBitNumber)0) { Channel = (FourBitNumber)i });
                    outputDeviceB.SendEvent(new ControlChangeEvent(ControlName.AllSoundOff.AsSevenBitNumber(), (SevenBitNumber)0) { Channel = (FourBitNumber)i });
                }
                playbackA.Speed = 0.5;
                playbackB.Speed = 0.5;
                playbackA.Start();
                playbackB.Start();
            }
            if (e.Key == Key.Space)
            {
                if (playbackA.IsRunning == true)
                {
                    playbackA.Stop();
                    playbackB.Stop();
                    for (int i = 0; i < 16; i++)
                    {
                        outputDeviceA.SendEvent(new ControlChangeEvent(ControlName.AllNotesOff.AsSevenBitNumber(), (SevenBitNumber)0) { Channel = (FourBitNumber)i });
                        outputDeviceB.SendEvent(new ControlChangeEvent(ControlName.AllNotesOff.AsSevenBitNumber(), (SevenBitNumber)0) { Channel = (FourBitNumber)i });
                        outputDeviceA.SendEvent(new ControlChangeEvent(ControlName.AllSoundOff.AsSevenBitNumber(), (SevenBitNumber)0) { Channel = (FourBitNumber)i });
                        outputDeviceB.SendEvent(new ControlChangeEvent(ControlName.AllSoundOff.AsSevenBitNumber(), (SevenBitNumber)0) { Channel = (FourBitNumber)i });
                    }
                }
                else
                {
                    playbackA.Start();
                    playbackB.Start();
                }
            }
        }
    }
    class LyricsTextBlock : TextBlock
    {
        public int line;
        public int indexofline;
        public int starttick;
        public int endtick;
        public int part = 3;
    }
}
