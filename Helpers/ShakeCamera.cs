using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics.CameraModifiers;

namespace FishyFishy.Helpers
{
    public class ShakeCamera : ICameraModifier
    {
        private int _framesToLast;
        private float _strength;
        private int _framesLasted;

        public string UniqueIdentity { get; private set; }
        public bool Finished { get; private set; }

        public ShakeCamera(float strength, int frames, string uniqueIdentity = null)
        {
            _strength = strength;
            _framesToLast = frames;
            UniqueIdentity = uniqueIdentity;
        }

        public void Update(ref CameraInfo cameraInfo)
        {
            cameraInfo.CameraPosition += Main.rand.NextVector2Circular(_strength, _strength);
            _framesLasted++;
            if (_framesLasted >= _framesToLast)
                Finished = true;
        }
    }
}
