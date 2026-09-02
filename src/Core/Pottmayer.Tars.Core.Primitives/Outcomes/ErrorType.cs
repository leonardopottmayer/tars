namespace Pottmayer.Tars.Core.Primitives.Outcomes
{
    /// <summary>Classification of an <see cref="Error"/>, used to map failures onto transport/status semantics.</summary>
    public enum ErrorType
    {
        /// <summary>Input failed validation.</summary>
        Validation = 1,

        /// <summary>A business/domain rule was violated.</summary>
        Business = 2,

        /// <summary>A requested resource was not found.</summary>
        NotFound = 3,

        /// <summary>The operation conflicts with the current state.</summary>
        Conflict = 4,

        /// <summary>Authentication is missing or invalid.</summary>
        Unauthorized = 5,

        /// <summary>The caller is authenticated but not allowed to perform the operation.</summary>
        Forbidden = 6,

        /// <summary>An unexpected failure that does not fit the other categories.</summary>
        Unexpected = 7
    }
}
