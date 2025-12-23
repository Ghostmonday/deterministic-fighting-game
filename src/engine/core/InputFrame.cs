/* ================================================================================
   NEURAL DRAFT LLC | DEEPSEEK INSTRUCTION HEADER
================================================================================
   FILE:    InputFrame.cs
   CONTEXT: Blittable input frame structure for deterministic rollback netcode.

   TASK:
   Create a blittable struct 'InputFrame' containing input data for all players.
   Must be deterministic and suitable for network transmission.

   CONSTRAINTS:
   - Must be blittable (byte/int only, no arrays or reference types)
   - Use fixed-size fields for determinism
   - No Unity Engine references in this file
   - Strict Determinism: No floats, no random execution order
================================================================================

*/
namespace NeuralDraft
{
    /// <summary>
    /// Blittable input frame structure containing input bits for all players.
    /// Size: 4 bytes (2 players * 2 bytes each)
    /// </summary>
    public struct InputFrame
    {
        /// <summary>
        /// Input bits for player 0 (16 bits = ushort)
        /// </summary>
        public ushort player0Inputs;

        /// <summary>
        /// Input bits for player 1 (16 bits = ushort)
        /// </summary>
        public ushort player1Inputs;

        /// <summary>
        /// Frame number this input corresponds to
        /// </summary>
        public int frameNumber;

        /// <summary>
        /// Creates a new InputFrame with specified inputs
        /// </summary>
        public InputFrame(int frameNumber, ushort p0Inputs, ushort p1Inputs)
        {
            this.frameNumber = frameNumber;
            this.player0Inputs = p0Inputs;
            this.player1Inputs = p1Inputs;
        }

        /// <summary>
        /// Gets input bits for the specified player index
        /// </summary>
        public ushort GetPlayerInputs(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= GameState.MAX_PLAYERS)
                throw new ArgumentOutOfRangeException(nameof(playerIndex), $"Player index must be 0 or 1, got {playerIndex}");

            return playerIndex == 0 ? player0Inputs : player1Inputs;
        }

        /// <summary>
        /// Sets input bits for the specified player index
        /// </summary>
        public void SetPlayerInputs(int playerIndex, ushort inputs)
        {
            if (playerIndex < 0 || playerIndex >= GameState.MAX_PLAYERS)
                throw new ArgumentOutOfRangeException(nameof(playerIndex), $"Player index must be 0 or 1, got {playerIndex}");

            if (playerIndex == 0)
                player0Inputs = inputs;
            else
                player1Inputs = inputs;
        }

        /// <summary>
        /// Creates an empty input frame (no inputs pressed)
        /// </summary>
        public static InputFrame Empty(int frameNumber)
        {
            return new InputFrame(frameNumber, 0, 0);
        }

        /// <summary>
        /// Creates an input frame from byte array (network deserialization)
        /// </summary>
        public static InputFrame FromBytes(byte[] data, int offset)
        {
            // Validate input parameters
            if (data == null)
                throw new ArgumentNullException(nameof(data), "Data array cannot be null");

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), "Offset cannot be negative");

            if (offset + 8 > data.Length)
                throw new ArgumentException($"Invalid offset: need 8 bytes but only {data.Length - offset} available", nameof(offset));

            return new InputFrame(
                frameNumber: (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3],
                p0Inputs: (ushort)((data[offset + 4] << 8) | data[offset + 5]),
                p1Inputs: (ushort)((data[offset + 6] << 8) | data[offset + 7])
            );
        }

        /// <summary>
        /// Converts to byte array for network transmission
        /// </summary>
        public byte[] ToBytes()
        {
            byte[] data = new byte[8]; // 4 bytes frameNumber + 2 bytes p0 + 2 bytes p1

            data[0] = (byte)((frameNumber >> 24) & 0xFF);
            data[1] = (byte)((frameNumber >> 16) & 0xFF);
            data[2] = (byte)((frameNumber >> 8) & 0xFF);
            data[3] = (byte)(frameNumber & 0xFF);

            data[4] = (byte)((player0Inputs >> 8) & 0xFF);
            data[5] = (byte)(player0Inputs & 0xFF);

            data[6] = (byte)((player1Inputs >> 8) & 0xFF);
            data[7] = (byte)(player1Inputs & 0xFF);

            return data;
        }

        /// <summary>
        /// Checks if this input frame is equal to another (for rollback detection)
        /// </summary>
        public bool Equals(InputFrame other)
        {
            return frameNumber == other.frameNumber &&
                   player0Inputs == other.player0Inputs &&
                   player1Inputs == other.player1Inputs;
        }

        /// <summary>
        /// Creates a copy of this input frame
        /// </summary>
        public InputFrame Copy()
        {
            return new InputFrame(frameNumber, player0Inputs, player1Inputs);
        }
    }
}
