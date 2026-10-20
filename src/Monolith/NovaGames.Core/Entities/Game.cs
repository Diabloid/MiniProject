using NovaGames.Core.Interfaces;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace NovaGames.Core.Entities
{
    // 1. Спеціальний клас для JSON-колонки в базі даних
    public class GameMedia
    {
        public string CoverUrl { get; set; } = "/Assets/default_game.png";
        public string IconUrl { get; set; } = "/Assets/default_icon.png";
        public List<string> Screenshots { get; set; } = new List<string>();
    }

    // 2. Базовий клас
    [JsonDerivedType(typeof(DigitalGame), typeDiscriminator: "digital")]
    [JsonDerivedType(typeof(PhysicalGame), typeDiscriminator: "physical")]
    [JsonDerivedType(typeof(SubscriptionGame), typeDiscriminator: "subscription")]
    public abstract class Game : IEntity, INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal Discount { get; set; } = 0;
        private double _averageRating = 0.0;
        public double AverageRating
        {
            get => _averageRating;
            set { _averageRating = value; OnPropertyChanged(); }
        }

        private int _totalReviews = 0;
        public int TotalReviews
        {
            get => _totalReviews;
            set { _totalReviews = value; OnPropertyChanged(); }
        }
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public List<string> Genres { get; set; } = new List<string>();
        public bool IsPublished { get; set; } = true;
        public GameMedia Media { get; set; } = new GameMedia();
        [JsonIgnore]
        public bool HasDiscount => Discount > 0;
        [JsonIgnore]
        public decimal FinalPrice => Price * (1 - Discount / 100);
        [JsonIgnore]
        public string DisplayGenres => Genres != null && Genres.Any()
            ? string.Join(", ", Genres)
            : "Без жанру";
        [JsonIgnore]
        public string ImagePath
        {
            get
            {
                // Якщо картинки немає взагалі
                if (string.IsNullOrEmpty(Media?.CoverUrl))
                    return "/Assets/default_game.png";

                // Якщо шлях відносний
                if (Media.CoverUrl.StartsWith("/"))
                    return $"https://localhost:7232{Media.CoverUrl}";

                // Якщо це вже повний шлях
                return Media.CoverUrl;
            }
        }

        [JsonIgnore]
        public string IconPath
        {
            get
            {
                if (string.IsNullOrEmpty(Media?.IconUrl))
                    return "/Assets/default_icon.png";

                if (Media.IconUrl.StartsWith("/"))
                    return $"https://localhost:7232{Media.IconUrl}";

                return Media.IconUrl;
            }
        }

        [JsonIgnore]
        public List<string> FullScreenshotPaths => Media?.Screenshots?.Select(url =>
            url.StartsWith("/") ? $"https://localhost:7232{url}" : url
        ).ToList() ?? new List<string>();

        [JsonIgnore]
        private bool _isInCart;

        [JsonIgnore]
        [NotMapped]
        public bool IsInCart
        {
            get => _isInCart;
            set
            {
                _isInCart = value;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        private bool _isOwned;

        [JsonIgnore]
        [NotMapped]
        public bool IsOwned
        {
            get => _isOwned;
            set
            {
                _isOwned = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}