using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsPreview.Kinect;

namespace Yogat
{
    public abstract class GeometryBase
    {
        protected readonly double ε;

        public GeometryBase()
        {
            ε = 0.0001D;
        }
    }

    public abstract class Dimision3 : GeometryBase
    {
        public readonly double X;
        public readonly double Y;
        public readonly double Z;

        public Dimision3() {}

        public Dimision3(double xx, double yy, double zz) : base()
        {
            X = xx;
            Y = yy;
            Z = zz;
        }

        public double Mag
        {
            get
            {
                return Math.Sqrt(X * X + Y * Y + Z * Z);
            }
        }
    }

    public class Point3 : Dimision3
    {
        public static readonly Point3 NaN = new Point3(double.NaN, double.NaN, double.NaN);

        public static readonly Point3 Zero = new Point3(0D, 0D, 0D);

        public Point3(CameraSpacePoint p) : this(p.X, p.Y, p.Z) {}

        public Point3(double x, double y, double z) : base(x, y, z) {}
    }

    public class Vector3 : Dimision3
    {
        public static readonly Vector3 NaN = new Vector3(double.NaN, double.NaN, double.NaN);

        public Vector3(double x, double y, double z) : base(x, y, z) {}

        /// <summary>
        /// From 2 points
        /// </summary>
        /// <param name="f">from point</param>
        /// <param name="t">togo point </param>
        public Vector3(Point3 f, Point3 t) : base(t.X - f.X, t.Y - f.Y, t.Z - f.Z) {}

        /// <summary>
        /// From 2 points
        /// </summary>
        /// <param name="f">from point</param>
        /// <param name="t">togo point </param>
        public Vector3(CameraSpacePoint f, CameraSpacePoint t) : base(t.X - f.X, t.Y - f.Y, t.Z - f.Z) { }

        public static Vector3 operator + (Vector3 v0, Vector3 v1)
        {
            return new Vector3(v0.X + v1.X, v0.Y + v1.Y, v0.Z + v1.Z);
        }

        /// <summary>
        /// Cross Product
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Vector3 operator % (Vector3 v0, Vector3 v1)
        {
            return new Vector3(v0.Y * v1.Z - v0.Z * v1.Y, v0.Z * v1.X - v0.X * v1.Z, v0.X * v1.Y - v0.Y * v1.X);
        }

        /// <summary>
        /// Mirror
        /// </summary>
        /// <param name="v0"></param>
        /// <returns></returns>
        public static Vector3 operator - (Vector3 v0)
        {
            return new Vector3(-v0.X, -v0.Y, -v0.Z);
        }

        public static double operator * (Vector3 v0, Vector3 v1)
        {
            return v0.X * v1.X + v0.Y * v1.Y + v0.Z * v1.Z;
        }

        public static Vector3 operator * (Vector3 v, double s)
        {
            return new Vector3(v.X * s, v.Y * s, v.Z * s);
        }

        public static Vector3 operator / (Vector3 v, double s)
        {
            return new Vector3(v.X / s, v.Y / s, v.Z / s);
        }

        /// <summary>
        /// Normalize
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Vector3 operator --(Vector3 v)
        {
            double m = v.Mag;
            return new Vector3(v.X / m, v.Y / m, v.Z / m);
        }

        public bool Equals(Vector3 that)
        {
            if (that == null) return false;
            return Math.Abs(X - that.X) < ε && Math.Abs(Y - that.Y) < ε && Math.Abs(Z - that.Z) < ε;
        }
    }

    public class Angle : GeometryBase
    {
        public readonly double Value;
        public string Name;

        public Angle(double v) : this(v, string.Empty) { }

        public Angle(double v, string name) : base()
        {
            Value = v;
            Name = name;
        }

        public Angle(Vector3 v0, Vector3 v1) : this(v0, v1, string.Empty) {}

        public Angle(Vector3 v0, Vector3 v1, string name) : base()
        {
            Value = Math.Acos((v0 * v1) / v0.Mag / v1.Mag);
            Name = name;
        }

        public static Angle operator -(Angle a)
        {
            return new Angle(-a.Value, a.Name);
        }
    }
}
