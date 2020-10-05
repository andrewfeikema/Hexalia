/* This class performs operations to build board environment data on which the game is played
 * The class currently includes functionality to:
 *  
 *  Construct an island elevation map from layering random noise maps 
 *  MakeTiles(): Build a grid of hexagonal tiles on the noise map
 *  FillSea(): Establish the set of tiles belonging to the ocean surrounding the island
 *  MakeCoast(): Establish the set of tiles bordering other tiles belonging to the ocean
 *  MakeRivers(): Genarate a set of rivers flowing from tile edges of the highest elevation determinted by FindPeaks()
 *  
 * @Author: Andrew Feikema
 * @Date: May, 07, 2020
*/

using System;
using System.Linq;
using System.Collections.Generic;
using SimplexNoise;
using Accord.Statistics.Distributions.Univariate;

namespace Hexalia.v2
{
    class Board
    {

        private Dictionary<string, Tile> tiles = new Dictionary<string, Tile>();
        private Dictionary<string, Node> nodes = new Dictionary<string, Node>();
        // private Dictionary<string, Nation> countries = new Dictionary<string, Nation>();
        private Dictionary<Tile, int> coast = new Dictionary<Tile, int>();
        private List<Tile> ringbuffer = new List<Tile>();
        private List<Tile> ocean = new List<Tile>();
        private List<Node> oceannodes = new List<Node>();
        private List<Node> peaks = new List<Node>();
        private Dictionary<string, Queue<Node>> rivers = new Dictionary<string, Queue<Node>>();

        private float[,] myNoisemap, map1, map2, map3, moisturemap;
        private int myHeight, myWidth, myRings, minDim, maxDim;

        public static float EVAP = .9f;
        public static float SEA_LEVEL = 132f;
        public static float ALPINE_LEVEL = 200f;
        public static float DESERT_MOISTURE = 64f;
        public static float RIVER_THRESHOLD = 600f;
        public static int DEEP_BUFFER = 2;
        public static int SHALLOW_BUFFER = 3;
        public static int RIVER_LIMIT = 3;
        public static int COAST_PERM = 4;

        // Game board constructor 
        public Board(int rings, int width, int height)
        {
            myHeight = height;
            myWidth = width;
            myRings = rings;

            minDim = Math.Min(myHeight, myWidth);
            maxDim = Math.Max(myHeight, myWidth);

            Random rand = new Random();
            Noise.Seed = rand.Next(0, 4096);
            map1 = Noise.Calc2D(width, height, 3f / maxDim);
            Noise.Seed = rand.Next(0, 4096);
            map2 = Noise.Calc2D(width, height, 5f / maxDim);
            Noise.Seed = rand.Next(0, 4096);
            map3 = Noise.Calc2D(width, height, 12f / maxDim);
            Noise.Seed = rand.Next(0, 4096);
            moisturemap = Noise.Calc2D(width, height, 2f / maxDim);

            myNoisemap = TDFA.CenterFactor(
                TDFA.AddTDFA(TDFA.MulTDFA(.08f, map3),
                TDFA.AddTDFA(
                    TDFA.MulTDFA(.64f,
                    map1
                    ),
                    TDFA.MulTDFA(.28f, map2)
                )
                )
            );

            Maketiles();

            Fillsea(tiles.First<KeyValuePair<string, Tile>>().Value);

            MakeCoast();

            Findpeaks();

            MakeRivers();

            FillBiomes(); // TODO: Populate with Biomes

            FillResources(); // TODO: Populate with resources

        }

        public void FillResources()
        {

        }
        public void Fillsea(Tile t)
        {
            foreach (Tile tc in TileNeighbors(t))
            {
                if (!ocean.Contains(tc) && tc.ValidTile(PathType.Water))
                {
                    ocean.Add(tc);
                    foreach (Node n in AdjacentNodes(tc))
                    {
                        if (!oceannodes.Contains(n)) oceannodes.Add(n);
                    }

                    Fillsea(tc);
                } else if (!coast.ContainsKey(tc) && !tc.ValidTile(PathType.Water)) {
                    coast.Add(tc, 1);
                }
            }
        }

