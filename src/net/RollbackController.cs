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

        private GameState[] stateBuffer;
        private InputFrame[] inputBuffer;
        private int currentFrame;
        private int confirmedFrame;
        private MapData mapData;
        private CharacterDef[] characterDefs;
        private bool isDevelopment;

        public RollbackController(MapData map, CharacterDef[] characterDefs, bool isDevelopment = true)
        {
            stateBuffer = new GameState[MAX_ROLLBACK_FRAMES];
            inputBuffer = new InputFrame[MAX_ROLLBACK_FRAMES];

            for (int i = 0; i < MAX_ROLLBACK_FRAMES; i++)
            {
                stateBuffer[i] = new GameState();
                inputBuffer[i] = InputFrame.Empty(i);
            }

            currentFrame = 0;
            confirmedFrame = -1;
            mapData = map;
            this.characterDefs = characterDefs;
            this.isDevelopment = isDevelopment;
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

        public void SaveInputs(int frame, InputFrame inputs)
        {
            int bufferIndex = frame % MAX_ROLLBACK_FRAMES;
            inputBuffer[bufferIndex] = inputs;
        }

        public void SavePlayerInputs(int frame, ushort inputs, int playerIndex)
        {
            int bufferIndex = frame % MAX_ROLLBACK_FRAMES;
            InputFrame current = inputBuffer[bufferIndex];
            current.SetPlayerInputs(playerIndex, inputs);
            inputBuffer[bufferIndex] = current;
        }

        public InputFrame GetInputs(int frame)
        {
            int bufferIndex = frame % MAX_ROLLBACK_FRAMES;
            return inputBuffer[bufferIndex];
        }

        public ushort GetPlayerInputs(int frame, int playerIndex)
        {
            int bufferIndex = frame % MAX_ROLLBACK_FRAMES;
            return inputBuffer[bufferIndex].GetPlayerInputs(playerIndex);
        }

        public GameState GetState(int frame)
        {
            int bufferIndex = frame % MAX_ROLLBACK_FRAMES;
            return stateBuffer[bufferIndex];
        }

        public void OnRemoteInput(int frame, InputFrame inputs)
        {
            // Check if we need to rollback
            if (frame < currentFrame)
            {
                RollbackToFrame(frame);
            }

            // Save the remote inputs
            SaveInputs(frame, inputs);

            // If we rolled back, resimulate from that frame
            if (frame < currentFrame)
            {
                ResimulateFromFrame(frame);
            }
        }

        public void OnRemotePlayerInput(int frame, ushort inputs, int playerIndex)
        {
            // Check if we need to rollback
            if (frame < currentFrame)
            {
                RollbackToFrame(frame);
            }

            // Save the remote inputs
            SavePlayerInputs(frame, inputs, playerIndex);

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

        public void TickPrediction(InputFrame localInputs)
        {
            currentFrame++;
            SaveState(currentFrame);
            SaveInputs(currentFrame, localInputs);
            SimulateFrame(currentFrame);
        }

        public void TickPrediction(ushort p0Inputs, ushort p1Inputs)
        {
            currentFrame++;
            SaveState(currentFrame);
            InputFrame inputs = new InputFrame(currentFrame, p0Inputs, p1Inputs);
            SaveInputs(currentFrame, inputs);
            SimulateFrame(currentFrame);
        }

        private void SimulateFrame(int frame)
        {
            // Get the state for this frame
            GameState state = GetState(frame);
            InputFrame inputs = GetInputs(frame);

            // Use the new Simulation.Tick method
            Simulation.Tick(ref state, inputs, mapData, characterDefs, isDevelopment);

            // Save the updated state
            int bufferIndex = frame % MAX_ROLLBACK_FRAMES;
            state.CopyTo(stateBuffer[bufferIndex]);
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
                inputBuffer[bufferIndex] = InputFrame.Empty(oldestFrame);
            }
        }

        /// <summary>
        /// Predict inputs for a frame when remote inputs are missing
        /// </summary>
        public InputFrame PredictInputs(int frame)
        {
            if (frame <= 0)
                return InputFrame.Empty(frame);

            // Simple prediction: repeat last known inputs
            InputFrame lastKnown = GetInputs(frame - 1);
            return new InputFrame(frame, lastKnown.player0Inputs, lastKnown.player1Inputs);
        }

        /// <summary>
        /// Check if predicted inputs match actual inputs (for rollback detection)
        /// </summary>
        public bool CheckPrediction(int frame, InputFrame actualInputs)
        {
            InputFrame predicted = GetInputs(frame);
            return predicted.Equals(actualInputs);
        }
    }
}
