using Xunit;
using NeuralDraft;

namespace NeuralDraft.Tests.Core
{
    public class FixedMathTests
    {
        [Fact]
        public void Sqrt_Zero_ReturnsZero()
        {
            // Arrange & Act
            var result = FixedMath.Sqrt(0);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void Sqrt_One_ReturnsOne()
        {
            // Arrange & Act
            var result = FixedMath.Sqrt(1);

            // Assert
            Assert.Equal(1, result);
        }

        [Fact]
        public void Sqrt_Four_ReturnsTwo()
        {
            // Arrange & Act
            var result = FixedMath.Sqrt(4);

            // Assert
            Assert.Equal(2, result);
        }

        [Fact]
        public void Sqrt_Nine_ReturnsThree()
        {
            // Arrange & Act
            var result = FixedMath.Sqrt(9);

            // Assert
            Assert.Equal(3, result);
        }

        [Fact]
        public void Sqrt_LargeNumber_ReturnsCorrectValue()
        {
            // Arrange
            long largeNumber = 1000000; // 1000 * 1000

            // Act
            var result = FixedMath.Sqrt(largeNumber);

            // Assert
            Assert.Equal(1000, result);
        }

        [Fact]
        public void Sqrt_NegativeNumber_ReturnsZero()
        {
            // Arrange & Act
            var result = FixedMath.Sqrt(-100);

            // Assert
            Assert.Equal(0, result);
        }
    }

    public class FxTests
    {
        [Fact]
        public void SCALE_Constant_IsCorrect()
        {
            // Arrange & Act
            var scale = Fx.SCALE;

            // Assert
            Assert.Equal(1000, scale);
            Assert.IsType<int>(scale);
        }
    }

    public class AABBTests
    {
        [Fact]
        public void Overlaps_IdenticalBoxes_ReturnsTrue()
        {
            // Arrange
            var a = new AABB { minX = 0, maxX = 10, minY = 0, maxY = 10 };
            var b = new AABB { minX = 0, maxX = 10, minY = 0, maxY = 10 };

            // Act
            var result = AABB.Overlaps(a, b);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Overlaps_OverlappingBoxes_ReturnsTrue()
        {
            // Arrange
            var a = new AABB { minX = 0, maxX = 10, minY = 0, maxY = 10 };
            var b = new AABB { minX = 5, maxX = 15, minY = 5, maxY = 15 };

            // Act
            var result = AABB.Overlaps(a, b);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Overlaps_NonOverlappingBoxes_ReturnsFalse()
        {
            // Arrange
            var a = new AABB { minX = 0, maxX = 10, minY = 0, maxY = 10 };
            var b = new AABB { minX = 20, maxX = 30, minY = 20, maxY = 30 };

            // Act
            var result = AABB.Overlaps(a, b);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Overlaps_TouchingBoxes_ReturnsTrue()
        {
            // Arrange
            var a = new AABB { minX = 0, maxX = 10, minY = 0, maxY = 10 };
            var b = new AABB { minX = 10, maxX = 20, minY = 10, maxY = 20 };

            // Act
            var result = AABB.Overlaps(a, b);

            // Assert
            Assert.True(result); // AABB.Overlaps considers touching as overlapping
        }

        [Fact]
        public void Overlaps_OneInsideAnother_ReturnsTrue()
        {
            // Arrange
            var outer = new AABB { minX = 0, maxX = 100, minY = 0, maxY = 100 };
            var inner = new AABB { minX = 25, maxX = 75, minY = 25, maxY = 75 };

            // Act
            var result = AABB.Overlaps(outer, inner);

            // Assert
            Assert.True(result);
        }
    }
}
