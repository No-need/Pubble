using Microsoft.CodeAnalysis.Elfie.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Pubble.Models
{

    public enum ShapeType
    {
        Ball,
    }

    public class Shape 
    {
        public int X { get; set; } 

        public int Y { get; set; } 

        public virtual ShapeType Type { get; }
    }

    public class Ball : Shape
    {
        public override ShapeType Type { get; } = ShapeType.Ball;

        public int Radius { get; set; } = 25;
    }
}
