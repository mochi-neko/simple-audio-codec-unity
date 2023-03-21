# simple-audio-codec-unity

Provides simple decoding and encoding of audio codecs by [NAudio](https://github.com/naudio/NAudio) for Unity.

## Features

- Low load to the main thread of Unity
  - Runs decoding of data on a thread pool using [UniTask](https://github.com/Cysharp/UniTask) and set data to AudioClip on the main thread.
- Low memory allocation
  - Decodes WAV file for each block of frames with array buffers.

## How to import by UnityPackageManager

```json
{
  "dependencies": {
    "com.mochineko.simple-audio-codec-unity": "https://github.com/mochi-neko/simple-audio-codec-unity.git?path=/Assets/Mochineko/SimpleAudioCodec#0.2.0",
    "com.naudio.core": "https://github.com/mochi-neko/simple-audio-codec-unity.git?path=/Assets/NAudio/NAudio.Core#0.2.0",
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
    ...
  }
}
```

## Avairable codecs

- WAV
  - [x] Decoding
  - [x] Encoding
- MP3
  - [ ] Decoding
  - [ ] Encoding

## How to use

See [Samples](https://github.com/mochi-neko/simple-audio-codec-unity/tree/main/Assets/Mochineko/SimpleAudioCodec.Samples) or [Tests](https://github.com/mochi-neko/simple-audio-codec-unity/tree/main/Assets/Mochineko/SimpleAudioCodec.Tests). 

## Changelog

See [CHANGELOG](https://github.com/mochi-neko/simple-audio-codec-unity/blob/main/CHANGELOG.md).

## 3rd Party Notices

See [NOTICE](https://github.com/mochi-neko/simple-audio-codec-unityy/blob/main/NOTICE.md).

## License

[MIT License](https://github.com/mochi-neko/simple-audio-codec-unity/blob/main/LICENSE).
