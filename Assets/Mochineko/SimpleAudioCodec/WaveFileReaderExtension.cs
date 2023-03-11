#nullable enable
using System;
using System.IO;
using NAudio.Wave;

namespace Mochineko.SimpleAudioCodec
{
    internal static class WaveFileReaderExtension
    {
        /// <summary>
        /// Extends <see cref="NAudio.Wave.WaveFileReader.ReadNextSampleFrame()"/> to block samples.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="blockFramesCount"></param>
        /// <param name="blockSamples"></param>
        /// <returns>Not end of file</returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="InvalidDataException"></exception>
        public static bool ReadNextBlockFrames(
            this WaveFileReader reader,
            int blockFramesCount,
            out float[] blockSamples)
        {
            var waveFormat = reader.WaveFormat;
            switch (waveFormat.Encoding)
            {
                case WaveFormatEncoding.Pcm:
                case WaveFormatEncoding.IeeeFloat:
                case WaveFormatEncoding.Extensible:
                    // n.b. not necessarily PCM, should probably write more code to handle this case
                    break;
                default:
                    throw new InvalidOperationException("Only 16, 24 or 32 bit PCM or IEEE float audio data supported");
            }

            // 1 frame = 1 float
            var blockSamplesCount = waveFormat.Channels * blockFramesCount;
            blockSamples = new float[blockSamplesCount];

            var bytesCountPerSample = waveFormat.BitsPerSample / sizeof(byte);
            var blockBytesCount = blockSamplesCount * bytesCountPerSample;
            var buffer = new byte[blockBytesCount];

            // May throw
            // Read bytes to buffer
            var readBytesCount = reader.Read(buffer, 0, blockBytesCount);
            // End of file
            if (readBytesCount == 0)
            {
                return false;
            }

            var framesCountToRead = blockFramesCount;
            // Read bytes count is smaller than block
            if (readBytesCount < blockBytesCount)
            {
                // Recalculate frames count
                framesCountToRead
                    = readBytesCount / (waveFormat.Channels * bytesCountPerSample);
            }

            // Decode block frames
            var sampleOffset = 0;
            var bufferOffset = 0;
            for (var frameIndex = 0; frameIndex < framesCountToRead; frameIndex++)
            {
                for (var channel = 0; channel < waveFormat.Channels; channel++)
                {
                    blockSamples[sampleOffset] = DecodeSample(waveFormat, buffer, ref bufferOffset);
                    sampleOffset++;
                }
            }

            return true;
        }
        
        /// <summary>
        /// Extends <see cref="NAudio.Wave.WaveFileReader.ReadNextSampleFrame()"/> to block samples with buffer.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="blockFramesCount"></param>
        /// <param name="samples"></param>
        /// <param name="bytesBuffer"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static bool ReadNextBlockFramesWithBuffer(
            this WaveFileReader reader,
            int blockFramesCount,
            float[] samples,
            byte[] bytesBuffer)
        {
            var waveFormat = reader.WaveFormat;
            switch (waveFormat.Encoding)
            {
                case WaveFormatEncoding.Pcm:
                case WaveFormatEncoding.IeeeFloat:
                case WaveFormatEncoding.Extensible:
                    // n.b. not necessarily PCM, should probably write more code to handle this case
                    break;
                default:
                    throw new InvalidOperationException("Only 16, 24 or 32 bit PCM or IEEE float audio data supported");
            }

            // 1 frame = 1 float
            var blockSamplesCount = waveFormat.Channels * blockFramesCount;
            if (samples.Length != blockSamplesCount)
            {
                throw new InvalidOperationException($"Block samples count must be {blockSamplesCount} but {samples.Length}");
            }
            Array.Clear(samples, 0, samples.Length);

            var bytesCountPerSample = waveFormat.BitsPerSample / sizeof(byte);
            var blockBytesCount = blockSamplesCount * bytesCountPerSample;
            if (bytesBuffer.Length != blockBytesCount)
            {
                throw new InvalidOperationException($"Buffer bytes count must be {blockBytesCount} but {bytesBuffer.Length}");
            }
            Array.Clear(bytesBuffer, 0, bytesBuffer.Length);

            // May throw
            // Read bytes to buffer
            var readBytesCount = reader.Read(bytesBuffer, 0, blockBytesCount);
            // End of file
            if (readBytesCount == 0)
            {
                return false;
            }

            var framesCountToRead = blockFramesCount;
            // Read bytes count is smaller than block
            if (readBytesCount < blockBytesCount)
            {
                // Recalculate frames count
                framesCountToRead
                    = readBytesCount / (waveFormat.Channels * bytesCountPerSample);
            }

            // Decode block frames
            var sampleOffset = 0;
            var bufferOffset = 0;
            for (var frameIndex = 0; frameIndex < framesCountToRead; frameIndex++)
            {
                for (var channel = 0; channel < waveFormat.Channels; channel++)
                {
                    samples[sampleOffset] = DecodeSample(waveFormat, bytesBuffer, ref bufferOffset);
                    sampleOffset++;
                }
            }

            return true;
        }

        private static float DecodeSample(WaveFormat waveFormat, byte[] buffer, ref int offset)
        {
            if (waveFormat.BitsPerSample == 16)
            {
                offset += 2;
                return BitConverter.ToInt16(buffer, offset) / 32768f;
            }
            else if (waveFormat.BitsPerSample == 24)
            {
                offset += 3;
                return (((sbyte)buffer[offset + 2] << 16) | (buffer[offset + 1] << 8) | buffer[offset])
                       / 8388608f;
            }
            else if (waveFormat is { BitsPerSample: 32, Encoding: WaveFormatEncoding.IeeeFloat })
            {
                offset += 4;
                return BitConverter.ToSingle(buffer, offset);
            }
            else if (waveFormat.BitsPerSample == 32)
            {
                offset += 4;
                return BitConverter.ToInt32(buffer, offset) / (Int32.MaxValue + 1f);
            }
            else
            {
                throw new InvalidOperationException("Unsupported bit depth");
            }
        }
    }
}