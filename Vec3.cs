using System;

namespace craptracing
{
    public class Vec3
    {
        public double X, Y, Z;

        public Vec3()
        {
            (X, Y, Z) = (0, 0, 0);
        }

        public Vec3(double xx)
        {
            (X, Y, Z) = (xx, xx, xx);
        }

        public Vec3(double xx, double yy, double zz)
        {
            (X, Y, Z) = (xx, yy, zz);
        }

        public void Normalize()
        {
            var nor2 = Length2();
            if (nor2 > 0)
            {
                var invNor = 1 / Math.Sqrt(nor2);
                X *= invNor;
                Y *= invNor;
                Z *= invNor;
            }
        }

        public static Vec3 operator *(Vec3 v, double f)
        {
            return new Vec3(v.X * f, v.Y * f, v.Z * f);
        }

        public static Vec3 operator *(Vec3 v1, Vec3 v2)
        {
            return new Vec3(v1.X * v2.X, v1.Y * v2.Y, v1.Z * v2.Z);
        }

        public double Dot(Vec3 v)
        {
            return X * v.X + Y * v.Y + Z * v.Z;
        }

        public static Vec3 operator -(Vec3 v1, Vec3 v2)
        {
            return new Vec3(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);
        }

        public static Vec3 operator +(Vec3 v1, Vec3 v2)
        {
            return new Vec3(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);
        }

        public static Vec3 operator -(Vec3 v)
        {
            return new Vec3(-v.X, -v.Y, -v.Z);
        }

        public static implicit operator Vec3(int x)
        {
            return new Vec3(x);
        }

        public double Length2()
        {
            return X * X + Y * Y + Z * Z;
        }

        public double Length()
        {
            return Math.Sqrt(Length2());
        }
    }
}