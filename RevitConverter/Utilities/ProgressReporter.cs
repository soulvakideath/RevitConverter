using RevitConverter.Core.Interfaces;

namespace RevitConverter.Utilities
{
    public class ProgressReporter : IProgressReporter
    {
        public event EventHandler<double> ProgressChanged;
        public event EventHandler<string> StatusChanged;
        public event EventHandler<(string Message, Exception Exception)> ErrorOccurred;
        public event EventHandler<string> WarningOccurred;
        public event EventHandler<(string Message, bool IsSuccess)> OperationCompleted;
        
        public double CurrentProgress { get; private set; }
        public string CurrentStatus { get; private set; }
        public void ReportProgress(double percentage)
        {
            CurrentProgress = Math.Clamp(percentage, 0, 100);
            ProgressChanged?.Invoke(this, CurrentProgress);
        }

        public void ReportStatus(string message, double? percentage = null)
        {
            CurrentStatus = message;
            StatusChanged?.Invoke(this, message);

            if (percentage.HasValue)
            {
                ReportProgress(percentage.Value);
            }
        }
        public void ReportError(string errorMessage, Exception exception = null)
        {
            ErrorOccurred?.Invoke(this, (errorMessage, exception));
        }
        public void ReportWarning(string warningMessage)
        {
            WarningOccurred?.Invoke(this, warningMessage);
        }
        public void ReportCompletion(string message, bool isSuccess)
        {
            OperationCompleted?.Invoke(this, (message, isSuccess));
        }
    }
}