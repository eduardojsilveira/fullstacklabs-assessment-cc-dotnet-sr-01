namespace API.Services.Exceptions;


public class MonsterNotFoundException : Exception
{
    public MonsterNotFoundException(string message) : base(message)
    {
    }
}