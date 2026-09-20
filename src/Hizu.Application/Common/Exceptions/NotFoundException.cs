namespace Hizu.Application.Common.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string name, object key)
        : base($"مورد درخواستی با شناسه ({key}) در بخش {name} یافت نشد.")
    {
    }
}
