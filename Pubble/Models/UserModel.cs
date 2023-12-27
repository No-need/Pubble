namespace Pubble.Models
{
    public class User
    {
        public Ball Ball { get; set; }

        public string ConnectionId { get; set; }

        public string Name { get; set; }

        public string Color { get; set; } = "black";

        public int Speed { get; set; } = 5;
    }
}
