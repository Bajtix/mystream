using CSCore;
using CSCore.Codecs;
using CSCore.MediaFoundation;


var supportedFormats = MediaFoundationEncoder.GetEncoderMediaTypes(AudioSubTypes.MpegLayer3);
if (!supportedFormats.Any())
{
    Console.WriteLine("The current platform does not support mp3 encoding.");
    return;
}

IWaveSource source;
try
{
    source = CodecFactory.Instance.GetCodec("music/test.flac");

    if (
        supportedFormats.All(
            x => x.SampleRate != source.WaveFormat.SampleRate && x.Channels == source.WaveFormat.Channels))
    {
        //the encoder does not support the input sample rate -> convert it to any supported samplerate
        //choose the best sample rate with stereo (in order to make simple, we always use stereo in this sample)
        int sampleRate =
            supportedFormats.OrderBy(x => Math.Abs(source.WaveFormat.SampleRate - x.SampleRate))
                .First(x => x.Channels == source.WaveFormat.Channels)
                .SampleRate;

        Console.WriteLine("Samplerate {0} -> {1}", source.WaveFormat.SampleRate, sampleRate);
        Console.WriteLine("Channels {0} -> {1}", source.WaveFormat.Channels, 2);
        source = source.ChangeSampleRate(sampleRate);
    }
}
catch (Exception)
{
    Console.WriteLine("Format not supported.");
    return;
}


using (source)
{
    using var monos = source.ToMono();

    byte[] buffer = new byte[monos.WaveFormat.BytesPerSample];
    float[] samples = new float[(int)(monos.WaveFormat.SampleRate * source.GetLength().TotalSeconds)];
    int read;
    int i = 0;
    Console.WriteLine("Bytes per sample=" + buffer.Length);
    while (i < samples.Length && (read = monos.Read(buffer, 0, buffer.Length)) > 0)
    {
        double s = BitConverter.ToInt16(buffer) / (Math.Pow(2, 8 * buffer.Length));
        samples[i] = (float)s;

        i++;
        // Console.CursorLeft = 0;
        // Console.Write("{0:P}/{1:P}", (double)source.Position / source.Length, 1);
    }
    Console.WriteLine("convert done. array l = " + samples.Length);
    byte[] data = VorbisEncoder.GenerateFile([samples], source.WaveFormat.SampleRate, 1);
    File.WriteAllBytes("decoded.ogg", data);
}

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
