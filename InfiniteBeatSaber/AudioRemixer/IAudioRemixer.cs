using System;

namespace InfiniteBeatSaber
{
    internal interface IAudioRemixer : IDisposable
    {
        void AddRemix(Remix remix);
    }
}
