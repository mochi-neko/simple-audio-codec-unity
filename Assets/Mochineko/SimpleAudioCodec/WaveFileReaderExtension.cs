#nullable enable
using System;
using System.IO;
using NAudio.Wave;

namespace Mochineko.SimpleAudioCodec
{
    internal static class WaveFileReaderExtension
    {
        /// <summary>
        /// Reads next block of frames with buffers.
        /// Extends <see cref="NAudio.Wave.WaveFileReader.ReadNextSampleFrame()"/> to block of frames with buffer.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="framesCountInBlock"></param>
        /// <param name="samplesBuffer"></param>
        /// <param name="bytesBuffer"></param>
        /// <returns>Read samples count. Returns 0 at the end of file.</returns>
        /// <exception cref="InvalidOperationException">Validation errors</exception>
        /// <exception cref="IOException">I/O error</exception>
        /// <exception cref="NotSupportedException">Cannot read stream</exception>
        /// <exception cref="ObjectDisposedException">Stream is closed</exception>
        public static int ReadNextBlockOfFramesWithBuffer(
            this WaveFileReader reader,
            int framesCountInBlock,
            float[] samplesBuffer,
            byte[] bytesBuffer)
        {
            var waveFormat = reader.WaveFormat;
            // Encoding validation
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

            var samplesCountInBlock = waveFormat.Channels * framesCountInBlock;
            // Samples buffer length validation
            if (samplesBuffer.Length != samplesCountInBlock)
            {
                throw new InvalidOperationException(
                    $"Samples count in block must be {samplesCountInBlock} but {samplesBuffer.Length}");
            }
            Array.Clear(samplesBuffer, 0, samplesBuffer.Length);

            var bytesCountPerSample = waveFormat.BitsPerSample / sizeof(byte);
            var bytesCountInFrame = waveFormat.Channels * bytesCountPerSample;
            var bytesCountInBlock = samplesCountInBlock * bytesCountPerSample;
            // Bytes buffer length validation
            if (bytesBuffer.Length != bytesCountInBlock)
            {
                throw new InvalidOperationException(
                    $"Buffer bytes count must be {bytesCountInBlock} but {bytesBuffer.Length}");
            }
            Array.Clear(bytesBuffer, 0, bytesBuffer.Length);

            // Actually read bytes to buffer
            var readBytesCount = reader.Read(bytesBuffer, 0, bytesCountInBlock);
            // End of file
            if (readBytesCount == 0)
            {
                return 0;
            }

            var framesCountToRead = framesCountInBlock;
            if (readBytesCount < bytesCountInBlock)
            {
                // Recalculate frames count
                framesCountToRead = readBytesCount / bytesCountInFrame;
            }

            // Decode block of frames
            var sampleOffset = 0;
            var bufferOffset = 0;
            for (var frameIndex = 0; frameIndex < framesCountToRead; frameIndex++)
            {
                for (var channel = 0; channel < waveFormat.Channels; channel++)
                {
                    samplesBuffer[sampleOffset] = DecodeSample(waveFormat, bytesBuffer, ref bufferOffset);
                    sampleOffset++;
                }
            }

            return framesCountToRead * waveFormat.Channels;
        }

        /// <summary>
        /// Decodes a sample from buffer with offset.
        /// Excluded from <see cref="NAudio.Wave.WaveFileReader.ReadNextSampleFrame()"/>.
        /// </summary>
        /// <param name="waveFormat"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <returns>Decoded sample as float</returns>
        /// <exception cref="InvalidOperationException">Invalid bits per samples of format</exception>
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
                return BitConverter.ToInt32(buffer, offset) / (int.MaxValue + 1f);
            }
            else
            {
                throw new InvalidOperationException("Unsupported bit depth");
            }
        }
    }
}