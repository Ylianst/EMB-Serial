using System.IO;

namespace EmbroideryCommunicator
{
    /// <summary>
    /// Parser for .EXP embroidery file format (Melco/Bravo systems)
    /// </summary>
    public class ExpFileParser
    {
        /// <summary>
        /// Parses an .EXP file and returns the embroidery pattern
        /// </summary>
        /// <param name="filePath">Path to the .exp file</param>
        /// <returns>Parsed embroidery pattern</returns>
        public static EmbroideryPattern Parse(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("EXP file not found", filePath);

            var pattern = new EmbroideryPattern
            {
                FileName = Path.GetFileName(filePath)
            };

            // Current position (cumulative)
            float currentX = 0;
            float currentY = 0;

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                // Track if we're in a jump sequence
                bool inJump = false;

                while (reader.BaseStream.Position < reader.BaseStream.Length - 1)
                {
                    byte byte1 = reader.ReadByte();
                    byte byte2 = reader.ReadByte();

                    // Check for special commands (always start with 128)
                    if (byte1 == 128)
                    {
                        if (byte2 == 1)
                        {
                            // Color change / stop
                            pattern.Stitches.Add(new StitchPoint(currentX, currentY, StitchType.ColorChange));
                            inJump = false;
                        }
                        else if (byte2 == 4)
                        {
                            // Jump command - next coordinate will be a jump
                            inJump = true;
                        }
                        else if (byte2 == 128)
                        {
                            // End / cut thread
                            pattern.Stitches.Add(new StitchPoint(currentX, currentY, StitchType.End));
                            inJump = false;
                        }
                        else
                        {
                            // Unknown command
                        }
                        continue;
                    }

                    // Regular movement command
                    float deltaX = DecodeMovement(byte1);
                    float deltaY = DecodeMovement(byte2);

                    // Skip (0,0) movements unless they follow a special command
                    if (deltaX == 0 && deltaY == 0)
                    {
                        // (0,0) after a special command establishes the origin
                        if (pattern.Stitches.Count > 0)
                        {
                            var lastStitch = pattern.Stitches[pattern.Stitches.Count - 1];
                            if (lastStitch.Type == StitchType.ColorChange || lastStitch.Type == StitchType.End)
                            {
                                // Add a normal stitch at current position to start new segment
                                pattern.Stitches.Add(new StitchPoint(currentX, currentY, StitchType.Normal));
                            }
                        }
                        inJump = false;
                        continue;
                    }

                    // Update current position
                    currentX += deltaX;
                    currentY += deltaY;

                    // Add stitch point
                    StitchType type = inJump ? StitchType.Jump : StitchType.Normal;
                    pattern.Stitches.Add(new StitchPoint(currentX, currentY, type));

                    // Reset jump flag after processing the jump coordinate
                    if (inJump)
                    {
                        inJump = false;
                    }
                }
            }

            return pattern;
        }

        /// <summary>
        /// Decodes a single byte movement value
        /// </summary>
        /// <param name="value">Byte value to decode</param>
        /// <returns>Movement in 0.1mm units (positive or negative)</returns>
        private static float DecodeMovement(byte value)
        {
            // 0 = no movement
            if (value == 0)
                return 0;

            // Values 1-127 are positive movements
            if (value < 128)
                return value;

            // Values 128-255 are negative movements
            // Formula: value - 256
            return value - 256;
        }

        /// <summary>
        /// Validates if a file appears to be a valid .EXP file
        /// </summary>
        /// <param name="filePath">Path to check</param>
        /// <returns>True if file appears valid</returns>
        public static bool IsValidExpFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                // Check file extension
                if (!Path.GetExtension(filePath).Equals(".exp", StringComparison.OrdinalIgnoreCase))
                    return false;

