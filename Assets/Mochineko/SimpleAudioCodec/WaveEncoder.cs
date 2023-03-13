#nullable enable
using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using NAudio.Utils;
using NAudio.Wave;
using UnityEngine;

namespace Mochineko.SimpleAudioCodec
{
    /// <summary>
    /// A wave file encoder.
    /// </summary>
    public static class WaveEncoder
    {
        private const int RecommendedBitsPerSample = 16;
        private const int RecommendedFramesCountInBlock = 1024 * 32;

        /// <summary>
        /// Encodes samples in AudioClip to wave file with buffering a block of sample frames.
        /// </summary>
        /// <param name="outStream">Bytes written stream</param>
        /// <param name="audioClip">Source AudioClip</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="bitsPerSample">Number of bit per sample. It must 16(ordinary), 24 or 32</param>
        /// <param name="framesCountInBlock">Number of frames in a block</param>
        /// <exception cref="IOException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="OperationCanceledException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static async UniTask EncodeByBlockAsync(
            Stream outStream,
            AudioClip audioClip,
            CancellationToken cancellationToken,
            int bitsPerSample = RecommendedBitsPerSample,
            int framesCountInBlock = RecommendedFramesCountInBlock)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!outStream.CanWrite)
            {
                throw new IOException($"Cannot write to outStream.");
            }

            if (bitsPerSample is not (16 or 24 or 32))
            {
                throw new InvalidOperationException("Unsupported bit depth");
            }
            
            // Prevent to dispose outStream when WaveFileWriter has been disposed.
            var stream = new IgnoreDisposeStream(outStream);

            await UniTask.SwitchToMainThread(cancellationToken);

            if (audioClip.loadType != AudioClipLoadType.DecompressOnLoad)
            {
                throw new InvalidOperationException($"Only can get data from AudioClip set load type:{AudioClipLoadType.DecompressOnLoad}.");
            }

            var format = new WaveFormat(
                rate: audioClip.frequency,
                bits: bitsPerSample,
                channels: audioClip.channels);

            var totalSamplesCount = audioClip.samples * audioClip.channels;
            
            await UniTask.SwitchToThreadPool();

            await using var writer = new WaveFileWriter(stream, format);

            var samplesCountInBlock = framesCountInBlock * format.Channels;
            var blockBuffer = new float[samplesCountInBlock];
            var samplesOffset = 0;
            var framesOffset = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var residue = totalSamplesCount - (samplesOffset + samplesCountInBlock);
                
                Debug.Log($"Offset:{samplesOffset}");
                
                // Read full samples of a block
                if (residue > 0)
                {
                    await UniTask.SwitchToMainThread(cancellationToken);
                    
                    // A sample of AudioClip = sample frame of WAV, then specify frames count to offsetSamples
                    audioClip.GetData(blockBuffer, framesOffset);
                    
                    await UniTask.SwitchToThreadPool();
                    
                    writer.WriteSamples(blockBuffer, 0, samplesCountInBlock);

                    framesOffset += framesCountInBlock;
                    samplesOffset += samplesCountInBlock;
                }
                // Read last block that is full samples or less samples
                else
                {
                    var residueSamplesCount = totalSamplesCount - samplesOffset;
                    var residueBuffer = new float[residueSamplesCount];
                    
                    await UniTask.SwitchToMainThread(cancellationToken);
                    
                    audioClip.GetData(residueBuffer, framesOffset);
                    
                    await UniTask.SwitchToThreadPool();
                    
                    writer.WriteSamples(residueBuffer, 0, residueSamplesCount);
                    
                    await writer.FlushAsync(cancellationToken);
                    
                    // Return to the main thread
                    await UniTask.SwitchToMainThread(cancellationToken);
                    return;
                }
            }
        }
        
        /// <summary>
        /// Encodes samples to wave file into stream.
        /// </summary>
        /// <param name="outStream">Bytes written stream</param>
        /// <param name="samples">Encoded samples</param>
        /// <param name="channels">Channel count</param>
        /// <param name="sampleRate">Sampling rate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="bitsPerSample">Number of bit per sample. It must 16(ordinary), 24 or 32</param>
        /// <exception cref="IOException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="OperationCanceledException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static async UniTask EncodeAsync(
            Stream outStream,
            float[] samples,
            int channels,
            int sampleRate,
            CancellationToken cancellationToken,
            int bitsPerSample = RecommendedBitsPerSample)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!outStream.CanWrite)
            {
                throw new IOException($"Cannot write to outStream.");
            }

            if (bitsPerSample is not (16 or 24 or 32))
            {
                throw new InvalidOperationException("Unsupported bit depth");
            }
            
            await UniTask.SwitchToThreadPool();
            
            // Prevent to dispose outStream when WaveFileWriter has been disposed.
            var stream = new IgnoreDisposeStream(outStream);

            var format = new WaveFormat(
                rate: sampleRate,
                bits: bitsPerSample,
                channels: channels);

            await using var writer = new WaveFileWriter(stream, format);

            writer.WriteSamples(samples, 0, samples.Length);

            await writer.FlushAsync(cancellationToken);
            
            await UniTask.SwitchToMainThread(cancellationToken);
        }
    }
}