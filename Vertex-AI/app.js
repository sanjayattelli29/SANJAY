const { VertexAI } = require('@google-cloud/vertexai');
const cors = require('cors');
const express = require('express');
const multer = require('multer');
const dotenv = require('dotenv');
const { extractTextSmart } = require('./vision_extractor');
const { ANALYZE_PROMPT, CLAIM_PROMPT } = require('./prompts');

dotenv.config();

const project = process.env.GOOGLE_CLOUD_PROJECT || 'demoproject';
const location = process.env.GOOGLE_CLOUD_LOCATION || 'us-central1';

let vertexAI;
if (process.env.GCP_SERVICE_ACCOUNT_JSON) {
    try {
        const creds = JSON.parse(process.env.GCP_SERVICE_ACCOUNT_JSON);
        vertexAI = new VertexAI({ project, location, googleAuthOptions: { credentials: creds } });
        console.log('✅ Initialized Vertex AI with Service Account JSON string');
    } catch (err) {
        console.error('❌ Failed to parse GCP_SERVICE_ACCOUNT_JSON:', err);
        process.exit(1);
    }
} else if (process.env.GOOGLE_APPLICATION_CREDENTIALS) {
    console.log('✅ Found GOOGLE_APPLICATION_CREDENTIALS file path. Vertex AI will load it.');
    vertexAI = new VertexAI({ project, location });
} else {
    console.log('⚠️ No credential string or file found. Using Application Default Credentials.');
    vertexAI = new VertexAI({ project, location });
}

// Instantiate Model
const generativeModel = vertexAI.getGenerativeModel({
    model: process.env.AGENT_MODEL || 'gemini-2.5-flash',
    generationConfig: { responseMimeType: 'application/json' }
});

const app = express();

/**
 * 🔒 CORS Configuration - Production Ready
 */
const allowedOrigins = [
    'http://localhost:4200', // Local Angular
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

const upload = multer(); // Memory storage

// Helper to Clean response (remove markdown code fences)
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
 * 📊 endpoint: /analyze
 */
app.post('/analyze', upload.any(), async (req, res) => {
    const requestId = require('crypto').randomUUID();
    console.log(`[${new Date().toISOString()}] [${requestId}] POST /analyze started`);

    try {
        const customer = req.body.customer || '{}';
        const policy = req.body.policy || '{}';
        const claimsSummary = req.body.claimsSummary || '';
        const paymentsSummary = req.body.paymentsSummary || '';
        const files = req.files || [];

        console.log(`[${requestId}] Processing ${files.length} documents`);

        let extractedText = '';
        const parts = [{ text: '' }]; // Array of parts for Vertex AI

        for (const file of files) {
            // Direct Multimodal Upload for PDF and Images
            if (file.mimetype === 'application/pdf' || file.mimetype.startsWith('image/')) {
                parts.push({
                    inlineData: {
                        data: file.buffer.toString('base64'),
                        mimeType: file.mimetype
                    }
                });
                extractedText += `DOCUMENT: ${file.originalname} [Included as Multimodal Attachment]\n`;
                console.log(`[${requestId}] 📎 Attached file directly: ${file.originalname} (${file.mimetype})`);
            } else {
                // Fallback to text extraction for remaining types
                const text = await extractTextSmart(file.buffer, file.mimetype);
                extractedText += `DOCUMENT: ${file.originalname}\n${text}\n\n`;
            }
        }

        const finalInput = {
            customer: JSON.parse(customer),
            policy: JSON.parse(policy),
            claimsSummary: claimsSummary ? JSON.parse(claimsSummary) : {},
            paymentsSummary: paymentsSummary ? JSON.parse(paymentsSummary) : {}
        };

        const fullPrompt = `${ANALYZE_PROMPT}\n\nAPPLICATION DATA TO ANALYZE:\n${JSON.stringify(finalInput, null, 2)}\n\nEXTRACTED DOCUMENTS TEXT/ATTACHMENTS:\n${extractedText}`;
        parts[0].text = fullPrompt;

        console.log(`[${requestId}] 📊 Final Input Data (JSON Snapshot):\n${JSON.stringify(finalInput, null, 2).substring(0, 500)}...`);

        const result = await generativeModel.generateContent({
            contents: [{ role: 'user', parts: parts }]
        });

        const responseText = result.response.candidates[0].content.parts[0].text;
        console.log(`[${requestId}] ✨ AI Raw Response Text:\n`, responseText);
        const cleaned = cleanResponse(responseText);

        res.json(JSON.parse(cleaned));
    } catch (err) {
        console.error(`[${requestId}] Analyze Error:`, err);
        res.status(500).json({ error: err.message || 'Internal Server Error' });
    }
});

/**
 * 🏥 endpoint: /analyze-claim
 */
app.post('/analyze-claim', upload.any(), async (req, res) => {
    const requestId = require('crypto').randomUUID();
    console.log(`[${new Date().toISOString()}] [${requestId}] POST /analyze-claim started`);

    try {
        const claimData = req.body.claimData || '{}';
        const customerData = req.body.customerData || '{}';
        const policyData = req.body.policyData || '{}';
        const nomineeData = req.body.nomineeData || '{}';
        const files = req.files || [];

        console.log(`[${requestId}] Processing ${files.length} documents`);

        let extractedText = '';
        const parts = [{ text: '' }]; // Array of parts for Vertex AI

        for (const file of files) {
            // Direct Multimodal Upload for PDF and Images
            if (file.mimetype === 'application/pdf' || file.mimetype.startsWith('image/')) {
                parts.push({
                    inlineData: {
                        data: file.buffer.toString('base64'),
                        mimeType: file.mimetype
                    }
                });
                extractedText += `DOCUMENT: ${file.originalname} [Included as Multimodal Attachment]\n`;
                console.log(`[${requestId}] 📎 Attached file directly: ${file.originalname} (${file.mimetype})`);
            } else {
                // Fallback to text extraction
                const text = await extractTextSmart(file.buffer, file.mimetype);
                extractedText += `DOCUMENT: ${file.originalname}\n${text}\n\n`;
            }
        }

        const finalInput = {
            claimData: JSON.parse(claimData),
            customerData: JSON.parse(customerData),
            policyData: JSON.parse(policyData),
            nomineeData: JSON.parse(nomineeData)
        };

        const fullPrompt = `${CLAIM_PROMPT}\n\nCLAIM DATA TO ANALYZE:\n${JSON.stringify(finalInput, null, 2)}\n\nEXTRACTED DOCUMENTS TEXT/ATTACHMENTS:\n${extractedText}`;
        parts[0].text = fullPrompt;

        console.log(`[${requestId}] 📊 Final Input Data (JSON Snapshot):\n${JSON.stringify(finalInput, null, 2).substring(0, 500)}...`);

        const result = await generativeModel.generateContent({
            contents: [{ role: 'user', parts: parts }]
        });

        const responseText = result.response.candidates[0].content.parts[0].text;
        console.log(`[${requestId}] ✨ AI Raw Response Text:\n`, responseText);
        const cleaned = cleanResponse(responseText);

        res.json(JSON.parse(cleaned));
    } catch (err) {
        console.error(`[${requestId}] Analyze-Claim Error:`, err);
        res.status(500).json({ error: err.message || 'Internal Server Error' });
    }
});

const PORT = process.env.PORT || 8000;
app.listen(PORT, () => {
    console.log(`🚀 Node.js Vertex AI server listening on port ${PORT}`);
});
