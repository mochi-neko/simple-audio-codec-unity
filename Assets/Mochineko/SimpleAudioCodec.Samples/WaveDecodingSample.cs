using System;
using System.IO;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace Mochineko.SimpleAudioCodec.Samples
{
    internal sealed class WaveDecodingSample : MonoBehaviour
    {
        [SerializeField] private string fileName;
        [SerializeField] private AudioSource audioSource = null;

        private AudioClip audioClip;
        
        private void Awake()
        {
            Assert.IsNotNull(audioSource);
        }

        [ContextMenu("Decode")]
        public async Task Decode()
        {
            if (audioClip != null)
            {
                Object.Destroy(audioClip);
                audioClip = null;
            }

            var cancellationToken = this.GetCancellationTokenOnDestroy();
            
            var filePath = Path.Combine(
                Application.dataPath,
                "Mochineko/SimpleAudioCodec.Tests",
                fileName);

            Stream stream;
            try
            {
                stream = File.OpenRead(filePath);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return;
            }

            try
            {
                audioClip = await WaveDecoder.DecodeBlockByBlockAsync(stream, fileName, cancellationToken);
                Debug.Log($"Succeeded to decode wave:{audioClip.samples}");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return;
            }
            finally
            {
                await stream.DisposeAsync();
            }

            await UniTask.SwitchToMainThread(cancellationToken);

            audioSource.clip = audioClip;
            audioSource.PlayOneShot(audioClip);
        }
    }
}