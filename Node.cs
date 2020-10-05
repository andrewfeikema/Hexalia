using System;
using System.Collections.Generic;

namespace Hexalia.v2
{
    // This class contains helper functions for tile nodes on the game board
    class Node
    {
        public float Elevation { get; set; }
        public int[] Coords { get; } //on 2d noise plain
        public int[] Tilecoords { get; }
        private Board Board { get; }
        public float Volume { get; set; }
        public List<Node> InNodes { get; set; }
        public Node OutNode { get; set; }
        public Node MinNode{get; set;}

        public Node(float e, int[] c, int[] tc, Board b)
        {
            Elevation = e;
            Coords = c;
            Tilecoords = tc;
            Board = b;
            InNodes = new List<Node>(3);
            OutNode = null;

        }

        // Returns indices of neighboring nodes
        public List<int[]> NeighborIndices()
        {
            List<int[]> neigbors = new List<int[]>();
            if (Tilecoords[2] == 0)
            {
                neigbors.Add(new int[] {
                    Tilecoords[0] + 1,
                    Tilecoords[1] - 1,
                    1 });
                neigbors.Add(new int[] {
                    Tilecoords[0],
                    Tilecoords[1] - 1,
                    1 });
                neigbors.Add(new int[] {
                    Tilecoords[0] + 1,
                    Tilecoords[1] - 2,
                    1 });
            }
            else
            {
                neigbors.Add(new int[] {
                    Tilecoords[0],
                    Tilecoords[1] + 1,
                    0 });
                neigbors.Add(new int[] {
                    Tilecoords[0] - 1,
                    Tilecoords[1] + 2,
                    0 });
                neigbors.Add(new int[] {
                    Tilecoords[0] - 1,
                    Tilecoords[1] + 1,
                    0 });
            }
            return neigbors;
        }

        // Manhattan Distance
        public double mdistance(Node n)
        {
            int segments;
            if (this.Tilecoords[2] != n.Tilecoords[2])
            {
                if (this.Tilecoords[2] == 0)
                {
                    if (n.Tilecoords[1] >= this.Tilecoords[1])
                    {
                        if (n.Tilecoords[0] <= this.Tilecoords[0] && n.Tilecoords[0] + n.Tilecoords[1] >= this.Tilecoords[0] + this.Tilecoords[1])
                        {
                            segments = 3;
                        }
                        else segments = 1;
                    }
                    else
                    {
                        if (n.Tilecoords[0] > Tilecoords[0] && n.Tilecoords[0] + n.Tilecoords[1] < Tilecoords[0] + Tilecoords[1])
                        {
                            segments = -3;
                        }
                        else segments = -1;

                    }
                }
                else
                {
                    if (n.Tilecoords[1] >= this.Tilecoords[1])
                    {
                        if (n.Tilecoords[0] >= this.Tilecoords[0] && n.Tilecoords[0] + n.Tilecoords[1] <= this.Tilecoords[0] + this.Tilecoords[1])
                        {
                            segments = 3;
                        }
                        else segments = 1;
                    }
                    else
                    {
                        if (n.Tilecoords[0] < Tilecoords[0] && n.Tilecoords[0] + n.Tilecoords[1] > Tilecoords[0] + Tilecoords[1])
                        {
                            segments = -3;
                        }
                        else segments = -1;

                    }
                }
            }
            else segments = 0;
            return (
                Math.Abs(this.Tilecoords[0] - n.Tilecoords[0]) +
                Math.Abs(this.Tilecoords[0] + this.Tilecoords[1] - n.Tilecoords[0] - n.Tilecoords[1]) +
                Math.Abs(this.Tilecoords[1] - n.Tilecoords[1]) + segments) / Math.Sqrt(3);
        }

        // Euclidian Distance
        public double edistance(Node n)
        {
            double shift;
            if (Tilecoords[2] != Tilecoords[2])
            {
                if (Tilecoords[2] == 0)
                    shift = 1;
                else
                    shift = -1;
            } else shift = 0;

            return Math.Sqrt(
                (Math.Pow(Math.Abs(this.Tilecoords[0] - n.Tilecoords[0] + 4 / 3 * shift), 2) +
                Math.Pow(Math.Abs(this.Tilecoords[1] - n.Tilecoords[1] - 2 / 3 * shift), 2) +
                Math.Pow(Math.Abs(this.Tilecoords[0] - n.Tilecoords[0] + this.Tilecoords[1] - n.Tilecoords[1] - 2 / 3 * shift), 2)
                ) / 2);
        }
    }
}
