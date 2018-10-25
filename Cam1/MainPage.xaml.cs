using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.System.Threading;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Graphics.Imaging;
using System.Diagnostics;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x411 を参照してください

namespace Cam1
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private MediaCapture capture;
        private bool isPreview = false;
        private bool isRecording = false;
        private ThreadPoolTimer timer;
        private Timer recordingTimer;
        private SemaphoreSlim semaphore = new SemaphoreSlim(1);


        public MainPage()
        {
            this.InitializeComponent();
            Loaded += MainPage_Loaded;
        }

        public async void MainPage_Loaded(object s, object e)
        {
            await InitCameraAsync();
        }

        public async Task InitCameraAsync()
        {
            try
            {
                if (capture == null)
                {
                    if (isPreview)
                    {
                        await capture.StopPreviewAsync();
                        isPreview = false;
                    }
                    capture.Dispose();
                    capture = null;
                }

                //カメラの設定
                var captureInitSettings = new MediaCaptureInitializationSettings();
                captureInitSettings.VideoDeviceId = "";
                captureInitSettings.StreamingCaptureMode = StreamingCaptureMode.Video;

                var camera = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

                if(camera.Count() == 0)
                {
                    Debug.WriteLine("No Camera");
                    return;
                }
                else
                {
                    if(camera.Count() == 1)
                    {
                        captureInitSettings.VideoDeviceId = camera[0].Id;
                    }
                    else
                    {
                        captureInitSettings.VideoDeviceId = camera[1].Id;
                    }
                }

                capture = new MediaCapture();
                await capture.InitializeAsync(captureInitSettings);

                //ビデオの設定
                VideoEncodingProperties vp = new VideoEncodingProperties();
                vp.Height = 240;
                vp.Width = 320;
                vp.Subtype = "NV12";

                await capture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, vp);
                capturePreview.Source = capture;

                Debug.WriteLine("Camera Initialized");

                //プレビュー開始
                await StartPreview();

            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public async Task StartPreview()
        {
            Debug.WriteLine("Start Preview");
            await capture.StartPreviewAsync();
            isPreview = true;

            if(timer == null)
            {
                timer = ThreadPoolTimer.CreatePeriodicTimer(new TimerElapsedHandler(CurrentVideoFrame), TimeSpan.FromMilliseconds(66));
            }
        }

        public async void CurrentVideoFrame(ThreadPoolTimer timer)
        {
            if (!semaphore.Wait(0))
            {
                return;
            }

            try
            {
                using(VideoFrame previewFrame = new VideoFrame(BitmapPixelFormat.Nv12, 320, 240))
                {
                    await capture.GetPreviewFrameAsync(previewFrame);
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}
