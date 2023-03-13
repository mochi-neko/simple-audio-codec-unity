using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NAudio.Wave;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mochineko.SimpleAudioCodec.Tests
{
    [TestFixture]
    internal sealed class WaveConsistencyTest
    {
        [TestCase("synthesis_2023_03_11_05_42_51.wav", 1024 * 32)]
        [TestCase("Alesis-Fusion-Pizzicato-Strings-C4.wav", 1024 * 32)]
        [RequiresPlayMode(true)]
        public async Task DecodeAndEncodeTest(string fileName, int framesCountInBlock)
        {
            var filePath = Path.Combine(
                Application.dataPath,
                "Mochineko/SimpleAudioCodec.Tests",
                fileName);

            await using var fileStream = File.OpenRead(filePath);

            await using var originalReader = new WaveFileReader(fileStream);
            var originalFormat = originalReader.WaveFormat;
            
            fileStream.Seek(0, SeekOrigin.Begin);

            var audioClip = await WaveDecoder.DecodeByBlockAsync(
                fileStream, fileName, CancellationToken.None, framesCountInBlock);

            await using var encoded = new MemoryStream();

            await WaveEncoder.EncodeByBlockAsync(
                outStream: encoded,
                audioClip: audioClip,
                cancellationToken: CancellationToken.None,
                bitsPerSample: 16,
                framesCountInBlock: framesCountInBlock);

            encoded.Seek(0, SeekOrigin.Begin);

            await using var encodedReader = new WaveFileReader(encoded);
            var encodedFormat = encodedReader.WaveFormat;

            encodedReader.Length.Should().Be(originalReader.Length);
            encodedReader.SampleCount.Should().Be(originalReader.SampleCount);
            encodedFormat.BitsPerSample.Should().Be(originalFormat.BitsPerSample);
            encodedFormat.Channels.Should().Be(originalFormat.Channels);
            encodedFormat.SampleRate.Should().Be(originalFormat.SampleRate);

            UnityEngine.Object.Destroy(audioClip);
        }
    }
}