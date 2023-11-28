namespace Pubble.Models
{
    public class User
    {
        public Shape Shape { get; set; }

        public string ConnectionId { get; set; }

        public string Name { get; set; }

        public string Color { get; set; } = "black";

        public int Speed { get; set; } = 2;
    }
}
