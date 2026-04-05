const { VertexAI } = require('@google-cloud/vertexai');
const cors = require('cors');
const express = require('express');
const multer = require('multer');
const dotenv = require('dotenv');
const { ANALYZE_PROMPT, CLAIM_PROMPT } = require('./prompts');

// Load environment variables from .env file
dotenv.config();

// Setup Google Cloud Project and Location details
const project = process.env.GOOGLE_CLOUD_PROJECT || 'demoproject';
const location = process.env.GOOGLE_CLOUD_LOCATION || 'us-central1';

// --- STEP 1: Initialize Vertex AI ---
// We check if we have a Service Account JSON string or a file path to authenticate with Google Cloud
let vertexAI;
if (process.env.GCP_SERVICE_ACCOUNT_JSON) {
    try {
        const creds = JSON.parse(process.env.GCP_SERVICE_ACCOUNT_JSON);
        vertexAI = new VertexAI({ project, location, googleAuthOptions: { credentials: creds } });
        console.log(' Initialized Vertex AI with Service Account JSON string');
    } catch (err) {
        console.error(' Failed to parse GCP_SERVICE_ACCOUNT_JSON:', err);
        process.exit(1);
    }
} else if (process.env.GOOGLE_APPLICATION_CREDENTIALS) {
    console.log(' Found GOOGLE_APPLICATION_CREDENTIALS file path. Vertex AI will load it.');
    vertexAI = new VertexAI({ project, location });
} else {
    console.log(' No credential string or file found. Using Application Default Credentials.');
    vertexAI = new VertexAI({ project, location });
}

// --- STEP 2: Configure the AI Model ---
// We use the Gemini model (e.g., gemini-1.5-flash) and tell it to always respond in JSON format
const generativeModel = vertexAI.getGenerativeModel({
    model: process.env.AGENT_MODEL || 'gemini-2.1-flash',
    generationConfig: { responseMimeType: 'application/json' }
});

const app = express();

/**
 * CORS Configuration
 * This allows our Angular frontend (usually on port 4200) to talk to this Node.js server.
 */
const allowedOrigins = [
    'http://localhost:4200',
];
if (process.env.FRONTEND_URL) {
    allowedOrigins.push(process.env.FRONTEND_URL);
}

app.use(cors({
    origin: function (origin, callback) {
        if (!origin) return callback(null, true);
        if (allowedOrigins.indexOf(origin) === -1) {
            const msg = 'The CORS policy for this site does not allow access from the specified Origin.';
            return callback(new Error(msg), false);
        }
        return callback(null, true);
    },
    credentials: true
}));

// Multer handles the file uploads coming from the frontend
const upload = multer(); 

/**
 * Helper: cleanResponse
 * Sometimes the AI adds ```json ... ``` tags around the answer. 
 * This function removes those tags so we can parse the actual JSON data.
 */
function cleanResponse(text) {
    if (!text) return '{}';
    let cleaned = text.trim();
    if (cleaned.startsWith('```json')) {
        cleaned = cleaned.replace(/^```json/, '').replace(/```$/, '');
    } else if (cleaned.startsWith('```')) {
        cleaned = cleaned.replace(/^```/, '').replace(/```$/, '');
    }
    return cleaned.trim();
}

/**
 *  ENDPOINT: /analyze
 * Used for Policy Applications. It analyzes customer data and uploaded KYC documents.
 */
