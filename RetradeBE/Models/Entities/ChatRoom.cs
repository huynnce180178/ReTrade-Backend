using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class ChatRoom
{
    public string RoomId { get; set; } = null!;

    public string? BuyerId { get; set; }

    public string? SellerId { get; set; }

    public string? ProductId { get; set; }

    public string? RoomType { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual User? Buyer { get; set; }

    public virtual ICollection<Chat> Chat { get; set; } = new List<Chat>();

    public virtual Product? Product { get; set; }

    public virtual User? Seller { get; set; }
}
