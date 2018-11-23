using System;
using System.Collections.Generic;
using System.Linq;
using core.Models;
using core.Primitives;

namespace core.Simulation.Battle
{
    public class Physics
    {
        public static readonly Func<Vector, int> ChebyshevEnergyComputer =
            vector => vector.ChebyshevLength * vector.ChebyshevLength;

        private readonly Func<Vector, int> energyComputer;

        public Physics(Func<Vector, int> energyComputer)
        {
            this.energyComputer = energyComputer;
        }

        public int SolveShipsCollision(List<(Ship Ship, Vector Position)> shipsAndNextPositions)
        {
            var normals = GetNormal(shipsAndNextPositions);
            var (velocitiesAfterCollision, count) = ComputeVelocitiesAfterCollision(shipsAndNextPositions.Select(x => x.Ship.Velocity).ToList(), normals);

            for (var i = 0; i < shipsAndNextPositions.Count; i++)
            {
                var ship = shipsAndNextPositions[i].Ship;
                ship.Velocity = velocitiesAfterCollision[i];
            }

            return count;
        }

        private List<Vector> GetNormal(List<(Ship Ship, Vector Position)> shipsAndNextPositions)
        {
            var normals = new List<Vector>(shipsAndNextPositions.Count);
            for (var i = 0; i < shipsAndNextPositions.Count; i++)
            {
                normals.Add(new Vector(0, 0, 0));
            }
            for (var i = 0; i < shipsAndNextPositions.Count - 1; i++)
            {
                var ship1 = shipsAndNextPositions[i].Ship;
                var position1 = shipsAndNextPositions[i].Position;

                for (var j = i + 1; j < shipsAndNextPositions.Count; j++)
                {
                    var ship2 = shipsAndNextPositions[j].Ship;
                    var position2 = shipsAndNextPositions[j].Position;

                    var normal = GetNormal(ship1, ship2, position1, position2);
                    if (normal != null)
                    {
                        normals[i] += normal;
                        normals[j] -= normal;
                    }
                }
            }

            return normals;
        }

        private (List<Vector>, int) ComputeVelocitiesAfterCollision(List<Vector> velocities, List<Vector> normals)
        {
            var startEnergy = GetEnergy(velocities);

            var newVelocities = velocities.ToList();
            var count = 0;
            do
            {
                count++;
                for (var i = 0; i < newVelocities.Count; i++)
                    newVelocities[i] -= normals[i];
            } while (GetEnergy(newVelocities.Select((x, i) => x - normals[i])) <= startEnergy);

            return (newVelocities, count);

            int GetEnergy(IEnumerable<Vector> v) => v.Sum(x => energyComputer(x));
        }

        private static Vector GetNormal(Ship first, Ship second, Vector firstCollisionPosition,
            Vector secondCollisionPosition)
        {
            var intersectionVolume = Ship.GetRegion(firstCollisionPosition)
                .GetVolumeOfIntersectionWith(Ship.GetRegion(secondCollisionPosition));

            switch (intersectionVolume)
            {
                case 0:
                    return null;
                case 1:
                case 2:
                case 4:
                    return (secondCollisionPosition - firstCollisionPosition).Normalize();
                case 8:
                    return (second.Position - first.Position).Normalize();
                default:
                    throw new NotSupportedException("Unexpected number of collisions points");
            }
        }
    }
}