//Azure.AI.OpenAI 2.2.0-beta4の環境でのFunctionCalling実装例

using System;
using System.Collections.Generic;
using System.Text;
using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;

class Program2
{
    static void Main33()
    {
        // Azure 環境変数または設定からエンドポイントとキーを取得
        string key = Environment.GetEnvironmentVariable("AZURE_OPENAI_GPT4.1_API_KEY");

        var openAIClient = new AzureOpenAIClient(
            new Uri(endpoint),
            new AzureKeyCredential(key)
        );
        var chatClient = openAIClient.GetChatClient("gpt-4");

        // ツール関数：日付付きの気温取得
        string GetTemperature(string location, string date)
        {
            if (location == "Seattle" && date == "2025-06-01")
            {
                return "75";
            }
            return "50";
        }

        // JSON スキーマ文字列（明示的）
        string schemaJson = @"
        {
            ""type"": ""object"",
            ""properties"": {
                ""location"": {
                    ""type"": ""string"",
                    ""description"": ""The location of the weather.""
                },
                ""date"": {
                    ""type"": ""string"",
                    ""description"": ""The date of the projected weather.""
                }
            },
            ""required"": [""location"", ""date""],
            ""additionalProperties"": false
        }";
        byte[] schemaBytes = Encoding.UTF8.GetBytes(schemaJson);
        var schemaBinary = BinaryData.FromBytes(schemaBytes);

        // ツール定義
        ChatTool getTempTool = ChatTool.CreateFunctionTool(
            functionName: nameof(GetTemperature),
            functionDescription: "Get the projected temperature by date and location.",
            functionParameters: schemaBinary
        );

        // チャット／プロンプト
        var chat = new List<ChatMessage>()
        {
            new SystemChatMessage("Extract the event information and projected weather."),
            new UserChatMessage("Alice and Bob are going to a science fair in Seattle on June 1st, 2025.")
        };

        // チャット呼び出し
        var completion = chatClient.CompleteChat(
            chat,
            new ChatCompletionOptions()
            {
                Tools = { getTempTool }
            }
        );

        // ツール呼び出し確認
        Console.WriteLine("Function called: " + completion.Value.ToolCalls[0].FunctionName);
    }

}