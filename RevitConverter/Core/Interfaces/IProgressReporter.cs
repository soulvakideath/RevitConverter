using System;

namespace RevitConverter.Core.Interfaces
{
    public interface IProgressReporter
    {
        void ReportProgress(double percentage);
        void ReportStatus(string message, double? percentage = null);
        void ReportError(string errorMessage, Exception exception = null);
        void ReportWarning(string warningMessage);
        void ReportCompletion(string message, bool isSuccess);
    }
}