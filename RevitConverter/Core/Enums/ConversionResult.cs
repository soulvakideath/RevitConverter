namespace RevitConverter.Core.Enums
{
    public enum ConversionResult
    {
        Success,
        PartialSuccess,
        Failed,
        Cancelled,
        InvalidInput,
        UnsupportedGeometry,
        RevitError,
        FileWriteError,
        UnknownError
    }
}