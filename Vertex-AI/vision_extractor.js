const pdfParse = require('pdf-parse');

/**
 * Extracts text from a document buffer.
 * Replicates the behavior of vision_extractor.py
 */
async function extractTextSmart(buffer, contentType) {
    if (contentType === 'application/pdf' || contentType?.startsWith('application/pdf')) {
        try {
            const data = await pdfParse(buffer);
            const text = data.text || '';
            
            if (text.trim().length === 0) {
                console.warn("No text found — likely scanned PDF");
                return '[Scanned PDF detected — no extractable text found]';
            }
            return text;
        } catch (err) {
            console.error('Error parsing PDF:', err);
            return '[Error extracting text from PDF]';
        }
    }
    
    // For images or others, we rely on Gemini's multimodal capabilities later,
    // but the prompt expects text stream format.
    return '[Non-PDF or unsupported document format]';
}

module.exports = { extractTextSmart };