                // Check if file has content and byte count is even (pairs of bytes)
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length == 0 || fileInfo.Length % 2 != 0)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Generates a 72x64 black and white preview image of an EXP file
        /// </summary>
        /// <param name="expFileData">The .exp file data as byte array</param>
        /// <returns>Byte array containing 1-bit per pixel image data (72x64 = 576 bytes)</returns>
        public static byte[] GeneratePreviewImage(byte[] expFileData)
        {
            const int previewWidth = 72;
            const int previewHeight = 64;
            const int totalBytes = (previewWidth * previewHeight) / 8; // 1 bit per pixel = 576 bytes
            
            byte[] imageData = new byte[totalBytes];
            
            try
            {
                // Parse the file to get stitch segments (excluding jumps)
                // Each segment is a list of connected points (no jumps between them)
                var segments = new List<List<(float x, float y)>>();
                var currentSegment = new List<(float x, float y)>();
                
                float currentX = 0;
                float currentY = 0;
                bool inJump = false;
                bool hadJump = false;
                
                for (int i = 0; i < expFileData.Length - 1; i += 2)
                {
                    byte byte1 = expFileData[i];
                    byte byte2 = expFileData[i + 1];
                    
                    // Check for special commands
                    if (byte1 == 128)
                    {
                        if (byte2 == 4)
                        {
                            // Jump command - next coordinate will be a jump, skip it
                            inJump = true;
                            hadJump = true;
                        }
                        else if (byte2 == 1 || byte2 == 128)
                        {
                            // Color change or end - not a jump
                            inJump = false;
                        }
                        continue;
                    }
                    
                    // Decode movement
                    float deltaX = DecodeMovement(byte1);
                    float deltaY = DecodeMovement(byte2);
                    
                    // Skip (0,0) movements
                    if (deltaX == 0 && deltaY == 0)
                    {
                        inJump = false;
                        continue;
                    }
                    
                    // Update position
                    currentX += deltaX;
                    currentY += deltaY;
                    
                    // Handle jumps and segments
                    if (inJump)
                    {
                        // We're in a jump, skip this point and start a new segment after
                        inJump = false;
                    }
                    else
                    {
                        // If we just had a jump, start a new segment
                        if (hadJump && currentSegment.Count > 0)
                        {
                            segments.Add(currentSegment);
                            currentSegment = new List<(float x, float y)>();
                            hadJump = false;
                        }
                        
                        // Add point to current segment
                        currentSegment.Add((currentX, currentY));
                    }
                }
                
                // Add the last segment if it has points
                if (currentSegment.Count > 0)
                {
                    segments.Add(currentSegment);
                }
                
                // Check if we have any segments
                if (segments.Count == 0 || segments.All(s => s.Count == 0))
                {
                    return imageData; // Return empty image if no stitches
                }
                
                // Flatten all segments to find bounds
                var allPoints = segments.SelectMany(seg => seg).ToList();
                
                // Find bounds
                float minX = allPoints.Min(s => s.x);
                float maxX = allPoints.Max(s => s.x);
                float minY = allPoints.Min(s => s.y);
                float maxY = allPoints.Max(s => s.y);
                
                float width = maxX - minX;
                float height = maxY - minY;
                
                if (width <= 0 || height <= 0)
                {
                    return imageData; // Return empty image if invalid bounds
                }
                
                // Calculate scale to fit in preview with margin
                float marginPixels = 4; // 4 pixel margin
                float availableWidth = previewWidth - (2 * marginPixels);
                float availableHeight = previewHeight - (2 * marginPixels);
                
                float scaleX = availableWidth / width;
                float scaleY = availableHeight / height;
                float scale = Math.Min(scaleX, scaleY);
                
                // Calculate centering offset
                float centerOffsetX = (previewWidth - (width * scale)) / 2;
                float centerOffsetY = (previewHeight - (height * scale)) / 2;
                
                // Create a bitmap to render stitches
                bool[,] pixels = new bool[previewWidth, previewHeight];
                
                // Draw lines within each segment (no lines between segments)
                foreach (var segment in segments)
                {
                    for (int i = 1; i < segment.Count; i++)
                    {
                        var prev = segment[i - 1];
                        var curr = segment[i];
                        
                        // Transform coordinates to preview space
                        // Flip Y-axis by subtracting from height
                        int x1 = (int)((prev.x - minX) * scale + centerOffsetX);
                        int y1 = (int)(previewHeight - ((prev.y - minY) * scale + centerOffsetY));
                        int x2 = (int)((curr.x - minX) * scale + centerOffsetX);
                        int y2 = (int)(previewHeight - ((curr.y - minY) * scale + centerOffsetY));
                        
                        // Draw line using Bresenham's algorithm
                        DrawLine(pixels, x1, y1, x2, y2, previewWidth, previewHeight);
                    }
                }
                
                // Convert bool array to 1-bit per pixel byte array
                // Bit 7 (MSB) is leftmost pixel, bit 0 is rightmost pixel in each byte
                for (int y = 0; y < previewHeight; y++)
                {
                    for (int x = 0; x < previewWidth; x++)
                    {
                        if (pixels[x, y])
                        {
                            int byteIndex = (y * previewWidth + x) / 8;
                            int bitIndex = 7 - ((y * previewWidth + x) % 8);
                            imageData[byteIndex] |= (byte)(1 << bitIndex);
                        }
                    }
                }
            }
            catch
            {
                // Return empty image on any error
                return new byte[totalBytes];
            }
            
            return imageData;
        }
        
        /// <summary>
        /// Draws a line on a boolean pixel array using Bresenham's line algorithm
        /// </summary>
        private static void DrawLine(bool[,] pixels, int x1, int y1, int x2, int y2, int width, int height)
        {
            int dx = Math.Abs(x2 - x1);
            int dy = Math.Abs(y2 - y1);
            int sx = x1 < x2 ? 1 : -1;
            int sy = y1 < y2 ? 1 : -1;
            int err = dx - dy;
            
            while (true)
            {
                // Set pixel if within bounds
                if (x1 >= 0 && x1 < width && y1 >= 0 && y1 < height)
                {
                    pixels[x1, y1] = true;
                }
                
                if (x1 == x2 && y1 == y2)
                    break;
                
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x1 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y1 += sy;
                }
            }
        }
    }
}
