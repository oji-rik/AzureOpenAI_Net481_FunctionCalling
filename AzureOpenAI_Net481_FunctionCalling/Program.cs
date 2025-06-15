

//Azure.AI.OpenAI 2.2.0-beta4の環境でのFunctionCalling実装例

//using System;
//using System.Collections.Generic;
//using System.Text;
//using Azure;
//using Azure.AI.OpenAI;
//using OpenAI.Chat;

//class Program
//{
//    static void Main()
//    {
//        // Azure 環境変数または設定からエンドポイントとキーを取得
//        string endpoint = 
//        string key = Environment.GetEnvironmentVariable("AZURE_OPENAI_GPT4.1_API_KEY");

//        var openAIClient = new AzureOpenAIClient(
//            new Uri(endpoint),
//            new AzureKeyCredential(key)
//        );
//        var chatClient = openAIClient.GetChatClient("gpt-4");

//        // ツール関数：日付付きの気温取得
//        string GetTemperature(string location, string date)
//        {
//            if (location == "Seattle" && date == "2025-06-01")
//            {
//                return "75";
//            }
//            return "50";
//        }

//        // JSON スキーマ文字列（明示的）
//        string schemaJson = @"
//        {
//            ""type"": ""object"",
//            ""properties"": {
//                ""location"": {
//                    ""type"": ""string"",
//                    ""description"": ""The location of the weather.""
//                },
//                ""date"": {
//                    ""type"": ""string"",
//                    ""description"": ""The date of the projected weather.""
//                }
//            },
//            ""required"": [""location"", ""date""],
//            ""additionalProperties"": false
//        }";
//        byte[] schemaBytes = Encoding.UTF8.GetBytes(schemaJson);
//        var schemaBinary = BinaryData.FromBytes(schemaBytes);

//        // ツール定義
//        ChatTool getTempTool = ChatTool.CreateFunctionTool(
//            functionName: nameof(GetTemperature),
//            functionDescription: "Get the projected temperature by date and location.",
//            functionParameters: schemaBinary
//        );

//        // チャット／プロンプト
//        var chat = new List<ChatMessage>()
//        {
//            new SystemChatMessage("Extract the event information and projected weather."),
//            new UserChatMessage("Alice and Bob are going to a science fair in Seattle on June 1st, 2025.")
//        };

//        // チャット呼び出し
//        var completion = chatClient.CompleteChat(
//            chat,
//            new ChatCompletionOptions()
//            {
//                Tools = { getTempTool }
//            }
//        );

//        // ツール呼び出し確認
//        Console.WriteLine("Function called: " + completion.Value.ToolCalls[0].FunctionName);
//    }

//}

//Azure.AI.OpenAI 2.2.0-beta4の環境でのFunctionCalling実装例、会話記憶型挑戦

//using Azure;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Azure.AI.Inference;
//using Azure.AI.OpenAI;
//using Newtonsoft.Json.Linq;
//using OpenAI;

//namespace AzureOpenAI_Net481_FunctionCalling
//{
//    internal class Program
//    {
//        static async Task Main()
//        {
//            var endpoint = new Uri
//            var deploymentName = "gpt-4.1";
//            var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_GPT4.1_API_KEY");

//            var azureClient = new AzureOpenAIClient(
//                endpoint,
//                new AzureKeyCredential(apiKey));

//            // 関数の定義
//            var functions = new[]
//            {
//                new FunctionDefinition
//                {
//                    Name = "prime_factorization",
//                    Description = "整数を素因数分解する",
//                    Parameters = BinaryData.FromObjectAsJson(new
//                    {
//                        type = "object",
//                        properties = new
//                        {
//                            number = new
//                            {
//                                type = "integer",
//                                description = "素因数分解する整数"
//                            }
//                        },
//                        required = new[] { "number" }
//                    })
//                },
//                new FunctionDefinition
//                {
//                    Name = "sum",
//                    Description = "整数のリストの合計を計算する",
//                    Parameters = BinaryData.FromObjectAsJson(new
//                    {
//                        type = "object",
//                        properties = new
//                        {
//                            list = new
//                            {
//                                type = "array",
//                                items = new { type = "integer" }
//                            }
//                        },
//                        required = new[] { "list" }
//                    })
//                }
//            };


//            // コンソールで会話ループ
//            while (true)
//            {
//                Console.WriteLine("\n質問を入力してください(終了するならexitと入力):");
//                var userInput = Console.ReadLine();
//                if (userInput?.ToLower() == "exit")
//                {
//                    break;
//                }

//                // プロンプト入力
//                var chatCompletionsOptions = new ChatCompletionsOptions
//                {
//                    DeploymentName = deploymentName,
//                    Messages =
//                    {
//                        new ChatRequestSystemMessage("あなたは優秀なアシスタントです。"),
//                        new ChatRequestUserMessage(userInput)
//                    }
//                };

