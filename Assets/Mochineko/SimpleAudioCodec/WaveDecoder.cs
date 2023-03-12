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
        private const int RecommendedFramesCountInBlock = 1024 * 32;
        
        /// <summary>
        /// Decodes wave file to AudioClip by decoding block by block.
        /// If it decodes large file at once, it uses large memories.
        /// If it decodes large file frame by frame, thread switching is overhead of process.
        /// Therefore it decodes in blocks of frames for memory and process.
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
        public static async UniTask<AudioClip> DecodeBlockByBlockAsync(
            Stream stream,
            string fileName,
            CancellationToken cancellationToken,
            int framesCountInBlock = RecommendedFramesCountInBlock)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!stream.CanRead)
            {
                throw new IOException($"Cannot read stream.");
            }

            await using var reader = new WaveFileReader(stream);
            var format = reader.WaveFormat;
            if (format == null)
            {
                throw new Exception($"Wave format is null.");
            }
            
            // Can create AudioClip only on the main thread
            await UniTask.SwitchToMainThread(cancellationToken);

            var audioClip = AudioClip.Create(
                name:fileName,
                lengthSamples:(int)reader.SampleCount * format.Channels, // SampleCount does not take into account channels
                channels:format.Channels,
                frequency:format.SampleRate,
                stream:false
            );
            
            await UniTask.SwitchToThreadPool();
            
            // total samples = channels * frames
            var samplesBuffer = new float[framesCountInBlock * format.Channels];
            // block align = bytes per frame = channels * bytes per sample
            var bytesBuffer = new byte[framesCountInBlock * format.BlockAlign];
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
                    // Set just enough length of samples
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