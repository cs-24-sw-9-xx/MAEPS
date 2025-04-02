using System;

namespace Maes.Utilities
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Constructor, AllowMultiple = false, Inherited = true)]
    public sealed class ForbiddenKnowledgeAttribute : Attribute
    {
    }
}