//                // functionを渡す
//                foreach (var function in functions)
//                {
//                    chatCompletionsOptions.Functions.Add(function);
//                }

//                // 応答待ち
//                var response = await azureClient.GetChatCompletionsAsync(chatCompletionsOptions);
//                var choice = response.Value.Choices[0];

//                // 関数呼び出しがあれば
//                if (choice.Message.FunctionCall != null)
//                {
//                    Console.WriteLine("\n=======関数呼び出し");
//                    // 関数呼び出しを処理し結果を取得
//                    var functionResultString = await ExecuteFunctionCall(choice.Message.FunctionCall);
//                    Console.WriteLine(functionResultString);

//                    // GPTに関数結果をフィードバックするメッセージを作成
//                    var followupOptions = new ChatCompletionsOptions
//                    {
//                        DeploymentName = deploymentName,
//                        Messages =
//                        {
//                            new ChatRequestSystemMessage("あなたは優秀なアシスタントです。"),
//                            new ChatRequestUserMessage(userInput),
//                            new ChatRequestFunctionMessage(
//                                name: choice.Message.FunctionCall.Name,
//                                content: functionResultString
//                            )
//                        }
//                    };

//                    // 関数定義も渡す（再度）
//                    foreach (var function in functions)
//                    {
//                        followupOptions.Functions.Add(function);
//                    }

//                    // GPTに関数結果を踏まえた応答を生成してもらう
//                    var followupResponse = await azureClient.GetChatCompletionsAsync(followupOptions);
//                    var followupChoice = followupResponse.Value.Choices[0];

//                    // GPTの最終応答を表示
//                    Console.WriteLine(followupChoice.Message.Content);
//                }
//                else
//                {
//                    // 関数呼び出しがない場合は普通に返答表示
//                    Console.WriteLine(choice.Message.Content);
//                }
//            }
//        }

//        // 関数の実行と結果文字列返却
//        static async Task<string> ExecuteFunctionCall(FunctionCall functionCall)
//        {
//            switch (functionCall.Name)
//            {
//                case "prime_factorization":
//                    var factorArgs = JObject.Parse(functionCall.Arguments);
//                    int number;

//                    var numberToken = factorArgs["number"];
//                    if (numberToken is JArray numberArray)
//                    {
//                        number = numberArray.First().Value<int>();
//                    }
//                    else if (numberToken is JValue numberValue)
//                    {
//                        number = numberValue.Value<int>();
//                    }
//                    else
//                    {
//                        throw new InvalidOperationException("Unexpected format for 'number' parameter.");
//                    }
//                    Console.WriteLine($"\n変数:{number}\n");
//                    var factors = PrimeFactorization(number);
//                    var resultString = $"{number}の素因数分解結果: {string.Join(" × ", factors)}";
//                    return resultString;

//                case "sum":
//                    var factorArgs2 = JObject.Parse(functionCall.Arguments);
//                    var list = factorArgs2["list"].ToObject<List<int>>();
//                    Console.WriteLine($"\n変数: {string.Join(", ", list)}\n");
//                    var total = Sum(list);
//                    return $"合計は {total} です。";

//                default:
//                    return "未対応の関数呼び出しです。";
//            }
//        }

//        public static List<int> PrimeFactorization(int number)
//        {
//            var factors = new List<int>();

//            // 偶数の素因数 2 を処理
//            while (number % 2 == 0)
//            {
//                factors.Add(2);
//                number /= 2;
//            }

//            // 奇数の素因数を sqrt(number) まで試す
//            for (int i = 3; i * i <= number; i += 2)
//            {
//                while (number % i == 0)
//                {
//                    factors.Add(i);
//                    number /= i;
//                }
//            }

//            // 残った number が 2 より大きければそれ自体が素数
//            if (number > 2)
//            {
//                factors.Add(number);
//            }

//            return factors;
//        }

//        public static int Sum(List<int> list)
//        {
//            return list.Sum();
//        }
//    }
//}



//using Azure;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Azure.AI.OpenAI;
//using Newtonsoft.Json.Linq;

//namespace AzureOpenAI_Net481_FunctionCalling
//{
//    internal class Program
//    {
//        static async Task Main()
//        {
//            var deploymentName = "gpt-4.1";
//            var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_GPT4.1_API_KEY");

//            var azureClient = new OpenAIClient(
//                endpoint,
//                new AzureKeyCredential(apiKey),
//                new OpenAIClientOptions(OpenAIClientOptions.ServiceVersion.V2023_12_01_Preview));

