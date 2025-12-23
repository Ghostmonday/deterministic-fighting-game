using Xunit;
using NeuralDraft;

namespace NeuralDraft.Tests.Core
{
    public class InputFrameTests
    {
        [Fact]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange
            int frameNumber = 123;
            ushort p0Inputs = 0x00FF;
            ushort p1Inputs = 0xFF00;

            // Act
            var inputFrame = new InputFrame(frameNumber, p0Inputs, p1Inputs);

            // Assert
            Assert.Equal(frameNumber, inputFrame.frameNumber);
            Assert.Equal(p0Inputs, inputFrame.player0Inputs);
            Assert.Equal(p1Inputs, inputFrame.player1Inputs);
        }

        [Fact]
        public void Empty_CreatesFrameWithNoInputs()
        {
            // Arrange
            int frameNumber = 456;

            // Act
            var inputFrame = InputFrame.Empty(frameNumber);

            // Assert
            Assert.Equal(frameNumber, inputFrame.frameNumber);
            Assert.Equal(0, inputFrame.player0Inputs);
            Assert.Equal(0, inputFrame.player1Inputs);
        }

        [Fact]
        public void GetPlayerInputs_ValidPlayerIndex_ReturnsCorrectInputs()
        {
            // Arrange
            var inputFrame = new InputFrame(0, 0x00FF, 0xFF00);

            // Act & Assert
            Assert.Equal(0x00FF, inputFrame.GetPlayerInputs(0));
            Assert.Equal(0xFF00, inputFrame.GetPlayerInputs(1));
        }

