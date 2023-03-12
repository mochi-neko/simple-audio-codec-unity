using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

namespace Mochineko.SimpleAudioCodec.Tests
{
    [TestFixture]
    internal sealed class WaveDecoderTest
    {
        [TestCase("synthesis_2023_03_11_05_42_51.wav", 1024 * 512)]
        [TestCase("synthesis_2023_03_11_05_42_51.wav", 1024 * 256)]
        [TestCase("synthesis_2023_03_11_05_42_51.wav", 1024 * 128)]
        [TestCase("synthesis_2023_03_11_05_42_51.wav", 1024 * 64)]
        [TestCase("synthesis_2023_03_11_05_42_51.wav", 1024 * 32)]
        [TestCase("synthesis_2023_03_11_05_42_51.wav", 1024 * 16)]
        [TestCase("synthesis_2023_03_11_05_42_51.wav", 1024 * 8)]
        [TestCase("synthesis_2023_03_11_05_42_51.wav", 1024 * 4)]
        [TestCase("synthesis_2023_03_11_05_42_51.wav", 1024 * 2)]
        [TestCase("synthesis_2023_03_11_05_42_51.wav", 1024 * 1)]
        [TestCase("Alesis-Fusion-Pizzicato-Strings-C4.wav", 1024 * 512)]
        [TestCase("Alesis-Fusion-Pizzicato-Strings-C4.wav", 1024 * 256)]
        [TestCase("Alesis-Fusion-Pizzicato-Strings-C4.wav", 1024 * 128)]
        [TestCase("Alesis-Fusion-Pizzicato-Strings-C4.wav", 1024 * 64)]
        [TestCase("Alesis-Fusion-Pizzicato-Strings-C4.wav", 1024 * 32)]
        [TestCase("Alesis-Fusion-Pizzicato-Strings-C4.wav", 1024 * 16)]
        [TestCase("Alesis-Fusion-Pizzicato-Strings-C4.wav", 1024 * 8)]
        [TestCase("Alesis-Fusion-Pizzicato-Strings-C4.wav", 1024 * 4)]
        [TestCase("Alesis-Fusion-Pizzicato-Strings-C4.wav", 1024 * 2)]
        [TestCase("Alesis-Fusion-Pizzicato-Strings-C4.wav", 1024 * 1)]
        [RequiresPlayMode(true)]
        public async Task DecodeAllTest(string fileName, int framesCountInBlock)
        {
            var filePath = Path.Combine(
                Application.dataPath,
                "Mochineko/SimpleAudioCodec.Tests",
                fileName);

            await using var stream = File.OpenRead(filePath);

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var audioClip = await WaveDecoder.DecodeBlockByBlockAsync(
                stream, fileName, CancellationToken.None, framesCountInBlock);

            stopWatch.Stop();

            Debug.Log($"Decoding time:{stopWatch.ElapsedMilliseconds}ms with frames count in block:{framesCountInBlock} for {fileName}");

            audioClip.Should().NotBeNull();
            
            UnityEngine.Object.Destroy(audioClip);
        }
    }
}