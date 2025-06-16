from langchain.prompts import ChatPromptTemplate, MessagesPlaceholder
from langchain.agents import create_openai_functions_agent, AgentExecutor
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
    # numbers がリストの場合、また文字列の場合も処理可能
    if isinstance(numbers, str):
        nums = [int(x.strip()) for x in numbers.strip("[]").split(",")]
    elif isinstance(numbers, list):
        nums = numbers
    else:
        raise ValueError("Invalid input for sum_of_numbers")
    return sum(nums)


# ツール登録
tools = [
    Tool(name="prime_factors", func=prime_factors, description="Factorize a given integer"),
    Tool(name="sum_of_numbers", func=sum_of_numbers, description="Sum a list of numbers"),
]

# LLM 初期化
llm = AzureChatOpenAI(
    azure_endpoint="myendpoint",
    azure_deployment="gpt-4.1",
    api_version="2024-12-01-preview",
    temperature=0.7
)

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
agent = create_openai_functions_agent(
    llm=llm,
    tools=tools,
    prompt=prompt
)

# AgentExecutorの作成
agent_executor = AgentExecutor(
    agent=agent,
    tools=tools,
    memory=memory,
    verbose=True
)

# 実行
resp1 = agent_executor.invoke({"input": "360を素因数分解したときの、すべての素因数の総和を求めてください。"})
print(resp1["output"])

resp2 = agent_executor.invoke({"input": "その算出した数を2倍にしてほしいです。関数使わずに"})
print(resp2["output"])


print(memory.load_memory_variables({}))