using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;

class Program
{
    static string YourSubscriptionKey = "ENTERYOURSUBSCRIPTIONKEY";
    static string YourServiceRegion = "ENTERYOURSERVICEREGION";

    // Define the speech detection and translation languages.
    const string recognitionLanguage = "en-US"; // https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/language-support?tabs=speechtotext#speech-to-text
    const string translationLanguage = "es";  // https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/language-support?tabs=speechtotext#speech-translation

    async static Task Main(string[] args)
    {
        var recognitionEnd = new TaskCompletionSource<string?>();

        var translationConfig = SpeechTranslationConfig.FromSubscription(YourSubscriptionKey, YourServiceRegion);
        translationConfig.SpeechRecognitionLanguage = recognitionLanguage;
        translationConfig.AddTargetLanguage(translationLanguage);

        using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
        using var translationRecognizer = new TranslationRecognizer(translationConfig, audioConfig);

        translationConfig.SetProperty(PropertyId.SpeechServiceResponse_PostProcessingOption, "2");

        translationRecognizer.Recognizing += (object? sender, TranslationRecognitionEventArgs  e) =>
            {
                if (ResultReason.RecognizingSpeech == e.Result.Reason && e.Result.Text.Length > 0)
                {

                    Console.Clear();
                    Console.WriteLine($"{e.Result.Text}");
                }
                else if (ResultReason.NoMatch == e.Result.Reason)
                {
                    Console.WriteLine($"NOMATCH: Speech could not be recognized.{Environment.NewLine}");
                }

            };
        translationRecognizer.Recognized += (object? sender, TranslationRecognitionEventArgs  e) =>
            {

                if (ResultReason.TranslatedSpeech == e.Result.Reason && e.Result.Text.Length > 0)
                {

                    Console.Clear();
                    Console.WriteLine($"{e.Result.Translations[translationLanguage]}");
                }
                else if (ResultReason.NoMatch == e.Result.Reason)
                {
                    Console.WriteLine($"NOMATCH: Speech could not be recognized.{Environment.NewLine}");
                }
            };

        translationRecognizer.Canceled += (object? sender, TranslationRecognitionCanceledEventArgs  e) =>
            {
                if (CancellationReason.EndOfStream == e.Reason)
                {
                    Console.WriteLine($"End of stream reached.{Environment.NewLine}");
                    recognitionEnd.TrySetResult(null); 
                }
                else if (CancellationReason.CancelledByUser == e.Reason)
                {
                    Console.WriteLine($"User canceled request.{Environment.NewLine}");
                    recognitionEnd.TrySetResult(null); 
                }
                else if (CancellationReason.Error == e.Reason)
                {
                    var error = $"Encountered error.{Environment.NewLine}Error code: {(int)e.ErrorCode}{Environment.NewLine}Error details: {e.ErrorDetails}{Environment.NewLine}";
                    Console.WriteLine($"{error}");
                    recognitionEnd.TrySetResult(error); 
                }
                else
                {
                    var error = $"Request was cancelled for an unrecognized reason: {(int)e.Reason}.{Environment.NewLine}";
                    Console.WriteLine($"{error}");
                    recognitionEnd.TrySetResult(error); 
                }
            };

        translationRecognizer.SessionStopped += (object? sender, SessionEventArgs e) =>
            {
                
                Console.WriteLine($"Session stopped.{Environment.NewLine}");
                recognitionEnd.TrySetResult(null); 
            };

        Console.WriteLine($"Ready");
        await translationRecognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);
        Console.WriteLine($"Speak");
        // Waits for recognition end.
        Task.WaitAll(new[] { recognitionEnd.Task });

        // Stops recognition.
        await translationRecognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);

        return ;
    }
}
