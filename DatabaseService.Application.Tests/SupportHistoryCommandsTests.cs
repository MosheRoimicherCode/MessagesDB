using System.Text.Json;
using DatabaseService.Application.Commands.GetSupportHistory;
using DatabaseService.Application.Commands.SaveSupportMessageWithFiles;
using DatabaseService.Application.Tests.Fakes;
using Shared.Protocol;

namespace DatabaseService.Application.Tests;

public sealed class SupportHistoryCommandsTests
{
    [Fact]
    public async Task SaveWithFiles_WithValidFileOnlyMessage_SavesCompleteMetadata()
    {
        var users = new FakeUserRepository();
        var messages = new FakeMessageRepository();
        var handler = new SaveSupportMessageWithFilesCommandHandler(users, messages);
        var request = new RequestPacket
        {
            Type = "SaveSupportMessageWithFiles",
            Data = JsonSerializer.SerializeToElement(new SaveSupportMessageWithFilesData
            {
                SessionId = "session-file",
                UserName = "Moshe",
                PhoneNumber = "0585200517",
                ProjectName = "project.fly",
                TelegramMessageId = 123,
                Direction = "FrontendToTelegram",
                Files =
                [
                    new SaveSupportMessageFileData
                    {
                        TelegramFileId = "file-id",
                        TelegramFileUniqueId = "unique-id",
                        FileName = "drawing.pdf",
                        MimeType = "application/pdf",
                        FileSize = 2048,
                        FileKind = "Document",
                        Thumbnail = new SaveSupportMessageFileThumbnailData
                        {
                            TelegramFileId = "thumbnail-id",
                            Width = 320,
                            Height = 200,
                            FileSize = 512
                        }
                    }
                ]
            })
        };

        var response = await handler.HandleAsync(request);

        Assert.True(response.Ok);
        var saved = Assert.Single(messages.Messages);
        Assert.Equal(123, saved.TelegramMessageId);
        var file = Assert.Single(saved.Files);
        Assert.Equal("file-id", file.TelegramFileId);
        Assert.Equal("thumbnail-id", file.Thumbnail?.TelegramFileId);
    }

    [Fact]
    public async Task GetHistory_WithSavedFile_ReturnsGatewayResponseShape()
    {
        var users = new FakeUserRepository();
        var messages = new FakeMessageRepository();
        var saveHandler = new SaveSupportMessageWithFilesCommandHandler(users, messages);
        var historyHandler = new GetSupportHistoryCommandHandler(messages);

        await saveHandler.HandleAsync(new RequestPacket
        {
            Type = "SaveSupportMessageWithFiles",
            Data = JsonSerializer.SerializeToElement(new SaveSupportMessageWithFilesData
            {
                SessionId = "session-history",
                UserName = "Moshe",
                PhoneNumber = "0585200517",
                ProjectName = "project.fly",
                Text = "attached",
                TelegramMessageId = 456,
                Direction = "TelegramToFrontend",
                Files =
                [
                    new SaveSupportMessageFileData
                    {
                        TelegramFileId = "file-id",
                        FileName = "photo.jpg",
                        MimeType = "image/jpeg",
                        FileKind = "Photo"
                    }
                ]
            })
        });

        var response = await historyHandler.HandleAsync(new RequestPacket
        {
            Type = "GetSupportHistory",
            Data = JsonSerializer.SerializeToElement(new GetSupportHistoryData
            {
                PhoneNumber = "0585200517",
                ProjectName = "project.fly"
            })
        });

        Assert.True(response.Ok);
        var page = Assert.IsType<SupportHistoryPageResponse>(response.Data);
        var message = Assert.Single(page.Messages);
        Assert.Equal("TelegramToFrontend", message.Direction);
        Assert.Equal("session-history", message.SessionId);
        Assert.Equal("file-id", Assert.Single(message.Files).TelegramFileId);
        Assert.False(page.HasMore);
        Assert.Equal(1, page.NextMessageId);
    }

    [Fact]
    public async Task GetHistory_WithLimit_ReturnsNextPageAfterMessageId()
    {
        var messages = new FakeMessageRepository();
        await messages.CreateAsync(new DatabaseService.Domain.Entities.Message
        {
            UserId = 1,
            SessionId = "session-first",
            ProjectName = "project.fly",
            Text = "first"
        });
        await messages.CreateAsync(new DatabaseService.Domain.Entities.Message
        {
            UserId = 1,
            SessionId = "session-second",
            ProjectName = "project.fly",
            Text = "second"
        });

        var handler = new GetSupportHistoryCommandHandler(messages);
        var firstResponse = await handler.HandleAsync(new RequestPacket
        {
            Type = "GetSupportHistory",
            Data = JsonSerializer.SerializeToElement(new GetSupportHistoryData
            {
                PhoneNumber = "0585200517",
                ProjectName = "project.fly",
                Limit = 1
            })
        });

        var firstPage = Assert.IsType<SupportHistoryPageResponse>(firstResponse.Data);
        Assert.Equal("first", Assert.Single(firstPage.Messages).Text);
        Assert.Equal(1, firstPage.NextMessageId);
        Assert.True(firstPage.HasMore);

        var secondResponse = await handler.HandleAsync(new RequestPacket
        {
            Type = "GetSupportHistory",
            Data = JsonSerializer.SerializeToElement(new GetSupportHistoryData
            {
                PhoneNumber = "0585200517",
                ProjectName = "project.fly",
                AfterMessageId = firstPage.NextMessageId,
                Limit = 1
            })
        });

        var secondPage = Assert.IsType<SupportHistoryPageResponse>(secondResponse.Data);
        Assert.Equal("second", Assert.Single(secondPage.Messages).Text);
        Assert.Equal(2, secondPage.NextMessageId);
        Assert.False(secondPage.HasMore);
    }
}
