using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseService.Application.Commands.SaveSupportMessage;

public class SaveSupportMessageData
{
    public string UserName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string SessionId { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
        public string Text { get; init; } = string.Empty;
        public long TelegramChatId { get; init; }
        public long TelegramMessageId { get; init; }
    public string Direction { get; init; } = "FrontendToTelegram";
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}
