#nullable enable
using System;
using System.IO;
using NAudio.Wave;
using UnityEngine;

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
            var format = reader.WaveFormat;
            // Encoding validation
            switch (format.Encoding)
            {
                case WaveFormatEncoding.Pcm:
                case WaveFormatEncoding.IeeeFloat:
                case WaveFormatEncoding.Extensible:
                    // n.b. not necessarily PCM, should probably write more code to handle this case
                    break;
                default:
                    throw new InvalidOperationException("Only 16, 24 or 32 bit PCM or IEEE float audio data supported");
            }

            var samplesCountInBlock = framesCountInBlock * format.Channels;
            if (samplesBuffer.Length != samplesCountInBlock)
            {
                throw new InvalidOperationException(
                    $"Samples count in block must be {samplesCountInBlock} but {samplesBuffer.Length}");
            }
            Array.Clear(samplesBuffer, 0, samplesBuffer.Length);

            var bytesCountInBlock = framesCountInBlock * format.BlockAlign;
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
                framesCountToRead = readBytesCount / format.BlockAlign;
            }

            // Decode block of frames
            var sampleOffset = 0;
            var bufferOffset = 0;
            for (var frameIndex = 0; frameIndex < framesCountToRead; frameIndex++)
            {
                for (var channel = 0; channel < format.Channels; channel++)
                {
                    samplesBuffer[sampleOffset] = DecodeSample(format, bytesBuffer, ref bufferOffset);
                    sampleOffset++;
                }
            }

            return framesCountToRead * format.Channels;
        }

        /// <summary>
        /// Decodes one sample of one channel from buffer with offset.
        /// Excluded from <see cref="NAudio.Wave.WaveFileReader.ReadNextSampleFrame()"/>.
        /// </summary>
        /// <param name="waveFormat"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <returns>Decoded one sample of one channel as float</returns>
        /// <exception cref="InvalidOperationException">Invalid bits per samples of format</exception>
        private static float DecodeSample(WaveFormat waveFormat, byte[] buffer, ref int offset)
        {
            float result;
            if (waveFormat.BitsPerSample == 16)
            {
                result = BitConverter.ToInt16(buffer, offset) / 32768f;
                offset += 2;
            }
            else if (waveFormat.BitsPerSample == 24)
            {
                result = (((sbyte)buffer[offset + 2] << 16) | (buffer[offset + 1] << 8) | buffer[offset])
                         / 8388608f;
                offset += 3;
            }
            else if (waveFormat is { BitsPerSample: 32, Encoding: WaveFormatEncoding.IeeeFloat })
            {
                result = BitConverter.ToSingle(buffer, offset);
                offset += 4;
            }
            else if (waveFormat.BitsPerSample == 32)
            {
                result = BitConverter.ToInt32(buffer, offset) / (int.MaxValue + 1f);
                offset += 4;
            }
            else
            {
                throw new InvalidOperationException("Unsupported bit depth");
            }

            return result;
        }
    }
}