/* This class contains helper methods for 2d Float Arrays
 * AddTDFA: adds two TDFAs
 * MulTDFA: multiplies each item in the TDFA by a factor
 * CenterFactor: Gives pythagorean distance of a 2d index from center of array as displayed
 * Islandshape0_25to50_100: Reshapes noise array between range of values determined by
 * linear lower and functions
 * @Author: Andrew Feikema
 * @Date: May, 07, 2020
*/
using System;
using System.Linq;

namespace Hexalia.v2
{
    static class TDFA
    {
        // multiplies enitre array by float m
        static public float[,] MulTDFA(float m, float[,] fa)
        {
            for (int i = 0; i < fa.GetLength(0); i++)
            {
                for (int j = 0; j < fa.GetLength(1); j++)
                {
                    fa[i, j] *= m;
                }
            }
            return fa;
        }

        //adds two TDFAs of same dimensions
        static public float[,] AddTDFA( float[,] a1, float[,] a2)
        {
            if (a1.GetLength(0) != a2.GetLength(0))
            {
                throw new IndexOutOfRangeException("Lengths do not match for dimension 0");
            }
            if (a1.GetLength(1) != a2.GetLength(1))
            {
                throw new IndexOutOfRangeException("Lengths do not match for dimension 1");
            }
            float[,] a3 = new float[a1.GetLength(0), a1.GetLength(1)];
            for (int i = 0; i < a1.GetLength(0); i++)
            {
                for (int j = 0; j < a1.GetLength(1); j++)
                {
                    a3[i, j] = a1[i, j] + a2[i, j];
                }
            }
            return a3;
        }

        /* Finds pythagorean distance of each 2d index from the center.
        * Distance is used to reshape each value of a new array
        * with the islandshape() function
        */
        static public float[,] CenterFactor(float[,] fa)
        {
            float[,] fb = new float[fa.GetLength(0), fa.GetLength(1)];
            float d;

            foreach (int xval in Enumerable.Range(0, fa.GetLength(0)))
            {
                foreach (int yval in Enumerable.Range(0, fa.GetLength(1)))
                {
                    // relative distance between 0 and 1
                    d = (float) Math.Sqrt(2 * (
                                Math.Pow(Math.Abs((float)xval / fa.GetLength(0) - .5), 2)
                                +
                                Math.Pow(Math.Abs((float)yval / fa.GetLength(1) - .5), 2)
                                ));
                    fb[xval, yval] = islandshape0_25to50_100(d, fa[xval, yval]);
                }
            }
            return fb;
        }

        /* Reshapes each value in array within range of upper and lower functions
         * Inputs:
         *      d: relative distance between 0 and 1
         *      e: elevation (divided by 256 for value between 0 and 1)
         * Lower function: .5 - .5 * d
         * Upper function: 1 - .75 * d
         * elevation = l(d) + e( u(d) - l(d) )
         * Returns: float value between 0 and 1
         */
        static public float islandshape0_25to50_100(float d, float e)
        {
            return
            256f * 
            (.5f -
            .5f * d +
            (e / 256f) * .5f -
            .25f * d * (e / 256f));
        }
    }
}
