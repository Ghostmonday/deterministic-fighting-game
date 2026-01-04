/* MAP DATA - Stage definition */
namespace NeuralDraft {
    public struct MapData {
        public int width;
        public int height;
        public int floorY;
        public int ceilingY;
        public int leftWall;
        public int rightWall;
        public int KillFloorY;
        public AABB[] SolidBlocks;
    }
}
