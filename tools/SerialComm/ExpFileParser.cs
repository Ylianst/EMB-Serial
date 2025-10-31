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
    }
}
