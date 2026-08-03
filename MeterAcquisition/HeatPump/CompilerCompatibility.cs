namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false)]
    internal sealed class RequiredMemberAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    internal sealed class CompilerFeatureRequiredAttribute : Attribute
    {
        public CompilerFeatureRequiredAttribute(string featureName)
        {
            FeatureName = featureName;
        }

        public string FeatureName { get; }

        public bool IsOptional { get; init; }
    }

    [AttributeUsage(AttributeTargets.Constructor, Inherited = false)]
    internal sealed class SetsRequiredMembersAttribute : Attribute
    {
    }

    internal static class IsExternalInit
    {
    }
}
