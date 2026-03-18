import fitz  # PyMuPDF
import logging
import io


def extract_text_smart(file_bytes: bytes, mime_type: str) -> str:
    """
    Fast PDF text extraction using PyMuPDF (Azure-safe, no OCR, no external deps)

    Supports:
    - Text-based PDFs (best performance)
    - Returns fallback message for images/scanned PDFs
    """

    try:
        # -------- PDF HANDLING --------
        if mime_type and "pdf" in mime_type.lower():
            logging.info("Using PyMuPDF for PDF extraction")

            text = ""
            with fitz.open(stream=file_bytes, filetype="pdf") as doc:
                for page_num, page in enumerate(doc):
                    page_text = page.get_text("text")
                    if page_text:
                        text += page_text + "\n"

            if len(text.strip()) == 0:
                logging.warning("No text found — likely scanned PDF")
                return "[Scanned PDF detected — no extractable text found]"

            return text.strip()

        # -------- NON-PDF --------
        else:
            logging.warning(f"Unsupported file type: {mime_type}")
            return "[Only PDF text extraction supported — image OCR not enabled]"

    except Exception as e:
        logging.error(f"Extraction failed: {e}")
        return f"[Extraction error: {e}]"