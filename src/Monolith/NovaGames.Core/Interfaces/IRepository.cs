namespace NovaGames.Core.Interfaces
{
    // Репозиторій приймає ТІЛЬКИ сутності бази даних
    public interface IRepository<T> where T : class, IEntity
    {
        Task<IEnumerable<T>> GetAllAsync();   // Отримання всіх елементів
        Task<T?> GetByIdAsync(int id);       // Отримання конкретного елемента за його первинним ключем

        Task AddAsync(T item);               // Асинхронне додавання
        Task UpdateAsync(T item);            // НОВЕ: Асинхронне оновлення
        Task RemoveAsync(T item);            // Асинхронне видалення
        Task SaveAsync();                    // Збереження транзакції
    }
}