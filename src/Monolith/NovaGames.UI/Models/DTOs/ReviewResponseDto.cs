using System;
using System.Collections.Generic;
using System.Text;

namespace NovaGames.UI.Models.DTOs
{
    public class ReviewResponseDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string AvatarBorderColorHex { get; set; } = "#00ADEF";
        public int Rating { get; set; }
        public string? Text { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsEdited { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public bool IsMine { get; set; }
    }
}