        public void MakeCoast()
        {
            Queue<Tile> coastTiles = new Queue<Tile>(coast.Keys.AsEnumerable<Tile>());
            while (coastTiles.Count > 0)
            {
                Tile t = coastTiles.Dequeue();
                foreach (Tile child in TileNeighbors(t))
                {
                    if (!ocean.Contains(child) && !coast.ContainsKey(child))
                    {
                        if (!t.ValidTile(PathType.Water) || coast[coastTiles.Peek()] > COAST_PERM)
                        {
                            coast.Add(child, coast[t] + 1);
                            coastTiles.Enqueue(child);
                        }
                        else
                        {
                            Fillsea(child);
                            MakeTileChannel(t);
                            MakeChannel(child);
                        }
                    }

                }
            }
        }

        public void MakeChannel(Tile t)
        {
            SortedSet<string> seacoords = new SortedSet<string> { };
            int[] p = t.Coords;
            List<int[]> lowcoords = new List<int[]>();

            // FindSea(seacoords, lowcoords, p);

            SortedSet<string> watershedcoords = new SortedSet<string>();
            // FindWatershed(watershedcoords, lowcoords);
        }

        public void FindSea(SortedSet<string> seacoords, List<int[]> lowestcoords, int[] pixel)
        {
            int[][] neighbors = new int[][] { 
                                    new int[] { pixel[0] - 1, pixel[1] },   // up
                                    new int[] { pixel[0], pixel[1] - 1 },   // left
                                    new int[] { pixel[0] + 1, pixel[1] },   // down
                                    new int[] { pixel[0], pixel[1] + 1 } }; // right
            bool lowest = true;
            foreach (int[] coord in neighbors)
            {
                if (!seacoords.Contains(PToS(coord)) && myNoisemap[coord[0], coord[1]] < SEA_LEVEL){
                    seacoords.Add(PToS(coord));
                    FindSea(seacoords, lowestcoords, coord);

                    if (myNoisemap[coord[0], coord[1]] <= myNoisemap[pixel[0], pixel[1]])
                    {
                        lowest = false;
                    }
                }
            }
            if (lowest) lowestcoords.Add(pixel);
        }

        //public SortedSet<string> FindWatershed(SortedSet<string> watershedcoords, List<int[]> lowcoords)
        //{
        //    int[][] pixelneighbors = { };
        //    foreach (int[] coord in pixelneighbors) { } 

        //    return watershedcoords;
        //}

        public void MakeTileChannel(Tile t)
        {
            foreach (Tile neighbor in TileNeighbors(t))
            {
                if (coast.ContainsKey(neighbor) && coast[neighbor] == coast[t] - 1)
                {
                    foreach (Node n in AdjacentNodes(t))
                    {
                        if (n.Elevation < SEA_LEVEL)
                            n.Elevation = SEA_LEVEL * .9f;

                        if (!oceannodes.Contains(n))
                            oceannodes.Add(n);
                    }
                    t.Height = SEA_LEVEL * .9f;
                    ocean.Add(t);
                    MakeTileChannel(neighbor);
                    return;  
                }
            }
        }

        public void FillBiomes()
        {
            foreach (KeyValuePair<string, Tile> kvp in tiles) 
            {

            }
        }

        public void Findpeaks()
        {
            Node n;
            float e;
            bool peak;

            foreach (KeyValuePair<string, Node> kvp in nodes)
            {
                
                n = kvp.Value;
                e = n.Elevation;
                peak = true;
                Node myOutNode = n;

                if (oceannodes.Contains(n)) 
                    continue;

                foreach (Node m in NodeNeighbors(n))
                {

                    if (e < m.Elevation) {
                        peak = false;
                    } else if (e > m.Elevation && myOutNode.Elevation > m.Elevation) {
                        myOutNode = m;
                    }
                }

                if (peak) peaks.Add(n);
                if (myOutNode != n && !oceannodes.Contains(n)) 
                {
                    n.OutNode = myOutNode;
                    myOutNode.InNodes.Add(n);
                }

            }
        }

