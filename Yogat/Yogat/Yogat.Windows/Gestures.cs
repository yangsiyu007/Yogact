using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsPreview.Kinect;

namespace Yogat
{
    public class AGesture
    {
        protected Body body;

        public AGesture (Body body)
        {
            this.body = body;
        }

    }

    public class SquatGestures : AGesture
    {
        public Angle RightKneeAngle;
        public Angle RightShinDeviation;
        public Angle LeftKneeAngle;
        public CameraSpacePoint RightKneePosition;

        public SquatGestures (Body body) : base(body)
        {
            var js = body.Joints;
            var HipRight = js[JointType.HipRight].Position;
            RightKneePosition = js[JointType.KneeRight].Position;
            var AnkleRight = js[JointType.AnkleRight].Position;

            printPoint("HipRight", HipRight);
            printPoint("KneeRight", RightKneePosition);
            printPoint("AnkleRight", AnkleRight);

            var v0 = new Vector3(js[JointType.KneeRight].Position, js[JointType.HipRight].Position);
            var v1 = new Vector3(js[JointType.KneeRight].Position, js[JointType.AnkleRight].Position);
            RightKneeAngle = new Angle(v0, v1, "RightKnee");

            Debug.WriteLine($"Angle: {RightKneeAngle.Degree}");

            //v0 = new Vector3(js[JointType.HipLeft].Position, js[JointType.KneeLeft].Position);
            //v1 = new Vector3(js[JointType.KneeLeft].Position, js[JointType.AnkleLeft].Position);
            RightShinDeviation = new Angle(new Vector3(v1.X,v1.Y,0), new Vector3(0,-1,0), "RightShinDeviation");
        }

        public string printPoint(string name, CameraSpacePoint HipRight)
        {
            Debug.WriteLine(name + ": " + HipRight.X + " " + HipRight.Y + " " + HipRight.Z);
            return name + ": " + HipRight.X + " " + HipRight.Y + " " + HipRight.Z;
        }

        //public string Report
        //{
        //    get
        //    {
        //        return "";
        //        //return $"({LeftKneeAngle.Name}:{LeftKneeAngle.Degree},{RightKneeAngle.Name}:{RightKneeAngle.Degree})";
        //    }        
        //}
    }
}
