using System;

namespace Maes.Utilities
{
    public interface ICloneable<out T> : ICloneable
        where T : notnull
    {
        new T Clone();
        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}