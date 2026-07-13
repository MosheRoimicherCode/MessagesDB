using DatabaseService.Application.Commands;
using DatabaseService.Application.Commands.SaveSupportMessage;
using DatabaseService.Application.Tests.Fakes;
using Shared.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DatabaseService.Application.Tests;

public class SaveSupportMessage
{
   
    [Theory]
    [MemberData(nameof(MissingFieldCases))]
    public async Task HandleAsync_WithMissingRequiredField_ReturnsFailure(string missingField)
    {
        var users = new FakeUserRepository();
        var messages = new FakeMessageRepository();
        var handler = new SaveSupportMessageCommandHandler(users, messages);

        var data = CreateDataCreator(missingField);

        var request = new RequestPacket
        {
            Type = "SaveSupportMessage",
            Data = JsonSerializer.SerializeToElement(data)
        };

        var response = await handler.HandleAsync(request);

        Assert.False(response.Ok);
        Assert.Contains(missingField, response.Error);
    }

    private static SaveSupportMessageData CreateDataCreator(string missingField)
    {
        switch (missingField)
        {
            case nameof(SaveSupportMessageData.UserName):
                return new SaveSupportMessageData
                {
                    SessionId = "session-f4sl6xc",
                    UserName = "",
                    PhoneNumber = "0585200517",
                    ProjectName = "_5_test_server.fly",
                    Text = "בדיקה מרחוק"
                };
            case nameof(SaveSupportMessageData.PhoneNumber):
                return new SaveSupportMessageData
                {
                    SessionId = "session-f4sl6xc",
                    UserName = "Moshe_test",
                    PhoneNumber = "",
                    ProjectName = "_5_test_server.fly",
                    Text = "בדיקה מרחוק"
                };
            case nameof(SaveSupportMessageData.SessionId):
                return new SaveSupportMessageData
                {
                    SessionId = "",
                    UserName = "Moshe_test",
                    PhoneNumber = "0585200517",
                    ProjectName = "_5_test_server.fly",
                    Text = "בדיקה מרחוק"
                };
            case nameof(SaveSupportMessageData.Text):
                return new SaveSupportMessageData
                {
                    SessionId = "session-f4sl6xc",
                    UserName = "Moshe_test",
                    PhoneNumber = "0585200517",
                    ProjectName = "_5_test_server.fly",
                    Text = ""
                };
        }

        throw new ArgumentOutOfRangeException(nameof(missingField), missingField, null);
    }

    [Fact]
    public async Task HandleAsync_WithValidData_CreatesUserAndMessage()
    {
        var users = new FakeUserRepository();
        var messages = new FakeMessageRepository();
        var handler = new SaveSupportMessageCommandHandler(users, messages);

        var request = new RequestPacket
        {
            Type = "SaveSupportMessage",
            Data = JsonSerializer.SerializeToElement(new SaveSupportMessageData
            {
                SessionId = "session-f4sl6xc",
                UserName = "Moshe_test",
                PhoneNumber = "0585200517",
                ProjectName = "_5_test_server.fly",
                Text = "בדיקה מרחוק"
            })
        };

        var response = await handler.HandleAsync(request);

        Assert.True(response.Ok);
        Assert.Single(users.Users);
        Assert.Single(messages.Messages);
        Assert.Equal(users.Users[0].Id, messages.Messages[0].UserId);
    }

    [Fact]
    public async Task HandleAsync_WithExistingUser_ReusesExistingUserAndCreatesSecondMessage()
    {
        var users = new FakeUserRepository();
        var messages = new FakeMessageRepository();
        var handler = new SaveSupportMessageCommandHandler(users, messages);

        var firstRequest = new RequestPacket
        {
            Type = "SaveSupportMessage",
            Data = JsonSerializer.SerializeToElement(new SaveSupportMessageData
            {
                SessionId = "session-first",
                UserName = "Moshe_test",
                PhoneNumber = "0585200517",
                ProjectName = "_5_test_server.fly",
                Text = "first message"
            })
        };

        var secondRequest = new RequestPacket
        {
            Type = "SaveSupportMessage",
            Data = JsonSerializer.SerializeToElement(new SaveSupportMessageData
            {
                SessionId = "session-second",
                UserName = "Moshe_test",
                PhoneNumber = "0585200517",
                ProjectName = "_5_test_server.fly",
                Text = "second message"
            })
        };

        var firstResponse = await handler.HandleAsync(firstRequest);
        var secondResponse = await handler.HandleAsync(secondRequest);

        Assert.True(firstResponse.Ok);
        Assert.True(secondResponse.Ok);
        Assert.Single(users.Users);
        Assert.Equal(2, messages.Messages.Count);
        Assert.All(messages.Messages, message => Assert.Equal(users.Users[0].Id, message.UserId));
    }
    public static IEnumerable<object[]> MissingFieldCases()
    {
        yield return [nameof(SaveSupportMessageData.UserName)];
        yield return [nameof(SaveSupportMessageData.PhoneNumber)];
        yield return [nameof(SaveSupportMessageData.SessionId)];
        yield return [nameof(SaveSupportMessageData.Text)];
    }
}

