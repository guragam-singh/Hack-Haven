using UnityEngine;

namespace Reverbat
{
    public enum SoundResponse { Wall, Reflector, Bell, HiddenMechanism }

    // Put this beside any Collider2D that should receive an echo.
    public sealed class SoundInteractable : MonoBehaviour
    {
        public SoundResponse response;
        public Level01Manager level;

        public void OnSoundHit(bool reflected)
        {
            if (response == SoundResponse.Bell) level.HitBell(reflected);
            if (response == SoundResponse.HiddenMechanism && reflected) level.HitCoffinByReflection();
        }

        public void OnEchoReturned()
        {
            if (response == SoundResponse.HiddenMechanism) level.RevealMechanism();
        }
    }
}
