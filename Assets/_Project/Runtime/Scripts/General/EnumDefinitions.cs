using System.Collections.Concurrent;

namespace PlaneWaver
{
    public enum SynthElementType { Blank, Speaker, Emitter, Frame };
    
    public static class EnumExtensions
    {
        // https://www.meziantou.net/caching-enum-tostring-to-improve-performance.htm
        
        private static readonly ConcurrentDictionary<SynthElementType, string> Cache = 
                new ConcurrentDictionary<SynthElementType, string>();

        public static string ToStringCached(this SynthElementType value)
        {
            return Cache.GetOrAdd(value, v => v.ToString());
        }
    }
}