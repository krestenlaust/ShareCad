using System.Windows;

namespace Networking.Models
{
    public abstract class RegionDto
    {
        public int ID { get; set; }
        public Point GridPosition { get; set; }
    }
}
