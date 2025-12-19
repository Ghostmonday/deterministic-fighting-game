/* ================================================================================
   NEURAL DRAFT LLC | DEEPSEEK INSTRUCTION HEADER
================================================================================
   FILE:    RollbackController.cs
   CONTEXT: Prediction & rollback.

   TASK:
   Implement RollbackController using Ring Buffers. Logic: Snapshot State -> Tick Prediction -> OnRemoteInput -> Rollback -> Resimulate.

   CONSTRAINTS:
   - Use Fixed-Point Math (Fx.SCALE = 1000) for all physics.
   - No Unity Engine references in this file (unless specified in Bridge).
   - Strict Determinism: No floats, no random execution order.
================================================================================

*/
namespace NeuralDraft
{
    public class RollbackController
    {
        private const int MAX_ROLLBACK_FRAMES = 120;
        private const int INPUT_BUFFER_SIZE = 8;

        private GameState[] stateBuffer;
        private byte[][] inputBuffer;
        private int currentFrame;
        private int confirmedFrame;

        public RollbackController()
        {
            stateBuffer = new GameState[MAX_ROLLBACK_FRAMES];
            inputBuffer = new byte[MAX_ROLLBACK_FRAMES][];

            for (int i = 0; i < MAX_ROLLBACK_FRAMES; i++)
            {
                stateBuffer[i] = new GameState();
                inputBuffer[i] = new byte[GameState.MAX_PLAYERS * INPUT_BUFFER_SIZE];
            }

            currentFrame = 0;
            confirmedFrame = -1;
        }

        public void SaveState(int frame)
        {
            int bufferIndex = frame % MAX_ROLLBACK_FRAMES;
            stateBuffer[bufferIndex].frameIndex = frame;

            // Deep copy current state
            if (frame > 0)
            {
                int prevIndex = (frame - 1) % MAX_ROLLBACK_FRAMES;
                stateBuffer[prevIndex].CopyTo(stateBuffer[bufferIndex]);
            }
        }

        public void SaveInputs(int frame, byte[] inputs, int playerIndex)
        {
            int bufferIndex = frame % MAX_ROLLBACK_FRAMES;
            int inputOffset = playerIndex * INPUT_BUFFER_SIZE;

            for (int i = 0; i < INPUT_BUFFER_SIZE; i++)
            {
                inputBuffer[bufferIndex][inputOffset + i] = inputs[i];
            }
        }

        public byte[] GetInputs(int frame, int playerIndex)
        {
            int bufferIndex = frame % MAX_ROLLBACK_FRAMES;
            int inputOffset = playerIndex * INPUT_BUFFER_SIZE;
            byte[] result = new byte[INPUT_BUFFER_SIZE];

            for (int i = 0; i < INPUT_BUFFER_SIZE; i++)
            {
                result[i] = inputBuffer[bufferIndex][inputOffset + i];
            }

            return result;
        }

        public GameState GetState(int frame)
        {
            int bufferIndex = frame % MAX_ROLLBACK_FRAMES;
            return stateBuffer[bufferIndex];
        }

        public void OnRemoteInput(int frame, byte[] inputs, int playerIndex)
        {
            // Check if we need to rollback
            if (frame < currentFrame)
            {
                RollbackToFrame(frame);
            }

            // Save the remote inputs
            SaveInputs(frame, inputs, playerIndex);

            // If we rolled back, resimulate from that frame
            if (frame < currentFrame)
            {
                ResimulateFromFrame(frame);
            }
        }

        private void RollbackToFrame(int targetFrame)
        {
            // Rollback to the target frame
            int bufferIndex = targetFrame % MAX_ROLLBACK_FRAMES;
            stateBuffer[bufferIndex].CopyTo(stateBuffer[currentFrame % MAX_ROLLBACK_FRAMES]);
            currentFrame = targetFrame;
        }

        private void ResimulateFromFrame(int startFrame)
        {
            // Resimulate from startFrame to currentFrame
            for (int frame = startFrame; frame <= currentFrame; frame++)
            {
                SimulateFrame(frame);
            }
        }

        public void TickPrediction()
        {
            currentFrame++;
            SaveState(currentFrame);
            SimulateFrame(currentFrame);
        }

        private void SimulateFrame(int frame)
        {
            // Get the state for this frame
            GameState state = GetState(frame);

            // Apply inputs for each player
            for (int playerIndex = 0; playerIndex < GameState.MAX_PLAYERS; playerIndex++)
            {
                byte[] inputs = GetInputs(frame, playerIndex);
                ApplyPlayerInputs(ref state.players[playerIndex], inputs);
            }

            // Apply physics
            for (int playerIndex = 0; playerIndex < GameState.MAX_PLAYERS; playerIndex++)
            {
                PhysicsSystem.ApplyGravity(ref state.players[playerIndex]);
                // Note: MapData would need to be passed in - this is simplified
            }

            // Update projectiles
            // Note: MapData would need to be passed in - this is simplified

            // Save the updated state
            int bufferIndex = frame % MAX_ROLLBACK_FRAMES;
            state.CopyTo(stateBuffer[bufferIndex]);
        }

        private void ApplyPlayerInputs(ref PlayerState player, byte[] inputs)
        {
            // Simplified input parsing
            // In a real implementation, this would parse the byte array into specific actions
            int inputX = 0;
            bool jumpPressed = false;

            if (inputs.Length > 0)
            {
                // Simple interpretation: first byte for horizontal, second for jump
                inputX = inputs[0] - 127; // Convert to signed
                jumpPressed = inputs[1] > 0;
            }

            // Apply movement (grounded check would need actual collision detection)
            bool grounded = player.grounded > 0;
            PhysicsSystem.ApplyMovementInput(ref player, inputX, jumpPressed, grounded);
        }

        public int GetCurrentFrame()
        {
            return currentFrame;
        }

        public void ConfirmFrame(int frame)
        {
            confirmedFrame = frame;

            // Clean up old states (optional optimization)
            if (frame > MAX_ROLLBACK_FRAMES)
            {
                int oldestFrame = frame - MAX_ROLLBACK_FRAMES;
                // Clear inputs for frames we no longer need
                int bufferIndex = oldestFrame % MAX_ROLLBACK_FRAMES;
                for (int i = 0; i < inputBuffer[bufferIndex].Length; i++)
                {
                    inputBuffer[bufferIndex][i] = 0;
                }
            }
        }
    }
}
