using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;
using Xunit;
using static Arbor.Docker.PortMapping;

namespace Arbor.Docker.Xunit.Tests.Integration;

public class SampleDockerTest(ITestOutputHelper outputHelper) : DockerTest(outputHelper.ToLogger())
{
    protected override async IAsyncEnumerable<ContainerSettings> AddContainersAsync()
    {
        PortMapping[] portMappings = [new(new PortRange(3125), new PortRange(80)), MapSinglePort(12526, 25)];
        yield return new ContainerSettings(
            "rnwood/smtp4dev:v3",
            "smtp4devtest",
            portMappings,
            new Dictionary<string, string> {["ServerOptions:TlsMode"] = "None"}
        );
    }

    [Trait("Category", "Integration")]
    [Fact]
    public async Task SendMail()
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse("test@test.local"));
        message.To.Add(MailboxAddress.Parse("test@test.local"));
        message.Subject = "testsubject";

        message.Body = new TextPart("plain") {Text = "test"};

        Exception? exception = null;
        try
        {
            using var client = new SmtpClient();

            client.AuthenticationMechanisms.Remove("XOAUTH2");

            await client.ConnectAsync("localhost", 12526, false, TestContext.Current.CancellationToken);

            await client.SendAsync(message, TestContext.Current.CancellationToken);
            await client.DisconnectAsync(true, TestContext.Current.CancellationToken);
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        if (exception is { } && Context is { })
        {
            Context.Logger.Error(exception, "Failed to send email");
        }

        Assert.Null(exception);
    }
}