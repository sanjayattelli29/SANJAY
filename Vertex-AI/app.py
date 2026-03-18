from fastapi import FastAPI, UploadFile, File, Form
from fastapi.middleware.cors import CORSMiddleware
from dotenv import load_dotenv
import vertexai
from vertexai.generative_models import GenerativeModel
import json, re, uuid, datetime, logging, os, io
from typing import List, Optional
import json, re, uuid, datetime, logging, os, io, tempfile
from typing import List, Optional
import asyncio

load_dotenv()  # loads GOOGLE_APPLICATION_CREDENTIALS from .env if present

# Support loading GCP Service Account JSON from Environment Variable for Cloud Deployment
gcp_json = os.getenv("GCP_SERVICE_ACCOUNT_JSON")
if gcp_json:
    try:
        # Validate JSON structure
        json.loads(gcp_json)
        with tempfile.NamedTemporaryFile(delete=False, suffix=".json", mode="w") as f:
            f.write(gcp_json)
            f.flush()
            os.environ["GOOGLE_APPLICATION_CREDENTIALS"] = f.name
            logging.info(f"Loaded GCP credentials from Environment Variable string to {f.name}")
    except json.JSONDecodeError:
        logging.error("GCP_SERVICE_ACCOUNT_JSON is not a valid JSON string")
    except Exception as e:
        logging.error(f"Failed to load GCP credentials from Environment Variable: {e}")

app = FastAPI()

# CORS — allow Angular frontend
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

vertexai.init(
    project=os.getenv("GOOGLE_CLOUD_PROJECT", "accisure-490603"),
    location=os.getenv("GOOGLE_CLOUD_LOCATION", "us-central1")
)
model = GenerativeModel("gemini-2.5-flash")

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s",
    datefmt="%H:%M:%S"
)

# In-memory history (replace with DB later)
history = []


# ─── Extraction Helpers ──────────────────────────────────────────────────────



def format_document(filename: str, text: str) -> str:
    """Format document text for structured delivery to the AI."""
    return f"""
==============================
DOCUMENT NAME: {filename}
==============================

CONTENT:
{text}

==============================
END OF DOCUMENT
==============================
"""


# ─── Vertex AI Prompt & Helper ───────────────────────────────────────────────

PROMPT = """
You are a highly strict insurance risk analysis AI.

You will receive structured JSON data AND multiple extracted documents.

CRITICAL INSTRUCTIONS:
1. You MUST read ALL document text completely.
2. Do NOT skip any document.
3. Cross-check all values between documents and input JSON.
4. If any mismatch exists, mark as risk.
5. If any field is missing in documents, explicitly mention it.

You must extract and validate:
- Full Name
- Date of Birth
- Aadhaar Number
- Address
- Annual Income
- Employer details
- Medical health indicators

For EACH document:
- Identify document type
- Extract key fields
- Validate consistency with other documents

STRICT RULES:
- Never ignore extracted text
- Never assume values not present
- Always mention inconsistencies
- Always mention missing fields

Return ONLY valid JSON — no markdown code fences, no extra text.

The JSON must exactly match this schema:

{
  "authenticityScore": 72,
  "overallRiskLevel": "MEDIUM",
  "aiOpinion": "Brief overall assessment of the application's trustworthiness (2-3 sentences).",
  "extractedFields": {
     "fullName": "Name found in documents",
     "dob": "DOB found",
     "aadhaar": "Aadhaar number",
     "address": "Address found",
     "annualIncome": "Income details",
     "employer": "Employer details",
     "medicalIndicators": "Medical summary"
  },
  "mismatches": [
    "Mismatch 1 description",
    "Mismatch 2 description"
  ],
  "missingData": [
    "Field 1 is missing",
    "Field 2 is missing"
  ],
  "analysisSummaries": {
     "overall": "Overall document report and assessment",
     "documentConsistency": "Assessment of visual and text indicators."
  },
  "riskFactors": [
    {
      "factor": "Annual Income vs Coverage",
      "value": "₹5,00,000 income / ₹50,00,000 coverage",
      "riskPercentage": 65,
      "riskLevelColor": "yellow"
    },
    {
      "factor": "Smoking Habit",
      "value": "SMOKER",
      "riskPercentage": 80,
      "riskLevelColor": "red"
    },
    {
      "factor": "Document Completeness",
      "value": "4 of 4 documents uploaded",
      "riskPercentage": 15,
      "riskLevelColor": "green"
    }
  ]
}

NOTE: riskLevelColor must be one of: "red", "yellow", "green"
NOTE: authenticityScore is 0-100 (higher = more authentic/trustworthy)
NOTE: riskFactors must have 4-8 entries covering the key risk dimensions
"""


