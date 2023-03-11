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
        [TestCase("Alesis-Fusion-Pizzicato-Strings-C4.wav", 1024 * 128)]
        [RequiresPlayMode(true)]
        public async Task DecodeAllTest(string fileName, int blockFrameCount)
        {
            var filePath = Path.Combine(
                Application.dataPath,
                "Mochineko/SimpleAudioCodec.Tests",
                fileName);

            await using var stream = File.OpenRead(filePath);

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var audioClip = await WaveDecoder.DecodeAllAsync(
                stream, fileName, CancellationToken.None, blockFrameCount);
            stopWatch.Stop();
            
            Debug.Log($"Decode time is {stopWatch.ElapsedMilliseconds}ms for {fileName}");

            audioClip.Should().NotBeNull();
        }
    }
}
