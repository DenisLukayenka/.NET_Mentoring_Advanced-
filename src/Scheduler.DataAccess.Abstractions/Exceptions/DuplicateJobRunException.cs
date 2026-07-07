namespace Scheduler.DataAccess.Abstractions.Exceptions;

public class DuplicateJobRunException : DataAccessException
{
    public DuplicateJobRunException(string message) : base(message) { }
    public DuplicateJobRunException(string message, Exception innerException) : base(message, innerException) { }
}