app.post('/analyze', upload.any(), async (req, res) => {
    const requestId = require('crypto').randomUUID();
    console.log(`[${new Date().toISOString()}] [${requestId}] POST /analyze started`);

    try {
        // Collect data sent from the frontend
        const customer = req.body.customer || '{}';
        const policy = req.body.policy || '{}';
        const claimsSummary = req.body.claimsSummary || '';
        const paymentsSummary = req.body.paymentsSummary || '';
        const files = req.files || [];

        console.log(`[${requestId}] Processing ${files.length} documents`);

        let extractedText = '';
        const parts = [{ text: '' }]; 

        // --- STEP 3: Convert PDFs and Images to Base64 ---
        // We loop through each file and turn its binary data into a Base64 string.
        // This is how Gemini "reads" the visual content of the documents.
        for (const file of files) {
            if (file.mimetype === 'application/pdf' || file.mimetype.startsWith('image/')) {
                parts.push({
                    inlineData: {
                        data: file.buffer.toString('base64'), // <--- THIS IS THE CONVERSION
                        mimeType: file.mimetype
                    }
                });
                extractedText += `DOCUMENT: ${file.originalname} [Included as Multimodal Attachment]\n`;
                console.log(`[${requestId}] 📎 Attached file directly: ${file.originalname} (${file.mimetype})`);
            } else {
                extractedText += `DOCUMENT: ${file.originalname} [Unsupported format: ${file.mimetype}]\n`;
            }
        }

        const finalInput = {
            customer: JSON.parse(customer),
            policy: JSON.parse(policy),
            claimsSummary: claimsSummary ? JSON.parse(claimsSummary) : {},
            paymentsSummary: paymentsSummary ? JSON.parse(paymentsSummary) : {}
        };

        // Combine the main instruction (PROMPT) with the data and file info
        const fullPrompt = `${ANALYZE_PROMPT}\n\nAPPLICATION DATA TO ANALYZE:\n${JSON.stringify(finalInput, null, 2)}\n\nEXTRACTED DOCUMENTS TEXT/ATTACHMENTS:\n${extractedText}`;
        parts[0].text = fullPrompt;

        console.log(`[${requestId}]  Final Input Data (JSON Snapshot):\n${JSON.stringify(finalInput, null, 2)}`);

        // --- STEP 4: Call Gemini AI ---
        const result = await generativeModel.generateContent({
            contents: [{ role: 'user', parts: parts }]
        });

        // Get the text answer and clean it up
        const responseText = result.response.candidates[0].content.parts[0].text;
        console.log(`[${requestId}]  AI Raw Response Text:\n`, responseText);
        const cleaned = cleanResponse(responseText);

        // Send the JSON answer back to the Angular app
        res.json(JSON.parse(cleaned));
    } catch (err) {
        console.error(`[${requestId}] Analyze Error:`, err);
        res.status(500).json({ error: err.message || 'Internal Server Error' });
    }
});

/**
 *  ENDPOINT: /analyze-claim
 * Used for Insurance Claims. It analyzes claim details, hospital bills, and medical reports.
 */
app.post('/analyze-claim', upload.any(), async (req, res) => {
    const requestId = require('crypto').randomUUID();
    console.log(`[${new Date().toISOString()}] [${requestId}] POST /analyze-claim started`);

    try {
        // Collect claim data and customer info
        const claimData = req.body.claimData || '{}';
        const customerData = req.body.customerData || '{}';
        const policyData = req.body.policyData || '{}';
        const nomineeData = req.body.nomineeData || '{}';
        const files = req.files || [];

        console.log(`[${requestId}] Processing ${files.length} documents`);

        let extractedText = '';
        const parts = [{ text: '' }]; 

        // --- STEP 3: Convert PDFs and Images to Base64 ---
        // Same as above, we turn the PDF/Image bytes into text strings (Base64)
        for (const file of files) {
            if (file.mimetype === 'application/pdf' || file.mimetype.startsWith('image/')) {
                parts.push({
                    inlineData: {
                        data: file.buffer.toString('base64'), // <--- THIS IS THE CONVERSION
                        mimeType: file.mimetype
                    }
                });
                extractedText += `DOCUMENT: ${file.originalname} [Included as Multimodal Attachment]\n`;
                console.log(`[${requestId}] 📎 Attached file directly: ${file.originalname} (${file.mimetype})`);
            } else {
                extractedText += `DOCUMENT: ${file.originalname} [Unsupported format: ${file.mimetype}]\n`;
            }
        }

        const finalInput = {
            claimData: JSON.parse(claimData),
            customerData: JSON.parse(customerData),
            policyData: JSON.parse(policyData),
            nomineeData: JSON.parse(nomineeData)
        };

        // Create the full prompt for Forensic Analysis
        const fullPrompt = `${CLAIM_PROMPT}\n\nCLAIM DATA TO ANALYZE:\n${JSON.stringify(finalInput, null, 2)}\n\nEXTRACTED DOCUMENTS TEXT/ATTACHMENTS:\n${extractedText}`;
        parts[0].text = fullPrompt;

        console.log(`[${requestId}]  Final Input Data (JSON Snapshot):\n${JSON.stringify(finalInput, null, 2)}`);

        // --- STEP 4: Call Gemini AI ---
        const result = await generativeModel.generateContent({
            contents: [{ role: 'user', parts: parts }]
        });

        const responseText = result.response.candidates[0].content.parts[0].text;
        console.log(`[${requestId}] ✨ AI Raw Response Text:\n`, responseText);
        const cleaned = cleanResponse(responseText);

        // Send the JSON answer back to the frontend
        res.json(JSON.parse(cleaned));
    } catch (err) {
        console.error(`[${requestId}] Analyze-Claim Error:`, err);
        res.status(500).json({ error: err.message || 'Internal Server Error' });
    }
});

// --- STEP 5: Start the Server ---
const PORT = process.env.PORT || 8000;
app.listen(PORT, () => {
    console.log(` Node.js Vertex AI server listening on port ${PORT}`);
});
