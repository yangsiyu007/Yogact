using System;
using System.Collections.Generic;
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
        public Angle LeftKneeAngle;

        public SquatGestures (Body body) : base(body)
        {
            var js = body.Joints;

            var v0 = new Vector3(js[JointType.HipRight].Position, js[JointType.KneeRight].Position);
            var v1 = new Vector3(js[JointType.KneeRight].Position, js[JointType.AnkleRight].Position);
            RightKneeAngle = new Angle(v0, v1, "RightKnee");

            v0 = new Vector3(js[JointType.HipLeft].Position, js[JointType.KneeLeft].Position);
            v1 = new Vector3(js[JointType.KneeLeft].Position, js[JointType.AnkleLeft].Position);
            LeftKneeAngle = new Angle(v0, v1, "LeftKnee");
        }

        public string Report
        {
            get
            {
                return $"({LeftKneeAngle.Name}:{LeftKneeAngle.Value},{RightKneeAngle.Name}:{RightKneeAngle.Value})";
            }        
        }
    }
}
