using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseService.Application.Commands.SaveSupportMessage
{
    public class SaveSupportMessageData
    {
        public long UserId { get; init; }
        public string UserName { get; init; } = string.Empty;
        public string PhoneNumber { get; init; } = string.Empty;
        public string SessionId { get; init; } = string.Empty;
        public string ProjectName { get; init; } = string.Empty;
        public string Text { get; init; } = string.Empty;
        public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    }
}
