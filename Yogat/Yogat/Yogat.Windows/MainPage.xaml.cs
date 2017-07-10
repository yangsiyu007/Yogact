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
using WindowsPreview.Kinect;
using Windows.UI.Xaml.Media.Imaging;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Yogat
{
    public sealed partial class MainPage : Page
    {
        // constants for pixel color conversion

        /// <summary>
        /// The highest value that can be returned in the InfraredFrame. It is cast to a float for readability in the visualization code.
        /// </summary>
        private const float InfraredSourceValueMaximum = (float)ushort.MaxValue;

        /// <summary>
        /// Used to set the lower limit, post processing, of the infrared data that we will render.
        /// Increasing or decreasing this value sets a brightness "wall" either closer or further away.
        /// </summary>
        private const float InfraredOutputValueMinimum = 0.01f;

        /// <summary>
        /// The upper limit, post processing, of the infrared data that will render.
        /// </summary>
        private const float InfraredOutputValueMaximum = 1.0f;

        /// <summary>
        /// The InfraredSceneValueAverage value specifies the average infrared value of the scene. 
        /// This value was selected by analyzing the average pixel intensity for a given scene.
        /// This could be calculated at runtime to handle different IR conditions of a scene (outside vs inside).
        /// </summary>
        private const float InfraredSceneValueAverage = 0.08f;

        /// <summary>
        /// The InfraredSceneStandardDeviations value specifies the number of standard deviations to apply to InfraredSceneValueAverage.
        /// Can be calculated at runtime
        /// </summary>
        private const float InfraredSceneStandardDeviations = 3.0f;

        // other private instance variables

        // size of the RGB pixel in the bitmap
        private const int BytesPerPixel = 4;

        // the default sensor for the Kinect
        private KinectSensor kinectSensor = null;

        // bitmap object to write to
        private WriteableBitmap bitmap = null;

        // infra-red frame
        private InfraredFrameReader infraredFrameReader = null;
        private ushort[] infraredFrameData = null;
        private byte[] infraredPixels = null;

        public MainPage()
        {
            // select the default sensor; only one sensor is currently supported by the SDK
            this.kinectSensor = KinectSensor.GetDefault();

            // get the infraredFrameDescription from the InfraredFrameSource
            FrameDescription infraredFrameDescription =
                this.kinectSensor.InfraredFrameSource.FrameDescription;

            // reader for infrared frames
            this.infraredFrameReader =
                this.kinectSensor.InfraredFrameSource.OpenReader();

            // event handler for frame arrival
            this.infraredFrameReader.FrameArrived += this.Reader_InfraredFrameArrived;

            // intermediate storage for receiving frame data from the sensor
            this.infraredFrameData =
                new ushort[infraredFrameDescription.Width * infraredFrameDescription.Height];
            
            // intermediate storage for frame data converted to color pixels for display
            this.infraredPixels =
                new byte[infraredFrameDescription.Width * infraredFrameDescription.Height * BytesPerPixel];

            // create the bitmap to display, which will replace the contents of an image in XAML
            this.bitmap =
                new WriteableBitmap(infraredFrameDescription.Width, infraredFrameDescription.Height);

            // open the sensor
            this.kinectSensor.Open();

            this.InitializeComponent();
        }

        /// <summary>
        /// this method extracts a single InfraredFrame from the FrameReference in the event args,
        /// checks that the frame is not null and its dimensions match the bitmap initialized
        /// </summary>
        private void Reader_InfraredFrameArrived(object sender, InfraredFrameArrivedEventArgs e)
        {
            bool infraredFrameProcessed = false;

            // InfraredFrame is IDisposable
            using (InfraredFrame infraredFrame = e.FrameReference.AcquireFrame())
            {
                if (infraredFrame != null)
                {
                    FrameDescription infraredFrameDescription =infraredFrame.FrameDescription;

                    // verify data and write the new infrared frame data to the display bitmap
                    if (((infraredFrameDescription.Width * infraredFrameDescription.Height) == this.infraredFrameData.Length) &&
                        (infraredFrameDescription.Width == this.bitmap.PixelWidth) &&
                        (infraredFrameDescription.Height == this.bitmap.PixelHeight))
                    {
                        // copy the infrared frame into the infraredFrameData array class variable which is used in the next stage
                        infraredFrame.CopyFrameDataToArray(this.infraredFrameData);

                        infraredFrameProcessed = true;
                    }
                }
            }

            // if a frame is received successfully, convert and render
            if (infraredFrameProcessed)
            {
                ConvertInfraredDataToPixels();
                RenderPixelArray(this.infraredPixels);
            }
        }

        /// <summary>
        /// convert the infrared data, each an ushort (0 to 65535), to RGB-alpha values (0 to 255);
        /// output the pixel colors to be rendered
        /// </summary>
        private void ConvertInfraredDataToPixels()
        {
            // Convert the infrared to RGB
            int colorPixelIndex = 0;
            for (int i = 0; i < this.infraredFrameData.Length; ++i)
            {
                // normalize the incoming infrared data (ushort) to a float ranging from InfraredOutputValueMinimum
                // to InfraredOutputValueMaximum] by

                // 1. dividing the incoming value by the source maximum value
                float intensityRatio = (float)this.infraredFrameData[i] / InfraredSourceValueMaximum;

                // 2. dividing by the (average scene value * standard deviations)
                intensityRatio /= InfraredSceneValueAverage * InfraredSceneStandardDeviations;

                // 3. limiting the value to InfraredOutputValueMaximum
                intensityRatio = Math.Min(InfraredOutputValueMaximum, intensityRatio);

                // 4. limiting the lower value InfraredOutputValueMinimum
                intensityRatio = Math.Max(InfraredOutputValueMinimum, intensityRatio);

                // 5. converting the normalized value to a byte and using  the result as the RGB components required by the image
                byte intensity = (byte)(intensityRatio * 255.0f);
                this.infraredPixels[colorPixelIndex++] = intensity; //Blue
                this.infraredPixels[colorPixelIndex++] = intensity; //Green
                this.infraredPixels[colorPixelIndex++] = intensity; //Red
                this.infraredPixels[colorPixelIndex++] = 255;       //Alpha - always opaque for now         
            }
        }
    
        /// <summary>
        /// get the pixels in the byte array into something xaml can use (a WritableBitmap which can be the source of an Image)
        /// </summary>
        /// <param name="pixels"></param>
        private void RenderPixelArray(byte[] pixels)
        {
            pixels.CopyTo(this.bitmap.PixelBuffer);
            this.bitmap.Invalidate();
            FrameDisplayImage.Source = this.bitmap;
        }
    }
}
