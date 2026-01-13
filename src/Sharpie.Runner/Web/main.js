import { dotnet } from './_framework/dotnet.js'

const playButton = document.getElementById('play-button');
const canvas = document.getElementById('canvas');

playButton.addEventListener('click', async () => {
    playButton.classList.add('hidden');
    
    const { getAssemblyExports, getConfig, runMain } = await dotnet
        .withDiagnosticTracing(false)
        .create();

    const config = getConfig();
    const exports = await getAssemblyExports(config.mainAssemblyName);

    dotnet.instance.Module['canvas'] = canvas;

    function mainLoop() {
        exports.Sharpie.Runner.Web.Program.UpdateFrame();
        window.requestAnimationFrame(mainLoop);
    }

    await runMain();
    window.requestAnimationFrame(mainLoop);
});
