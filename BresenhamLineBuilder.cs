using System;
using System.Collections.Generic;
using core.Primitives;

namespace core.Algorithms
{
    public static class BresenhamLineBuilder
    {
        public static List<Vector> GetLine(Vector start, Vector end)
        {
            return ComputeLine(start.X, start.Y, start.Z, end.X, end.Y, end.Z);
        }

        public static List<Vector> GetRay(Vector start, Vector end, int mapSize)
        {
            if(start == end)
                return new List<Vector>();

            return ComputeLine(start.X, start.Y, start.Z, end.X, end.Y, end.Z, mapSize);
        }
        
        private static List<Vector> ComputeLine(int x0, int y0, int z0, int x1, int y1, int z1, int mapSize = -1)
        {
            int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int dz = Math.Abs(z1 - z0), sz = z0 < z1 ? 1 : -1;
            int dm = Math.Max(1, Math.Max(Math.Max(dx, dy), dz)), i = dm;
            x1 = y1 = z1 = dm / 2;
            
            var line = new List<Vector>{Capacity = dm + 1};

            while (true)
            {
                if (mapSize != -1)
                {
                    if (x0 < 0 || x0 >= mapSize)
                        break;
                    
                    if (y0 < 0 || y0 >= mapSize)
                        break;
                    
                    if (z0 < 0 || z0 >= mapSize)
                        break;
                }
                
                line.Add(new Vector(x0, y0, z0));

                if (i == 0 && mapSize == -1) 
                    break;

                i--;
                
                x1 -= dx;
                if (x1 < 0)
                {
                    x1 += dm;
                    x0 += sx;
                }

                y1 -= dy;
                if (y1 < 0)
                {
                    y1 += dm;
                    y0 += sy;
                }

                z1 -= dz;
                if (z1 < 0)
                {
                    z1 += dm;
                    z0 += sz;
                }
            }

            return line;
        }
    }
}