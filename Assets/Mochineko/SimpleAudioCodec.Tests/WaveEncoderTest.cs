using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NAudio.Wave;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

namespace Mochineko.SimpleAudioCodec.Tests
{
    [TestFixture]
    internal sealed class WaveEncoderTest
    {
        [TestCase("synthesis_2023_03_11_05_42_51.wav", 1024 * 32)]
        [TestCase("Alesis-Fusion-Pizzicato-Strings-C4.wav", 1024 * 32)]
        [TestCase("Alesis-Fusion-Pizzicato-Strings-C4.wav", 1024)]
        [RequiresPlayMode(true)]
        public async Task EncodeTest(string fileName, int framesCountInBlock)
        {
            var assetPath = Path.Combine(
                "Assets/Mochineko/SimpleAudioCodec.Tests",
                fileName);

            var audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            
            await using var encoded = new MemoryStream();

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            
            await WaveEncoder.EncodeByBlockAsync(
                outStream: encoded,
                audioClip: audioClip,
                cancellationToken: CancellationToken.None,
                bitsPerSample: 16,
                framesCountInBlock: framesCountInBlock);
            
            stopWatch.Stop();

            Debug.Log($"Encoding time:{stopWatch.ElapsedMilliseconds}ms with frames count in block:{framesCountInBlock} for {fileName}");

            encoded.Length.Should().NotBe(0);

            encoded.Seek(0, SeekOrigin.Begin);

            await using var reader = new WaveFileReader(encoded);
            var format = reader.WaveFormat;

            reader.SampleCount.Should().Be(audioClip.samples);
            format.SampleRate.Should().Be(audioClip.frequency);
            format.Channels.Should().Be(audioClip.channels);
        }
    }
}