        public void MakeRivers()
        {
        Dictionary<string, List<Node>> streams = 
            new Dictionary<string, List<Node>>();
        Queue<Node> nextnodes = new Queue<Node>();
        Dictionary<string, List<Node>> _2in = 
            new Dictionary<string, List<Node>>();
        Dictionary<string, List<Node>> _3in1 =
            new Dictionary<string, List<Node>>();
        Dictionary<string, List<Node>> _3in2 =
            new Dictionary<string, List<Node>>();
        Node LastNode;

        // all rivers start at peaks
        foreach (Node n in peaks)
        {
            n.Volume = ToTile(n).Moisture;
            streams.Add(
                ToS(n), 
                new List<Node>() { n });
            nextnodes.Enqueue(n);  
        }

        while (nextnodes.Count != 0)
        {
            LastNode = nextnodes.Dequeue();
            List<Node> StreamNodes = streams[ToS(LastNode)];
            streams.Remove(ToS(LastNode));

            // node has a neigbor of lower elevation
            if (LastNode.OutNode != null)
            {
                foreach (Node nn in NodeNeighbors(LastNode))
                {
                    if (nn == LastNode.OutNode)
                    {
                        nn.Volume += LastNode.Volume;
                        if (nn.InNodes.Count == 1) // node has one source
                        {
                            if (!streams.ContainsKey(ToS(nn))) // Removing condition yields repetition of same path
                                    streams.Add(ToS(nn), new List<Node>(StreamNodes) { nn });
                            if (!oceannodes.Contains(nn))
                                    nextnodes.Enqueue(nn);
                            nn.Volume = EVAP * (nn.Volume + ToTile(nn).Moisture); // evaporate some volume downstram

                        }
                        else if (nn.InNodes.Count == 2) // node has two sources
                        {
                            if (!_2in.ContainsKey(ToS(nn)))
                                _2in.Add(ToS(nn), new List<Node>(StreamNodes) { nn });
                            else
                            {
                                if (LastNode.Volume > _2in[ToS(nn)].Last<Node>().Volume)
                                {
                                    streams.Add(ToS(nn), new List<Node>(StreamNodes) { nn });
                                }
                                else
                                {
                                    streams.Add(ToS(nn), new List<Node>(_2in[ToS(nn)]));
                                }
                                _2in.Remove(ToS(nn));
                                if (!oceannodes.Contains(nn)) 
                                        nextnodes.Enqueue(nn);
                                nn.Volume = EVAP * (nn.Volume + ToTile(nn).Moisture); //evaporate moisture downstream
                            }
                        }
                        
                    } else if (nn.Elevation < LastNode.Elevation)
                    {
                        if (nn.InNodes.Count == 0 && !streams.ContainsKey(ToS(nn)))
                        {
                            streams.Add(ToS(nn), new List<Node>() { nn });
                            if (!oceannodes.Contains(nn)) 
                                    nextnodes.Enqueue(nn);
                            nn.Volume = ToTile(nn).Moisture;

                        }
                    }
                }
            }
        }
        foreach (KeyValuePair<string, List<Node>> stream in streams)
        {
            Queue<Node> queuestream = new Queue<Node>(stream.Value);
            while (queuestream.Count != 0 && queuestream.Peek().Volume < RIVER_THRESHOLD)
            {
                queuestream.Dequeue();
            }
            if (queuestream.Count >= RIVER_LIMIT ||
                    (queuestream.Count > 1 && 
                    oceannodes.Contains(queuestream.Last<Node>())))
                rivers.Add(ToS(queuestream.Last<Node>()), queuestream);
        }
    }

        public List<Node> PathTo(Node n1, Node n2)
        {
            SortedList<double, List<Node>> paths = new SortedList<double, List<Node>>();
            List<Node> reachedtiles = new List<Node> { n1 };
            paths.Add(n1.edistance(n2), new List<Node> { n1 });

            while (!reachedtiles.Contains(n2))
            {
                if (paths.Count == 0) { return null; }

                List<Node> toppath = paths.Values[0];
                double key = paths.Keys[0];
                paths.RemoveAt(0);
                if (toppath.Last<Node>() == n2) break;

                foreach (Node n in this.NodeNeighbors(toppath.Last<Node>()))
                {
                    List<Node> path = new List<Node>(toppath) { n };
                    double cost = path.Count + n.edistance(n2) - 1;
                    if (!reachedtiles.Contains(n) || cost < key)
                    {
                        if (!reachedtiles.Contains(n))
                            reachedtiles.Add(n);
                        while (paths.ContainsKey(cost))
                        { cost += new Random().NextDouble() / 1000000d; }
                        paths.Add(cost, path);
                    }
                }
            }
            return paths.Values[0];
        }

