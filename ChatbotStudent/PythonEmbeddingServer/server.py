"""
Python Embedding Server for Local Models
Supports: multilingual-e5-base, bge-m3, PhoBERT-base
Run: pip install -r requirements.txt && python server.py
"""
import os
import json
from fastapi import FastAPI
from pydantic import BaseModel
from typing import List, Union
import uvicorn

app = FastAPI(title="Local Embedding Server")

# Model cache
_models = {}

SUPPORTED_MODELS = {
    "multilingual-e5-base": "intfloat/multilingual-e5-base",
    "bge-m3": "BAAI/bge-m3",
    "phobert": "vinai/phobert-base",
}

class EmbedRequest(BaseModel):
    model: str
    input: Union[str, List[str]]

class EmbedResponse(BaseModel):
    embedding: List[float] = None
    embeddings: List[List[float]] = None

def get_model(model_name: str):
    if model_name not in _models:
        from sentence_transformers import SentenceTransformer
        model_id = SUPPORTED_MODELS.get(model_name, model_name)
        _models[model_name] = SentenceTransformer(model_id)
    return _models[model_name]

@app.post("/embed")
async def embed(req: EmbedRequest):
    model = get_model(req.model)
    text = req.input if isinstance(req.input, str) else req.input[0]
    embedding = model.encode(text, normalize_embeddings=True).tolist()
    return EmbedResponse(embedding=embedding)

@app.post("/embed_batch")
async def embed_batch(req: EmbedRequest):
    model = get_model(req.model)
    texts = req.input if isinstance(req.input, list) else [req.input]
    embeddings = model.encode(texts, normalize_embeddings=True, batch_size=32).tolist()
    return EmbedResponse(embeddings=embeddings)

@app.get("/models")
async def list_models():
    return {"supported": list(SUPPORTED_MODELS.keys())}

@app.get("/health")
async def health():
    return {"status": "ok", "models_loaded": list(_models.keys())}

if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0", port=8000)
