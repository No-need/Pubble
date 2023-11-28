namespace Pubble.Models
{
    public class Game
    {
        public int Width { get; set; }

        public int Height { get; set; }
        
        public Game() {
            Width = 800;
            Height = 600;          
        }
    }
}
