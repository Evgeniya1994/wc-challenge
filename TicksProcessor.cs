using System;
using System.Collections.Generic;
using System.Linq;
using core.Algorithms;
using core.Models;
using core.Primitives;
using core.View;
using Ship = core.Models.Ship;

namespace core.Simulation.Battle
{
    public class TicksProcessor
    {
        private const int fact = 2 * 3 * 4 * 5 * 6 * 7 * 8;
        private static readonly Physics physics = new Physics(Physics.ChebyshevEnergyComputer);

        public static void Process(State state)
        {
            var tickToShipsOffsets = new SortedDictionary<int, List<(Ship ship, Vector offset)>>();
            var shipIdToTicksWhenShipMoves = new Dictionary<int, HashSet<int>>();

            foreach (var ship in state.AliveShips)
                AddTicks(ship, tickToShipsOffsets, shipIdToTicksWhenShipMoves);

            if (tickToShipsOffsets.Count == 0)
                return;

            var currentTick = 0;
            while (currentTick < fact)
            {
                foreach (var kvp in tickToShipsOffsets.SkipWhile(t => t.Key <= currentTick))
                {
                    currentTick = kvp.Key;
                    var shipsOffsets = kvp.Value;

                    var shipIdToShipAndNextPosition = state.AliveShips
                        .ToDictionary(s => s.Id, s => (ship: s, nextPosition: s.Position));

                    var activeShips = shipsOffsets
                        .Where(x => !x.ship.IsDead() && shipIdToTicksWhenShipMoves[x.ship.Id].Contains(currentTick))
                        .ToArray();

                    if (activeShips.Length == 0)
                        continue;

                    foreach (var activeShipAndOffset in activeShips)
                    {
                        shipIdToShipAndNextPosition[activeShipAndOffset.ship.Id] = (activeShipAndOffset.ship, activeShipAndOffset.ship.Position + activeShipAndOffset.offset);
                    }

                    var shipsAndNextPositions = shipIdToShipAndNextPosition.Values.ToList();

                    var collidedShips = ResolveCollisions(shipsAndNextPositions);

                    foreach (var ship in collidedShips)
                    {
                        AddTicks(ship, tickToShipsOffsets, shipIdToTicksWhenShipMoves);
                    }

                    if (collidedShips.Count != 0)
                    {
                        currentTick--;
                        break;
                    }

                    foreach (var ship in activeShips.Select(x => x.ship))
                    {
                        var newPos = shipIdToShipAndNextPosition[ship.Id].nextPosition;
                        if (newPos.ShipPositionIsOutside())
                        {
                            shipIdToShipAndNextPosition[ship.Id].ship.Kill();

                            continue;
                        }

                        shipIdToShipAndNextPosition[ship.Id].ship.Position = newPos;
                    }
                }
            }
        }

        private static HashSet<Ship> ResolveCollisions(List<(Ship Ship, Vector Position)> shipAndNextPosition)
        {
            var collidedShips = new HashSet<Ship>();

            var v = new bool[shipAndNextPosition.Count];
            var systems = new List<List<int>>();
            var collisionCountsAndNoseDamages = new List<(int Count, int SharpNoseDamage)>();
            for (var i = 0; i < shipAndNextPosition.Count; i++)
            {
                var (system, collisionCount, nose) = BuildSystem(i, shipAndNextPosition, v);
                collisionCountsAndNoseDamages.Add((collisionCount, nose));

                if (system.Count >= 2)
                    systems.Add(system);
            }

            foreach (var system in systems)
            {
                foreach (var index in system)
                    collidedShips.Add(shipAndNextPosition[index].Ship);

                var count = physics.SolveShipsCollision(shipAndNextPosition.Where((x, i) => system.Contains(i)).ToList());

                foreach (var index in system)
                {
                    var collisionCount = collisionCountsAndNoseDamages[index].Count;
                    var sharpNoseDamage = collisionCountsAndNoseDamages[index].SharpNoseDamage;
                    shipAndNextPosition[index].Ship.BeAttacked(collisionCount * count + sharpNoseDamage);
                }
            }

            return collidedShips;
        }

        private static (List<int> ids, int collisionCount, int sharpNoseDamage) BuildSystem(int i, List<(Ship Ship, Vector Position)> shipsAndNextPositions, bool[] v)
        {
            v[i] = true;
            var collisionCount = 0;
            var sharpNoseDamage = 0;

            var shipAndNextPosition1 = shipsAndNextPositions[i];
            var ids = new List<int> { i };
            for (var j = 0; j < shipsAndNextPositions.Count; j++)
            {
                if (i == j)
                    continue;

                var shipAndNextPosition2 = shipsAndNextPositions[j];
                var collision = (shipAndNextPosition1.Position - shipAndNextPosition2.Position).ChebyshevLength < 2;

                if (collision)
                {
                    collisionCount++;
                    sharpNoseDamage += shipAndNextPosition2.Ship.SharpNoseDamage;
                }

                if (v[j])
                    continue;

                if (collision)
                    ids.AddRange(BuildSystem(j, shipsAndNextPositions, v).ids);
            }

            return (ids, collisionCount, sharpNoseDamage);
        }

        private static void AddTicks(Ship ship, SortedDictionary<int, List<(Ship, Vector)>> tickToShipsOffsets,
            Dictionary<int, HashSet<int>> shipIdToTicksWhenShipMoves)
        {
            if (shipIdToTicksWhenShipMoves.ContainsKey(ship.Id))
                shipIdToTicksWhenShipMoves[ship.Id].Clear();

            var v = ship.Velocity.ChebyshevLength;
            var t = v == 0 ? fact : fact / v;

            var line = GetLines(ship);

            var curPosition = ship.Position;

            for (int i = 1, cur = t; cur <= fact; cur += t, i++)
            {
                var linePosition = line[i];
                var offset = linePosition - curPosition;
                curPosition = linePosition;

                if (!tickToShipsOffsets.ContainsKey(cur))
                    tickToShipsOffsets.Add(cur, new List<(Ship, Vector)>());
                tickToShipsOffsets[cur].Add((ship, offset));

                if (!shipIdToTicksWhenShipMoves.ContainsKey(ship.Id))
                    shipIdToTicksWhenShipMoves.Add(ship.Id, new HashSet<int>());
                shipIdToTicksWhenShipMoves[ship.Id].Add(cur);
            }
        }

        private static List<Vector> GetLines(Ship ship)
        {
            var line = BresenhamLineBuilder.GetLine(ship.Position, ship.Position + ship.Velocity);
            return line;
        }
    }
}