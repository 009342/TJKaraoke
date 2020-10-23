using Melanchall.DryWetMidi.Devices;
using lzo.net;
using System.IO;
using System;
using System.Windows;

namespace TJOpenKaraoke
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void mainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (OutputDevice item in OutputDevice.GetAll())
            {
                MIDI_PortA.Items.Add(item.Name);
                MIDI_PortB.Items.Add(item.Name);
            }
            if (MIDI_PortA.Items.Count > 0) MIDI_PortA.SelectedIndex = 0;
            if (MIDI_PortB.Items.Count > 1) MIDI_PortB.SelectedIndex = 1;
            for (int i = 0; i < 11; i++) RegionSelect.Items.Add(i);
            RegionSelect.SelectedIndex = 0;
            foreach (string var in Directory.GetFiles(Directory.GetCurrentDirectory()))
            {
                files.Items.Add(System.IO.Path.GetFileName(var));
            }
        }
        KaraokeWindow karaokeWindow;
        private void run_Click(object sender, RoutedEventArgs e)
        {
            byte[] file = File.ReadAllBytes((string)files.SelectedItem);
            if (file[0] == 0x00)
            {
                try
                {
                    using (var compressed = File.OpenRead((string)files.SelectedItem))
                    {
                        using (var decompressed = new LzoStream(compressed, System.IO.Compression.CompressionMode.Decompress))
                        {
                            using (MemoryStream ms = new MemoryStream())
                            {
                                decompressed.CopyTo(ms);
                                file = ms.ToArray();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
            //File.WriteAllBytes("temp", file);
            if (karaokeWindow != null && karaokeWindow.IsEnabled == true) karaokeWindow.Close();
            karaokeWindow = new KaraokeWindow(file, RegionSelect.SelectedIndex, MIDI_PortA.SelectedIndex, MIDI_PortB.SelectedIndex);
            karaokeWindow.Show();
        }
    }
}
