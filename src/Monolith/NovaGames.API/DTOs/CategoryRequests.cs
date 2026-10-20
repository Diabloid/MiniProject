namespace NovaGames.API.DTOs
{
    // Коробка для зміни категорії гри
    public class ChangeGameCategoryRequest
    {
        public int ProductId { get; set; }
        public string NewCategory { get; set; } = string.Empty;
    }

    // Коробка для перейменування категорії
    public class RenameCategoryRequest
    {
        public string OldCategory { get; set; } = string.Empty;
        public string NewCategory { get; set; } = string.Empty;
    }
}
