using System;

namespace craptracing
{
    public class Sphere
    {
        /// <summary>
        ///     Position of the sphere
        /// </summary>
        public Vec3 Center;

        /// <summary>
        ///     Emission (light) vector
        /// </summary>
        public Vec3 EmissionColor;

        /// <summary>
        ///     Sphere radius
        /// </summary>
        public double Radius;

        /// <summary>
        ///     Sphere radius^2
        /// </summary>
        public double Radius2;

        /// <summary>
        ///     Surface reflectivity
        /// </summary>
        public double Reflection;

        /// <summary>
        ///     Surface color vector
        /// </summary>
        public Vec3 SurfaceColor;

        /// <summary>
        ///     Surface transparency
        /// </summary>
        public double Transparency;

        public Sphere(Vec3 center, double radius, Vec3 surfaceColor, double reflection, double transparency,
            Vec3 emissionColor)
        {
            Center = center;
            Radius = radius;
            Radius2 = radius * radius;
            SurfaceColor = surfaceColor;
            EmissionColor = emissionColor;
            Transparency = transparency;
            Reflection = reflection;
        }

        /// <summary>
        ///     Compute a ray-sphere intersection using the geometric solution
        /// </summary>
        /// <returns></returns>
        public bool Intersect(Vec3 rayorig, Vec3 raydir, ref double t0, ref double t1)
        {
            var l = Center - rayorig;
            var tca = l.Dot(raydir);
            if (tca < 0) return false;
            var d2 = l.Dot(l) - tca * tca;
            if (d2 > Radius2) return false;
            var thc = Math.Sqrt(Radius2 - d2);
            t0 = tca - thc;
            t1 = tca + thc;

            return true;
        }
    }
}