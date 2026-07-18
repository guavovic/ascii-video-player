# ASCII Video Player

A C# application that reads a video frame by frame, converts each frame into ASCII characters based on pixel brightness, and renders the result in real time. It uses **OpenCV** for video processing and **NAudio** for synchronized audio playback when available.

## Features

* **Real-time conversion:** transforms video frames into ASCII and displays them as text.
* **Audio support:** plays audio synchronized with the video, when available.

## Tech Stack

C#, .NET, OpenCvSharp4 and NAudio.

## Prerequisites

* .NET Core SDK installed
* OpenCvSharp4 and NAudio libraries available in your environment

## How to Run

1. Clone the repository, restore the dependencies and run the project passing the path to a video file.
```
git clone https://github.com/guavovic/ascii-video-player.git
```
## License

This project is licensed under the [MIT License](LICENSE.md).

## Notes

Developed as a personal learning project.
