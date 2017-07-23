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
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using WindowsPreview.Kinect;
using System.ComponentModel;
using Windows.Storage.Streams;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Windows.UI.Xaml.Shapes;
using Windows.UI;
using Microsoft.Kinect;

//lab 13
using Windows.Storage.Pickers;
using Windows.Graphics.Imaging;
using Windows.Graphics.Display;
using Windows.Storage;
//using System.Windows.Controls;
//using System.Windows.Media;
//using System.Windows.Media.Imaging;
//using System.Windows.Shapes;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Yogat
{
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        // constants for pixel color conversion

        /// <summary> The highest value that can be returned in the InfraredFrame. It is cast to a float for readability in the visualization code.</summary>
        private const float InfraredSourceValueMaximum = (float)ushort.MaxValue;

        /// <summary>
        /// Used to set the lower limit, post processing, of the infrared data that we will render.
        /// Increasing or decreasing this value sets a brightness "wall" either closer or further away.
        /// </summary>
        private const float InfraredOutputValueMinimum = 0.01f;

        /// <summary> The upper limit, post processing, of the infrared data that will render.</summary>
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
        private ushort[] infraredFrameData  = null;
        private byte[] infraredPixels = null;

        //Body Joints are drawn here
        private Canvas drawingCanvas;
        private CoordinateMapper coordinateMapper = null;
        private BodiesManager bodiesManager = null;

        // gesture detection
        private MultiSourceFrameReader multiSourceFrameReader = null;


        //lab 13
        /// <summary> List of gesture detectors, there will be one detector created for each potential body (max of 6) </summary>
        private List<GestureDetector> gestureDetectorList = null;

        public event PropertyChangedEventHandler PropertyChanged;

        public MainPage()
        {

            this.kinectSensor = KinectSensor.GetDefault();

            this.coordinateMapper = this.kinectSensor.CoordinateMapper;

            this.multiSourceFrameReader = this.kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Infrared | FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.BodyIndex | FrameSourceTypes.Body);

            this.multiSourceFrameReader.MultiSourceFrameArrived += this.Reader_MultiSourceFrameArrived;


            // get the infraredFrameDescription from the InfraredFrameSource
            // FrameDescription infraredFrameDescription = this.kinectSensor.InfraredFrameSource.FrameDescription;

            // reader for infrared frames
            // this.infraredFrameReader = this.kinectSensor.InfraredFrameSource.OpenReader();

            // event handler for frame arrival
            // this.infraredFrameReader.FrameArrived += this.Reader_InfraredFrameArrived;

            // intermediate storage for receiving frame data from the sensor
            //this.infraredFrameData =
            //    new ushort[infraredFrameDescription.Width * infraredFrameDescription.Height];

            //// intermediate storage for frame data converted to color pixels for display
            //this.infraredPixels =
            //    new byte[infraredFrameDescription.Width * infraredFrameDescription.Height * BytesPerPixel];

            //// create the bitmap to display, which will replace the contents of an image in XAML
            //this.bitmap =
            //    new WriteableBitmap(infraredFrameDescription.Width, infraredFrameDescription.Height);

            // open the sensor
            this.kinectSensor.Open();

            this.InitializeComponent();

            this.Loaded += MainPage_Loaded;

            //lab 13
            // Initialize the gesture detection objects for our gestures
            this.gestureDetectorList = new List<GestureDetector>();

            //lab 13
            // Create a gesture detector for each body (6 bodies => 6 detectors)
            int maxBodies = 6; // this.kinectSensor.BodyFrameSource.BodyCount;
            for (int i = 0; i < maxBodies; ++i)
            {
                GestureResultView result = new GestureResultView(i, false, false, 0.0f);
                GestureDetector detector = new GestureDetector(this.kinectSensor, result);
                result.PropertyChanged += GestureResult_PropertyChanged;
                this.gestureDetectorList.Add(detector);
            }
        }

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            SetupDisplay();
        }

        private void SetupDisplay()
        {
            if (this.FrameDisplayImage != null)
            {
                this.FrameDisplayImage.Source = null;
            }

            this.BodyJointsGrid.Visibility = Visibility.Visible;

            // for color video
            FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.FrameDescription;
            // create the bitmap to display
            this.bitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height);

            FrameDescription depthFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;
            // instantiate a new Canvas
            this.drawingCanvas = new Canvas();
            // set the clip rectangle to prevent rendering outside the canvas
            this.drawingCanvas.Clip = new RectangleGeometry();
            this.drawingCanvas.Clip.Rect = new Rect(0.0, 0.0, this.BodyJointsGrid.Width, this.BodyJointsGrid.Height);
            this.drawingCanvas.Width = this.BodyJointsGrid.Width;
            this.drawingCanvas.Height = this.BodyJointsGrid.Height;
            // reset the body joints grid
            this.BodyJointsGrid.Visibility = Visibility.Visible;
            this.BodyJointsGrid.Children.Clear();
            // add canvas to DisplayGrid
            this.BodyJointsGrid.Children.Add(this.drawingCanvas);
            bodiesManager = new BodiesManager(this.coordinateMapper, this.drawingCanvas, this.kinectSensor.BodyFrameSource.BodyCount);
        }

        /// <summary>
        /// this method extracts a single InfraredFrame from the FrameReference in the event args,
        /// checks that the frame is not null and its dimensions match the bitmap initialized
        /// </summary>
        //    private void Reader_InfraredFrameArrived(object sender, InfraredFrameArrivedEventArgs e)
        //{
        //    bool infraredFrameProcessed = false;

        //    // InfraredFrame is IDisposable
        //    using (InfraredFrame infraredFrame = e.FrameReference.AcquireFrame())
        //    {
        //        if (infraredFrame != null)
        //        {
        //            FrameDescription infraredFrameDescription = infraredFrame.FrameDescription;

        //            // verify data and write the new infrared frame data to the display bitmap
        //            if (((infraredFrameDescription.Width * infraredFrameDescription.Height) == this.infraredFrameData.Length) &&
        //                (infraredFrameDescription.Width == this.bitmap.PixelWidth) &&
        //                (infraredFrameDescription.Height == this.bitmap.PixelHeight))
        //            {
        //                // copy the infrared frame into the infraredFrameData array class variable which is used in the next stage
        //                infraredFrame.CopyFrameDataToArray(this.infraredFrameData);

        //                infraredFrameProcessed = true;
        //            }
        //        }
        //    }

        //    // if a frame is received successfully, convert and render
        //    if (infraredFrameProcessed)
        //    {
        //        ConvertInfraredDataToPixels();
        //        RenderPixelArray(this.infraredPixels);
        //    }
        //}

        private void Reader_MultiSourceFrameArrived(MultiSourceFrameReader sender, MultiSourceFrameArrivedEventArgs e)
        {
            MultiSourceFrame reference = e.FrameReference.AcquireFrame();

            // If the Frame has expired by the time we process this event, return.
            if (reference == null)
            {
                return;
            }

            ColorFrame colorFrame = null;
            BodyFrame bodyFrame = null;
            //InfraredFrame infraredFrame = null;
            //BodyIndexFrame bodyIndexFrame = null;
            //IBuffer bodyIndexFrameData = null;

            // Open color frame
            using (colorFrame= reference.ColorFrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    ShowColorFrame(colorFrame);
                }
            }

            // Gesture detection and joints overlay=
            using (bodyFrame = reference.BodyFrameReference.AcquireFrame())
            {

                if (bodyFrame != null)
                {

                    var bodies = new Body[bodyFrame.BodyCount];
                    bodyFrame.GetAndRefreshBodyData(bodies);

                    foreach(Body body in bodies)
                    {
                        if (body.IsTracked)
                        {
                            RegisterGesture(bodyFrame);
                            ShowBodyJoints(bodyFrame);
                            PrintJointAngles(bodyFrame);
                        }
                    }
                }           
            }
        }

        private void ShowBodyJoints(BodyFrame bodyFrame)
        {
            Body[] bodies = new Body[this.kinectSensor.BodyFrameSource.BodyCount];
            bool dataReceived = false;
            if (bodyFrame != null)
            {
                bodyFrame.GetAndRefreshBodyData(bodies);
                dataReceived = true;
            }

            if (dataReceived)
            {
                this.bodiesManager.UpdateBodiesAndEdges(bodies);
            }
        }

        private void ShowColorFrame(ColorFrame colorFrame)
        {
            bool colorFrameProcessed = false;

            if (colorFrame != null)
            {
                FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                // verify data and write the new color frame data to the Writeable bitmap
                if ((colorFrameDescription.Width == this.bitmap.PixelWidth) && (colorFrameDescription.Height == this.bitmap.PixelHeight))
                {
                    if (colorFrame.RawColorImageFormat == ColorImageFormat.Bgra)
                    {
                        colorFrame.CopyRawFrameDataToBuffer(this.bitmap.PixelBuffer);
                    }
                    else
                    {
                        colorFrame.CopyConvertedFrameDataToBuffer(this.bitmap.PixelBuffer, ColorImageFormat.Bgra);
                    }

                    colorFrameProcessed = true;
                }
            }

            if (colorFrameProcessed)
            {
                this.bitmap.Invalidate();
                FrameDisplayImage.Source = this.bitmap;
            }
        }


        void GestureResult_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            GestureResultView result = sender as GestureResultView;

            if (result.Confidence > 0.1)
            {
                this.GestureName.Text = result.GestureName;
            }
            else
            {
                this.GestureName.Text = "__";
            }

            this.GestureConfidence.Text = result.Confidence.ToString();
        }

        private void RegisterGesture(BodyFrame bodyFrame)
        {
            bool dataReceived = false;
            Body[] bodies = null;

            if (bodyFrame != null)
            {
                if (bodies == null)
                {
                    // Creates an array of 6 bodies, which is the max number of bodies that Kinect can track simultaneously
                    bodies = new Body[bodyFrame.BodyCount];
                }

                // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                // As long as those body objects are not disposed and not set to null in the array,
                // those body objects will be re-used.
                bodyFrame.GetAndRefreshBodyData(bodies);
                dataReceived = true;
            }

            if (dataReceived)
            {
                // We may have lost/acquired bodies, so update the corresponding gesture detectors
                if (bodies != null)
                {
                    // Loop through all bodies to see if any of the gesture detectors need to be updated
                    for (int i = 0; i < bodyFrame.BodyCount; ++i)
                    {
                        Body body = bodies[i];
                        ulong trackingId = body.TrackingId;

                        // If the current body TrackingId changed, update the corresponding gesture detector with the new value
                        if (trackingId != this.gestureDetectorList[i].TrackingId)
                        {
                            this.gestureDetectorList[i].TrackingId = trackingId;

                            // If the current body is tracked, unpause its detector to get VisualGestureBuilderFrameArrived events
                            // If the current body is not tracked, pause its detector so we don't waste resources trying to get invalid gesture results
                            this.gestureDetectorList[i].IsPaused = trackingId == 0;
                        }
                    }
                }
            }
        }


        private void PrintJointAngles(BodyFrame bodyFrame)
        {
            var bodies = new Body[bodyFrame.BodyCount];
            bodyFrame.GetAndRefreshBodyData(bodies);
            
            foreach (var body in bodies)
            {
                if (body.IsTracked)
                {
                    SquatGestures obj = new SquatGestures(body);

                    this.JointAngle.Text = $"Right knee angle: { obj.RightKneeAngle.Degree }";
                    this.JointPosition.Text = $"Right knee position: { obj.printPoint("Right knee position", obj.RightKneePosition) }";
                    this.RightShinDeviation.Text = $"Right shin deviation: { obj.RightShimDeviation.Degree }";
                    //Debug.WriteLine(obj.Report);
                }
            }         
        }

        private void JointAngle_SelectionChanged(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// convert the infrared data, each an ushort (0 to 65535), to RGB-alpha values (0 to 255);
        /// output the pixel colors to be rendered
        /// </summary>
        //private void ConvertInfraredDataToPixels()
        //{
        //    // Convert the infrared to RGB
        //    int colorPixelIndex = 0;
        //    for (int i = 0; i < this.infraredFrameData.Length; ++i)
        //    {
        //        // normalize the incoming infrared data (ushort) to a float ranging from InfraredOutputValueMinimum
        //        // to InfraredOutputValueMaximum] by

        //        // 1. dividing the incoming value by the source maximum value
        //        float intensityRatio = (float)this.infraredFrameData[i] / InfraredSourceValueMaximum;

        //        // 2. dividing by the (average scene value * standard deviations)
        //        intensityRatio /= InfraredSceneValueAverage * InfraredSceneStandardDeviations;

        //        // 3. limiting the value to InfraredOutputValueMaximum
        //        intensityRatio = Math.Min(InfraredOutputValueMaximum, intensityRatio);

        //        // 4. limiting the lower value InfraredOutputValueMinimum
        //        intensityRatio = Math.Max(InfraredOutputValueMinimum, intensityRatio);

        //        // 5. converting the normalized value to a byte and using  the result as the RGB components required by the image
        //        byte intensity = (byte)(intensityRatio * 255.0f);
        //        this.infraredPixels[colorPixelIndex++] = intensity; //Blue
        //        this.infraredPixels[colorPixelIndex++] = intensity; //Green
        //        this.infraredPixels[colorPixelIndex++] = intensity; //Red
        //        this.infraredPixels[colorPixelIndex++] = 255;       //Alpha - always opaque for now         
        //    }
        //}

        ///// <summary>
        ///// get the pixels in the byte array into something xaml can use (a WritableBitmap which can be the source of an Image)
        ///// </summary>
        ///// <param name="pixels"></param>
        //private void RenderPixelArray(byte[] pixels)
        //{
        //    pixels.CopyTo(this.bitmap.PixelBuffer);
        //    this.bitmap.Invalidate();
        //    FrameDisplayImage.Source = this.bitmap;
        //}
    }
}