def run_vertex_ai(data: dict, request_id: str) -> dict:
    full_prompt = f"{PROMPT}\n\nAPPLICATION DATA TO ANALYZE:\n{json.dumps(data, indent=2)}"

    logging.info(f"[{request_id}] ===== FINAL INPUT TO AI =====")
    logging.info(json.dumps(data, indent=2))
    logging.info(f"[{request_id}] Calling Vertex AI (gemini-2.5-flash)...")

    response = model.generate_content(full_prompt)
    raw = response.text

    logging.info(f"[{request_id}] ===== RAW AI RESPONSE =====")
    logging.info(raw)

    # Strip markdown code fences if present
    cleaned = raw.replace("```json", "").replace("```", "").strip()

    match = re.search(r"\{.*\}", cleaned, re.DOTALL)
    if match:
        try:
            parsed = json.loads(match.group(0))
            logging.info(f"[{request_id}] JSON parsed successfully")
            return parsed
        except json.JSONDecodeError as e:
            logging.error(f"[{request_id}] JSON parse error: {e}")
            return {"error": f"JSON parse error: {e}", "raw": raw[:500]}
    else:
        logging.error(f"[{request_id}] No JSON found in AI response")
        return {"error": "Invalid JSON from AI", "raw": raw[:500]}


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

    # ── Parse incoming JSON fields ────────────────────────────────────────────
    customer_data = json.loads(customer)
    policy_data   = json.loads(policy)

    policy_number = policy_data.get('policyNumber') or policy_data.get('policyId', 'N/A')
    policy_name   = policy_data.get('policyName',   'N/A')
    customer_name = customer_data.get('fullName',   customer_data.get('email', 'N/A'))

    logging.info(f"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━")
    logging.info(f"[{request_id}] ▶  NEW ANALYSIS REQUEST")
    logging.info(f"[{request_id}] Policy No : {policy_number}")
    logging.info(f"[{request_id}] Policy    : {policy_name}")
    logging.info(f"[{request_id}] Customer  : {customer_name}")
    logging.info(f"[{request_id}] Category  : {policy_data.get('policyCategory', 'N/A')}")
    logging.info(f"[{request_id}] Coverage  : {policy_data.get('coverageAmount', 'N/A')}")
    logging.info(f"[{request_id}] Premium   : {policy_data.get('calculatedPremium', 'N/A')}")
    logging.info(f"[{request_id}] Income    : {policy_data.get('applicant', {}).get('annualIncome', 'N/A')}")
    logging.info(f"[{request_id}] Files uploaded: {len(files) if files else 0}")

    # Log full received data for debugging
    logging.info(f"[{request_id}] === CUSTOMER DATA ===\n{json.dumps(customer_data, indent=2)}")
    logging.info(f"[{request_id}] === POLICY DATA ===\n{json.dumps(policy_data, indent=2)}")

    # ── Extract document text ─────────────────────────────────────────────────
    raw_document_text = ""
    divider = "\n\n" + "=" * 30 + "\n\n"
    documents_processed = 0

    if files:
        for file in files:
            content = await file.read()
            filename = file.filename or ""
            content_type = file.content_type or ""

            logging.info(f"[{request_id}] Triggering Vision Extraction for {filename} ({content_type})")

            from vision_extractor import extract_text_smart
            extracted = extract_text_smart(content, content_type)
            section = format_document(filename, extracted)

            raw_document_text += (divider if raw_document_text else "") + section
            documents_processed += 1
            
            # Logging (VERY IMPORTANT)
            extracted_len = len(extracted) if extracted else 0
            logging.info(f"[{request_id}] Extracted {extracted_len} chars from {filename}")
            if extracted:
                logging.info(f"[{request_id}] ===== FULL EXTRACTED TEXT ({filename}) =====")
                logging.info(extracted)
                logging.info(f"[{request_id}] ===== END OF DOCUMENT =====")
    else:
        logging.info(f"[{request_id}] No files uploaded — AI will analyze based on JSON data only")


    # ── Build final payload for Vertex AI ─────────────────────────────────────
    claims_data = None
    if claimsSummary:
        try:
            claims_data = json.loads(claimsSummary)
        except:
            claims_data = claimsSummary

    final_input = {
        "customer":          customer_data,
        "policy":            policy_data,
        "claimsData":        claims_data,
        "paymentsSummary":   paymentsSummary or "No payment data provided",
        "rawDocumentText":   raw_document_text or "No documents uploaded — analyze based on JSON data",
        "documentsCount":    documents_processed,
    }

    logging.info(f"[{request_id}] Sending to Vertex AI...")
    parsed = run_vertex_ai(final_input, request_id)

    # ── Build response ─────────────────────────────────────────────────────────
    final_response = {
        **parsed,
        "meta": {
            "requestId":          request_id,
            "timestamp":          timestamp,
            "model":              "gemini-2.5-flash",
            "provider":           "Google Vertex AI",
            "documentsProcessed": documents_processed,
            "policyNumber":       policy_number,
            "customerName":       customer_name,
        }
    }

    history.append({"input": final_input, "output": final_response})

    logging.info(f"[{request_id}] ✅ Done. Risk: {parsed.get('overallRiskLevel', 'N/A')} | Score: {parsed.get('authenticityScore', 'N/A')} | RiskFactors: {len(parsed.get('riskFactors', []))}")
    logging.info(f"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━")

    return final_response


