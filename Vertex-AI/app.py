from fastapi import FastAPI, UploadFile, File, Form
from fastapi.middleware.cors import CORSMiddleware
from dotenv import load_dotenv
import vertexai
from vertexai.generative_models import GenerativeModel
import json, re, uuid, datetime, logging, os, io
from typing import List, Optional
import pdfplumber
import pytesseract
from PIL import Image

load_dotenv()  # ✅ loads GOOGLE_APPLICATION_CREDENTIALS from .env

app = FastAPI()

# ✅ CORS — allow Angular frontend
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

vertexai.init(project="accisure-490603", location="us-central1")
model = GenerativeModel("gemini-2.5-flash")

logging.basicConfig(level=logging.INFO)

# ✅ In-memory history (replace with DB later)
history = []

# ─── Extraction Helpers ──────────────────────────────────────────────────────

def extract_pdf(file_bytes: bytes) -> str:
    """Extract text from a PDF using pdfplumber."""
    text = ""
    try:
        with pdfplumber.open(io.BytesIO(file_bytes)) as pdf:
            for page in pdf.pages:
                text += (page.extract_text() or "") + "\n"
    except Exception as e:
        text = f"[PDF extraction error: {e}]"
    return text.strip()


def extract_image(file_bytes: bytes) -> str:
    """Extract text from an image using Tesseract OCR."""
    try:
        image = Image.open(io.BytesIO(file_bytes))
        return pytesseract.image_to_string(image).strip()
    except Exception as e:
        return f"[OCR extraction error: {e}]"


# ─── Vertex AI Helper ────────────────────────────────────────────────────────

PROMPT = """
You are an AI insurance fraud detection expert.

Your job is to analyze:
1. Customer profile (age, income, habits)
2. Policy details
3. Extracted document data (from uploaded PDFs/images)

Goal:
Determine how authentic and trustworthy the claim/customer is.

Consider:
- Mismatch between documents and declared data
- Suspicious patterns (income vs policy size)
- Medical or accident inconsistencies
- Claim vs policy coverage mismatch
- Fake or incomplete documentation signals

Return STRICT valid JSON only, no markdown fences:

{
  "authenticityScore": 0,
  "overallRiskLevel": "LOW",
  "aiOpinion": "",
  "documentReport": "",
  "claimsReport": "",
  "paymentsReport": "",
  "locationAnalysis": "",
  "fraudSignals": ["", ""],
  "agentRecommendations": ["", "", ""]
}
"""


def run_vertex_ai(data: dict) -> dict:
    full_prompt = f"{PROMPT}\n\nDATA:\n{json.dumps(data)}"
    response = model.generate_content(full_prompt)
    raw = response.text

    # Strip markdown code fences
    cleaned = raw.replace("```json", "").replace("```", "").strip()

    match = re.search(r"\{.*\}", cleaned, re.DOTALL)
    if match:
        return json.loads(match.group(0))
    else:
        return {"error": "Invalid JSON from AI", "raw": raw}


# ─── Endpoints ───────────────────────────────────────────────────────────────

@app.post("/analyze")
async def analyze(
    customer: str = Form(...),
    policy: str = Form(...),
    claimsSummary: str = Form(""),
    paymentsSummary: str = Form(""),
    files: Optional[List[UploadFile]] = File(None)
):
    request_id = str(uuid.uuid4())
    timestamp = str(datetime.datetime.now())

    customer_data = json.loads(customer)
    policy_data = json.loads(policy)

    logging.info(f"[{request_id}] Received analyze request for policy: {policy_data.get('policyNumber', 'N/A')}")

    # ✅ Extract text from uploaded files (PDF / Image)
    raw_document_text = ""
    divider = "\n\n" + "=" * 30 + "\n\n"

    if files:
        for file in files:
            content = await file.read()
            filename = file.filename or ""
            content_type = file.content_type or ""

            is_pdf = filename.lower().endswith(".pdf") or "pdf" in content_type
            is_image = any(filename.lower().endswith(ext) for ext in [".png", ".jpg", ".jpeg", ".webp", ".bmp", ".tiff"]) or "image" in content_type

            if is_pdf:
                extracted = extract_pdf(content)
                section = f"[PDF: {filename}]\n{extracted}"
            elif is_image:
                extracted = extract_image(content)
                section = f"[IMAGE: {filename}]\nOCR TEXT:\n{extracted}"
            else:
                section = f"[FILE: {filename}]\n(Unsupported format — skipped)"

            raw_document_text += (divider if raw_document_text else "") + section
            logging.info(f"[{request_id}] Extracted {len(extracted) if 'extracted' in dir() else 0} chars from {filename}")

    final_input = {
        "customer": customer_data,
        "policy": policy_data,
        "claimsSummary": claimsSummary,
        "paymentsSummary": paymentsSummary,
        "rawDocumentText": raw_document_text or "No documents uploaded"
    }

    logging.info(f"[{request_id}] Sending to Vertex AI...")
    parsed = run_vertex_ai(final_input)

    final_response = {
        **parsed,
        "meta": {
            "requestId": request_id,
            "timestamp": timestamp,
            "model": "gemini-2.5-flash",
            "provider": "Google Vertex AI",
            "documentsProcessed": len(files) if files else 0
        }
    }

    history.append({"input": final_input, "output": final_response})
    logging.info(f"[{request_id}] Done. Risk: {parsed.get('overallRiskLevel', 'N/A')}")

    return final_response


@app.get("/history")
async def get_history():
    return history