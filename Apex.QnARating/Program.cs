using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Diagnostics;
using Microsoft.SemanticKernel.Orchestration;
using Serilog;

var loggerConfiguration = new LoggerConfiguration()
    //.MinimumLevel.Debug()
    .MinimumLevel.Information()
    //.MinimumLevel.Override("Microsoft.SemanticKernel", Serilog.Events.LogEventLevel.Debug)
    .MinimumLevel.Override("Microsoft.SemanticKernel", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Console()
    .CreateLogger();

var loggerFactory = new LoggerFactory()
    .AddSerilog(loggerConfiguration);

var kernel = new KernelBuilder()
    .WithCompletionService()
    .WithLoggerFactory(loggerFactory)
    .Build();

var logger = kernel.LoggerFactory.CreateLogger("QnARatingPlugin");

var function = "RatingWithGroundTruth";
var pluginsFolder = "plugins";
var ratingPlugin = "QnARatingPlugin";

var question = "Give me a definition for black hole. No more than 30 words.";
var groundTruth = "A black hole is a cosmic body of intense gravity from which nothing, not even light, can escape. It is formed by the death of a massive star.";

var answers = new List<string> 
{
    "A black hole is a region in space with intense gravity, formed by the death of a massive star. Nothing, not even light, can escape from it.",
    "A black hole is a collapsed dead star.",
    "A black hole is an object that keeps the light in it.",
    "A black hole is a collapsed star which is so heavy that even the light cannot escape.",
};

logger.LogInformation("======================================================================================================");
logger.LogInformation("Ground truth: {groundTruth}", groundTruth);
logger.LogInformation("======================================================================================================");

foreach (var answer in answers)
{
    logger.LogInformation("Provided answer: {answer}", answer);

    try
    {
        var variables = new ContextVariables();
        variables.Set("question", question);
        variables.Set("answer", answer);
        variables.Set("ground_truth", groundTruth);

        if (!kernel.Functions.TryGetFunction(function, out var ratingFunction))
        {
            var functions = kernel.ImportSemanticFunctionsFromDirectory(pluginsFolder, ratingPlugin);
            ratingFunction = functions[function];
        }

        var ratingResult = await kernel.RunAsync(ratingFunction, variables);
        var rating = ratingResult.FunctionResults.First().GetValue<string>();

        logger.LogInformation("Rating: {rating}", rating);
        logger.LogInformation("------------------------------------------------------------------------------------------------------");
    }
    catch (SKException exc)
    {
        logger.LogError("FAILED with exception: {message}", exc.Message);
    }
}