//            // 関数の定義
//            var functions = new[]
//            {
//                new FunctionDefinition
//                {
//                    Name = "prime_factorization",
//                    Description = "整数を素因数分解する",
//                    Parameters = BinaryData.FromObjectAsJson(new
//                    {
//                        type = "object",
//                        properties = new
//                        {
//                            number = new
//                            {
//                                type = "integer",
//                                description = "素因数分解する整数"
//                            }
//                        },
//                        required = new[] { "number" }
//                    })
//                },
//                new FunctionDefinition
//                {
//                    Name = "sum",
//                    Description = "整数のリストの合計を計算する",
//                    Parameters = BinaryData.FromObjectAsJson(new
//                    {
//                        type = "object",
//                        properties = new
//                        {
//                            list = new
//                            {
//                                type = "array",
//                                items = new { type = "integer" }
//                            }
//                        },
//                        required = new[] { "list" }
//                    })
//                }
//            };


//            // コンソールで会話ループ
//            while (true)
//            {
//                Console.WriteLine("\n質問を入力してください(終了するならexitと入力):");
//                var userInput = Console.ReadLine();
//                if (userInput?.ToLower() == "exit")
//                {
//                    break;
//                }

//                // プロンプト入力
//                var chatCompletionsOptions = new ChatCompletionsOptions
//                {
//                    DeploymentName = deploymentName,
//                    Messages =
//                    {
//                        new ChatRequestSystemMessage("あなたは優秀なアシスタントです。"),
//                        new ChatRequestUserMessage(userInput)
//                    }
//                };

//                // functionを渡す
//                foreach (var function in functions)
//                {
//                    chatCompletionsOptions.Functions.Add(function);
//                }

//                // 応答待ち
//                var response = await azureClient.GetChatCompletionsAsync(chatCompletionsOptions);
//                var choice = response.Value.Choices[0];

//                // 関数呼び出しがあれば
//                if (choice.Message.FunctionCall != null)
//                {
//                    Console.WriteLine("\n=======関数呼び出し");
//                    // 関数呼び出しを処理し結果を取得
//                    var functionResultString = await ExecuteFunctionCall(choice.Message.FunctionCall);
//                    Console.WriteLine(functionResultString);

//                    // GPTに関数結果をフィードバックするメッセージを作成
//                    var followupOptions = new ChatCompletionsOptions
//                    {
//                        DeploymentName = deploymentName,
//                        Messages =
//                        {
//                            new ChatRequestSystemMessage("あなたは優秀なアシスタントです。"),
//                            new ChatRequestUserMessage(userInput),
//                            new ChatRequestFunctionMessage(
//                                name: choice.Message.FunctionCall.Name,
//                                content: functionResultString
//                            )
//                        }
//                    };

//                    // 関数定義も渡す（再度）
//                    foreach (var function in functions)
//                    {
//                        followupOptions.Functions.Add(function);
//                    }

//                    // GPTに関数結果を踏まえた応答を生成してもらう
//                    var followupResponse = await azureClient.GetChatCompletionsAsync(followupOptions);
//                    var followupChoice = followupResponse.Value.Choices[0];

//                    // GPTの最終応答を表示
//                    Console.WriteLine(followupChoice.Message.Content);
//                }
//                else
//                {
//                    // 関数呼び出しがない場合は普通に返答表示
//                    Console.WriteLine(choice.Message.Content);
//                }
//            }
//        }

//        // 関数の実行と結果文字列返却
//        static async Task<string> ExecuteFunctionCall(FunctionCall functionCall)
//        {
//            switch (functionCall.Name)
//            {
//                case "prime_factorization":
//                    var factorArgs = JObject.Parse(functionCall.Arguments);
//                    int number;

//                    var numberToken = factorArgs["number"];
//                    if (numberToken is JArray numberArray)
//                    {
//                        number = numberArray.First().Value<int>();
//                    }
//                    else if (numberToken is JValue numberValue)
//                    {
//                        number = numberValue.Value<int>();
//                    }
//                    else
//                    {
//                        throw new InvalidOperationException("Unexpected format for 'number' parameter.");
//                    }
//                    Console.WriteLine($"\n変数:{number}\n");
//                    var factors = PrimeFactorization(number);
//                    var resultString = $"{number}の素因数分解結果: {string.Join(" × ", factors)}";
//                    return resultString;

//                case "sum":
//                    var factorArgs2 = JObject.Parse(functionCall.Arguments);
//                    var list = factorArgs2["list"].ToObject<List<int>>();
//                    Console.WriteLine($"\n変数: {string.Join(", ", list)}\n");
//                    var total = Sum(list);
//                    return $"合計は {total} です。";

//                default:
//                    return "未対応の関数呼び出しです。";
//            }
//        }

//        public static List<int> PrimeFactorization(int number)
//        {
//            var factors = new List<int>();

