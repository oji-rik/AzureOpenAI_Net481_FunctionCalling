from langchain.chat_models import ChatOpenAI
from langchain.agents import initialize_agent, Tool, AgentType
from langchain.memory import ConversationBufferMemory

# ツールの定義（整数や文字列入力を受け付け）
def prime_factors(n):
    n = int(n)
    factors = []
    divisor = 2
    while divisor * divisor <= n:
        while n % divisor == 0:
            factors.append(divisor)
            n //= divisor
        divisor += 1
    if n > 1:
        factors.append(n)
    return factors

# 合計ツール
def sum_of_numbers(numbers):
    # numbers がリストの場合、また文字列の場合も処理可能
    if isinstance(numbers, str):
        nums = [int(x.strip()) for x in numbers.strip("[]").split(",")]
    elif isinstance(numbers, list):
        nums = numbers
    else:
        raise ValueError("Invalid input for sum_of_numbers")
    return sum(nums)

# LLM 初期化
llm = AzureChatOpenAI(
    azure_endpoint="https://weida-mbw67lla-swedencentral.cognitiveservices.azure.com/",
    azure_deployment="gpt-4.1",
    api_version="2024-12-01-preview",
    temperature=0.0
)

# メモリ初期化（会話履歴を保持）
memory = ConversationBufferMemory(memory_key="chat_history", return_messages=True)

# ツール登録
tools = [
    Tool(name="prime_factors", func=prime_factors, description="Factorize a given integer"),
    Tool(name="sum_of_numbers", func=sum_of_numbers, description="Sum a list of numbers"),
]

# エージェント初期化（メモリ付き＆関数呼び出し）
agent = initialize_agent(
    tools=tools,
    llm=llm,
    agent=AgentType.OPENAI_FUNCTIONS,
    memory=memory,
    verbose=True
)

# ----- 実行フロー -----

# ① 素因数分解のリクエスト
resp1 = agent.invoke("Please prime factor 360.")
print(resp1["output"])

# メモリに保存
memory.save_context({"input": "Please prime factor 360."}, {"output": resp1["output"]})

# ② 因数の合計をリクエスト
resp2 = agent.invoke("What is the sum of these factors?")
print(resp2["output"])

memory.save_context({"input": "What is the sum of these factors?"}, {"output": resp2["output"]})

# ③ 記憶が残っているか確認
resp3 = agent.invoke("Do you remember the factors?")
print(resp3["output"])