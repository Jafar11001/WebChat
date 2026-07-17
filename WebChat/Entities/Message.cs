using System.ComponentModel.DataAnnotations.Schema;
using WebChat.Entities;

namespace WebChat.Models
{
    public class Message
    {
        public int Id { get; set; }

        public string ConversationId { get; set; } = default!;

        public Conversation? Conversation { get; set; }

       
        public string SenderId { get; set; } = default!;


        public AppUser? Sender { get; set; }


        public string Content { get; set; } = default!;

        public DateTime CreatedAt { get; set; }

        
    }
}
