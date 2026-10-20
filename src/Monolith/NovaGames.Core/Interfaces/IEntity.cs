namespace NovaGames.Core.Interfaces
{
    // Цей інтерфейс є контрактом для ВСІХ таблиць у базі даних
    public interface IEntity
    {
        int Id { get; set; }
    }
}