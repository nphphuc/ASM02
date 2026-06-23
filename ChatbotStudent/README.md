# AI Chatbot Sinh Viên — RAG-Based Q&A System

Hệ thống chatbot hỏi đáp tài liệu môn học sử dụng **RAG (Retrieval Augmented Generation)**, nghiên cứu và so sánh hiệu quả giữa RAG và Fine-tuning trong bối cảnh tiếng Việt.

## Kiến trúc hệ thống

```
┌─────────────┐    ┌──────────┐    ┌────────────┐    ┌───────────┐
│  Upload PDF  │───▶│ Chunking │───▶│  Embedding  │───▶│ SQL Server│
│  DOCX/PPTX   │    │ 4 strats │    │ 4 models    │    │  Vectors  │
└─────────────┘    └──────────┘    └────────────┘    └─────┬─────┘
                                                          │
┌─────────────┐    ┌──────────┐    ┌────────────┐         │
│   Chat UI    │◀──▶│ SignalR  │◀──▶│  RAG Query  │◀────────┘
│  Razor Pages │    │   Hub    │    │ Retriever   │
└─────────────┘    └──────────┘    └──────┬──────┘
                                          │
                                   ┌──────▼──────┐
                                   │  LLM (GPT)  │
                                   │  Generation  │
                                   └─────────────┘
```

## Tính năng

### 1. Quản lý tài liệu
- Upload PDF, DOCX, PowerPoint
- Tự động chunk & embed tài liệu
- 4 chunking strategies: FixedSize, SentenceBased, ParagraphBased, Recursive
- Quản lý theo môn học / chương

### 2. Chat & Hỏi đáp
- Chat real-time với SignalR
- Trích dẫn nguồn tài liệu gốc
- Giới hạn trả lời trong phạm vi tài liệu
- Lịch sử hội thoại theo phiên

### 3. Nghiên cứu (RBL)
- So sánh RAG vs Fine-tuned model
- Benchmark 4 chunking strategies
- Benchmark 4 embedding models:
  - `text-embedding-3-small` (OpenAI)
  - `multilingual-e5-base` (HuggingFace)
  - `bge-m3` (BAAI)
  - `PhoBERT-base` (VietAI)
- Dashboard RAGAS benchmark với Chart.js

## Tech Stack

- **Backend:** ASP.NET Core 8, Razor Pages, SignalR
- **Database:** SQL Server (Code First + EF Core)
- **AI/ML:** OpenAI API, Sentence-Transformers (Python)
- **Document Processing:** PdfPig (PDF), OpenXml (DOCX/PPTX)

## Cài đặt

### Yêu cầu
- .NET 8 SDK
- SQL Server (server: `nphphuc\FEAX`)
- Python 3.10+ (cho local embedding server)
- OpenAI API Key

### Bước 1: Cấu hình

Sửa `appsettings.json`:
```json
{
  "OpenAI": {
    "ApiKey": "sk-your-api-key-here"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=nphphuc\\FEAX;Database=ChatbotDB;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

### Bước 2: Build & Run (C# Backend)

```bash
cd ChatbotStudent/ChatbotStudent
dotnet restore
dotnet build
dotnet run
```

App sẽ tự động tạo database và migration khi khởi động.

### Bước 3: Python Embedding Server (tùy chọn)

```bash
cd PythonEmbeddingServer
pip install -r requirements.txt
python server.py
```

Server chạy tại `http://localhost:8000`.

### Bước 4: Truy cập

- Trang chủ: `https://localhost:5001`
- Chat: `https://localhost:5001/Chat`
- Tài liệu: `https://localhost:5001/Documents`
- Nghiên cứu: `https://localhost:5001/Research`

## Benchmark Test Set

File `TestSet/test_questions.json` chứa 50 câu hỏi ground truth với các chủ đề:
- Cơ bản về AI, ML, Deep Learning
- Thuật toán tìm kiếm
- NLP và Transformer
- RAG và Vector Search
- Đánh giá (RAGAS metrics)

## Python Embedding Server

Hỗ trợ các models:
| Model | Slug | Kích thước |
|-------|------|-----------|
| multilingual-e5-base | `intfloat/multilingual-e5-base` | ~500MB |
| bge-m3 | `BAAI/bge-m3` | ~2.2GB |
| PhoBERT-base | `vinai/phobert-base` | ~500MB |

## License

MIT
