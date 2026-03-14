const fs = require('fs');
const path = require('path');

function replacePrefixes(filePath, prefixMap) {
    let content = fs.readFileSync(filePath, 'utf8');
    for (const [find, replace] of Object.entries(prefixMap)) {
        content = content.split(find).join(replace);
        // Fix double prefixing just in case
        content = content.split('ctx.ctx.').join('ctx.');
    }
    fs.writeFileSync(filePath, content);
}

replacePrefixes('C:/Sanjay/frontend-insurance/src/app/pages/dashboard/customer-components/part1/customer-part1.html', {
    'config?.': 'ctx.config?.',
    'config.policyCategories': 'ctx.config.policyCategories',
    'hasActivePolicy(': 'ctx.hasActivePolicy(',
    'onAadharFileChange(': 'ctx.onAadharFileChange(',
    'takePicture()': 'ctx.takePicture()',
    'stopCamera()': 'ctx.stopCamera()'
});

replacePrefixes('C:/Sanjay/frontend-insurance/src/app/pages/dashboard/customer-components/part2/customer-part2.html', {
    'claimFiles.length': 'ctx.claimFiles.length',
    'claimFiles': 'ctx.claimFiles',
    'openChatHelper(': 'ctx.openChatHelper(',
    'isValidName(': 'ctx.isValidName(',
    'isValidPhone(': 'ctx.isValidPhone(',
    'isValidEmail(': 'ctx.isValidEmail(',
    'isValidBankAccount(': 'ctx.isValidBankAccount(',
    'isValidIFSC(': 'ctx.isValidIFSC(',
    'isValidAadhar(': 'ctx.isValidAadhar('
});

console.log('Final missing bindings prefixed successfully.');
