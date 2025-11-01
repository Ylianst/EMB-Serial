using System.Drawing;

namespace EmbroideryCommunicator
{
    /// <summary>
    /// Represents the type of stitch command in an embroidery file
    /// </summary>
    public enum StitchType
    {
        Normal,      // Regular stitch with thread
        Jump,        // Move without stitching
        ColorChange, // Stop for color change
        End          // End of pattern / cut thread
    }

    /// <summary>
    /// Represents a single stitch point in an embroidery pattern
    /// </summary>
    public class StitchPoint
    {
        /// <summary>
        /// X coordinate in 0.1mm units
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// Y coordinate in 0.1mm units
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// Type of stitch
        /// </summary>
        public StitchType Type { get; set; }

        public StitchPoint(float x, float y, StitchType type)
        {
            X = x;
            Y = y;
            Type = type;
        }
    }

    /// <summary>
    /// Contains all the parsed data from an embroidery file
    /// </summary>
    public class EmbroideryPattern
    {
        public List<StitchPoint> Stitches { get; set; } = new List<StitchPoint>();
        
        public string FileName { get; set; } = string.Empty;
        
        public int TotalStitches => Stitches.Count(s => s.Type == StitchType.Normal);
        
        public int JumpCount => Stitches.Count(s => s.Type == StitchType.Jump);
        
        public int ColorChangeCount => Stitches.Count(s => s.Type == StitchType.ColorChange);

        /// <summary>
        /// Gets the bounding box of the pattern in 0.1mm units
        /// </summary>
        public RectangleF GetBounds()
        {
            if (Stitches.Count == 0)
                return RectangleF.Empty;

            float minX = Stitches.Min(s => s.X);
            float maxX = Stitches.Max(s => s.X);
            float minY = Stitches.Min(s => s.Y);
            float maxY = Stitches.Max(s => s.Y);

            return new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// Gets the pattern dimensions in millimeters
        /// </summary>
        public SizeF GetDimensionsMm()
        {
            var bounds = GetBounds();
            return new SizeF(bounds.Width / 10f, bounds.Height / 10f);
        }
    }
}