        public List<Node> NodeNeighbors(Node n)
        {
            List<Node> neigbors = new List<Node>();
            List<string> indices = ToS(n.NeighborIndices());
            foreach (string index in indices) {
                if (nodes.ContainsKey(index))
                {
                    neigbors.Add(nodes[index]);
                }
            }
            return neigbors;
        }

        public List<Node> AdjacentNodes(Tile t)
        {
            List<Node> neighbors = new List<Node>();
            List<string> indices = ToS(t.AdjacentIndices());
            foreach (string index in indices)
            {
                if (nodes.ContainsKey(index))
                {
                    neighbors.Add(nodes[index]);
                }
            }
            return neighbors;
        }

        public enum PathType
        {
            Basic,
            Goods,
            Water,
            Ocean,
            Travel
        }

        public enum Biome
        {
            Water_Deep,
            Water_Shallow,
            Beach,
            Forest,
            Jungle,
            Plain,
            Tundra,
            Alpine,

        }


        public List<Tile> PathTo(Tile n1, Tile n2, PathType p)
        {
            SortedList<double, List<Tile>> paths =  new SortedList<double, List<Tile>>();
            List<Tile> reachedtiles = new List<Tile>{ n1 };
            paths.Add(n1.edistance(n2), new List<Tile>{ n1 });

            while (!reachedtiles.Contains(n2))
            {
                if (paths.Count == 0) { return null; }

                List<Tile> toppath = paths.Values[0];
                double key = paths.Keys[0];
                paths.RemoveAt(0);
                if (toppath.Last<Tile>() == n2) break;

                foreach (Tile t in this.TileNeighbors(toppath.Last<Tile>()))
                {
                    List<Tile> path = new List<Tile>(toppath) { t };
                    double cost = path.Count + t.edistance(n2) - 1;
                    if (
                        (
                        !reachedtiles.Contains(t) 
                        || cost < key) 
                        && t.ValidTile(p))
                    {
                        if (!reachedtiles.Contains(t)) 
                            reachedtiles.Add(t);
                        while (paths.ContainsKey(cost)) 
                            { cost += new Random().NextDouble() / 1000000d; }
                        paths.Add(cost, path);
                    }
                }  
            }
            return paths.Values[0];
        }

        public List<Tile> TileNeighbors(Tile t)
        {
            List<Tile> neighbors = new List<Tile>();
            List<string> indices = ToS(t.neighborindices());
            foreach (string index in indices)
            {
                if (tiles.ContainsKey(index))
                {
                    neighbors.Add(tiles[index]);
                }
            }
            return neighbors;
        }

        public IEnumerable<int> RRange(int q) 
        {
            if (q < 0)
            {
                return Enumerable.Range(-q - myRings, 2 * myRings + q + 1);
            } else {
                return Enumerable.Range(-myRings, 2 * myRings + 1 - q);
            }
        }

        public void Maketiles(){
            int posX, posY, posY0, posY1;
            float elevation, moisture, e;
            int BETA_SUM = 5;
            BetaDistribution bd;
            // Creates a "flat" grid with "pointy" tiles
            for ( int q = -myRings; q<= myRings; q++)
            {
                    foreach (int r in RRange(q))
                    {   //TODO: decide outer ring ocean buffer

                        posX = Xtranslate(q, r);
                        posY = Ytranslate(r);
                        int[] tilecoords = { q, r };
                        int[] coords = { posX, posY };

                        elevation = myNoisemap[posX, posY];
                        e = elevation / 256;
                        bd = new BetaDistribution(e * BETA_SUM, (1 - e) * BETA_SUM);
                        moisture = (float) bd.InverseDistributionFunction(moisturemap[posX, posY] / 256) * 256;
                        
                        Tile t = new Tile(elevation, coords, tilecoords, moisture, this);

                        tiles.Add(
                            ToS(q) + "," + ToS(r),
                            t);

                    if (elevation > SEA_LEVEL &&
                        (q < -myRings + SHALLOW_BUFFER ||
                        r > myRings - SHALLOW_BUFFER ||
                        r < -q - myRings + SHALLOW_BUFFER))
                    {
                        ringbuffer.Add(t);
                    }

                    if (r != -q - myRings && r != -myRings && q != myRings) // top left, top & top right
                        {
                            posY0 = Ytranslate(r - 2 / 3);
                            Node n = new Node(
                                myNoisemap[posX, posY0],
                                new int[] { posX, posY0 },
                                new int[] { q, r, 0 },
                                this);
                            nodes.Add(
                                ToS(q) + "," + ToS(r) + ",0",
                                n);
                        }

                        if (r != myRings && q != -myRings && r != myRings - q) // bottom & bottom left, bottom right
                        {
                            posY1 = Ytranslate(r + 2 / 3);
                        Node n = new Node(
                            myNoisemap[posX, posY1],
                            new int[] { posX, posY1 },
                            new int[] { q, r, 1 },
                            this);
                        nodes.Add(
                                ToS(q) + "," + ToS(r) + ",1",
                                 n);
                        }
                    }
                
            }

            SetRing();

            // testing operations to ensure channel between ends of board

            List<Tile> wt = PathTo(tiles["-20,00"],
                tiles["20,00"], PathType.Water);

            foreach (Tile t in wt)
            {
                Console.Out.WriteLine(t.Tilecoords[0] + "," + t.Tilecoords[1]);
            }

            
        }

