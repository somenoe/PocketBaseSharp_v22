using FluentAssertions;
using PocketBaseSharp.Json;
using PocketBaseSharp.Models;
using PocketBaseSharp.Models.Files;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PocketBaseSharp.Tests
{
    [TestClass]
    public class ModelAndHelperUnitTests
    {
        [TestMethod]
        public void AuthStore_returns_false_when_exp_is_not_numeric()
        {
            var store = new AuthStore();
            store.Save(CreateToken("{\"exp\":\"later\"}"), null);

            store.IsValid.Should().BeFalse();
        }

        [TestMethod]
        public async Task SseMessage_parses_fields_and_formats_output()
        {
            var message = await SseMessage.FromReceivedMessageAsync(
                "id: 1\n" +
                "event: entry\n" +
                "retry: 1500\n" +
                "data: first\n" +
                "data: second\n\n");

            message.Should().NotBeNull();
            message!.Id.Should().Be("1");
            message.Event.Should().Be("entry");
            message.Retry.Should().Be(1500);
            message.Data.Should().Be($"first{Environment.NewLine}second");
            message.ToString().Should().Contain("Event:entry");

            var nullMessage = await SseMessage.FromReceivedMessageAsync(null);
            nullMessage.Should().BeNull();
        }

        [TestMethod]
        public void DateTimeConverter_reads_invalid_values_as_null_and_serializes_values_and_nulls()
        {
            var options = new JsonSerializerOptions();

            var valid = JsonSerializer.Deserialize<DateHolder>("{\"Value\":\"2024-01-02T03:04:05Z\"}", options);
            valid.Should().NotBeNull();
            valid!.Value.Should().Be(DateTime.SpecifyKind(DateTime.Parse("2024-01-02T03:04:05Z"), DateTimeKind.Utc));

            var invalid = JsonSerializer.Deserialize<DateHolder>("{\"Value\":\"not-a-date\"}", options);
            invalid.Should().NotBeNull();
            invalid!.Value.Should().BeNull();

            var timestamp = new DateTime(2024, 01, 02, 03, 04, 05, DateTimeKind.Utc);
            var serializedValue = JsonSerializer.Serialize(new DateHolder { Value = timestamp }, options);
            serializedValue.Should().Contain(timestamp.ToString());

            var serializedNull = JsonSerializer.Serialize(new DateHolder { Value = null }, options);
            serializedNull.Should().Contain("null");
        }

        [TestMethod]
        public async Task File_models_return_expected_streams()
        {
            using var sourceStream = new MemoryStream(Encoding.UTF8.GetBytes("stream-value"));
            var streamFile = new StreamFile(sourceStream);
            streamFile.GetStream().Should().BeSameAs(sourceStream);

            var missingFile = new FilepathFile("missing-file.txt");
            missingFile.GetStream().Should().BeNull();

            var filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");
            await File.WriteAllTextAsync(filePath, "file-value");

            try
            {
                var pathFile = new FilepathFile(filePath);
                using var fileStream = pathFile.GetStream();
                fileStream.Should().NotBeNull();

                using var reader = new StreamReader(fileStream!);
                (await reader.ReadToEndAsync()).Should().Be("file-value");
            }
            finally
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
        }

        [TestMethod]
        public void ClientException_exposes_current_message_format()
        {
            var exception = new ClientException(
                "https://example.com/api/health",
                isAbort: true,
                statusCode: 418,
                response: new Dictionary<string, object?> { ["message"] = "teapot" },
                originalError: new InvalidOperationException("boom"));

            exception.Message.Should().StartWith("ClientException: ");
            exception.ToString().Should().StartWith("ClientException: ClientException: ");
            exception.StatusCode.Should().Be(418);
            exception.IsAbort.Should().BeTrue();
            exception.Url.Should().Be("https://example.com/api/health");
        }

        private static string CreateToken(string payload)
        {
            return $"{EncodeSegment("{\"alg\":\"HS256\",\"typ\":\"JWT\"}")}.{EncodeSegment(payload)}.signature";
        }

        private static string EncodeSegment(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value)).TrimEnd('=');
        }

        private sealed class DateHolder
        {
            [JsonConverter(typeof(DateTimeConverter))]
            public DateTime? Value { get; set; }
        }
    }
}
