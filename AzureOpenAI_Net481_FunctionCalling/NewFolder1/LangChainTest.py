from langchain.prompts import ChatPromptTemplate, MessagesPlaceholder
from langchain.agents import create_openai_functions_agent, AgentExecutor, load_tools
from langchain.memory import ConversationBufferMemory
from langchain_core.runnables import Runnable
from langchain_openai import AzureChatOpenAI
from langchain.agents import initialize_agent, Tool, AgentType

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
        if isinstance(numbers, list):
    # すでにリストならそのまま合計
            return sum(numbers)
    
        elif isinstance(numbers, str):
    # [] を除去して空白を除き、数式として評価を試みる
            cleaned = numbers.strip("[]").replace(" ", "")
            
    # 式として評価できる場合（例: 2+3+4）
    try:
    return eval(cleaned)
    except Exception:
    pass
        
# カンマ区切りとして処理（例: "2,3,4"）
    try:
    nums = [int(x.strip()) for x in cleaned.split(",")]
    return sum(nums)
    except Exception:
    raise ValueError(f"String形式の解釈に失敗しました: {numbers}")
    
    else:
    raise ValueError("Invalid input type for sum_of_numbers. Supported types: list or str")

# LLM 初期化
llm = AzureChatOpenAI(
    azure_endpoint="myendpoint",
    azure_deployment="gpt-4.1",
    api_version="2024-12-01-preview",
    temperature=0.7
)

# ツール登録
tools = [
    Tool(name="prime_factors", func=prime_factors, description="Factorize a given integer"),
    Tool(name="sum_of_numbers", func=sum_of_numbers, description="Sum a list of numbers"),
]+load_tools(["llm-math"], llm = llm)

# メモリ定義
memory = ConversationBufferMemory(
    return_messages=True,
    memory_key="chat_history"
)

# Promptテンプレート（3つのinput: input, chat_history, agent_scratchpad）
prompt = ChatPromptTemplate.from_messages([
    ("system", "You are a helpful assistant."),
    MessagesPlaceholder(variable_name="chat_history"),
    ("human", "{input}"),
    MessagesPlaceholder(variable_name="agent_scratchpad"),
])

# Agentの作成
# Agentの種類：AgentType.OPENAI_FUNCTIONS
# 特徴：OpenAIのFunction Callingに対応。LangChainが関数の選択と入力の構築を自動化。
# 使用LLM：ChatOpenAI / AzureChatOpenAI など Function対応モデル
agent = create_openai_functions_agent(
    llm=llm,
    tools=tools,
    prompt=prompt
)

#複雑な推論やステップバイステップが必要	create_react_agent
#情報検索や調査系の質問を扱いたい	create_self_ask_with_search_agent

# AgentExecutorの作成
agent_executor = AgentExecutor(
    agent=agent,
    tools=tools,
    memory=memory,
    verbose=True
)

# ==========================
# 対話モード：Consoleから入力
# ==========================
print("対話モードを開始します。'exit' または '終了' と入力すると終了します。")

while True:
user_input = input("あなた: ")

if user_input.lower() in {"exit", "quit", "終了"}:
print("対話モードを終了します。")
break

try:
response = agent_executor.invoke({"input": user_input})
print("アシスタント:", response["output"])
except Exception as e:
print(f"エラーが発生しました: {e}")

# # 実行
# resp1 = agent_executor.invoke({"input": "360を素因数分解したときの、すべての素因数の総和を求めてください。"})
# print(resp1["output"])

# resp2 = agent_executor.invoke({"input": "その算出した数を2倍にしてほしいです。関数使わずに"})
# print(resp2["output"])

# resp3 = agent_executor.invoke({"input": "その算出した数を0.43乗してほしいです。"})
# print(resp3["output"])

# #入力形式が結構ランダム
# #ex)二つ目のinvokingで引数に[2,2,3,3,5]入れたり、[2+2+3+3+5]入れたり...
# print(memory.load_memory_variables({}))