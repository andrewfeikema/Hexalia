/* WBMDemo creates a WritableBitmap interface for displaying 
 * heightmaps created by noise funcitons stored in two-dimentional
 * float arrays.
 * @Author: Andrew Feikema 
 * @Date: May 7, 2020
 */

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;


namespace Hexalia.v2
{
    class WBMDemo
    {
        static WriteableBitmap writeableBitmap;
        static Window w;
        static Image i;
        private static int myWidth, myHeight;
        private Point clickedpos;
        private static Board board;
        static private Int32[] heightcolors = new Int32[8] {    //color values for height bands
                                                        0xFF0000, 
                                                        0xFFA500, 
                                                        0xFFFF00, 
                                                        0x008000, 
                                                        0x0000FF, 
                                                        0x4B0082, 
                                                        0xEE82EE, 
                                                        0x800000};

        
        public WBMDemo(int width, int height, Board b, MainWindow w)
        {
            myWidth = width;
            myHeight = height;
            board = b;

            i = new Image();
            RenderOptions.SetBitmapScalingMode(i, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetEdgeMode(i, EdgeMode.Aliased);


            w.Height = height + 37;
            w.Width = width + 14;
            w.Content = i;  // window's content is image
            w.Title = "Heightmap Display";


            // Bitmap composed of width x height pixels
            writeableBitmap = new WriteableBitmap( (int)width, (int)height, 96, 96, PixelFormats.Bgr24, null);

            // Image source is bitmap
            i.Source = writeableBitmap;

            i.Stretch = Stretch.None;
            i.HorizontalAlignment = HorizontalAlignment.Left;
            i.VerticalAlignment = VerticalAlignment.Top;

            //Mouse events defined by functions
            i.MouseMove += new MouseEventHandler(i_MouseMove);
            i.MouseLeftButtonDown +=
                new MouseButtonEventHandler(i_MouseLeftButtonDown);
            i.MouseLeftButtonUp +=
                new MouseButtonEventHandler(i_MouseLeftButtonUp);
            i.MouseRightButtonDown +=
                new MouseButtonEventHandler(i_MouseRightButtonDown);
            w.MouseWheel += new MouseWheelEventHandler(w_MouseWheel);

            // draw myNoiseMap on image
            DrawAllPixels();

            w.Activate();
        }

        // The DrawPixel method updates the WriteableBitmap by using
        // unsafe code to write a pixel into the back buffer.
        static void DrawAllPixels()
        {
            IntPtr pBackBuffer;
            foreach (int x in Enumerable.Range(0, myWidth))
            {
                foreach(int y in Enumerable.Range(0, myHeight))
                {
                    try
                    {
                        // Reserve the back buffer for updates.
                        writeableBitmap.Lock();

                        unsafe
                        {
                            // Get a pointer to the back buffer.
                            pBackBuffer = writeableBitmap.BackBuffer;

                            // Find the address of the pixel to draw.
                            pBackBuffer += x * 3;
                            pBackBuffer += y * writeableBitmap.BackBufferStride;

                            // Compute the pixel's color.
                            int color_data = (int) board.GetNoiseValue(x, y); 

                            // Assign the color data to the pixel.
                            if ((color_data % 16) == 0) 
                            {   // grayscale value based on height
                                *((int*)pBackBuffer) = heightcolors[(color_data / 16) % 8];
                            }  
                            else if (    // Hexagon shape
                                        ((Math.Abs(y-400) < 346) && 
                                        (Math.Abs(x-400)==(int)(400 - Math.Abs(y-400)/Math.Sqrt(3)))
                                        ) || (
                                        Math.Abs(y-400) == 346 && 
                                        Math.Abs(x-400) <= 200)
                                        ) 
                            { *((int*)pBackBuffer) = 0; } 
                            else 
                            {   // color rings
                                *((int*)pBackBuffer) = color_data | color_data << 8 | color_data << 16;
                            }
                        }

                        // Specify the area of the bitmap that changed.
                        writeableBitmap.AddDirtyRect(new Int32Rect(x, y, 1, 1));
                    }
                    finally
                    {
                        // Release the back buffer and make it available for display.
                        writeableBitmap.Unlock();
                    }
                }
            }
        }

        static void DrawLines()
        {

        }
        
        static void DrawPixel(MouseEventArgs e)
        {
            int column = (int)e.GetPosition(i).X;
            int row = (int)e.GetPosition(i).Y;

            try
            {
                // Reserve the back buffer for updates.
                writeableBitmap.Lock();

                unsafe
                {
                    // Get a pointer to the back buffer.
                    IntPtr pBackBuffer = writeableBitmap.BackBuffer;

                    // Find the address of the pixel to draw.
                    pBackBuffer += row * writeableBitmap.BackBufferStride;
                    pBackBuffer += column * 3;

                    // Compute the pixel's color.
                    int color_data = 0xFF0000;

                    // Assign the color data to the pixel.
                    *((int*)pBackBuffer) = color_data | color_data << 8 | color_data << 16;
                }

                // Specify the area of the bitmap that changed.
                writeableBitmap.AddDirtyRect(new Int32Rect(column, row, 1, 1));
            }
            finally
            {
                // Release the back buffer and make it available for display.
                writeableBitmap.Unlock();
            }
        }

        static void ErasePixel(MouseEventArgs e)
        {
            byte[] ColorData = { 0, 0, 0, 0 }; // B G R

            Int32Rect rect = new Int32Rect(
                    (int)(e.GetPosition(i).X),
                    (int)(e.GetPosition(i).Y),
                    1,
                    1);

            writeableBitmap.WritePixels(rect, ColorData, 4, 0);
        }

        public void i_MouseRightButtonDown(object sender, MouseButtonEventArgs e) {;}


        public void i_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            clickedpos = e.GetPosition(i);
            Console.WriteLine(clickedpos.X + " " + clickedpos.Y);
        }

        // release of left button moves image
        public void i_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Matrix m = i.RenderTransform.Value;
            m.Translate(e.GetPosition(i).X - clickedpos.X, e.GetPosition(i).Y - clickedpos.Y);
            i.RenderTransform = new MatrixTransform(m);
        }

        static void i_MouseMove(object sender, MouseEventArgs e)
        {

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                ;
            }
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                ;
            }
        }

        // Zooms in/out of image
        static void w_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            System.Windows.Media.Matrix m = i.RenderTransform.Value;

            if (e.Delta > 0)
            {
                m.ScaleAt(
                    1.5,
                    1.5,
                    e.GetPosition(w).X,
                    e.GetPosition(w).Y);
            }
            else
            {
                m.ScaleAt(
                    1.0 / 1.5,
                    1.0 / 1.5,
                    e.GetPosition(w).X,
                    e.GetPosition(w).Y);
            }

            i.RenderTransform = new MatrixTransform(m);
        }
    }
}