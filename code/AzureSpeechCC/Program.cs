using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;


class Program
{

    static string YourSubscriptionKey = "ENTERYOURSUBSCRIPTIONKEY";
    static string YourServiceRegion = "ENTERYOURSERVICEREGION";

    async static Task Main(string[] args)
    {
        var recognitionEnd = new TaskCompletionSource<string?>();

        var speechConfig = SpeechConfig.FromSubscription(YourSubscriptionKey, YourServiceRegion);
        speechConfig.SpeechRecognitionLanguage = "en-US";

        using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
        using var speechRecognizer = new SpeechRecognizer(speechConfig, audioConfig);

        speechConfig.SetProperty(PropertyId.SpeechServiceResponse_PostProcessingOption, "2");

        speechRecognizer.Recognizing += (object? sender, SpeechRecognitionEventArgs e) =>
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
        speechRecognizer.Recognized += (object? sender, SpeechRecognitionEventArgs e) =>
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

        speechRecognizer.Canceled += (object? sender, SpeechRecognitionCanceledEventArgs e) =>
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

        speechRecognizer.SessionStopped += (object? sender, SessionEventArgs e) =>
            {
                
                Console.WriteLine($"Session stopped.{Environment.NewLine}");
                recognitionEnd.TrySetResult(null); 
            };

        Console.WriteLine($"Ready");
        await speechRecognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);
        Console.WriteLine($"Speak");
        // Waits for recognition end.
        Task.WaitAll(new[] { recognitionEnd.Task });

        // Stops recognition.
        await speechRecognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);

        return ;
    }
}