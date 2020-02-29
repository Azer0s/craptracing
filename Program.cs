using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace craptracing
{
    internal class Program
    {
        private const int MaxRayDepth = 5;

        private double Mix(double a, double b, double mix)
        {
            return b * mix + a * (1 - mix);
        }

        /// <summary>
        /// This is the main trace function. It takes a ray as argument (defined by its origin
        /// and direction). We test if this ray intersects any of the geometry in the scene.
        /// If the ray intersects an object, we compute the intersection point, the normal
        /// at the intersection point, and shade this point using this information.
        /// Shading depends on the surface property (is it transparent, reflective, diffuse).
        /// The function returns a color for the ray. If the ray intersects an object that
        /// is the color of the object at the intersection point, otherwise it returns
        /// the background color.
        /// </summary>
        /// <param name="rayorig"></param>
        /// <param name="raydir"></param>
        /// <param name="spheres"></param>
        /// <param name="depth"></param>
        /// <returns></returns>
        private Vec3 Trace(
            Vec3 rayorig,
            Vec3 raydir,
            IReadOnlyList<Sphere> spheres,
            int depth)
        {
            var tnear = 1e8;
            Sphere sphere = null;
            // find intersection of this ray with the sphere in the scene
            foreach (var t in spheres)
            {
                double t0 = 1e8, t1 = 1e8;
                if (t.Intersect(rayorig, raydir, ref t0, ref t1))
                {
                    if (t0 < 0) t0 = t1;
                    if (t0 < tnear)
                    {
                        tnear = t0;
                        sphere = t;
                    }
                }
            }

            // if there's no intersection return black or background color
            if (sphere == null) return new Vec3(2);
            Vec3 surfaceColor = 0; // color of the ray/surfaceof the object intersected by the ray
            var phit = rayorig + raydir * tnear; // point of intersection
            var nhit = phit - sphere.Center; // normal at the intersection point
            nhit.Normalize(); // normalize normal direction
            // If the normal and the view direction are not opposite to each other
            // reverse the normal direction. That also means we are inside the sphere so set
            // the inside bool to true. Finally reverse the sign of IdotN which we want
            // positive.
            var bias = 1e-4d; // add some bias to the point from which we will be tracing
            var inside = false;
            if (raydir.Dot(nhit) > 0)
            {
                nhit = -nhit;
                inside = true;
            }

            if ((sphere.Transparency > 0 || sphere.Reflection > 0) && depth < MaxRayDepth)
            {
                var facingratio = -raydir.Dot(nhit);
                // change the mix value to tweak the effect
                var fresneleffect = Mix(Math.Pow(1 - facingratio, 3), 1, 0.1d);
                // compute reflection direction (not need to normalize because all vectors
                // are already normalized)
                var refldir = raydir - nhit * 2.0 * raydir.Dot(nhit);
                refldir.Normalize();
                var reflection = Trace(phit + nhit * bias, refldir, spheres, depth + 1);
                Vec3 refraction = 0;
                // if the sphere is also transparent compute refraction ray (transmission)
                if (Math.Abs(sphere.Transparency) > 0)
                {
                    var ior = 1.1d;
                    var eta = inside ? ior : 1 / ior; // are we inside or outside the surface?
                    var cosi = -nhit.Dot(raydir);
                    var k = 1 - eta * eta * (1 - cosi * cosi);
                    var refrdir = raydir * eta + nhit * (eta * cosi - Math.Sqrt(k));
                    refrdir.Normalize();
                    refraction = Trace(phit - nhit * bias, refrdir, spheres, depth + 1);
                }

                // the result is a mix of reflection and refraction (if the sphere is transparent)
                surfaceColor = (
                    reflection * fresneleffect +
                    refraction * (1 - fresneleffect) * sphere.Transparency) * sphere.SurfaceColor;
            }
            else
            {
                // it's a diffuse object, no need to raytrace any further
                for (var i = 0; i < spheres.Count; ++i)
                    if (spheres[i].EmissionColor.X > 0)
                    {
                        // this is a light
                        Vec3 transmission = 1;
                        var lightDirection = spheres[i].Center - phit;
                        lightDirection.Normalize();
                        for (var j = 0; j < spheres.Count; ++j)
                            if (i != j)
                            {
                                double t0 = 0d, t1 = 0d;
                                if (spheres[j].Intersect(phit + nhit * bias, lightDirection, ref t0, ref t1))
                                {
                                    transmission = 0;
                                    break;
                                }
                            }

                        surfaceColor += sphere.SurfaceColor * transmission *
                                        Math.Max(0.0, nhit.Dot(lightDirection)) * spheres[i].EmissionColor;
                    }
            }

            return surfaceColor + sphere.EmissionColor;
        }

        /// <summary>
        /// Main rendering function. We compute a camera ray for each pixel of the image
        /// trace it and return a color. If the ray hits a sphere, we return the color of the
        /// sphere at the intersection point, else we return the background color.
        /// </summary>
        /// <param name="spheres"></param>
        private void Render(List<Sphere> spheres)
        {
            uint width = 640, height = 480;
            var image = new Vec3[width * height];
            var pixel = 0;
            double invWidth = 1d / width, invHeight = 1d / height;
            // ReSharper disable once PossibleLossOfFraction
            double fov = 30, aspectratio = width / height;
            var angle = Math.Tan(MathF.PI * 0.5d * fov / 180d);
            // Trace rays
            for (uint y = 0; y < height; ++y)
            for (uint x = 0; x < width; ++x, ++pixel)
            {
                var xx = (2d * ((x + 0.5d) * invWidth) - 1d) * angle * aspectratio;
                var yy = (1d - 2d * ((y + 0.5d) * invHeight)) * angle;
                var raydir = new Vec3(xx, yy, -1);
                raydir.Normalize();
                image[pixel] = Trace(new Vec3(), raydir, spheres, 0);
            }

            // Save result to a PPM image (keep these flags if you compile under Windows)
            var bytes = new List<byte>();
            bytes.AddRange(Encoding.ASCII.GetBytes("P6\n" + width + " " + height + "\n255\n"));
            for (uint i = 0; i < width * height; ++i)
            {
                bytes.Add((byte) (Math.Min(1d, image[i].X) * 255));
                bytes.Add((byte) (Math.Min(1d, image[i].Y) * 255));
                bytes.Add((byte) (Math.Min(1d, image[i].Z) * 255));
            }

            File.WriteAllBytes("out.ppm", bytes.ToArray());
        }
        
        public static void Main()
        {
            var spheres = new List<Sphere>
            {
                new Sphere(new Vec3(0.0d, -10004, -20), 10000, new Vec3(0.20d, 0.20d, 0.20d), 0, 0.0d,
                    new Vec3()),
                new Sphere(new Vec3(0.0d, 0, -20), 4, new Vec3(1.00d, 0.32d, 0.36d), 1, 0.5d, new Vec3(0)),
                new Sphere(new Vec3(5.0d, -1, -15), 2, new Vec3(0.90d, 0.76d, 0.46d), 1, 0.0d, new Vec3(0)),
                new Sphere(new Vec3(5.0d, 0, -25), 3, new Vec3(0.65d, 0.77d, 0.97d), 1, 0.0d, new Vec3(0)),
                new Sphere(new Vec3(-5.5d, 0, -15), 3, new Vec3(0.90d, 0.90d, 0.90d), 1, 0.0d, new Vec3(0)),
                new Sphere(new Vec3(0.0d, 20, -30), 3, new Vec3(0.00d, 0.00d, 0.00d), 0, 0.0d, new Vec3(3))
            };
            // position, radius, surface color, reflectivity, transparency, emission color
            // light
            new Program().Render(spheres);
        }
    }
}