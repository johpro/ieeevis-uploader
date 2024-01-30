

# IEEE VIS Uploader
Web app and libs for collecting and checking paper presentation material such as presentation videos and teaser images. Uploaded videos, images, and subtitles undergo several checks to make sure that they adhere to predefined format and quality requirements, including checks for background noise and speech volume. Uploads are transferred to and stored in the cloud (BunnyCDN). This project was originally developed and deployed to help with collecting additional material of papers and other presentations at the IEEE VIS 2023 conference and associated events.

# Requirements
The web app needs three configuration files in the folder 'config'.

### General Settings: config/settings.json

```
{
  "BunnyStorageZoneName": "",
  "BunnyCdnRootUrl": "",
  "BunnyAccessKey": "",
  "BunnyBasePath": "",
  "BunnyTokenKey": "",
  "BunnyUserApiKey": "",
  "AuthSignaturePrivateKey": "",
  "FfprobePath": "",
  "FfmpegPath": ""
}
```


### File Types: config/fileTypes.json
<details>
```
[
  {
    "Id": "video-full",
    "Name": "Presentation Video",
    "FileName": "Presentation",
    "FileExtensions": [
      "mp4"
    ],
    "FileType": 0,
    "IsOptional": false,
    "PerformChecks": true,
    "CheckInfo": {
      "MinFileSize": 1024,
      "MaxFileSize": 524288000,
      "VideoRequirements": {
        "MinDuration": "00:01:00",
        "MaxDuration": "01:30:00",
        "MaxRecommendedDuration": "00:20:00",
        "PackageFormat": [
          "mp4"
        ],
        "VideoCodecs": [
          "h264"
        ],
        "AudioCodecs": [
          "aac"
        ],
        "FrameRates": [
          "30/1"
        ],
        "FrameSizes": [
          {
            "Width": 1920,
            "Height": 1080
          }
        ],
        "MaxNumAudioChannels": 1,
        "AspectRatio": "16:9",
        "CheckVoiceRecording": true
      },
      "ImageMaxSize": null
    }  
  },
  {
    "Id": "video-ff",
    "Name": "Video Preview",
    "FileName": "Preview",
    "FileExtensions": [
      "mp4"
    ],
    "FileType": 0,
    "IsOptional": false,
    "PerformChecks": true,
    "CheckInfo": {
      "MinFileSize": 1024,
      "MaxFileSize": 31457280,
      "VideoRequirements": {
        "MinDuration": "00:00:15",
        "MaxDuration": "00:00:26",
        "MaxRecommendedDuration": null,
        "PackageFormat": [
          "mp4"
        ],
        "VideoCodecs": [
          "h264"
        ],
        "AudioCodecs": [
          "aac"
        ],
        "FrameRates": [
          "30/1"
        ],
        "FrameSizes": [
          {
            "Width": 1920,
            "Height": 1080
          }
        ],
        "MaxNumAudioChannels": 1,
        "AspectRatio": "16:9",
        "CheckVoiceRecording": true
      },
      "ImageMaxSize": null
    }
  },
  {
    "Id": "video-full-subs",
    "Name": "Presentation Video Subtitles",
    "FileName": "Presentation",
    "FileExtensions": [
      "srt",
      "sbv"
    ],
    "FileType": 1,
    "IsOptional": false,
    "PerformChecks": true,
    "CheckInfo": {
      "MinFileSize": 10,
      "MaxFileSize": 2097152,
      "VideoRequirements": null,
      "ImageMaxSize": null
    }
  },
  {
    "Id": "video-ff-subs",
    "Name": "Video Preview Subtitles",
    "FileName": "Preview",
    "FileExtensions": [
      "srt",
      "sbv"
    ],
    "FileType": 1,
    "IsOptional": false,
    "PerformChecks": true,
    "CheckInfo": {
      "MinFileSize": 10,
      "MaxFileSize": 2097152,
      "VideoRequirements": null,
      "ImageMaxSize": null
    }
  },
  {
    "Id": "image",
    "Name": "Representative Image",
    "FileName": "Image",
    "FileExtensions": [
      "png"
    ],
    "FileType": 3,
    "IsOptional": false,
    "PerformChecks": true,
    "CheckInfo": {
      "MinFileSize": 10,
      "MaxFileSize": 5242880,
      "VideoRequirements": null,
      "ImageMaxSize": {
        "Width": 1920,
        "Height": 1080
      }
    }
  },
  {
    "Id": "image-caption",
    "Name": "Representative Image Caption",
    "FileName": "Image",
    "FileExtensions": [
      "txt"
    ],
    "FileType": 4,
    "IsOptional": false,
    "PerformChecks": true,
    "CheckInfo": {
      "MinFileSize": 10,
      "MaxFileSize": 102400,
      "VideoRequirements": null,
      "ImageMaxSize": null
    }
  }
]
```
</details>

### Events: config/events.json

```
[  
  {
    "EventId": "v",
    "FilesToCollect": [
      "video-full",
      "video-full-subs",
      "video-ff",
      "video-ff-subs",
      "image",
      "image-caption"
    ]
  }
]
```




# License

This project is MIT-licensed. Please refer to the LICENSE file in the root directory.

Copyright © Johannes Knittel
