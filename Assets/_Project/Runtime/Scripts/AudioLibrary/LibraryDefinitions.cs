using System;

namespace PlaneWaver.Library
{
    #region TYPE AND STATIC DECLARATIONS
    
    [Serializable]
    public enum PlaybackType
    {
        Default = 0,
        OneShot = 1,
        Short = 2,
        Long = 3,
        Loop = 4
    }

    [Serializable]
    public struct LibraryConfig
    {
        public static string AssetEntityPrefix = "AudioClip";
        public static string AudioFilePath = "Assets/_Project/Resources/Audio/Wav";
        public static string AudioObjectPath = "Assets/_Project/Resources/Audio/Objects";
        public static string EmitterObjectPath = "Assets/_Project/Resources/Emitters";
        public static string SourceTypeFilter = "t:AudioClip";
        public static string SourceExtensionFilter = ".wav";
    }

    #endregion
}