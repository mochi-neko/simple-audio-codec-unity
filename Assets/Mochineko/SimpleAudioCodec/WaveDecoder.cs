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
        /// 
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="fileName"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="blockFramesCount"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="OperationCanceledException"></exception>
        /// <exception cref="FormatException"></exception>
        /// <exception cref="InvalidDataException"></exception>
        public static async UniTask<AudioClip> DecodeAllAsync(
            Stream stream,
            string fileName,
            CancellationToken cancellationToken,
            int blockFramesCount = 1024 * 128)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            await UniTask.SwitchToThreadPool();

            await using var reader = new WaveFileReader(stream);
            var header = reader.WaveFormat;
            if (header == null)
            {
                throw new Exception($"Wave format header is null.");
            }
            
            await UniTask.SwitchToMainThread();

            var audioClip = AudioClip.Create(
                name:fileName,
                lengthSamples:(int)reader.SampleCount,
                channels:header.Channels,
                frequency:header.SampleRate,
                stream:false
            );
            
            await UniTask.SwitchToThreadPool();
            
            var samplesBuffer = new float[header.Channels * blockFramesCount];
            var bytesBuffer = new byte[header.Channels * blockFramesCount * (header.BitsPerSample / sizeof(byte))];
            var sampleOffset = 0;
            
            while (reader.ReadNextBlockFramesWithBuffer(blockFramesCount, samplesBuffer, bytesBuffer))
            {
                await UniTask.SwitchToMainThread();
                
                // Can write on the main thread
                audioClip.SetData(samplesBuffer, sampleOffset);
                
                await UniTask.SwitchToThreadPool();
                
                sampleOffset += blockFramesCount;
                
                cancellationToken.ThrowIfCancellationRequested();
            }

            return audioClip;
        }
    }
}