//            // 偶数の素因数 2 を処理
//            while (number % 2 == 0)
//            {
//                factors.Add(2);
//                number /= 2;
//            }

//            // 奇数の素因数を sqrt(number) まで試す
//            for (int i = 3; i * i <= number; i += 2)
//            {
//                while (number % i == 0)
//                {
//                    factors.Add(i);
//                    number /= i;
//                }
//            }

//            // 残った number が 2 より大きければそれ自体が素数
//            if (number > 2)
//            {
//                factors.Add(number);
//            }

//            return factors;
//        }

//        public static int Sum(List<int> list)
//        {
//            return list.Sum();
//        }
//    }
//}



using Azure;
using Azure.AI.Inference;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        var deploymentName = "gpt-4.1";
        var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_GPT4.1_API_KEY");
        var client = new AzureOpenAIClient(endpoint, new AzureKeyCredential(apiKey));
        var chatClient = client.GetChatClient(deploymentName);

        // 🚀 Tool（関数）定義
        var factorTool = ChatTool.CreateFunctionTool(
            functionName: "prime_factorization",
            functionDescription: "整数を素因数分解する",
            functionParameters: BinaryData.FromObjectAsJson(new
            {
                type = "object",
                properties = new
                {
                    number = new { type = "integer", description = "対象の整数" }
                },
                required = new[] { "number" }
            })
        );

        var sumTool = ChatTool.CreateFunctionTool(
            functionName: "sum",
            functionDescription: "整数リストの合計を計算する",
            functionParameters: BinaryData.FromObjectAsJson(new
            {
                type = "object",
                properties = new
                {
                    list = new
                    {
                        type = "array",
                        items = new { type = "integer" }
                    }
                },
                required = new[] { "list" }
            })
        );

        var tools = new[] { factorTool, sumTool };
        var history = new List<ChatMessage>
        {
            new SystemChatMessage("あなたは優秀なアシスタントです。")
        };

        while (true)
        {
            Console.Write("入力> ");
            var input = Console.ReadLine();
            if (input?.Trim().ToLower() == "exit") break;

            history.Add(new UserChatMessage(input));

            var response = chatClient.CompleteChat(
                history,
                new ChatCompletionOptions()
                {
                    Tools = { factorTool, sumTool }
                }
            );

            var choice = response.Value;
            if (choice.ToolCalls != null && choice.ToolCalls.Count > 0)
            {
                var call = choice.ToolCalls[0];
                string funcName = call.FunctionName;
                var argsJson = call.FunctionArguments.ToString();
                var d = JsonDocument.Parse(argsJson).RootElement;

                // 関数実行
                string resultJson;
                switch (funcName)
                {
                    case "prime_factorization":
                        int n = d.GetProperty("number").GetInt32();
                        var factors = PrimeFactorization(n);
                        resultJson = JsonSerializer.Serialize(factors);
                        Console.WriteLine($"素因数分解の結果は、{string.Join(" × ", factors)}です。");
                        history.Add(new UserChatMessage($"素因数分解の結果は、{string.Join(" × ", factors)}です。"));
                        break;
                    case "sum":
                        var list = d.GetProperty("list").EnumerateArray()
                            .Select(x => x.GetInt32());
                        int sum = list.Sum();
                        resultJson = JsonSerializer.Serialize(sum);
                        Console.WriteLine($"リストの合計は、{sum} です");
                        history.Add(new UserChatMessage($"リストの合計は、{sum} です"));
                        break;
                    default:
                        resultJson = JsonSerializer.Serialize(new { error = "unknown" });
                        break;
                }

                // ツール結果を履歴に追加し、最終応答を取得
                //history.Add(new ToolChatMessage(funcName, resultJson));
            }
            else
            {
                //Console.WriteLine($"{choice.Content}");
                string contentString = choice.Content + "";
                Console.WriteLine(contentString);
            }

            // choice を JSON 形式でファイルに保存
            //string filePath = "response.txt";
            //string jsonString = JsonSerializer.Serialize(choice, new JsonSerializerOptions { WriteIndented = true });
            //await WriteToFileAsync(filePath, jsonString);
            //Console.WriteLine($"応答が {filePath} に保存されました。");
        }
    }

    static List<int> PrimeFactorization(int n)
    {
        var list = new List<int>();
        for (int i = 2; i * i <= n; i++)
            while (n % i == 0) { list.Add(i); n /= i; }
        if (n > 1) list.Add(n);
        return list;
    }

    //static async Task WriteToFileAsync(string path, string content)
    //{
    //    byte[] encodedText = System.Text.Encoding.UTF8.GetBytes(content);

    //    using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
    //    {
    //        await fs.WriteAsync(encodedText, 0, encodedText.Length);
    //    }
    //}
}

