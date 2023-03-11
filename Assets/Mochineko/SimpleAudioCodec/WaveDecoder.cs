#nullable enable
using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using NAudio.Wave;
using UnityEngine;

namespace Mochineko.SimpleAudioCodec
{
    public static class WaveDecoder
    {
        /// <summary>
        /// Decodes wave file to AudioClip.
        /// Because frames count is very large, thread switching can be overhead.
        /// Therefore it decodes in blocks of frames.
        /// </summary>
        /// <param name="stream">Wave data stream</param>
        /// <param name="fileName">File name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="framesCountInBlock">Total number of frames in a frame block</param>
        /// <returns>Created AudioClip from Wave file</returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="OperationCanceledException"></exception>
        /// <exception cref="FormatException"></exception>
        /// <exception cref="InvalidDataException"></exception>
        /// <exception cref="IOException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        public static async UniTask<AudioClip> DecodeAsync(
            Stream stream,
            string fileName,
            CancellationToken cancellationToken,
            int framesCountInBlock = 1024 * 16)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!stream.CanRead)
            {
                throw new IOException($"Cannot read stream.");
            }
            
            await UniTask.SwitchToThreadPool();

            await using var reader = new WaveFileReader(stream);
            var header = reader.WaveFormat;
            if (header == null)
            {
                throw new Exception($"Wave format header is null.");
            }
            
            // Can create AudioClip only on the main thread
            await UniTask.SwitchToMainThread(cancellationToken);

            var audioClip = AudioClip.Create(
                name:fileName,
                lengthSamples:(int)reader.SampleCount,
                channels:header.Channels,
                frequency:header.SampleRate,
                stream:false
            );
            
            await UniTask.SwitchToThreadPool();
            
            // total samples = channels * frames
            var samplesBuffer = new float[framesCountInBlock * header.Channels];
            var bytesBuffer = new byte[header.Channels * framesCountInBlock * (header.BitsPerSample / sizeof(byte))];
            var sampleOffset = 0;
            int readSamplesCount;
            while ((readSamplesCount = reader.ReadNextBlockOfFramesWithBuffer(framesCountInBlock, samplesBuffer, bytesBuffer)) 
                   != 0) // 0 is the end of file
            {
                // Can write to AudioClip only on the main thread
                await UniTask.SwitchToMainThread(cancellationToken);
             
                if (readSamplesCount == framesCountInBlock)
                {
                    audioClip.SetData(samplesBuffer, sampleOffset);
                }
                else
                {
                    var justLengthSamples = new float[readSamplesCount];
                    Array.Copy(samplesBuffer, justLengthSamples, readSamplesCount);
                    audioClip.SetData(justLengthSamples, sampleOffset);
                }

                await UniTask.SwitchToThreadPool();
                
                sampleOffset += readSamplesCount;
                
                cancellationToken.ThrowIfCancellationRequested();
            }
            
            await UniTask.SwitchToMainThread(cancellationToken);

            return audioClip;
        }
    }
}