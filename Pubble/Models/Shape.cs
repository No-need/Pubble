using Microsoft.CodeAnalysis.Elfie.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Drawing;

namespace Pubble.Models
{

    public enum ShapeType
    {
        Ball,
    }

    public class Shape 
    {
        public double X { get; set; } 

        public double Y { get; set; } 

        public virtual ShapeType Type { get; }

        public int Speed { get; set; }

        public string Color { get; set; }

        public int Direction { get; set; }
    }

    public class Ball : Shape
    {
        public override ShapeType Type { get; } = ShapeType.Ball;

        public int Radius { get; set; } = 25;
    }
}
