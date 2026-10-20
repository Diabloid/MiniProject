using System;
using System.Collections.Generic;
using System.Text;

namespace NovaGames.UI.Models.DTOs
{
    public class CreateReviewDto
    {
        public int ProductId { get; set; }
        public int Rating { get; set; }
        public string? Text { get; set; }
    }
}