        [Fact]
        public void GetPlayerInputs_InvalidPlayerIndex_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var inputFrame = new InputFrame(0, 0x00FF, 0xFF00);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => inputFrame.GetPlayerInputs(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => inputFrame.GetPlayerInputs(2));
            Assert.Throws<ArgumentOutOfRangeException>(() => inputFrame.GetPlayerInputs(100));
        }

        [Fact]
        public void SetPlayerInputs_ValidPlayerIndex_SetsCorrectInputs()
        {
            // Arrange
            var inputFrame = new InputFrame(0, 0, 0);

            // Act
            inputFrame.SetPlayerInputs(0, 0x00FF);
            inputFrame.SetPlayerInputs(1, 0xFF00);

            // Assert
            Assert.Equal(0x00FF, inputFrame.player0Inputs);
            Assert.Equal(0xFF00, inputFrame.player1Inputs);
        }

        [Fact]
        public void SetPlayerInputs_InvalidPlayerIndex_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var inputFrame = new InputFrame(0, 0, 0);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => inputFrame.SetPlayerInputs(-1, 0x00FF));
            Assert.Throws<ArgumentOutOfRangeException>(() => inputFrame.SetPlayerInputs(2, 0xFF00));
        }

        [Fact]
        public void FromBytes_ValidData_CreatesCorrectFrame()
        {
            // Arrange
            byte[] data = new byte[] {
                0x00, 0x00, 0x00, 0x7B, // frameNumber = 123
                0x00, 0xFF,             // player0Inputs = 0x00FF
                0xFF, 0x00              // player1Inputs = 0xFF00
            };

            // Act
            var inputFrame = InputFrame.FromBytes(data, 0);

            // Assert
            Assert.Equal(123, inputFrame.frameNumber);
            Assert.Equal(0x00FF, inputFrame.player0Inputs);
            Assert.Equal(0xFF00, inputFrame.player1Inputs);
        }

        [Fact]
        public void FromBytes_NullData_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => InputFrame.FromBytes(null, 0));
        }

        [Fact]
        public void FromBytes_NegativeOffset_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            byte[] data = new byte[8];

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => InputFrame.FromBytes(data, -1));
        }

        [Fact]
        public void FromBytes_InsufficientData_ThrowsArgumentException()
        {
            // Arrange
            byte[] data = new byte[7]; // Need 8 bytes

            // Act & Assert
            Assert.Throws<ArgumentException>(() => InputFrame.FromBytes(data, 0));
            Assert.Throws<ArgumentException>(() => InputFrame.FromBytes(data, 1));
        }

        [Fact]
        public void ToBytes_CreatesCorrectByteArray()
        {
            // Arrange
            var inputFrame = new InputFrame(0x12345678, 0x00FF, 0xFF00);

            // Act
            var bytes = inputFrame.ToBytes();

            // Assert
            Assert.Equal(8, bytes.Length);
            Assert.Equal(0x12, bytes[0]); // frameNumber byte 0
            Assert.Equal(0x34, bytes[1]); // frameNumber byte 1
            Assert.Equal(0x56, bytes[2]); // frameNumber byte 2
            Assert.Equal(0x78, bytes[3]); // frameNumber byte 3
            Assert.Equal(0x00, bytes[4]); // player0Inputs high byte
            Assert.Equal(0xFF, bytes[5]); // player0Inputs low byte
            Assert.Equal(0xFF, bytes[6]); // player1Inputs high byte
            Assert.Equal(0x00, bytes[7]); // player1Inputs low byte
        }

        [Fact]
        public void Equals_IdenticalFrames_ReturnsTrue()
        {
            // Arrange
            var frame1 = new InputFrame(123, 0x00FF, 0xFF00);
            var frame2 = new InputFrame(123, 0x00FF, 0xFF00);

            // Act & Assert
            Assert.True(frame1.Equals(frame2));
        }

        [Fact]
        public void Equals_DifferentFrameNumbers_ReturnsFalse()
        {
            // Arrange
            var frame1 = new InputFrame(123, 0x00FF, 0xFF00);
            var frame2 = new InputFrame(124, 0x00FF, 0xFF00);

            // Act & Assert
            Assert.False(frame1.Equals(frame2));
        }

        [Fact]
        public void Equals_DifferentInputs_ReturnsFalse()
        {
            // Arrange
            var frame1 = new InputFrame(123, 0x00FF, 0xFF00);
            var frame2 = new InputFrame(123, 0x00FE, 0xFF01);

            // Act & Assert
            Assert.False(frame1.Equals(frame2));
        }

        [Fact]
        public void Copy_CreatesIdenticalFrame()
        {
            // Arrange
            var original = new InputFrame(123, 0x00FF, 0xFF00);

            // Act
            var copy = original.Copy();

            // Assert
            Assert.Equal(original.frameNumber, copy.frameNumber);
            Assert.Equal(original.player0Inputs, copy.player0Inputs);
            Assert.Equal(original.player1Inputs, copy.player1Inputs);
            Assert.True(original.Equals(copy));
        }

        [Fact]
        public void InputBits_EnumValues_AreCorrect()
        {
            // Arrange & Act & Assert
            Assert.Equal(0u, (uint)InputBits.NONE);
            Assert.Equal(1u, (uint)InputBits.UP);
            Assert.Equal(2u, (uint)InputBits.DOWN);
            Assert.Equal(4u, (uint)InputBits.LEFT);
            Assert.Equal(8u, (uint)InputBits.RIGHT);
            Assert.Equal(16u, (uint)InputBits.JUMP);
            Assert.Equal(32u, (uint)InputBits.ATTACK);
            Assert.Equal(64u, (uint)InputBits.SPECIAL);
            Assert.Equal(128u, (uint)InputBits.DEFEND);
        }

        [Fact]
        public void InputBits_CanBeCombined()
        {
            // Arrange
            InputBits combined = InputBits.LEFT | InputBits.JUMP | InputBits.ATTACK;

            // Act & Assert
            Assert.True((combined & InputBits.LEFT) != 0);
            Assert.True((combined & InputBits.JUMP) != 0);
            Assert.True((combined & InputBits.ATTACK) != 0);
            Assert.False((combined & InputBits.RIGHT) != 0);
            Assert.False((combined & InputBits.DEFEND) != 0);
        }
    }
}
