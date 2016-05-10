using CSCore.Codecs.WAV;
using CSCore.SoundIn;
using Microsoft.Win32;
using System;
using System.IO;
using System.Threading;
using System.Windows;

namespace SoundCatcherWPF
{
    class Control
    {
        MainWindow mainWindow; // bidirectional association


        // stores whether the application is recording or ready to record
        ApplicationState state = ApplicationState.Ready;

        // recording objects
        WasapiCapture capture = null;
        WaveWriter waveWriter = null;

        // auto cut means that a new output sound file is being created when a specified number of recorded bytes has been equal to 0
        bool enableAutoCut = false; 

        bool saveAsMp3 = false; // convert recroded Wave file to MP3

        static string tempStorage = "tmp.wav"; // temporary file name

        ulong bytesRecorded = 0; // bytes recorded counter

        string outputPath = null; // only necessary for auto cut
        bool justCut = false; // prevents from cutting multiple times within one "sound-break"
        readonly int numberOfEmptyBytesToDoACut = 2048;
        int cutIndex = 0; // number of cuts (-1)

        string uuid;

        public Control(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow; // bidirectional association   
            uuid = DateTime.Now.Ticks.ToString();
        }

        public ApplicationState ToggleApplicationState()
        {
            // stops recording if a record is going on
            // starts recording if no record is going on

            switch (state)
            {
                case ApplicationState.Ready:
                    startRecording();
                    break;
                case ApplicationState.Recording:
                    stopRecording();
                    break;
            }

            return state; // return the new application state
        }

        private void startRecording()
        {
            state = ApplicationState.Recording;

            bytesRecorded = 0; // reset byte counter
            cutIndex = 0; // reset cut index

            try
            {
                // initialize audio capturing through wasapi
                capture = new WasapiLoopbackCapture();
                capture.Initialize();

                // create a wavewriter to write the data to a file
                waveWriter = new WaveWriter(tempStorage, capture.WaveFormat);

                justCut = true; // first record is like "just cut"

                // setup an eventhandler to receive the recorded data
                capture.DataAvailable += (s, e) =>
                {
                    // required to make sure that the thread doesn't prevent the window from being closed
                    Thread.CurrentThread.IsBackground = true;

                    #region only zeros in the new data
                    bool onlyZeros = true;
                    for (int i = 6; // first 6 bytes are always != 0
                        i < e.Data.Length && i < numberOfEmptyBytesToDoACut; 
                        i++)
                    {
                        if (e.Data[i] != 0)
                        {
                            onlyZeros = false;
                            break;
                        }
                    }
                    #endregion

                    if (onlyZeros && enableAutoCut)
                    {
                        if (!justCut)
                        {
                            // end wave writer
                            waveWriter.Dispose();

                            // move the temporarily created file 
                            File.Move(tempStorage, outputPath + "\\record_" + uuid + "_" + cutIndex.ToString() + ".mp3");

                            cutIndex++; // increment cut indext o make sure that every output file name is unique

                            waveWriter = new WaveWriter(tempStorage, capture.WaveFormat); // restart the wave writer

                            justCut = true;
                        }
                    }
                    else
                    {
                        justCut = false;

                        // save the recorded audio
                        waveWriter.Write(e.Data, e.Offset, e.ByteCount);

                        bytesRecorded += (uint)e.ByteCount; // increase bytes recorded


                        mainWindow.RefreshByteLabel(bytesRecorded); // update bytes label

                    }
                };

                //start recording
                capture.Start();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);

                waveWriter.Dispose();
                capture.Dispose();

                state = ApplicationState.Ready;
            }
        }

        private void stopRecording(bool stopImmediately = false)
        {
            state = ApplicationState.Ready;

            if (capture == null || waveWriter == null)
                return;

            try
            {
                //stop recording
                capture.Stop();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
            finally
            {
                waveWriter.Dispose();
                capture.Dispose();
            }

            if (!stopImmediately && !enableAutoCut)
            {
                // save file dialog
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = saveAsMp3 ? "MP3 Sound|*.mp3" : "Wave Sound|*.wav";
                saveFileDialog.Title = "Save Sound File";
                saveFileDialog.ShowDialog();

                // check if the user passed a path
                if (saveFileDialog.FileName != "")
                {
                    try
                    {
                        // move the temporarily created file to the requested path 
                        File.Move(tempStorage, saveFileDialog.FileName);
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message);
                    }
                }
            }
        }

        public void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (state == ApplicationState.Recording)
            {
                if (MessageBox.Show("Data might be lost. Do you want to continue?", "Recording runs", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.No)
                {
                    e.Cancel = true;
                }
                else
                {
                    stopRecording(true);
                }
            }
        }

        public bool SetCutEnable(bool newState)
        {
            if (newState)
            {
                // ask user for a path to store the single cuts
                System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
                System.Windows.Forms.DialogResult result = fbd.ShowDialog();
                if (fbd.SelectedPath != "")
                {
                    outputPath = fbd.SelectedPath;
                    enableAutoCut = true;
                }
            }
            else
            {
                enableAutoCut = false;
            }

            return enableAutoCut;
        }
    }

    enum ApplicationState
    {
        Ready,
        Recording
    }
}
