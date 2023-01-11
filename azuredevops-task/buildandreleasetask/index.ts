import tl = require('azure-pipelines-task-lib/task');

async function run() {
    try {
        const inputString: string | undefined = tl.getInput('templatename', true);
        // if (inputString == 'bad') {
        //     tl.setResult(tl.TaskResult.Failed, 'Bad input was given');
        //     return;
        // }
        console.log('Hello', inputString);
    }
    catch (err) {
        tl.setResult(tl.TaskResult.Failed, "Marche pas");
    }
}

run();