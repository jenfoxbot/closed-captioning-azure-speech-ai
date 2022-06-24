using System.Collections.Concurrent;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;

class Program
{
    static string YourSubscriptionKey = "58759f1e03cd476e88d4baeb2e1bec73";
    static string YourServiceRegion = "westus2";

    async static Task Main(string[] args)
    {
        // Define the speech detection and translation languages.
        // https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/language-support?tabs=speechtotext#speech-to-text
        const string recognitionLanguage = "en-US";
        // https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/language-support?tabs=speechtotext#speech-translation
        const string translationLanguage = "es"; 

        // Support objects for asynchronous output to the console.
        using var cancellationTokenSource = new CancellationTokenSource();
        using var messageReady = new Semaphore(0,1);
        var messageQueue = new ConcurrentQueue<string>();

        // Setup speech translation objects.
        var speechTranslationConfig = SpeechTranslationConfig.FromSubscription(YourSubscriptionKey, YourServiceRegion);
        speechTranslationConfig.SpeechRecognitionLanguage = recognitionLanguage;
        speechTranslationConfig.AddTargetLanguage(translationLanguage);

        using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
        using var speechRecognizer = new TranslationRecognizer(speechTranslationConfig, audioConfig);

        speechTranslationConfig.SetProperty(PropertyId.SpeechServiceResponse_PostProcessingOption, "2");      
        
        // A new translated message has been received from cognitive services.
        speechRecognizer.Recognized += (object? sender, TranslationRecognitionEventArgs e) =>
            {
                if (e.Result.Reason == ResultReason.TranslatedSpeech)
                {
                    messageQueue.Enqueue(e.Result.Translations[translationLanguage]);
                    messageReady.Release();
                }
                else if (ResultReason.NoMatch == e.Result.Reason)
                {
                    Console.Error.WriteLine($"(Speech could not be recognized.)");
                }
            };

        // The speech recognizer was cancelled.
        speechRecognizer.Canceled += (object? sender, TranslationRecognitionCanceledEventArgs e) =>
            {
                if (CancellationReason.Error == e.Reason)
                {
                    var error = $"Encountered error.{Environment.NewLine}Error code: {(int)e.ErrorCode}{Environment.NewLine}Error details: {e.ErrorDetails}{Environment.NewLine}";
                    Console.WriteLine($"{error}");
                    cancellationTokenSource.Cancel();
                }
                else {
                    Console.WriteLine(e.Reason);
                    cancellationTokenSource.Cancel();
                }
            };

        // The speech recognition session has stopped.
        speechRecognizer.SessionStopped += (object? sender, SessionEventArgs e) =>
            {
                Console.WriteLine($"Session stopped.{Environment.NewLine}");
                cancellationTokenSource.Cancel();
            };

        var printResultsTask = Task.Run(async () => {
            try {
                while(messageReady.WaitOne())
                {
                    if (messageQueue.TryDequeue(out string? result) && result != null)
                    {
                        Console.WriteLine(result);
                    }
                    await Task.Delay(0);
                }
            }
            catch (OperationCanceledException) {
                // Expected when exiting.
            }
        }, cancellationTokenSource.Token);

        // Start the speech listener.
        await speechRecognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);
        
        // Inform the user we are ready for input.
        Console.WriteLine($"--Speak--");
        
        // Waits for recognition end.
        Task.WaitAll(new[] { printResultsTask });

        // Stop the listener.
        await speechRecognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
    }
}
