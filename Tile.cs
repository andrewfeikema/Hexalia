using System;
using System.Collections.Generic;

namespace Hexalia.v2
{
    // This class contains helper functions for tiles on the game board
    class Tile
    {
        public float Height { get; set; }
        public int[] Coords { get; }
        public int[] Tilecoords { get; }
        public float Moisture { get; }
        private Board board;


        public Tile(float h, int[] c, int[] tc, float m, Board b)
        {
            Height = h;
            Coords = c;
            Tilecoords = tc;
            Moisture = m;
            board = b;

        }

        enum Resource
        {

        }

        public List<int[]> neighborindices()
        {
            return new List<int[]>
            {
                new int[] { this.Tilecoords[0] + 1, this.Tilecoords[1] },
                new int[] { this.Tilecoords[0], this.Tilecoords[1] + 1 },
                new int[] { this.Tilecoords[0] - 1, this.Tilecoords[1] + 1 },
                new int[] { this.Tilecoords[0] - 1, this.Tilecoords[1] },
                new int[] { this.Tilecoords[0], this.Tilecoords[1] -1 },
                new int[] { this.Tilecoords[0] + 1, this.Tilecoords[1] - 1 }
            };
        }

        public List<int[]> AdjacentIndices()
        {
            return new List<int[]>
            {
                new int[] { this.Tilecoords[0], this.Tilecoords[1], 0 },
                new int[] { this.Tilecoords[0] + 1, this.Tilecoords[1] - 1, 1 },
                new int[] { this.Tilecoords[0], this.Tilecoords[1] + 1, 0 },
                new int[] { this.Tilecoords[0], this.Tilecoords[1], 1 },
                new int[] { this.Tilecoords[0] - 1, this.Tilecoords[1] + 1, 0 },
                new int[] { this.Tilecoords[0], this.Tilecoords[1] - 1, 1 }
            };
        }

        enum Path
        {
            Distance,
            Cost
        }

        // Manhattan Distance
        public double mdistance(Tile t)
        {
            return (
                Math.Abs(this.Tilecoords[0] - t.Tilecoords[0]) +
                Math.Abs(this.Tilecoords[0] + this.Tilecoords[1] - t.Tilecoords[0] - t.Tilecoords[1]) +
                Math.Abs(this.Tilecoords[1] - t.Tilecoords[1])) / 2;
        }

        // Euclidian Distance
        public double edistance(Tile t)
        {
            return Math.Sqrt(
                (Math.Pow(Math.Abs(this.Tilecoords[0] - t.Tilecoords[0]), 2) +
                Math.Pow(Math.Abs(this.Tilecoords[1] - t.Tilecoords[1]), 2) +
                Math.Pow(Math.Abs(this.Tilecoords[0] - t.Tilecoords[0] + this.Tilecoords[1] - t.Tilecoords[1]), 2)
                ) / 2);
        }

        // Determines Biome of tile based on elevation and moisture
        public Board.Biome Biome()
        {
            if (Height < Board.SEA_LEVEL)
            {
                if (Height < .6 * Board.SEA_LEVEL)
                {
                    return Board.Biome.Water_Deep;
                }
                return Board.Biome.Water_Shallow;
            } else if (Math.Sqrt(
                Math.Pow(Moisture - Board.DESERT_MOISTURE, 2) + 
                Math.Pow(Height - Board.ALPINE_LEVEL, 2)) < 30d) 
                { return Board.Biome.Tundra; } 
            else if (Height > Board.ALPINE_LEVEL) {

                return Board.Biome.Alpine;
            } else if (Moisture + 80 < Height)
            {
                return Board.Biome.Jungle;
            } else if (Moisture + Height > 220)
            {
                return Board.Biome.Forest;
            } else
            {
                return Board.Biome.Plain;
            }
        }


        public bool ValidTile(Board.PathType pt)
        {

            switch (pt)
            {
                case Board.PathType.Water:
                    return WaterValidTile();
                case Board.PathType.Basic:
                    return BasicValidTile();
                default:
                    return false;
            }
        }

        // above sea level
          public bool WaterValidTile()
        {
            return (Height < Board.SEA_LEVEL);
        }

        public bool BasicValidTile()
        {
            return true;
        }
    }
}
