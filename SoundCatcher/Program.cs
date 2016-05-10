using CSCore.Codecs.WAV;
using CSCore.SoundIn;
using System;

namespace SoundCatcher
{
  class Program
  {
    static void Main(string[] args)
    {
      using(WasapiCapture capture = new WasapiLoopbackCapture()) {
        // initialize audio capture through wasapi
        capture.Initialize();

        // create a wavewriter to write the data to
        using(WaveWriter waveWriter = new WaveWriter("output.wav", capture.WaveFormat)) {
          // setup an eventhandler to receive the recorded data
          capture.DataAvailable += (s, e) =>
          {
            //save the recorded audio
            waveWriter.Write(e.Data, e.Offset, e.ByteCount);
          };

          // start recording
          capture.Start();

          // record until the user has pressed a key
          Console.ReadKey();

          // stop recording
          capture.Stop();
        }
      }
    }
  }
}