from claim import INSTRUCTION as CLAIM_PROMPT


@app.post("/analyze-claim")
async def analyze_claim(
    claimData: str = Form(...),
    customerData: str = Form("{}"),
    policyData: str = Form("{}"),
    nomineeData: str = Form("{}"),
    files: Optional[List[UploadFile]] = File(None)
):
    request_id = str(uuid.uuid4())
    timestamp = str(datetime.datetime.now())

    claim = json.loads(claimData)
    customer = json.loads(customerData)
    policy = json.loads(policyData)
    nominee = json.loads(nomineeData)

    claim_id = claim.get('claimId', 'N/A')
    customer_name = customer.get('fullName', customer.get('email', 'N/A'))

    logging.info(f"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━")
    logging.info(f"[{request_id}] ▶  NEW CLAIM ANALYSIS REQUEST")
    logging.info(f"[{request_id}] Claim ID   : {claim_id}")
    logging.info(f"[{request_id}] Customer   : {customer_name}")
    logging.info(f"[{request_id}] Incident   : {claim.get('incidentType', 'N/A')}")
    logging.info(f"[{request_id}] Requested  : {claim.get('requestedAmount', 'N/A')}")
    logging.info(f"[{request_id}] Files      : {len(files) if files else 0}")

    # Extract document text using Gemini Vision
    raw_document_text = ""
    divider = "\n\n" + "=" * 30 + "\n\n"
    documents_processed = 0

    if files:
        for file in files:
            content = await file.read()
            filename = file.filename or ""
            content_type = file.content_type or ""
            logging.info(f"[{request_id}] Vision extracting: {filename} ({content_type})")
            
            from vision_extractor import extract_text_smart
            extracted = extract_text_smart(content, content_type)
            section = format_document(filename, extracted)
            raw_document_text += (divider if raw_document_text else "") + section
            documents_processed += 1
            
            # Logging (VERY IMPORTANT)
            extracted_len = len(extracted) if extracted else 0
            logging.info(f"[{request_id}] Extracted {extracted_len} chars from {filename}")
            if extracted:
                logging.info(f"[{request_id}] ===== FULL EXTRACTED TEXT ({filename}) =====")
                logging.info(extracted)
                logging.info(f"[{request_id}] ===== END OF DOCUMENT =====")
            
            # Throttle request to avoid Vertex AI 429
            await asyncio.sleep(2)
    else:
        logging.info(f"[{request_id}] No files — analyzing based on JSON data only")

    final_input = {
        "claimData": claim,
        "customerData": customer,
        "policyData": policy,
        "nomineeData": nominee,
        "rawDocumentText": raw_document_text or "No documents uploaded — analyze based on JSON claim data",
        "documentsCount": documents_processed,
    }

    # Build prompt and call Vertex AI
    full_prompt = f"{CLAIM_PROMPT}\n\nCLAIM DATA TO ANALYZE:\n{json.dumps(final_input, indent=2)}"
    logging.info(f"[{request_id}] ===== FINAL INPUT TO AI =====")
    logging.info(json.dumps(final_input, indent=2))
    logging.info(f"[{request_id}] Calling Vertex AI for claim analysis...")
    response = model.generate_content(full_prompt)
    raw = response.text
    logging.info(f"[{request_id}] ===== RAW AI RESPONSE =====")
    logging.info(raw)
    cleaned = raw.replace("```json", "").replace("```", "").strip()

    match = re.search(r"\{.*\}", cleaned, re.DOTALL)
    if match:
        try:
            parsed = json.loads(match.group(0))
        except json.JSONDecodeError as e:
            parsed = {"error": f"JSON parse error: {e}", "raw": raw[:500]}
    else:
        parsed = {"error": "Invalid JSON from AI", "raw": raw[:500]}

    final_response = {
        **parsed,
        "meta": {
            "requestId": request_id,
            "timestamp": timestamp,
            "model": "gemini-2.5-flash",
            "provider": "Google Vertex AI",
            "documentsProcessed": documents_processed,
            "claimId": claim_id,
            "customerName": customer_name,
        }
    }

    logging.info(f"[{request_id}] ✅ Claim Analysis Done. Risk: {parsed.get('overallRiskLevel', 'N/A')} | Score: {parsed.get('authenticityScore', 'N/A')}")
    logging.info(f"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━")

    return final_response



async def get_history():
    return history