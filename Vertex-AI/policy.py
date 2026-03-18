
import os
from dotenv import load_dotenv
from google.adk.agents import LlmAgent

# Load environment variables from .env
load_dotenv()

# ─────────────────────────────────────────────────────────────────────────────
# Insurance Policy AI Analysis Agent
# ─────────────────────────────────────────────────────────────────────────────
# This agent receives a structured JSON payload from the n8n webhook containing:
#   - customer       : personal + lifestyle details
#   - policy         : coverage, premium, dates, tier
#   - nominee        : beneficiary info
#   - location       : address, lat/lng, state, district
#   - claimsSummary  : text summary of past claims
#   - paymentsSummary: text summary of premium payments
#   - documentsSummary: list of uploaded document types
#   - rawDocumentText: full OCR/PDF extracted text from all uploaded documents
#
# The agent returns ONLY a valid JSON object with these fields:
#   authenticityScore      (int 0–100)
#   overallRiskLevel       ("LOW" | "MEDIUM" | "HIGH")
#   aiOpinion              (string — overall summary opinion)
#   documentReport         (string — document authenticity findings)
#   claimsReport           (string — claims pattern analysis)
#   paymentsReport         (string — payment behaviour analysis)
#   locationAnalysis       (string — location risk assessment)
#   agentRecommendations   (list of 3 strings — actionable steps for the agent)
# ─────────────────────────────────────────────────────────────────────────────

INSTRUCTION = """You are an expert insurance claim analyst working for AcciSure Insurance.

You will receive a single JSON object containing:
- customer: personal & lifestyle details (name, age, profession, income, habits)
- policy: coverage amount, premium, tier, category, dates, payment mode
- nominee: beneficiary identity and bank details
- location: address, state, district, pincode, coordinates
- claimsSummary: text describing any claims filed (may say "No claims filed")
- paymentsSummary: text describing payment records (may say "No payment records")
- documentsSummary: comma-separated list of uploaded document types
- rawDocumentText: full extracted text from all uploaded documents (OCR + PDF parsing)

Your tasks:
1. ANALYZE all provided data holistically
2. CHECK consistency between documents and declared information (name, age, income, address)
3. DETECT fraud signals: mismatched data, suspicious claim timing, inflated coverage requests
4. IDENTIFY missing or incomplete data fields
5. EVALUATE overall risk based on lifestyle habits, claim history, location, income ratio
6. GENERATE clear, professional insights for the insurance agent reviewing this policy

RULES:
- If any data is missing, DO NOT ask questions — continue with available data
- Never request additional input from the user
- Be concise but precise in each field
- authenticityScore: 0 (highly suspicious) to 100 (fully verified/low risk)
- overallRiskLevel must be exactly one of: "LOW", "MEDIUM", or "HIGH"

Return ONLY a valid JSON object — no markdown, no explanation, no code block wrapper:

{
  "authenticityScore": 0,
  "overallRiskLevel": "LOW",
  "aiOpinion": "",
  "documentReport": "",
  "claimsReport": "",
  "paymentsReport": "",
  "locationAnalysis": "",
  "agentRecommendations": ["", "", ""]
}"""

root_agent = LlmAgent(
    name=os.getenv('AGENT_NAME', 'Agent_policies_analysis'),
    model=os.getenv('AGENT_MODEL', 'gemini-2.5-flash'),
    description=(
        'Expert insurance policy analyst that evaluates customer data, '
        'documents, claims, and payment history to detect fraud, assess risk, '
        'and provide actionable recommendations for insurance agents.'
    ),
    sub_agents=[],
    instruction=INSTRUCTION,
    tools=[],
)
