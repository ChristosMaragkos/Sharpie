import { dotnet } from './_framework/dotnet.js'

const playButton = document.getElementById('play-button');
const canvas = document.getElementById('canvas');

// Create hidden file input
const fileInput = document.createElement('input');
fileInput.type = 'file';
fileInput.accept = '.shr';
fileInput.style.display = 'none';
document.body.appendChild(fileInput);

let exports = null;

fileInput.addEventListener('change', async (e) => {
    const file = e.target.files[0];
    if (file && exports) {
        const arrayBuffer = await file.arrayBuffer();
        const uint8Array = new Uint8Array(arrayBuffer);
        exports.Sharpie.Runner.Web.Program.LoadCartridgeFromBytes(uint8Array);
    }
});

const handleInteraction = () => {
    if (exports && exports.Sharpie.Runner.Web.Program.IsInBootMode()) {
        fileInput.click();
    }
};

canvas.addEventListener('click', handleInteraction);
canvas.addEventListener('touchend', (e) => {
    e.preventDefault(); // Prevent duplicate click event
    handleInteraction();
});

// Handle drag and drop
canvas.addEventListener('dragover', (e) => {
    e.preventDefault();
    e.stopPropagation();
});

canvas.addEventListener('drop', async (e) => {
    e.preventDefault();
    e.stopPropagation();
    
    if (exports && exports.Sharpie.Runner.Web.Program.IsInBootMode()) {
        const files = e.dataTransfer.files;
        if (files.length > 0) {
            const file = files[0];
            if (file.name.endsWith('.shr')) {
                const arrayBuffer = await file.arrayBuffer();
                const uint8Array = new Uint8Array(arrayBuffer);
                exports.Sharpie.Runner.Web.Program.LoadCartridgeFromBytes(uint8Array);
            }
        }
    }
});

playButton.addEventListener('click', async () => {
    playButton.classList.add('hidden');
    
    const { getAssemblyExports, getConfig, runMain } = await dotnet
        .withDiagnosticTracing(false)
        .create();

    const config = getConfig();
    exports = await getAssemblyExports(config.mainAssemblyName);

    dotnet.instance.Module['canvas'] = canvas;

    function mainLoop() {
        exports.Sharpie.Runner.Web.Program.UpdateFrame();
        window.requestAnimationFrame(mainLoop);
    }

    await runMain();
    window.requestAnimationFrame(mainLoop);
});
