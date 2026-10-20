namespace NovaGames.Core.Exceptions
{
    // 1. Базовий клас
    public class NovaGameException : Exception
    {
        public NovaGameException(string message) : base(message) { }
        public NovaGameException(string message, Exception inner) : base(message, inner) { }
    }

    // 2. Дочірні класи
    // А. Помилка Валідації
    // Кидаємо, коли користувач ввів некоректні дані (пустий логін, короткий пароль).
    public class ValidationException : NovaGameException
    {
        public string InvalidField { get; } // Назва поля, де помилка

        public ValidationException(string message, string fieldName = "") : base(message)
        {
            InvalidField = fieldName;
        }
    }

    // Б. Помилка Авторизації
    // Кидаємо, коли логін зайнятий або пароль невірний.
    public class AuthException : NovaGameException
    {
        public AuthException(string message) : base(message) { }
    }

    // В. Технічна помилка даних
    // Кидаємо, коли падає база даних MySQL або транзакція не пройшла
    public class DataException : NovaGameException
    {
        public DataException(string message, Exception inner) : base(message, inner) { }
    }

    // Г. Не знайдено
    // Кидаємо, коли шукаємо гру або юзера в БД, а їх там немає.
    public class NotFoundException : NovaGameException
    {
        public NotFoundException(string message) : base(message) { }
    }
}