        public void SetRing() {
            foreach (Tile t in ringbuffer) {
                if (t.Height < SEA_LEVEL)
                t.Height = .9f * SEA_LEVEL;
                foreach (String s in ToS(t.AdjacentIndices()))
                {
                    nodes[s].Elevation = .9f * SEA_LEVEL;
                }
            }
        }

        public int Ytranslate(double r)
        {
            return (int) (myHeight * (0.5 + (1.5 * r) / (3 * myRings + 1) * Math.Sqrt(3) / 2
                                                                            )); 
        }

        public int Xtranslate(double q, double r)
        {
            return (int) (myWidth * (0.5 + (Math.Sqrt(3) * q + Math.Sqrt(3)/2 * r) / (2 * Math.Sqrt(3) * myRings + 1))); 
        }

        public float GetNoiseValue(int x, int y)
        {
            return myNoisemap[x, y];
        }

        public string PToS(int x)
        {
            int maxdigits = (int)Math.Log10(maxDim) + 1;
            int digits = Math.Max(0, (int)Math.Log10(Math.Abs(x))) + 1;
            if (x >= 0)
            {
                return string.Concat(Enumerable.Repeat("0", maxdigits - digits)) + x;
            }
            else
            {
                return "-" + string.Concat(Enumerable.Repeat("0", maxdigits - digits)) + Math.Abs(x);
            }
        }

        public string ToS(int x)
        {
            int maxdigits = (int) Math.Log10(myRings) + 1;
            int digits = Math.Max(0, (int)Math.Log10(Math.Abs(x))) + 1;
            if (x >=0)
            {
                return string.Concat(Enumerable.Repeat("0", maxdigits - digits)) + x;
            } else {
                return "-" + string.Concat(Enumerable.Repeat("0", maxdigits - digits)) + Math.Abs(x);
            }
        }

        public string PToS(int[] x)
        {
            if (x.Length == 2)
            {
                return (PToS(x[0]) + "," + PToS(x[1]));
            }
            else { throw new IndexOutOfRangeException(); }
        }

        public string ToS(int[] x)
        {
            if (x.Length == 2)
            {
                return (ToS(x[0]) + "," + ToS(x[1]));
            }
            else if (x.Length == 3)
            {
                return (ToS(x[0]) + "," + ToS(x[1]) + "," + x[2]);
            }
            else { throw new IndexOutOfRangeException(); }
        }

        public string ToS(Node n)
        {
            return ToS(n.Tilecoords);
        }

        public string ToS(Tile t)
        {
            return ToS(t.Tilecoords);
        }

        public List<string> PToS(List<int[]> list)
        {
            List<string> strings = new List<string>(list.Count);
            foreach (int[] item in list)
            {
                strings.Add(PToS(item));
            }
            return strings;
        }

        public List<string> ToS(List<int[]> list)
        {
            List<string> strings = new List<string>(list.Count);
            foreach (int[] item in list)
            {
                strings.Add(ToS(item));
            }
            return strings;
        }

        public Tile ToTile(Node n)
        {
            return tiles[ToS(new int[] { n.Tilecoords[0], n.Tilecoords[1] })];
        }
    }
}
