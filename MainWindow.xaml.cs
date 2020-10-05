using System.Windows;

namespace Hexalia.v2
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            BoardStart();
        }

        public void BoardStart()
        {
            int width = 800;    // (int)System.Windows.SystemParameters.MaximizedPrimaryScreenWidth;
            int height = 800;   // (int)System.Windows.SystemParameters.MaximizedPrimaryScreenHeight;
            // Builds initial game state of board
            Board b = new Board(20, width, height);
            /// Builds experimental display window for heightmap
            WBMDemo wbm = new WBMDemo(width, height, b, this);
            
        }
    }
}
